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
    public delegate void PacketHandler<TPacket>(TPacket packet, IPEndPoint endPoint) where TPacket : IOscPacket;


    /// <summary>
    /// Used to handle any non-OSC or corrupted-looking data that's been received.
    /// </summary>
    /// <param name="data"> The array containing supposedly non-OSC data. </param>
    /// <param name="endPoint"> The end point associated with the data. </param>
    public delegate void BadDataHandler(byte[] data, IPEndPoint endPoint);
    

    #endregion


    #region EXCEPTIONS
    /// <summary>
    /// Used to safely extract exceptions from tasks, preventing them from stopping prematurely.
    /// </summary>
    /// <param name="exception"> The exception raised by the task. </param>
    public delegate void TaskExceptionHandler(Exception exception);

    #endregion


    #region OSC ADDRESS SPACE
    /// <summary>
    /// Used to attach event handlers to OSC Methods - to be invoked when the encompassing Address Space dispatches a message to the method. 
    /// </summary>
    /// <param name="source"> The source of the arguments - could be the OSC Method, the OSC Receiver, the OSC Message, etc. </param>
    /// <param name="messageArguments"> Arguments attached to the received OSC Message. </param>
    public delegate void MethodEventHandler(object source, object[] messageArguments);

    #endregion


    #region PACKET MANAGEMENT
    /// <summary>
    /// Used with OscSender to specify the methods that will perform checks on OSC packets.
    /// </summary>
    /// <param name="packet"> An OSC Packet, containing serialized data. </param>
    public delegate bool CheckPacketDelegate<TPacket>(TPacket packet);


    /// <summary>
    /// Used with OscSender to specify the source of OSC Timetags. 
    /// </summary>
    /// <returns></returns>
    public delegate OscTimetag GetTimetagDelegate();

    #endregion
}
