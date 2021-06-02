using System;
using System.Net;

namespace OscLib
{

    #region MESSAGING

    /// <summary> 
    /// Used to handle sent or received OSC Messages after they've been deserialized.
    /// </summary>
    /// <param name="message"> An OSC Message, deserialized. </param>
    /// <param name="endPoint"> The end point associated with the message. </param>
    public delegate void MessageHandler(OscMessage message, IPEndPoint endPoint);


    /// <summary>
    /// Used to handle sent or received OSC Bundles after they've been deserialized.
    /// </summary>
    /// <param name="bundle"> An OSC Bundle, deserialized. </param>
    /// <param name="endPoint"> The end point associated with the bundle. </param>
    public delegate void BundleHandler(OscBundle bundle, IPEndPoint endPoint);


    /// <summary>
    /// Used to handle serialized OSC Packets.
    /// </summary>
    /// <param name="packet"> An OSC Packet, containing serialized data. </param>
    /// <param name="endPoint"> The end point associated with the packet. </param>
    /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
    public delegate void PacketHandler<Packet>(Packet packet, IPEndPoint endPoint) where Packet : IOscPacket;


    /// <summary>
    /// Used to handle any non-OSC or corrupted-looking data that's been received.
    /// </summary>
    /// <param name="data"> The array containing supposedly non-OSC data. </param>
    /// <param name="endPoint"> The end point associated with the data. </param>
    public delegate void BadDataHandler(byte[] data, IPEndPoint endPoint);
    

    #endregion


    #region EXCEPTIONS

    /// <summary>
    /// Used to safely extract exceptions from the tasks, preventing it from stopping.
    /// </summary>
    /// <param name="exception"></param>
    public delegate void OscTaskExceptionHandler(Exception exception);

    #endregion


    #region PACKET HEAP

    /// <summary>
    /// Used with OSC Sender's packet heap to pass methods that check OSC Packets for various conditions before doing stuff with them.
    /// </summary>
    /// <remarks>
    /// The delegate is generic to accomodate for possible "custom" OSC Packets.
    /// </remarks>
    /// <typeparam name="Packet"> Should implement the IOscPcketBinary interface. </typeparam>
    /// <param name="packet"> Packet to be checked. </param>
    public delegate bool OscPacketHeapCheck<Packet>(Packet packet) where Packet : IOscPacket;

    /// <summary>
    /// Used to pass methods that serve as a source for timetags.
    /// </summary>
    /// <returns> An OSC Timetag. </returns>
    public delegate OscTimetag OscTimetagSource();

    #endregion


    #region OSC ADDRESS SPACE

    /// <summary>
    /// Used in conjunction with OSC Methods to handle incoming messages and 
    /// </summary>
    /// <param name="source"> The source of the arguments - could be the OSC Method, the OSC Receiver, the OSC Message, etc. </param>
    /// <param name="messageArguments"> Arguments attached to the received OSC Message. </param>
    public delegate void OscMethodHandler(object source, object[] messageArguments);

    #endregion
}
