using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{

    /// <summary> 
    /// Used to receive a deserialized OSC message from the corresponding OSC Link event. 
    /// </summary>
    /// <param name="message"> Received OSC message, deserialized. </param>
    /// <param name="receivedFrom"> The IP end point from which the message was received. </param>
    public delegate void OscOnReceiveMessageDataHandler(OscMessage message, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to receive a batch of deserialized OSC bundles (consisting of deserialized OSC messages) from the corresponding OSC Link event.
    /// </summary>
    /// <param name="bundles"> A batch of received OSC bundles, deserialized. </param>
    /// <param name="receivedFrom"> The IP end point from which the bundles were received. </param>
    public delegate void OscOnReceiveBundlesDataHandler(OscBundle[] bundles, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to receive a serialized OSC packet (in its binary form) from a corresponding OSC Link event.
    /// </summary>
    /// <param name="packet"> Received serialized OSC packet. </param>
    /// <param name="receivedFrom"> The IP end point from which the packet was received. </param>
    public delegate void OscOnReceivePacketBinaryHandler(OscPacketBinary packet, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to get copies of OSC messages sent by OSC Link, in their deserialized form (for debugging and logging, for example)
    /// </summary>
    /// <param name="message"> Sent OSC message, deserialized. </param>
    /// <param name="sentTo"> The IP end point to which the message was sent. </param>
    public delegate void OscOnSendMessageHandler(OscMessage message, IPEndPoint sentTo);


    /// <summary>
    /// Used to get copies of OSC bundles sent by OSC Link, in their deserialized form (for debugging and logging, for example)
    /// </summary>
    /// <param name="bundles"> Sent OSC bundles, deserialized. </param>
    /// <param name="sentTo"> The IP end point to which the message was sent. </param>
    public delegate void OscOnSendBundlesHandler(OscBundle[] bundles, IPEndPoint sentTo);


    /// <summary>
    /// Designates the OSC Link's current mode of operation.
    /// </summary>
    /// 
    public enum LinkMode
    {
        /// <summary> OSC Link is closed and not in operation. </summary>
        Closed,
        /// <summary> OSC Link is open and connected to a specific end point, discarding all other communications, only being able to send packets to that end point. </summary>
        ToTarget,
        /// <summary> OSC Link is open to any end point, and can send packets to any end point it damn well pleases. </summary>
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
        protected string _name;

        /// <summary> The UDP client used internally to send and receive OSC packets. </summary>
        protected UdpClient _udpClient;

        /// <summary> The maximum buffer size of the internal UDP client, in kb.  </summary>
        protected readonly int _udpClientMaxBufferSize;

        /// <summary> The target end point of this OSC Link, when operating in targeted mode. </summary>
        protected IPEndPoint _targetEndPoint;


        // receive task
        /// <summary> Receives the packets from the UDP client and deserializes them. </summary>
        protected Task _receiveTask;

        /// <summary> Holds the latest message sent out by the receive task. </summary>
        protected string _receiveTaskOutput;

        /// <summary> The "return address" of the last-received packet - that is, the IP end point it came from. </summary>
        protected IPEndPoint _receiveReturnAddress;

        /// <summary> The buffer to hold received binary data. </summary>
        protected byte[] _receiveDataBuffer;

        /// <summary> The source for cancellation tokens controlling the receive task. </summary>
        protected CancellationTokenSource _receiveTaskTokenSource;

        /// <summary> The token controling the receive task. </summary>
        protected CancellationToken _receiveTaskToken;

        /// <summary> Time between receive task cycles. </summary>
        protected TimeSpan _receiveCycleWait;


        // settings
        /// <summary> Whether the events pertaining to sending packets will be called. </summary>
        protected bool _callSentPacketEvents;

        /// <summary> Whether the events pertaining to receiving and deserializing packets will be called. </summary>
        protected bool _callReceivedDataEvents;

        /// <summary> Whether the events pertaining to receiving packets in binary form will be called. </summary>
        protected bool _callReceivedBinaryEvents;

        /// <summary> Current mode of operation. </summary>
        protected LinkMode _mode;

       

        #endregion

        #region PROPERTIES
        /// <summary> Returns the name of this OSC Link. </summary>
        public string Name { get => _name;  }

        /// <summary> Returns whether this OSC Link is currently open. </summary>
        public bool IsOpen { get => (_mode != LinkMode.Closed); }

        /// <summary> Returns the end point at which this OSC Link is located. </summary>
        public IPEndPoint OwnEndPoint 
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

        /// <summary> Controls whether the received messages/bundles will trigger ReceivedAsData events </summary>
        public bool CallReceivedDataEvents { get => _callReceivedDataEvents; set => _callReceivedDataEvents = value; }

        /// <summary> Controls whether the received messages/bundles will trigger ReceivedAsBinary events </summary>
        public bool CallReceivedBinaryEvents { get => _callReceivedBinaryEvents; set => _callReceivedBinaryEvents = value; }

        /// <summary> Controls whether the sent messages/bundles will trigger Sent events </summary>
        public bool CallSentDataEvents { get => _callSentPacketEvents; set => _callSentPacketEvents = value; }

        /// <summary> The maximum buffer size of the internal UDP client, in kb. </summary>
        public int UdpClientMaxBufferSize { get => _udpClientMaxBufferSize; }

        /// <summary> Shows the status of the receive task. If the task is not running will default to "WaitingToRun". </summary>
        public TaskStatus ReceiveTaskStatus
        {
            get
            {
                if (_receiveTask != null)
                {
                    return _receiveTask.Status;
                }
                else
                    return TaskStatus.WaitingToRun;
            }

        }


        /// <summary> Holds the latest message sent out by the receive task. </summary>
        public string ReceiveTaskOutput
        {
            get
            {
                if (string.IsNullOrEmpty(_receiveTaskOutput))
                {
                    return string.Empty;
                }
                else
                {
                    return _receiveTaskOutput;
                }

            }

        }


        #endregion

        #region EVENTS
        /// <summary> Invoked when this OSC Link receives a message, passes it as deserealized data. </summary>
        public event OscOnReceiveMessageDataHandler MessageReceivedAsData;

        /// <summary> Invoked when this OSC Link receives a batch of bundles, passes it as deserealized data. </summary> 
        public event OscOnReceiveBundlesDataHandler BundlesReceivedAsData;


        /// <summary> Invoked when this OSC Link receives a message, passes it as serialized, binary data. </summary>
        public event OscOnReceivePacketBinaryHandler PacketReceivedAsBinary;


        /// <summary> Invoked when this OSC Link sends a batch of bundles, passes it as deserialized data </summary>
        public event OscOnSendBundlesHandler BundlesSent;

        /// <summary> Invoked when this OSC Link sends a message, passes it as deserialized data </summary>
        public event OscOnSendMessageHandler MessageSent;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Creates a new OSC Link.
        /// </summary>
        /// <param name="name"> Name of this OSC Link. </param>
        /// <param name="udpClientMaxBufferSize"> The maximum buffer size of the internal UDP client, in kilobytes. </param>
        /// <param name="callReceivedDataEvents"> Whether the events pertaining to receiving and deserializing packets will be called. </param>
        /// <param name="callReceivedBinaryEvents"> Whether the events pertaining to receiving packets in binary form will be called. </param>
        /// <param name="callSentPacketEvents"> Whether the events pertaining to sending packets will be called. </param>
        public OscLink(string name, int udpClientMaxBufferSize = 256, bool callReceivedDataEvents = true, bool callReceivedBinaryEvents = false, bool callSentPacketEvents = false)
        {

            _name = name;
            _receiveTaskTokenSource = new CancellationTokenSource();
            _udpClientMaxBufferSize = udpClientMaxBufferSize;

            // get settings
            _callReceivedDataEvents = callReceivedDataEvents;
            _callReceivedBinaryEvents = callReceivedBinaryEvents;
            _callSentPacketEvents = callSentPacketEvents;

            _targetEndPoint = null;
            _mode = LinkMode.Closed;

            _receiveCycleWait = new TimeSpan(10000);

        }
     
        #endregion

        #region METHODS

        /// <summary>
        /// Internal method that takes care of everything that's the same with both modes.
        /// </summary>
        private void Open()
        {
            _udpClient.Client.ReceiveBufferSize = _udpClientMaxBufferSize * 1024;

            _receiveReturnAddress = null;
            _receiveTaskToken = _receiveTaskTokenSource.Token;

            _receiveTask = ReceiveTask();

        }


        /// <summary>
        /// Opens a link to specific target address and port (only sending and receiving messages from that target), using random available port.
        /// </summary>
        /// <param name="targetEndPoint"> The target end point with which OSC Link will be communicating. </param>
        /// <exception cref="ArgumentNullException"> Thrown when provided EndPoint is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when OSC Link is already open. </exception>
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

            _mode = LinkMode.ToTarget;

        }


        /// <summary>
        /// Opens a link to specific target address and port (only sending and receiving messages from that target), using specified port.
        /// </summary>
        /// <param name="port"> Port number to open the OSC Link with. </param>
        /// <param name="targetEndPoint"> The OSC Link will only communicate with this IP end point. </param>
        /// <exception cref="ArgumentNullException"> Thrown when provided IP end point is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when OSC Link is already open. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> The port number is out of range. </exception>
        /// <exception cref="SocketException"> Couldn't open a socket with the provided port number. </exception>
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

            _mode = LinkMode.ToTarget;

        }

        /// <summary>
        /// Opens a link that can send to and receive from any address and port, using a random available port.
        /// </summary>
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
        /// Opens a link that can send and receive from any address and port, using a specific port.
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
        /// Closes the OSC link.
        /// </summary>
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
        /// Sends an OSC packet to the target end point of this OSC Link. The packet is passed by reference.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<Packet>(ref Packet oscPacket) where Packet : IOscPacketBinary
        {
            if (_mode != LinkMode.ToTarget)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }
            
            try
            {               
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length);
                OnDataSent(oscPacket.BinaryData, TargetEndPoint);
            }
            catch (ArgumentNullException e) 
            {
                throw e;                   
            }
            catch (ObjectDisposedException e)
            {
                throw e;
                //TODO: catch all this stuff for enchanced stability.
            }

        }


        /// <summary>
        /// Sends an OSC packet to the target end point of this OSC Link. The packet is passed directly.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC link is not in target mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToTarget<Packet>(Packet oscPacket) where Packet : IOscPacketBinary
        {
            if (_mode != LinkMode.ToTarget)
            {
                throw new InvalidOperationException("OSC Link Error: Can't send message to target, OSC Link " + _name + " needs to be in TARGET MODE (current mode: " + _mode.ToString() + ").");
            }

            try
            {
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length);
                OnDataSent(oscPacket.BinaryData, TargetEndPoint);
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }

        }


        /// <summary>
        /// Sends an OSC packet to the specified end point. The packet is passed by reference.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC Link is not in wide mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<Packet>(ref Packet oscPacket, IPEndPoint endPoint) where Packet : IOscPacketBinary
        {

            if (_mode != LinkMode.Wide)
            {
                throw new InvalidOperationException("OSC Link Error: OSC Link " + _name + " needs to be in WIDE MODE (current mode: " + _mode.ToString() + ").");
            }

            try
            {               
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length, endPoint);
                OnDataSent(oscPacket.BinaryData, endPoint);
            }
            catch (ArgumentNullException e)
            {
                throw e;
            }

        }


        /// <summary>
        /// Sends an OSC packet to the specified end point. The packet is passed directly.
        /// </summary>
        /// <typeparam name="Packet"> The packet should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacket"> OSC binary data packet to be sent. </param>
        /// <param name="endPoint"> The end point to which the packet will be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown if OSC Link is not in wide mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown if trying to send null instead of binary data. </exception>
        public void SendToEndPoint<Packet>(Packet oscPacket, IPEndPoint endPoint) where Packet : IOscPacketBinary
        {

            if (_mode != LinkMode.Wide)
            {
                throw new InvalidOperationException("OSC Link Error: OSC Link " + _name + " needs to be in WIDE MODE (current mode: " + _mode.ToString() + ").");
            }

            try
            {
                _udpClient.Send(oscPacket.BinaryData, oscPacket.Length, endPoint);
                OnDataSent(oscPacket.BinaryData, endPoint);
            }
            catch (ArgumentNullException)
            {
                throw;
            }

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
                            _receiveTaskOutput = "Exception at " + OscTime.Now.ToString() + ": " + e.ToString();
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

            if (_callReceivedDataEvents)
            {
                BundlesReceivedAsData?.Invoke(OscDeserializer.GetBundles(binaryData), receivedFrom);
            }

            if (_callReceivedBinaryEvents)
            {
                PacketReceivedAsBinary?.Invoke(new OscPacketBinary(binaryData), receivedFrom);

            }

        }


        /// <summary>
        /// Calls events when an OSC message is received.
        /// </summary>
        /// <param name="binaryData"> Byte array containing the OSC message. </param>
        /// <param name="receivedFrom"> The end point from which the message was received. </param>
        protected virtual void OnMessageReceived(byte[] binaryData, IPEndPoint receivedFrom)
        {

            if (_callReceivedDataEvents)
            {
                MessageReceivedAsData?.Invoke(OscDeserializer.GetMessage(binaryData), receivedFrom);
            }

            if (_callReceivedBinaryEvents)
            {
                PacketReceivedAsBinary?.Invoke(new OscPacketBinary(binaryData), receivedFrom);

            } 

        }


        /// <summary>
        /// Calls events when this OSC Link sends out OSC packets.
        /// </summary>
        /// <param name="binaryData"> Byte array containing the OSC data being sent out. </param>
        /// <param name="sentTo"> The end point to which the data is being sent. </param>
        protected virtual void OnDataSent(byte[] binaryData, IPEndPoint sentTo)
        {
            if (_callSentPacketEvents)
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
                returnString.Append(OwnEndPoint.ToString());

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
