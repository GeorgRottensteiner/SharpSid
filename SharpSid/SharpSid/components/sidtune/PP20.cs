using System;

namespace SharpSid
{
    /// <summary>
    /// PowerPacker
    /// </summary>
    public class PP20
    {
        /*
        private const string _pp20_txt_packeddatacorrupt = "PowerPacker: Packed data is corrupt";
        private const string _pp20_txt_unrecognized = "PowerPacker: Unrecognized compression method";
        private const string _pp20_txt_uncompressed = "Not compressed with PowerPacker (PP20)";
        private const string _pp20_txt_fast = "PowerPacker: fast compression";
        private const string _pp20_txt_mediocre = "PowerPacker: mediocre compression";
        private const string _pp20_txt_good = "PowerPacker: good compression";
        private const string _pp20_txt_verygood = "PowerPacker: very good compression";
        private const string _pp20_txt_best = "PowerPacker: best compression";
        */

        private const string PP_ID = "PP20";

        private short[] efficiency = new short[4];

        private short[] source;
        short[] dest;

        private int readPtr;

        private int writePtr;

        /// <summary>
        /// compressed data longword
        /// </summary>
        private int current;

        /// <summary>
        /// number of bits in 'current' to evaluate
        /// </summary>
        private int bits;

        /// <summary>
        /// exception-free version of code
        /// </summary>
        private bool globalError;

        //private string statusstring;


        public class Decompressed
        {
            internal short[] destBufRef;
        }

        public PP20()
        {
            //statusstring = _pp20_txt_uncompressed;
        }

        public bool isCompressed(short[] source, int size)
        {
            // Check minimum input size, PP20 ID, and efficiency table.
            if (size < 8)
            {
                return false;
            }
            // We hope that every file with a valid signature and a valid
            // efficiency table is PP-compressed actually.
            short[] idPtr = source;
#if !SILVERLIGHT
            if (!System.Text.Encoding.ASCII.GetString(new byte[] { (byte)idPtr[0], (byte)idPtr[1], (byte)idPtr[2], (byte)idPtr[3] }).Equals(PP_ID))
#endif
            {
                //statusstring = _pp20_txt_uncompressed;
                return false;
            }
#if !SILVERLIGHT
            return checkEfficiency(source, 4);
#endif
        }

        /// <summary>
        /// If successful, allocates a new buffer containing the uncompresse data and
        /// returns the uncompressed length. Else, returns 0
        /// </summary>
        /// <param name="source"></param>
        /// <param name="size"></param>
        /// <param name="decomp"></param>
        /// <returns></returns>
        public int decompress(short[] source, int size, Decompressed decomp)
        {
            this.source = source;
            globalError = false; // assume no error

            readPtr = 0;

            if (!isCompressed(source, size))
            {
                return 0;
            }

            // Uncompressed size is stored at end of source file.
            // Backwards decompression.
            readPtr += (size - 4);

            int lastDword = readBEdword(source, readPtr);
            // Uncompressed length in bits 31-8 of last dword.
            int outputLen = lastDword >> 8;

            // Allocate memory for output data.
            dest = new short[outputLen];

            // Lowest dest. address for range-checks.
            // Put destptr to end of uncompressed data.
            writePtr = outputLen;

            // Read number of unused bits in 1st data dword
            // from lowest bits 7-0 of last dword.
            bits = 32 - (lastDword & 0xFF);

            // Main decompression loop.
            bytesTOdword();
            if (bits != 32)
            {
                current >>= (32 - bits);
            }
            do
            {
                if (readBits(1) == 0)
                {
                    bytes();
                }
                if (writePtr > 0)
                {
                    sequence();
                }
                if (globalError)
                {
                    // statusstring already set.
                    outputLen = 0; // unsuccessful decompression
                    break;
                }
            } while (writePtr > 0);

            // Finished

            if (outputLen > 0) // successful
            {
                decomp.destBufRef = new short[dest.Length];
                // Free any previously existing destination buffer.
                Array.Copy(dest, 0, decomp.destBufRef, 0, dest.Length);
            }

            return outputLen;
        }

