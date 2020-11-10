using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{

    /// <summary> Used to receive a deserialized OSC message from the corresponding OSC Link event. </summary>
    /// <param name="message"> Received and deserialized OSC message. </param>
    /// <param name="receivedFrom"> Source of the message. </param>
    public delegate void OscOnReceiveMessageDataHandler(OscMessage message, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to receive a deserialized OSC bundle (consisting of deserialized OSC messages) from the corresponding OSC Link event
    /// </summary>
    /// <param name="bundles">Received and deserialized OSC bundles</param>
    /// <param name="receivedFrom">Source of the bundle</param>
    public delegate void OscOnReceiveBundlesDataHandler(OscBundle[] bundles, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to receive a serialized OSC packet (in its binary form) from a corresponding OSC Link event
    /// </summary>
    /// <param name="packet">Received serialized OSC packet.</param>
    /// <param name="receivedFrom">Source of the message.</param>
    public delegate void OscOnReceivePacketBinaryHandler(OscPacketBinary packet, IPEndPoint receivedFrom);


    /// <summary>
    /// Used to get copies of OSC messages sent by OSC Link, in their deserialized form (for debugging and logging, for example)
    /// </summary>
    /// <param name="message"> Sent OSC message, deserialized</param>
    /// <param name="sentTo"></param>
    public delegate void OscOnSendMessageHandler(OscMessage message, IPEndPoint sentTo);


    /// <summary>
    /// Used to get copies of OSC bundles sent by OSC Link, in their deserialized form (for debugging and logging, for example)
    /// </summary>
    /// <param name="bundles">Sent OSC bundles, deserialized</param>
    public delegate void OscOnSendBundlesHandler(OscBundle[] bundles, IPEndPoint sentTo);

    /// <summary>
    /// Designates whether the OSC link is currently closed, connected to a specific endpoint or can exchange messages with several endpoints
    /// </summary>
    public enum LinkMode
    {
        Closed, ToTarget, Wide
    }


    /// <summary>
    /// Used to send and receive OSC messages over UDP. Can operate in two modes: targeted to one specific endpoint, or wide-open free-for-all.
    /// </summary>
    public class OscLink
    {      
        #region FIELDS
        protected string _name;

        protected UdpClient _udpClient;
        protected readonly int _udpClientMaxBufferSize;
        protected IPEndPoint _targetEndPoint;
      
        protected CancellationTokenSource _tokenSource;
        protected CancellationToken _token;

        protected Task _receiverTask;
        // contains the return address of last received packet
        protected IPEndPoint _receiverReturnAddress;
        protected byte[] _receiverDataBuffer;

        // settings
        protected bool _callSentDataEvents;
        protected bool _callReceivedDataEvents;
        protected bool _callReceivedBinaryEvents;

        protected LinkMode _mode;

        protected TimeSpan _receiverCycleWait;

        #endregion

        #region PROPERTIES
        /// <summary> Returns the name of the OSC Link. </summary>
        public string Name { get => _name;  }

        /// <summary> Returns whether the OSC Link is currently open. </summary>
        public bool IsOpen { get => (_mode != LinkMode.Closed); }

        /// <summary> Returns the end point at which OSC Link is located. </summary>
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
        public bool CallSentDataEvents { get => _callSentDataEvents; set => _callSentDataEvents = value; }

        /// <summary> Returns maximum buffer size for the internal UDP client, in kb. </summary>
        public int UdpClientMaxBufferSize { get => _udpClientMaxBufferSize; }


        #endregion

        #region EVENTS
        /// <summary> Invoked when OSC Link receives a message, passes it as deserealized data. </summary>
        public event OscOnReceiveMessageDataHandler MessageReceivedAsData;

        /// <summary> Invoked when OSC Link receives a bundle, passes it as deserealized data. </summary> 
        public event OscOnReceiveBundlesDataHandler BundleReceivedAsData;

        /// <summary> Invoked when OSC Link receives a message, passes it as serialized, binary data. </summary>
        public event OscOnReceivePacketBinaryHandler PacketReceivedAsBinary;


        /// <summary> Invoked when OSC Link sends a message, passes it as deserialized data </summary>
        public event OscOnSendBundlesHandler BundleSent;

        /// <summary> Invoked when OSC Link sends a bundle, passes it as deserialized data </summary>
        public event OscOnSendMessageHandler MessageSent;

        #endregion

        #region CONSTRUCTORS
        public OscLink(string name, int udpReceiverMaxBufferSize = 256, bool callReceivedDataEvents = true, bool callReceivedBinaryEvents = false, bool callSentDataEvents = false)
        {

            _name = name;
            _tokenSource = new CancellationTokenSource();
            _udpClientMaxBufferSize = udpReceiverMaxBufferSize;

            // get settings
            _callReceivedDataEvents = callReceivedDataEvents;
            _callReceivedBinaryEvents = callReceivedBinaryEvents;
            _callSentDataEvents = callSentDataEvents;

            _targetEndPoint = null;
            _mode = LinkMode.Closed;

            _receiverCycleWait = new TimeSpan(10000);

        }
     
        #endregion

        #region METHODS

        /// <summary>
        /// Internal method that takes care of everything that's the same with both modes.
        /// </summary>
        private void Open()
        {
            _udpClient.Client.ReceiveBufferSize = _udpClientMaxBufferSize * 1024;

            _receiverReturnAddress = null;
            _token = _tokenSource.Token;

            _receiverTask = ReceiveTask();

        }

        /// <summary>
        /// Opens a link to specific target address and port (only sending and receiving messages from that target), using random available port.
        /// </summary>
        /// <param name="targetEndPoint">Target end point with which OSC Link will be communicating.</param>
        /// <exception cref="ArgumentNullException">Thrown when provided EndPoint is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when OSC Link is already open.</exception>
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
        /// <param name="ownPort"> Port number open the OSC Link with. </param>
        /// <param name="targetEndPoint"> The OSC Link will only send and receive data with this address. </param>
        /// <exception cref="ArgumentNullException"> Thrown when provided EndPoint is null. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when OSC Link is already open. </exception>
        public void OpenToTarget(int ownPort, IPEndPoint targetEndPoint)
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

            _udpClient = new UdpClient(ownPort);
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
        public void OpenWide(int port)
        {
            if (_mode != LinkMode.Closed)
            {
                throw new InvalidOperationException("OSCLink Error: Link " + _name + " is already open.");
            }
   
            _targetEndPoint = null;

            _udpClient = new UdpClient(port);

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
            
            _tokenSource.Cancel();

            _mode = LinkMode.Closed;

            _udpClient.Close();
            _udpClient = null;

            _receiverReturnAddress = null;
            _receiverDataBuffer = null;

            // get new token source
            _tokenSource = new CancellationTokenSource();

           
                     
        }


        /// <summary>
        /// Sends an OSC packet to the target end point the link is connected to.
        /// </summary>
        /// <param name="oscPacket"></param>
        /// <exception cref="InvalidOperationException"> Will throw if OSC link is not in target mode. </exception>
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
            catch (ArgumentNullException) 
            {
                throw;                   
            }

        }

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
            catch (ArgumentNullException)
            {
                throw;
            }

        }


        /// <summary>
        /// Sends a message to specified end point.
        /// </summary>
        /// <param name="oscPacket"></param>
        /// <param name="endPoint"></param>
        /// <exception cref="InvalidOperationException"> Will throw if OSC link is in target mode. </exception>
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
            catch (ArgumentNullException)
            {
                throw;
            }

        }


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
            
            while (!_token.IsCancellationRequested)
            {
                if (_mode != LinkMode.Closed)
                {
                    if (_udpClient.Available > 0)
                    {
                        _receiverDataBuffer = _udpClient.Receive(ref _receiverReturnAddress);

                        // per OSC protocol, first symbol of a bundle would always be "#"
                        // TODO: weak spot, come up with something more stable or with a way to control the task better
                        if (_receiverDataBuffer[0] == OscProtocol.SymbolBundleStart)
                            OnBundleReceived(_receiverDataBuffer, _receiverReturnAddress);
                        else
                            OnMessageReceived(_receiverDataBuffer, _receiverReturnAddress);

                    }
                    else
                        await Task.Delay(_receiverCycleWait);

                }
                else
                    await Task.Delay(_receiverCycleWait);
                         
            }
        
        }

        #endregion

        #region EVENT WRAPPERS

        protected virtual void OnBundleReceived(byte[] binaryData, IPEndPoint receivedFrom)
        {

            if (_callReceivedDataEvents)
            {
                BundleReceivedAsData?.Invoke(OscDeserializer.GetBundles(binaryData), receivedFrom);
            }

            if (_callReceivedBinaryEvents)
            {
                PacketReceivedAsBinary?.Invoke(new OscPacketBinary(binaryData), receivedFrom);

            }

        }


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

        protected virtual void OnDataSent(byte[] binaryData, IPEndPoint sentTo)
        {
            if (_callSentDataEvents)
            {
                if (binaryData[0] == (byte)'#')
                {
                    BundleSent?.Invoke(OscDeserializer.GetBundles(binaryData), sentTo);
                }
                else
                {
                    MessageSent?.Invoke(OscDeserializer.GetMessage(binaryData), sentTo);
                }

            }

        }

        #endregion

        public override string ToString()
        {
            string returnString = "OSCLink name: " + _name + "; Mode: " + _mode.ToString();

            if (_mode != LinkMode.Closed)
            {
                returnString += "; Own address: " + OwnEndPoint.ToString();

                if (_mode == LinkMode.ToTarget)
                {
                    returnString += "; Target end point: " + _targetEndPoint.ToString();
                }

            }

            return returnString;

        }

    }

}
