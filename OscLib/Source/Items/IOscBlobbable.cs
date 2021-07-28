using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Specifies common methods for data structures that can be converted into binary OSC-blobs, as per OSC Protocol spec.
    /// </summary>
    public interface IOscBlobbable
    {
        /// <summary>
        /// Returns the size of this data structure in bytes, when represented as an OSC-blob. This includes the bytes containing the blob length.
        /// </summary>
        int SizeAsBlob { get; }

        /// <summary>
        /// Creates a byte array containing this data structure represented as an OSC-blob.
        /// </summary>
        /// <returns></returns>
        byte[] GetAsBlob();
        
        /// <summary>
        /// Converts this data structure into an OSC-blob and adds it to the specified byte array.
        /// </summary>
        void AddAsBlob(byte[] array, int pointer);

        /// <summary>
        /// Converts this data structure into an OSC-blob and adds it to the specified byte array. Shifts the pointer forwards accordingly.
        /// </summary>
        void AddAsBlob(byte[] array, ref int extPointer);

    }

}