        /*
        public string getStatusstring()
        {
            return statusstring;
        }
        */

        private const int PP_BITS_FAST = 0x09090909;
        private const int PP_BITS_MEDIOCRE = 0x090a0a0a;
        private const int PP_BITS_GOOD = 0x090a0b0b;
        private const int PP_BITS_VERYGOOD = 0x090a0c0c;
        private const int PP_BITS_BEST = 0x090a0c0d;

        private bool checkEfficiency(short[] source, int pos)
        {
            // Copy efficiency table.
            Array.Copy((short[])source, pos, efficiency, 0, 4);
            int eff = readBEdword(efficiency, 0);
            if ((eff != PP_BITS_FAST) && (eff != PP_BITS_MEDIOCRE) && (eff != PP_BITS_GOOD) && (eff != PP_BITS_VERYGOOD) && (eff != PP_BITS_BEST))
            {
                //statusstring = _pp20_txt_unrecognized;
                return false;
            }

            /*
            // Define string describing compression encoding used.
            switch (eff)
            {
                case PP_BITS_FAST:
                    statusstring = _pp20_txt_fast;
                    break;
                case PP_BITS_MEDIOCRE:
                    statusstring = _pp20_txt_mediocre;
                    break;
                case PP_BITS_GOOD:
                    statusstring = _pp20_txt_good;
                    break;
                case PP_BITS_VERYGOOD:
                    statusstring = _pp20_txt_verygood;
                    break;
                case PP_BITS_BEST:
                    statusstring = _pp20_txt_best;
                    break;
            }
            */

            return true;
        }

        private void bytesTOdword()
        {
            readPtr -= 4;
            if (readPtr < 0)
            {
                //statusstring = _pp20_txt_packeddatacorrupt;
                globalError = true;
            }
            else
            {
                current = readBEdword(source, readPtr);
            }
        }

        private int readBits(int count)
        {
            int data = 0;
            // read 'count' bits of packed data
            for (; count > 0; count--)
            {
                // equal to shift left
                data += data;
                // merge bit 0
                data |= (current & 1);
                current >>= 1;
                if (--bits == 0)
                {
                    bytesTOdword();
                    bits = 32;
                }
            }
            return data;
        }

        private void bytes()
        {
            int count, add;
            count = (add = readBits(2));
            while (add == 3)
            {
                add = readBits(2);
                count += add;
            }
            for (++count; count > 0; count--)
            {
                if (writePtr > 0)
                {
                    dest[--writePtr] = (short)readBits(8);
                }
                else
                {
                    //statusstring = _pp20_txt_packeddatacorrupt;
                    globalError = true;
                }
            }
        }

        private void sequence()
        {
            int offset, add;
            int length = readBits(2); // is length-2
            int offsetBitLen = (int)efficiency[length];
            length += 2;
            if (length != 5)
            {
                offset = readBits(offsetBitLen);
            }
            else
            {
                if (readBits(1) == 0)
                {
                    offsetBitLen = 7;
                }
                offset = readBits(offsetBitLen);
                add = readBits(3);
                length += add;
                while (add == 7)
                {
                    add = readBits(3);
                    length += add;
                }
            }
            for (; length > 0; length--)
            {
                if (writePtr > 0)
                {
                    --writePtr;
                    dest[writePtr] = dest[writePtr + 1 + offset];
                }
                else
                {
                    //statusstring = _pp20_txt_packeddatacorrupt;
                    globalError = true;
                }
            }
        }
        

        /// <summary>
        /// Read a big-endian 32-bit word from four bytes in memory. No
        /// endian-specific optimizations applied
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int readBEdword(short[] ptr, int pos)
        {
            return (((((short)ptr[pos + 0]) << 24) + (((short)ptr[pos + 1]) << 16) + (((short)ptr[pos + 2]) << 8) + ((short)ptr[pos + 3])) << 0);
        }
    }
}