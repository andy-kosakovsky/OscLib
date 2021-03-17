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
        #region FIELDS
        /// <summary> This OSC Link's name. </summary>
        protected readonly string _name;

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
        /// <summary> Returns the name of this OSC Link. </summary>
        public string Name { get => _name;  }

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
        public event OscOnReceiveMessageDataHandler MessageReceived;

        /// <summary> Invoked when this OSC Link receives a bundle or a batch of bundles. Received data is deserialized into a "flat" array containing all valid bundles. </summary> 
        public event OscOnReceiveBundlesDataHandler BundlesReceived;


        /// <summary> Invoked when this OSC Link receives any packet of binary OSC data. The packet is passed as is. </summary>
        public event OscOnReceivePacketBinaryHandler PacketReceivedAsBytes;


        /// <summary> Invoked when this OSC Link sends out an OSC packet containing a bundle or a batch of bundles. On invocation, the bundles are deserialized back into readible form. </summary>
        public event OscOnSendBundlesHandler BundlesSent;

        /// <summary> Invoked when this OSC Link sends out an OSC packet containing a message. On invocation, the message is deserialized into readible form. </summary>
        public event OscOnSendMessageHandler MessageSent;


        /// <summary> Invoked when there is an exception inside the receive task. Allows to catch exceptions without crashing the task, if desired. </summary>
        public event OscTaskExceptionHandler ReceiveTaskExceptionRaised;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Creates a new OSC Link.
        /// </summary>
        /// <param name="name"> The name of this OSC Link. </param>
        /// <param name="udpClientMaxBufferSize"> The maximum buffer size of the internal UDP client, in kb. </param>
        /// <param name="callEventsOnReceive"> Controls whether the "message/bundles received" events get called. </param>
        /// <param name="callEventsOnReceiveAsBytes"> Controls whether the "packet received as bytes" events get called. </param>
        /// <param name="callEventsOnSend"> Controls whether the "bundles/message sent" events get called. </param>
        public OscLink(string name, int udpClientMaxBufferSize = 256, bool callEventsOnReceive = true, bool callEventsOnReceiveAsBytes = false, bool callEventsOnSend = false)
        {

            _name = name;
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
        /// Opens the OSC Link to the specified end point, discarding communications from any other end point. The OSC Link will use any random available port.
        /// </summary>
        /// <param name="targetEndPoint"> The target end point to which OSC Link will be connected. </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided EndPoint is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown if the OSC Link is already open. </exception>
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
        /// Opens the OSC Link to the specified end point, discarding communications from any other end point. The OSC Link will use the specified port.
        /// </summary>
        /// <param name="targetEndPoint"> The OSC Link will only communicate with this IP end point. </param>
        /// <param name="port"> The port that  </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided IP end point is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown if the OSC Link is already open. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket with the provided port number. </exception>
        public void OpenToTarget(IPEndPoint targetEndPoint, int port)
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

            try
            {
                _udpClient = new UdpClient(port);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw e;
            }
            catch (SocketException e)
            {
                throw e;
            }

            _udpClient.Connect(_targetEndPoint);

            Open();

            _mode = LinkMode.Targeted;

        }

        /// <summary>
        /// Opens the OSC Link to send and receive to and from any end point. The OSC Link will use any random available port.
        /// </summary>
        /// <exception cref="InvalidOperationException" > Thrown if the OSC Link is already open. </exception>
        public void OpenWide()
        {

            if (_mode != LinkMode.Closed)
            {
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }

            _targetEndPoint = null;
             
            _udpClient = new UdpClient();
            _udpClient.Client.Bind(OscUtil.LocalEndPoint);

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

            try
            {
                _udpClient = new UdpClient(port);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw e;
            }
            catch (SocketException e)
            {
                throw e;
            }

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
        /// Sends an OSC packet to the target end point of this OSC Link.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<Packet>(Packet oscPacket) where Packet : IOscPacketBytes
        {
            if (_mode != LinkMode.Targeted)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            try
            {
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length);
               
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }

            OnDataSent(oscPacket.BinaryData, TargetEndPoint);

        }


        /// <summary>
        /// Sends an OSC packet to the specified end point. The packet is passed directly.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC Link is not in wide mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<Packet>(Packet oscPacket, IPEndPoint endPoint) where Packet : IOscPacketBytes
        {

            if (_mode != LinkMode.Wide)
            {
                throw new InvalidOperationException("OSC Link Error: OSC Link " + _name + " needs to be in WIDE MODE (current mode: " + _mode.ToString() + ").");
            }

            try
            {
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length, endPoint);
                
            }
            catch (ArgumentNullException)
            {
                throw;
            }

            OnDataSent(oscPacket.BinaryData, endPoint);

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
                            // TODO: weak spot, come up with something more stable or with a way to control the task better
                            if (_receiveDataBuffer[0] == OscProtocol.SymbolBundleStart)
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
                BundlesReceived?.Invoke(OscDeserializer.GetBundles(binaryData), receivedFrom);
            }

            if (_callEventsOnReceiveAsBytes)
            {
                PacketReceivedAsBytes?.Invoke(new OscPacketBytes(binaryData), receivedFrom);

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
                MessageReceived?.Invoke(OscDeserializer.GetMessage(binaryData), receivedFrom);
            }

            if (_callEventsOnReceiveAsBytes)
            {
                PacketReceivedAsBytes?.Invoke(new OscPacketBytes(binaryData), receivedFrom);

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
                    BundlesSent?.Invoke(OscDeserializer.GetBundles(binaryData), sentTo);
                }
                else
                {
                    MessageSent?.Invoke(OscDeserializer.GetMessage(binaryData), sentTo);
                }

            }

        }

        #endregion

        /// <summary>
        /// Prints out the status of this OSC Link as a nicely formatted string.
        /// </summary>
        /// <returns></returns>
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
