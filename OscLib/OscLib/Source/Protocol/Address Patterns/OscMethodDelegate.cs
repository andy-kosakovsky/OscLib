using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{

    /// <summary>
    /// Used in conjunction with the OSC address system to assign actual methods to OSC Methods.
    /// </summary>
    /// <param name="arguments"> Arguments to be passed from the received OSC Message to the method when invoked. </param>
    public delegate void OscMethodDelegate(object[] arguments);
}
