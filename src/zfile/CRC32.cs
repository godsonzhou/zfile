using System;
using System.Runtime.InteropServices;

namespace zfile
{
    /// <summary>
    /// CRC32 calculation utility class
    /// </summary>
    public static unsafe class CRC32
    {
        private const uint DefaultPolynomial = 0xedb88320;
        private const uint DefaultSeed = 0xffffffff;
        private static readonly uint[] DefaultTable;

        /// <summary>
        /// Static constructor to initialize the CRC32 table
        /// </summary>
        static CRC32()
        {
            DefaultTable = InitializeTable(DefaultPolynomial);
        }

        /// <summary>
        /// Calculate CRC32 for a buffer pointed to by an IntPtr
        /// </summary>
        /// <param name="buffer">Pointer to the buffer</param>
        /// <param name="bytesRead">Number of bytes to process</param>
        /// <param name="seed">Initial CRC value</param>
        /// <returns>Updated CRC value</returns>
        public static uint Calculate(IntPtr buffer, int bytesRead, uint seed)
        {
            uint crc = seed;
            byte* ptr = (byte*)buffer;

            for (int i = 0; i < bytesRead; i++)
            {
                unchecked
                {
                    crc = (crc >> 8) ^ DefaultTable[ptr[i] ^ (crc & 0xff)];
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculate CRC32 for a byte array
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="bytesRead">Number of bytes to process</param>
        /// <param name="seed">Initial CRC value</param>
        /// <returns>Updated CRC value</returns>
        public static uint Calculate(byte[] buffer, int bytesRead, uint seed)
        {
            return CalculateHash(DefaultTable, seed, buffer, 0, bytesRead);
        }

        /// <summary>
        /// Calculate CRC32 for a byte array
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <param name="seed">Initial CRC value</param>
        /// <returns>Updated CRC value</returns>
        public static uint Calculate(byte[] buffer, uint seed)
        {
            return CalculateHash(DefaultTable, seed, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Calculate CRC32 for a byte array
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <returns>CRC value</returns>
        public static uint Calculate(byte[] buffer)
        {
            return CalculateHash(DefaultTable, DefaultSeed, buffer, 0, buffer.Length);
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            uint[] createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                    {
                        entry = (entry >> 1) ^ polynomial;
                    }
                    else
                    {
                        entry >>= 1;
                    }
                }

                createTable[i] = entry;
            }

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            uint crc = seed;
            for (int i = start; i < start + size; i++)
            {
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ (crc & 0xff)];
                }
            }

            return crc;
        }
    }
}
