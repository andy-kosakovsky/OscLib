using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC Method within an OSC Address Space. Allows connecting event handlers to OSC Methods, to be invoked when an appropriate message is received by the Address Space.
    /// </summary>
    public sealed class OscMethod : OscAddressElement
    {
        /// <summary>
        /// How many event handlers are connected to this <see cref="OscMethod"/>.
        /// </summary>
        public int TotalHandlersConnected
        {
            get
            {
                if (OscMethodInvokedEvent == null)
                {
                    return 0;
                }

                return OscMethodInvokedEvent.GetInvocationList().Length;
            }

        }


        /// <summary>
        /// Invoked when the encompassing <see cref="OscAddressSpace"/> receives a message and dispatches it to this <see cref="OscMethod"/>. 
        /// </summary>
        public event MethodEventHandler OscMethodInvokedEvent;


        /// <summary>
        /// Initializes a new instance of the <see cref="OscMethod"/> class, with a specified name.
        /// </summary>
        /// <param name="name"> The name of this <see cref="OscMethod"/>. Shouldn't contain reserved symbols, not even the "/" in the beginning. </param>
        /// <exception cref="ArgumentNullException"> Thrown when the provided delegate is null. </exception>
        internal OscMethod(OscString name)
            :base(name)
        {
        }


        /// <summary>
        /// Invokes all event handlers subscribed to this <see cref="OscMethod"/>.
        /// </summary>
        /// <param name="source"> The source of the invocation. </param>
        /// <param name="arguments"> The array of arguments to pass on to event handlers. </param>
        public void Invoke(object source, object[] arguments)
        {
            OscMethodInvokedEvent?.Invoke(source, arguments);
        }


        /// <summary>
        /// Disconnects all event handlers.
        /// </summary>
        public void ClearEventHandlers()
        {
            OscMethodInvokedEvent = null;
        }


        /// <summary>
        /// Returns a string containing the name of this <see cref="OscMethod"/> and the total number of connected event handlers.
        /// </summary>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("OSC METHOD: ");
            returnString.Append(_name.ToString());
            returnString.Append(" (event handlers subscribed: ");
            returnString.Append(TotalHandlersConnected);
            returnString.Append(')');

            return returnString.ToString();

        }


        /// <summary>
        /// Returns a formatted string containing the names of all event handlers connected to this <see cref="OscMethod"/>.
        /// </summary>
        public string GetConnectedEventHandlersNames()
        {
            StringBuilder returnString = new StringBuilder(_name.ToString());
            returnString.Append(" attached methods:\n");

            if (OscMethodInvokedEvent != null)
            {
                Delegate[] list = OscMethodInvokedEvent.GetInvocationList();

                for (int i = 0; i < list.Length; i++)
                {
                    returnString.Append(" > ");
                    returnString.Append(list[i].Method.Name);
                    returnString.Append('\n');
                }

            }
            else
            {
                returnString.Append(" > NONE");
                returnString.Append('\n');
            }

            return returnString.ToString();

        }

    }

}
