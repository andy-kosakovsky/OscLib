using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC Method, to be used with the OSC Address Space - that is called when an appropriate OSC message is received.
    /// </summary>
    public class OscMethod : OscAddressPart
    {
        private readonly OscMethodDelegate _delegate;
        private readonly string _delegateName;

        /// <summary> The name of the attached method delegate. </summary>
        public string DelegateName { get => _delegateName; }

        /// <summary>
        /// Creates a new OSC Method and links it with the provided delegate.
        /// </summary>
        /// <param name="name"> The name of the OSC Method. This will be used to invoke it when receiving messages. </param>
        /// <param name="method"> The delegate pointing to a method that should be invoked with this OSC Method. </param>
        /// <exception cref="ArgumentNullException"> Thrown when the provided delegate method is null. </exception>
        public OscMethod(OscString name, OscMethodDelegate method)
            :base(name)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            _delegate = method;
            _delegateName = method.Method.Name;
        }

        /// <summary>
        /// Invokes the attached method delegate.
        /// </summary>
        /// <param name="arguments"> An array of arguments to pass to the delegate. </param>
        public void Invoke(object[] arguments)
        {
            _delegate?.Invoke(arguments);
        }

    }

}
