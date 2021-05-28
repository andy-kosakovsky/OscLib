using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC Method inside the OSC Address Space. Allows to connect methods to specific messages or patterns.
    /// </summary>
    public class OscMethod : OscAddressElement
    {
        /// <summary> Points to a method that will be invoked when this OSC Method is called. </summary>
        protected readonly OscMethodDelegate _delegate;

        /// <summary> Caches the name of the delegate method. </summary>
        protected readonly string _delegateName;

        /// <summary> The name of the attached method delegate. </summary>
        public string DelegateName { get => _delegateName; }

        /// <summary>
        /// Creates a new OSC Method and links it with the provided delegate.
        /// </summary>
        /// <param name="name"> The name of the OSC Method. Shouldn't contain special symbols - that includes the "/" in the beginning. </param>
        /// <param name="method"> Points to a method that this OSC Method will invoke when called. </param>
        /// <exception cref="ArgumentNullException"> Thrown when the provided delegate is null. </exception>
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
        /// <param name="extras"> Used to optionally pass any extra arguments/information unrelated to the OSC Message's arguments. </param>
        public void Invoke(object[] arguments, object extras = null)
        {
            _delegate?.Invoke(arguments, extras);
        }

    }

}
