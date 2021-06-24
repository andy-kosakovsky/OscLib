using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Specifies common methods for structures holding OSC binary data.
    /// </summary>
    public interface IOscBinaryContainer
    {
        /// <summary> The total number of bytes in this container. Should be a multiple of 4. </summary>
        int Size { get; }

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
        byte[] GetContents();

        /// <summary>
        /// Returns a copy of the byte array containing OSC data.
        /// </summary>
        byte[] GetCopyOfContents();

        /// <summary>
        /// Copies the contents of this container to the provided byte array.
        /// </summary>
        /// <param name="target"> The target array to copy to. </param>
        /// <param name="index"> The index to which to copy. </param>
        void CopyContentsToArray(byte[] target, int index);

    }

}
