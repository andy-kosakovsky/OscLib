using System;
using System.Net;

namespace OscLib
{

    #region MESSAGING

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
    public delegate void OscOnReceivePacketBinaryHandler(OscPacketBytes packet, IPEndPoint receivedFrom);


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

    #endregion


    #region EXCEPTIONS

    /// <summary>
    /// Used to safely extract exceptions from the tasks, preventing it from stopping
    /// </summary>
    /// <param name="exception"></param>
    public delegate void OscTaskExceptionHandler(Exception exception);

    #endregion


    #region PACKET HEAP

    /// <summary>
    /// Checks whether the packet is eligible to be sent, when OscSender is processing its packet heap.
    /// </summary>
    /// <typeparam name="Packet"> Should implement the IOscPcketBinary interface. </typeparam>
    /// <param name="packet"> Packet to be checked for eligibility. </param>
    /// <returns> True if packet should be sent, False otherwise. </returns>
    public delegate bool OscPacketReadyChecker<Packet>(Packet packet) where Packet : IOscPacketBytes;


    /// <summary>
    /// Checks whether the packet needs to be removed from the packet heap for whatever reason, precluding it from ever being sent.
    /// </summary>
    /// <typeparam name="Packet"> Should implement the IOscPacketBinary interface. </typeparam>
    /// <param name="packet"> Packet to be checked for removal. </param>
    /// <returns> True if packet should be removed, False otherwise. </returns>
    public delegate bool OscPacketRemover<Packet>(Packet packet) where Packet : IOscPacketBytes;

    #endregion


    #region OSC ADDRESS SPACE

    /// <summary>
    /// Used in conjunction with the OSC address system to assign actual methods to OSC Methods.
    /// </summary>
    /// <param name="arguments"> Arguments to be passed from the received OSC Message to the method when invoked. </param>
    public delegate void OscMethodDelegate(object[] arguments);

    #endregion
}
