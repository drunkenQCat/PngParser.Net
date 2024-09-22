using System;

namespace PngParser
{
    public class Chunk
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Determines whether the chunk is critical or ancillary.
        /// </summary>
        public bool IsCritical => char.IsUpper(Name[0]);
    }
}

