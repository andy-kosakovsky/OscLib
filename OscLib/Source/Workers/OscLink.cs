using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OscLib
{
    /// <summary>
    /// Sends and receives OSC Packets over UDP. Represents both OSC Clients and Servers.
    /// Can operate in two modes: targeted to one specific endpoint, or wide-open free-for-all.
    /// <para> To Do: Add TCP support. </para>
    /// </summary>
    public class OscLink
    {
        /// <summary> Cached loopback IP address. </summary>
        protected static readonly IPAddress _loopbackIP = IPAddress.Parse("127.0.0.1");

        #region FIELDS
        /// <summary> This OSC Link's name. </summary>
        protected readonly string _name;

        /// <summary> Used internally to send and receive OSC packets. </summary>
        protected UdpClient _udpClient;

        /// <summary> The target end point of this OSC Link, when operating in targeted mode. </summary>
        protected IPEndPoint _targetEndPoint;
        
        // receive task
        /// <summary> Receives the packets from the UDP client and deserializes them. </summary>
        protected Task _receiveTask;

        /// <summary> The source for cancellation tokens controlling the receive task. </summary>
        protected CancellationTokenSource _receiveTaskTokenSource;

        /// <summary> Time between receive task cycles. </summary>
        protected TimeSpan _receiveCycleTime;

        /// <summary> The token controling the receive task. </summary>
        protected CancellationToken _receiveTaskToken;

        /// <summary> The "return address" of the last-received packet - that is, the end point it came from. </summary>
        protected IPEndPoint _receiveReturnAddress;

        /// <summary> The buffer to hold received byte data. </summary>
        protected byte[] _receiveDataBuffer;

        // settings
        /// <summary> The maximum buffer size of the internal UDP client, in kb. OSC Link will need to be restarted in order for changes to take effect. </summary>
        protected int _udpClientMaxBufferSize;

        /// <summary> Controls whether the "bundles/message sent" events get called. </summary>
        protected bool _callEventsOnSend;

        /// <summary> Current mode of operation. </summary>
        protected LinkMode _mode;

        #endregion


        #region PROPERTIES
        /// <summary> Loopback IP address. </summary>
        public static IPAddress LoopbackIP { get => _loopbackIP; }

        /// <summary> Returns the name of this OSC Link. </summary>
        public string Name { get => _name;  }
 
        /// <summary> Returns whether this OSC Link is currently open. </summary>
        public bool IsOpen { get => _mode != LinkMode.Closed; }

        /// <summary> Returns the end point at which this OSC Link is located. </summary>
        public IPEndPoint MyEndPoint 
        {
            get
            {
                if ((_udpClient != null) && (_udpClient.Client != null) && (_udpClient.Client.LocalEndPoint != null))
                {
                    return (IPEndPoint)_udpClient.Client.LocalEndPoint;
                }
                else
                {
                    return null;
                }
             
            }

        }


        /// <summary> Returns target EndPoint when OSC Link is in target mode, otherwise returns null. </summary>
        public IPEndPoint TargetEndPoint
        {
            get
            {
                if (_mode == LinkMode.ToTarget)
                {
                    return _targetEndPoint;
                }
                else
                {
                    return null;
                }

            }

        }


        /// <summary> Current mode of operation. </summary>
        public LinkMode Mode { get => _mode; }

        /// <summary> Controls whether the "packet sent" events get called. </summary>
        public bool CallEventsOnSend { get => _callEventsOnSend; set => _callEventsOnSend = value; }

        /// <summary> The maximum buffer size of the internal UDP client, in kb. OSC Link will need to be restarted in order for changes to take effect. </summary>
        public int UdpClientMaxBufferSize { get => _udpClientMaxBufferSize; set => _udpClientMaxBufferSize = value; }

        /// <summary> Shows the status of the receive task. Defaults to "WaitingToRun" when the task is not running. </summary>
        public TaskStatus ReceiveTaskStatus
        {
            get
            {
                if (_receiveTask != null)
                {
                    return _receiveTask.Status;
                }
                else
                {
                    return TaskStatus.WaitingToRun;
                }

            }

        }

        #endregion


        #region EVENTS
        /// <summary> Invoked when this OSC Link receives a packet of OSC binary data. </summary>
        public event PacketHandler<OscPacket> PacketReceived;

        /// <summary> Invoked when this OSC Link receives a binary packet that contains bad/invalid/non-OSC data. </summary>
        public event BadDataHandler BadDataReceived;

        /// <summary> Invoked when this OSC Link sends out an OSC Packet. </summary>
        public event PacketHandler<OscPacket> PacketSent;

        /// <summary> Invoked in an unlikely event of trying to send an OSC Packet that somehow contains bad/invalid/non-OSC data. </summary>
        public event BadDataHandler BadDataSent;
        
        /// <summary> Invoked when there is an exception inside the receive task. Allows to catch exceptions without crashing the task, if desired. </summary>
        public event TaskExceptionHandler ReceiveTaskExceptionRaised;

        #endregion


        #region CONSTRUCTORS
        /// <summary>
        /// Creates a new OSC Link.
        /// </summary>
        /// <param name="name"> The name of this OSC Link. </param>
        public OscLink(string name)
        {
            _name = name;

            _receiveTaskTokenSource = new CancellationTokenSource();

            // default settings
            _udpClientMaxBufferSize = 256;
            _callEventsOnSend = false;

            _targetEndPoint = null;
            _mode = LinkMode.Closed;

            _receiveCycleTime = new TimeSpan(10000);

        }
     
        #endregion


        #region METHODS
        /// <summary>
        /// Internal method that takes care of everything that's the same in both modes of operation.
        /// </summary>
        protected void Open()
        {
            _udpClient.Client.ReceiveBufferSize = _udpClientMaxBufferSize * 1024;

            _receiveReturnAddress = null;
            _receiveTaskToken = _receiveTaskTokenSource.Token;

            _receiveTask = ReceiveTask();

        }


        /// <summary>
        /// Opens the OSC Link to the specified end point, limiting communications to only that end point. The OSC Link will use any random available port.
        /// </summary>
        /// <param name="targetEndPoint"> The target end point to which OSC Link will be connected. </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided EndPoint is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket. </exception>
        public void OpenToTarget(IPEndPoint targetEndPoint)
        {
            if (targetEndPoint == null)
            {
                throw new ArgumentNullException(nameof(targetEndPoint));
            }

            if (_mode != LinkMode.Closed)
            {
                Close();

                _receiveTask.Wait();
            }

            _targetEndPoint = targetEndPoint;
            _udpClient = new UdpClient();
            _udpClient.Connect(_targetEndPoint);

            Open();

            _mode = LinkMode.ToTarget;

        }


        /// <summary>
        /// Opens the OSC Link to the specified end point, limiting communications to only that end point. The OSC Link will use the specified port.
        /// </summary>
        /// <param name="targetEndPoint"> The OSC Link will only communicate with this IP end point. </param>
        /// <param name="port"> The port used by this OSC Link.  </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided IP end point is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket. </exception>
        public void OpenToTarget(int port, IPEndPoint targetEndPoint)
        {
            if (targetEndPoint == null)
            {
                throw new ArgumentNullException(nameof(targetEndPoint));
            }

            if (_mode != LinkMode.Closed)
            {
                Close();

                _receiveTask.Wait();
            }

            _targetEndPoint = targetEndPoint;

            _udpClient = new UdpClient(port);

            _udpClient.Connect(_targetEndPoint);

            Open();

            _mode = LinkMode.ToTarget;

        }


        /// <summary>
        /// Opens the OSC Link to send and receive to and from any end point. The OSC Link will use any random available port.
        /// </summary>
        /// <exception cref="SocketException"> Couldn't open a socket. </exception>
        public void OpenToAll()
        {
            if (_mode != LinkMode.Closed)
            {
                Close();

                _receiveTask.Wait();
            }

            _targetEndPoint = null;

            _udpClient = new UdpClient();
            _udpClient.Client.Bind(GetLoopbackEndPoint());   

            Open();

            _mode = LinkMode.ToAll;

        }


        /// <summary>
        /// Opens the OSC Link to send and receive to and from any end point. The OSC Link will use the specified port.
        /// </summary>
        /// <param name="port"> Port number for the OSC link. </param>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket with the provided port number. </exception>
        public void OpenToAll(int port)
        {
            if (_mode != LinkMode.Closed)
            {
                Close();

                _receiveTask.Wait();
            }
   
            _targetEndPoint = null;


            _udpClient = new UdpClient();
            _udpClient.Client.Bind(GetLoopbackEndPointWithPort(port));

            Open();

            _mode = LinkMode.ToAll;
         
        }


        /// <summary>
        /// Closes the OSC Link.
        /// </summary>
        public void Close()
        {
            if (_mode == LinkMode.Closed)
            {
                return;
            }
            
            _receiveTaskTokenSource.Cancel();

            _mode = LinkMode.Closed;

            _udpClient.Close();
            _udpClient = null;

            _receiveReturnAddress = null;
            _receiveDataBuffer = null;

            // get new token source
            _receiveTaskTokenSource = new CancellationTokenSource();
                    
        }


        /// <summary>
        /// Sends an OSC Packet to the target end point of this OSC Link.
        /// </summary>
        /// <typeparam name="TPacket"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<TPacket>(TPacket oscPacket) where TPacket : IOscPacket
        {
            if (_mode != LinkMode.ToTarget)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + "'s mode of operation needs to be set to ToTarget (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.GetContents(), oscPacket.Size);

            OnPacketSent(oscPacket.GetContents(), TargetEndPoint);
            
        }


        /// <summary>
        /// Sends a *part* of the provided OSC Packet to the target end point of this OSC Link.
        /// </summary>
        /// <remarks>
        /// This method is useful when the packet contains rubbish/nulls at the end of it - for example, this can happen when using cache arrays to concatinate multiple packets into one bundle.
        /// This helps to avoid copying data to a new array in order to truncate it, which in turn avoids creating unnecessary GC pressure.
        /// Obviously, this method is *to be used with caution* - there are all sorts of bugs and weird edge cases that might crop up, and it's much safer to use isolated, non-cache arrays in general. 
        /// </remarks>
        /// <typeparam name="TPacket"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <param name="length"> The length of useful data within the packet (in bytes) starting from the beginning of the packet. Everything outside this length will be discarded. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<TPacket>(TPacket oscPacket, int length) where TPacket : IOscPacket
        {
            if (_mode != LinkMode.ToTarget)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + "'s mode of operation needs to be set to ToTarget (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.GetContents(), length);

            OnPacketSent(oscPacket.GetContents(), TargetEndPoint);
            
        }


        /// <summary>
        /// Sends an OSC Packet to the specified end point.
        /// </summary>
        /// <typeparam name="TPacket"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC Link is not in wide mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<TPacket>(TPacket oscPacket, IPEndPoint endPoint) where TPacket : IOscPacket
        {

            if (_mode != LinkMode.ToAll)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to end point, OSC Link " + _name + "'s mode of operation needs to be set to ToAll (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.GetContents(), oscPacket.Size, endPoint);

            OnPacketSent(oscPacket.GetContents(), endPoint);         

        }


        /// <summary>
        /// Sends a *part* of the provided OSC Packet to the specified end point.
        /// </summary>
        /// <remarks>
        /// This method is useful when the packet contains rubbish/nulls at the end of it - for example, this can happen when using cache arrays to concatinate multiple packets into one bundle.
        /// This helps to avoid copying data to a new array in order to truncate it, which in turn avoids creating unnecessary GC pressure.
        /// Obviously, this method is *to be used with caution* - there are all sorts of bugs and weird edge cases that might crop up, and it's much safer to use isolated, non-cache arrays in general. 
        /// </remarks>
        /// <typeparam name="TPacket"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <param name="length"> The length of useful data within the packet (in bytes) starting from the beginning of the packet. Everything outside this length will be discarded. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<TPacket>(TPacket oscPacket, int length, IPEndPoint endPoint) where TPacket : IOscPacket
        {
            if (_mode != LinkMode.ToTarget)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to end point, OSC Link " + _name + "'s mode of operation needs to be set to ToAll (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.GetContents(), length, endPoint);

            OnPacketSent(oscPacket.GetContents(), TargetEndPoint);
            
        }

        #endregion


        #region TASKS
        /// <summary>
        /// The task responsible for receiving binary data from the internal UDP client and packing it into OSC Packets.
        /// </summary>
        protected virtual async Task ReceiveTask()
        {       
            while (!_receiveTaskToken.IsCancellationRequested)
            {
                if (_mode != LinkMode.Closed)
                {
                    if (_udpClient.Available > 0)
                    {
                        try
                        {
                            _receiveDataBuffer = _udpClient.Receive(ref _receiveReturnAddress);

                            OnPacketReceived(_receiveDataBuffer, _receiveReturnAddress);

                        }
                        catch (Exception e)
                        {
                            ReceiveTaskExceptionRaised?.Invoke(e);
                        }

                    }
                    else
                    {
                        await Task.Delay(_receiveCycleTime);
                    }

                }
                else
                {
                    await Task.Delay(_receiveCycleTime);
                }
                         
            }
        
        }

        #endregion


        #region EVENT WRAPPERS
        /// <summary>
        /// Calls events when an OSC packet is received.
        /// </summary>
        /// <param name="data"> Byte array containing one or more OSC bundles. </param>
        /// <param name="receivedFrom"> The end point from which the data was received. </param>
        protected virtual void OnPacketReceived(byte[] data, IPEndPoint receivedFrom)
        {
            if (data.IsValidOscData())
            {
                PacketReceived?.Invoke(new OscPacket(data, true), receivedFrom);
            }
            else
            {
                BadDataReceived?.Invoke(data, receivedFrom);
            }

        }


        /// <summary>
        /// Calls events when this OSC Link sends out OSC packets.
        /// </summary>
        /// <param name="data"> Byte array containing the OSC data being sent out. </param>
        /// <param name="sentTo"> The end point to which the data is being sent. </param>
        protected virtual void OnPacketSent(byte[] data, IPEndPoint sentTo)
        {
            if (_callEventsOnSend)
            {
                if (data.IsValidOscData())
                {
                    PacketSent?.Invoke(new OscPacket(data, true), sentTo);
                }
                else
                {
                    BadDataSent?.Invoke(data, sentTo);
                }

            }

        }

        #endregion // EVENT WRAPPERS


        #region STATIC METHODS

        /// <summary> Returns an end point at a local address on a random open port. </summary> 
        public static IPEndPoint GetLoopbackEndPoint()
        {
            return new IPEndPoint(LoopbackIP, 0);
        }


        /// <summary> Returns an end point at a local address with the requested port number (subject to availability). </summary> 
        public static IPEndPoint GetLoopbackEndPointWithPort(int port)
        {
            return new IPEndPoint(LoopbackIP, port);
        }

        #endregion // STATIC METHODS


        /// <summary>
        /// Prints out the status of this OSC Link as a nicely formatted string.
        /// </summary>
        /// <returns> A formatted string containing status information. </returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder("OSCLink name: ");

            returnString.Append(_name);
            returnString.Append("; Mode: ");
            returnString.Append(_mode.ToString());

            if (_mode != LinkMode.Closed)
            {
                returnString.Append("; Own address: ");
                returnString.Append(MyEndPoint.ToString());

                if (_mode == LinkMode.ToTarget)
                {
                    returnString.Append("; Target end point: ");
                    returnString.Append(_targetEndPoint.ToString());
                }

            }

            return returnString.ToString();

        }

    }

}
