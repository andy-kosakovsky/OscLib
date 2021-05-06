using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{
    /// <summary>
    /// Describes the current mode of operation of an OSC Link.
    /// </summary>
    /// 
    public enum LinkMode
    {
        /// <summary> OSC Link is closed. </summary>
        Closed,
        /// <summary> OSC Link is open and connected to a specific end point. It will only receive and send packets from and to that end point. </summary>
        Targeted,
        /// <summary> OSC Link is open to communicate with any end point. </summary>
        Wide
    }

    /// <summary>
    /// Used to send and receive OSC messages over UDP. Can operate in two modes: targeted to one specific endpoint, or wide-open free-for-all.
    /// <para> To Do: Add TCP support. </para>
    /// </summary>
    public class OscLink
    {
        private static readonly IPAddress _localIP = IPAddress.Parse("127.0.0.1");

        #region FIELDS
        /// <summary> This OSC Link's name. </summary>
        protected readonly string _name;

        /// <summary> The OSC Converter currently in use by this link. </summary>
        protected readonly OscConvert _converter;

        /// <summary> Used internally to send and receive OSC packets. </summary>
        protected UdpClient _udpClient;

        /// <summary> The maximum buffer size of the internal UDP client, in kb. OSC Link will need to be restarted in order for changes to take effect. </summary>
        protected int _udpClientMaxBufferSize;

        /// <summary> The target end point of this OSC Link, when operating in targeted mode. </summary>
        protected IPEndPoint _targetEndPoint;

        // receive task
        /// <summary> Receives the packets from the UDP client and deserializes them. </summary>
        protected Task _receiveTask;

     

        /// <summary> The "return address" of the last-received packet - that is, the end point it came from. </summary>
        protected IPEndPoint _receiveReturnAddress;

        /// <summary> The buffer to hold received byte data. </summary>
        protected byte[] _receiveDataBuffer;

        /// <summary> The source for cancellation tokens controlling the receive task. </summary>
        protected CancellationTokenSource _receiveTaskTokenSource;

        /// <summary> The token controling the receive task. </summary>
        protected CancellationToken _receiveTaskToken;

        /// <summary> Time between receive task cycles. </summary>
        protected TimeSpan _receiveCycleWait;


        // settings
        /// <summary> Controls whether the "bundles/message sent" events get called. </summary>
        protected bool _callEventsOnSend;

        /// <summary> Controls whether the "message/bundles received" events get called. </summary>
        protected bool _callEventsOnReceive;

        /// <summary> Controls whether the "packet received as bytes" events get called. </summary>
        protected bool _callEventsOnReceiveAsBytes;

        /// <summary> Current mode of operation. </summary>
        protected LinkMode _mode;

        #endregion


        #region PROPERTIES
        /// <summary> Local IP address. </summary>
        public static IPAddress LocalIP { get => _localIP; }


        /// <summary> Returns the name of this OSC Link. </summary>
        public string Name { get => _name;  }

        /// <summary> The OSC Converter currently in use by this link. </summary>
        public OscConvert Converter { get => _converter; }

        /// <summary> Returns whether this OSC Link is currently open. </summary>
        public bool IsOpen { get => (_mode != LinkMode.Closed); }

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
                if (_mode == LinkMode.Targeted)
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

        /// <summary> Controls whether the "message/bundles received" events get called. </summary>
        public bool CallEventsOnReceive { get => _callEventsOnReceive; set => _callEventsOnReceive = value; }

        /// <summary> Controls whether the "packet received as bytes" events get called. </summary>
        public bool CallEventsOnReceiveAsBytes { get => _callEventsOnReceiveAsBytes; set => _callEventsOnReceiveAsBytes = value; }

        /// <summary> Controls whether the "bundles/message sent" events get called. </summary>
        public bool CallEventsOnSend { get => _callEventsOnSend; set => _callEventsOnSend = value; }

        /// <summary> The maximum buffer size of the internal UDP client, in kb. OSC Link will need to be restarted in order for changes to take effect. </summary>
        public int UdpClientMaxBufferSize { get => _udpClientMaxBufferSize; set => _udpClientMaxBufferSize = value; }

        /// <summary> Shows the status of the receive task. Defaults to "WaitingToRun" when the task is not present. </summary>
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
        /// <summary> Invoked when this OSC Link receives a message. The message is deserialized. </summary>
        public event OscMessageHandler MessageReceived;

        /// <summary> Invoked when this OSC Link receives a bundle or a batch of bundles. Received data is deserialized into a "flat" array containing all valid bundles. </summary> 
        public event OscBundleHandler BundleReceived;


        /// <summary> Invoked when this OSC Link receives any packet of binary OSC data. The packet is passed as is. </summary>
        public event OscPacketHandler<OscPacket> PacketReceived;


        /// <summary> Invoked when this OSC Link sends out an OSC packet containing a message. On invocation, the message is deserialized into readible form. </summary>
        public event OscMessageHandler MessageSent;

        /// <summary> Invoked when this OSC Link sends out an OSC packet containing a bundle or a batch of bundles. On invocation, the bundles are deserialized back into readible form. </summary>
        public event OscBundleHandler BundleSent;

        
        /// <summary> Invoked when there is an exception inside the receive task. Allows to catch exceptions without crashing the task, if desired. </summary>
        public event OscTaskExceptionHandler ReceiveTaskExceptionRaised;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Creates a new OSC Link.
        /// </summary>
        /// <param name="name"> The name of this OSC Link. </param>
        /// <param name="converter"> The OSC Converter that this link will be using. </param>
        /// <param name="udpClientMaxBufferSize"> The maximum buffer size of the internal UDP client, in kb. </param>
        /// <param name="callEventsOnReceive"> Controls whether the "message/bundles received" events get called. </param>
        /// <param name="callEventsOnReceiveAsBytes"> Controls whether the "packet received as bytes" events get called. </param>
        /// <param name="callEventsOnSend"> Controls whether the "bundles/message sent" events get called. </param>
        public OscLink(string name, OscConvert converter, int udpClientMaxBufferSize = 256, bool callEventsOnReceive = true, bool callEventsOnReceiveAsBytes = false, bool callEventsOnSend = false)
        {

            _name = name;
            _converter = converter;

            _receiveTaskTokenSource = new CancellationTokenSource();
            _udpClientMaxBufferSize = udpClientMaxBufferSize;

            // get settings
            _callEventsOnReceive = callEventsOnReceive;
            _callEventsOnReceiveAsBytes = callEventsOnReceiveAsBytes;
            _callEventsOnSend = callEventsOnSend;

            _targetEndPoint = null;
            _mode = LinkMode.Closed;

            _receiveCycleWait = new TimeSpan(10000);

        }
     
        #endregion

        #region METHODS

        /// <summary>
        /// Internal method that takes care of everything that's the same in both modes of operation.
        /// </summary>
        private void Open()
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
        /// <exception cref="InvalidOperationException"> Thrown if the OSC Link is already open. </exception>
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
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }

            _targetEndPoint = targetEndPoint;
            _udpClient = new UdpClient();
            _udpClient.Connect(_targetEndPoint);

            Open();

            _mode = LinkMode.Targeted;

        }


        /// <summary>
        /// Opens the OSC Link to the specified end point, limiting communications to only that end point. The OSC Link will use the specified port.
        /// </summary>
        /// <param name="targetEndPoint"> The OSC Link will only communicate with this IP end point. </param>
        /// <param name="port"> The port that  </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided IP end point is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown if the OSC Link is already open. </exception>
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
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }

            _targetEndPoint = targetEndPoint;

            _udpClient = new UdpClient(port);

            _udpClient.Connect(_targetEndPoint);

            Open();

            _mode = LinkMode.Targeted;

        }


        /// <summary>
        /// Opens the OSC Link to send and receive to and from any end point. The OSC Link will use any random available port.
        /// </summary>
        /// <exception cref="InvalidOperationException" > Thrown if the OSC Link is already open. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket. </exception>
        public void OpenWide()
        {
            if (_mode != LinkMode.Closed)
            {
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }

            _targetEndPoint = null;

            _udpClient = new UdpClient();
            _udpClient.Client.Bind(GetLocalEndPoint());   

            Open();

            _mode = LinkMode.Wide;

        }


        /// <summary>
        /// Opens the OSC Link to send and receive to and from any end point. The OSC Link will use the specified port.
        /// </summary>
        /// <param name="port"> Port number for the OSC link. </param>
        /// <exception cref="InvalidOperationException"> Thrown when OSC Link is already open. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket with the provided port number. </exception>
        public void OpenWide(int port)
        {
            if (_mode != LinkMode.Closed)
            {
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }
   
            _targetEndPoint = null;


            _udpClient = new UdpClient();
            _udpClient.Client.Bind(GetLocalEndPointWithPort(port));

            Open();

            _mode = LinkMode.Wide;
         
        }


        /// <summary>
        /// Closes the OSC Link.
        /// </summary>
        /// <exception cref="InvalidOperationException"> Thrown if the OSC Link is already closed. </exception>
        public void Close()
        {
            if (_mode == LinkMode.Closed)
            {
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already closed.");
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
        /// Converts an OSC Message into an OSC Packet and sends it to the target end point of this OSC Link.
        /// </summary>
        /// <param name="oscMessage"> The OSC Message to be sent. </param>
        public void SendToTarget(OscMessage oscMessage)
        {
            if (_mode != LinkMode.Targeted)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            OscPacket packet = _converter.GetPacket(oscMessage);

            _udpClient.Send(packet.BinaryData, packet.OscLength);

            OnDataSent(packet.BinaryData, TargetEndPoint);

        }

        /// <summary>
        /// Converts an OSC Message into an OSC Packet and sends it to the specified end point.
        /// </summary>
        /// <param name="oscMessage"> The OSC Message to be sent. </param>
        /// <param name="endPoint"> The end point to which the message will be sent. </param>
        public void SentToEndPoint(OscMessage oscMessage, IPEndPoint endPoint)
        {
            if (_mode != LinkMode.Wide)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            OscPacket packet = _converter.GetPacket(oscMessage);

            _udpClient.Send(packet.BinaryData, packet.OscLength, endPoint);

            OnDataSent(packet.BinaryData, endPoint);
        }


        /// <summary>
        /// Sends an OSC packet to the target end point of this OSC Link.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            if (_mode != LinkMode.Targeted)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.BinaryData, oscPacket.OscLength);
              
            OnDataSent(oscPacket.BinaryData, TargetEndPoint);

        }


        /// <summary>
        /// Sends a *part* of the provided OSC Packet to the target end point of this OSC Link.
        /// </summary>
        /// <remarks>
        /// This method is useful when the packet contains rubbish/nulls at the end of it - for example, this can happen when using cache arrays to concatinate multiple packets into one bundle.
        /// This helps to avoid copying data to a new array in order to truncate it, which in turn avoids creating unnecessary GC pressure.
        /// Obviously, this method is *to be used with caution* - there are all sorts of bugs and weird edge cases that might crop up, and it's much safer to use isolated, non-cache arrays in general. 
        /// </remarks>
        /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <param name="length"> The length of useful data within the packet (in bytes) starting from the beginning of the packet. Everything outside this length will be discarded. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<Packet>(Packet oscPacket, int length) where Packet : IOscPacket
        {
            if (_mode != LinkMode.Targeted)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.BinaryData, length);

            OnDataSent(oscPacket.BinaryData, TargetEndPoint);

        }


        /// <summary>
        /// Sends an OSC packet to the specified end point.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC Link is not in wide mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<Packet>(Packet oscPacket, IPEndPoint endPoint) where Packet : IOscPacket
        {

            if (_mode != LinkMode.Wide)
            {
                throw new InvalidOperationException("OSC Link Error: OSC Link " + _name + " needs to be in WIDE MODE (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.BinaryData, oscPacket.OscLength, endPoint);
                
            OnDataSent(oscPacket.BinaryData, endPoint);

        }


        /// <summary>
        /// Sends a *part* of the provided OSC Packet to the specified end point.
        /// </summary>
        /// <remarks>
        /// This method is useful when the packet contains rubbish/nulls at the end of it - for example, this can happen when using cache arrays to concatinate multiple packets into one bundle.
        /// This helps to avoid copying data to a new array in order to truncate it, which in turn avoids creating unnecessary GC pressure.
        /// Obviously, this method is *to be used with caution* - there are all sorts of bugs and weird edge cases that might crop up, and it's much safer to use isolated, non-cache arrays in general. 
        /// </remarks>
        /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
        /// <param name="oscPacket"> OSC Packet to be sent. </param>
        /// <param name="length"> The length of useful data within the packet (in bytes) starting from the beginning of the packet. Everything outside this length will be discarded. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<Packet>(Packet oscPacket, int length, IPEndPoint endPoint) where Packet : IOscPacket
        {
            if (_mode != LinkMode.Targeted)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            _udpClient.Send(oscPacket.BinaryData, length, endPoint);

            OnDataSent(oscPacket.BinaryData, TargetEndPoint);

        }


        #endregion

        #region TASKS

        private async Task ReceiveTask()
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

                            // per OSC protocol, first symbol of a bundle would always be "#"
                            if (_receiveDataBuffer[0] == OscProtocol.BundleMarker)
                                OnBundleReceived(_receiveDataBuffer, _receiveReturnAddress);
                            else
                                OnMessageReceived(_receiveDataBuffer, _receiveReturnAddress);
                        }
                        catch (Exception e)
                        {
                            ReceiveTaskExceptionRaised?.Invoke(e);
                        }

                    }
                    else
                        await Task.Delay(_receiveCycleWait);

                }
                else
                    await Task.Delay(_receiveCycleWait);
                         
            }
        
        }

        #endregion

        #region EVENT WRAPPERS
        /// <summary>
        /// Calls events when a batch of OSC bundles is received.
        /// </summary>
        /// <param name="binaryData"> Byte array containing one or more OSC bundles. </param>
        /// <param name="receivedFrom"> The end point from which the data was received. </param>
        protected virtual void OnBundleReceived(byte[] binaryData, IPEndPoint receivedFrom)
        {

            if (_callEventsOnReceive)
            {
                int pointer = 0;

                BundleReceived?.Invoke(_converter.GetBundle(binaryData, ref pointer, binaryData.Length), receivedFrom);
            }

            if (_callEventsOnReceiveAsBytes)
            {
                PacketReceived?.Invoke(new OscPacket(binaryData), receivedFrom);
            }

        }


        /// <summary>
        /// Calls events when an OSC message is received.
        /// </summary>
        /// <param name="binaryData"> Byte array containing the OSC message. </param>
        /// <param name="receivedFrom"> The end point from which the message was received. </param>
        protected virtual void OnMessageReceived(byte[] binaryData, IPEndPoint receivedFrom)
        {

            if (_callEventsOnReceive)
            {
                MessageReceived?.Invoke(_converter.GetMessage(binaryData), receivedFrom);
            }

            if (_callEventsOnReceiveAsBytes)
            {
                PacketReceived?.Invoke(new OscPacket(binaryData), receivedFrom);

            } 

        }


        /// <summary>
        /// Calls events when this OSC Link sends out OSC packets.
        /// </summary>
        /// <param name="binaryData"> Byte array containing the OSC data being sent out. </param>
        /// <param name="sentTo"> The end point to which the data is being sent. </param>
        protected virtual void OnDataSent(byte[] binaryData, IPEndPoint sentTo)
        {
            if (_callEventsOnSend)
            {
                if (binaryData[0] == (byte)'#')
                {
                    BundleSent?.Invoke(_converter.GetBundle(binaryData), sentTo);
                }
                else
                {
                    MessageSent?.Invoke(_converter.GetMessage(binaryData), sentTo);
                }

            }

        }

        #endregion // EVENT WRAPPERS


        #region STATIC METHODS

        /// <summary> Returns an end point at a local address on a random open port. </summary> 
        public static IPEndPoint GetLocalEndPoint()
        {
            return new IPEndPoint(LocalIP, 0);
        }

        /// <summary> Returns an end point at a local address with the requested port number (subject to availability). </summary> 
        public static IPEndPoint GetLocalEndPointWithPort(int port)
        {
            return new IPEndPoint(LocalIP, port);
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

                if (_mode == LinkMode.Targeted)
                {
                    returnString.Append("; Target end point: ");
                    returnString.Append(_targetEndPoint.ToString());
                }

            }

            return returnString.ToString();

        }

    }

}
