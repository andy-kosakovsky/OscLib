using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Specifies common methods for elements containing binary data.
    /// </summary>
    public interface IBinaryContainer
    {

        /// <summary>
        /// Indexer access to the binary data inside this container.
        /// </summary>
        /// <param name="index"> Byte index. </param>
        /// <returns></returns>
        byte this[int index] { get; }

        /// <summary>
        /// Retrieves the array containing binary data from this container.
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();

        /// <summary>
        /// Returns a copy of the byte array containing OSC data.
        /// </summary>
        byte[] GetCopyOfBytes();

        /// <summary>
        /// Copies the contents of this container to the provided byte array.
        /// </summary>
        /// <param name="target"> The target array to copy to. </param>
        /// <param name="index"> The index to which to copy. </param>
        void CopyBytesToArray(byte[] target, int index);

    }

}
