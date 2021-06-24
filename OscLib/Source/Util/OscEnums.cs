using System;
using System.Collections.Generic;
using System.Text;

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
        ToTarget,
        /// <summary> OSC Link is open to communicate with any end point. </summary>
        ToAll
    }


    /// <summary>
    /// Used to specify whether an OSC binary data packet contains a single OSC Message or a bundle. 
    /// </summary>
    public enum PacketContents
    {
        /// <summary> The packet contains a single OSC Message. </summary>
        Message,
        /// <summary> The packet contains an OSC Bundle. </summary>
        Bundle,
        /// <summary> The packet doesn't seem to contain OSC data. </summary>
        BadData
    }




}
