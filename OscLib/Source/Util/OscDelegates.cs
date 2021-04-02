using System;
using System.Net;

namespace OscLib
{

    #region MESSAGING

    /// <summary> 
    /// Used to handle sent or received OSC Messages after they've been deserialized.
    /// </summary>
    /// <param name="message"> An OSC Message, deserialized. </param>
    /// <param name="from"> The source end point of the message. </param>
    public delegate void OscMessageHandler(OscMessage message, IPEndPoint from);


    /// <summary>
    /// Used to handle sent or received OSC Bundles after they've been deserialized.
    /// </summary>
    /// <param name="bundle"> An OSC Bundle, deserialized. </param>
    /// <param name="from"> The source end point of the bundle. </param>
    public delegate void OscBundleHandler(OscBundle bundle, IPEndPoint from);


    /// <summary>
    /// Used to handle serialized OSC Packets.
    /// </summary>
    /// <param name="packet"> An OSC Packet, containing serialized data. </param>
    /// <param name="from"> The source end point of the packet. </param>
    /// <typeparam name="Packet"> The packet should implement the IOscPacket interface. </typeparam>
    public delegate void OscPacketHandler<Packet>(Packet packet, IPEndPoint from) where Packet : IOscPacket;

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
    /// Used in conjunction with the OSC address system to assign actual methods to OSC Methods.
    /// </summary>
    /// <param name="arguments"> Arguments to be passed from the received OSC Message to the method when invoked. </param>
    public delegate void OscMethodDelegate(object[] arguments);

    #endregion
}
