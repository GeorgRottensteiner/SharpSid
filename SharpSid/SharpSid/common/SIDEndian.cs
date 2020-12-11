using System;

namespace SharpSid
{
    public static class SIDEndian
    {
        /// <summary>
        /// byte-order: HIHI..3210..LO
        /// </summary>
        public const int SID_WORDS_BIGENDIAN = 0;

        /// <summary>
        /// byte-order: LO..0123..HIHI
        /// </summary>
        public const int SID_WORDS_LITTLEENDIAN = 1;

        /// <summary>
        /// SID_WORDS_LITTLEENDIAN or SID_WORDS_BIGENDIAN
        /// </summary>
        public const int SID_WORDS = SID_WORDS_LITTLEENDIAN;

        // /////////////////////////////////////////////////////////////////
        // INT16 FUNCTIONS
        // /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set the lo byte (8 bit) in a word (16 bit)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="thebyte"></param>
        /// <returns></returns>
        public static int endian_16lo8(int word, short thebyte)
        {
            word &= 0xff00;
            word |= (ushort)thebyte;
            return word;
        }

        /// <summary>
        /// Get the lo byte (8 bit) in a word (16 bit)
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static short endian_16lo8(int word)
        {
            return (short)(word & 0xff);
        }

        /// <summary>
        /// Set the hi byte (8 bit) in a word (16 bit)
        /// </summary>
        /// <param name="word"></param>
        /// <param name="thebyte"></param>
        /// <returns></returns>
        public static int endian_16hi8(int word, short thebyte)
        {
            word &= 0x00ff;
            word |= (int)thebyte << 8;
            return word;
        }

        /// <summary>
        /// Get the hi byte (8 bit) in a word (16 bit)
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static short endian_16hi8(int word)
        {
            return (short)((word >> 8) & 0xff);
        }

        /// <summary>
        /// Swap word endian
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static int endian_16swap8(int word)
        {
            short lo = endian_16lo8(word);
            short hi = endian_16hi8(word);
            word = 0;
            word = endian_16lo8(word, hi);
            word |= endian_16hi8(word, lo);
            return word;
        }

        /// <summary>
        /// Convert high-byte and low-byte to 16-bit word
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="lo"></param>
        /// <returns></returns>
        public static int endian_16(short hi, short lo)
        {
            int word = 0;
            word = endian_16lo8(word, lo);
            word |= endian_16hi8(word, hi);
            return word;
        }

        /// <summary>
        /// Convert high-byte and low-byte to 16-bit little endian word
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <param name="word"></param>
        public static void endian_16(short[] ptr, int pos, int word)
        {
            ptr[pos + 0] = endian_16lo8(word);
            ptr[pos + 1] = endian_16hi8(word);
        }

        public static void endian_16(char[] ptr, int pos, int word)
        {
            short[] newptr = new short[] { (short)ptr[pos + 0], (short)ptr[pos + 1] };
            endian_16(newptr, 0, word);
            ptr[pos + 0] = (char)newptr[0];
            ptr[pos + 1] = (char)newptr[1];
        }

        /// <summary>
        /// Convert high-byte and low-byte to 16-bit little endian word
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int endian_little16(short[] ptr, int pos)
        {
            return endian_16(ptr[pos + 1], ptr[pos + 0]);
        }

        /// <summary>
        /// Write a little-endian 16-bit word to two bytes in memory
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <param name="word"></param>
        public static void endian_little16(short[] ptr, int pos, int word)
        {
            ptr[pos + 0] = endian_16lo8(word);
            ptr[pos + 1] = endian_16hi8(word);
        }

        /// <summary>
        /// Convert high-byte and low-byte to 16-bit big endian word
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int endian_big16(short[] ptr, int pos)
        {
            return endian_16(ptr[pos + 0], ptr[pos + 1]);
        }

        /// <summary>
        /// Write a big-endian 16-bit word to two bytes in memory
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <param name="word"></param>
        public static void endian_big16(short[] ptr, int pos, int word)
        {
            ptr[pos + 0] = endian_16hi8(word);
            ptr[pos + 1] = endian_16lo8(word);
        }

        // /////////////////////////////////////////////////////////////////
        // INT32 FUNCTIONS
        // /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set the lo word (16bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public static long endian_32lo16(long dword, int word)
        {
            dword &= (long)0xffff0000;
            dword |= (uint)word;
            return dword;
        }

        /// <summary>
        /// Get the lo word (16bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        public static int endian_32lo16(long dword)
        {
            return (int)dword & 0xffff;
        }

        /// <summary>
        /// Set the hi word (16bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public static long endian_32hi16(long dword, int word)
        {
            dword &= (long)0x0000ffff;
            dword |= (long)word << 16;
            return dword;
        }

        /// <summary>
        /// Get the hi word (16bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        public static int endian_32hi16(long dword)
        {
            return (int)dword >> 16;
        }

        /// <summary>
        /// Set the lo byte (8 bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <param name="theByte"></param>
        /// <returns></returns>
        public static long endian_32lo8(long dword, short theByte)
        {
            dword &= (long)0xffffff00;
            dword |= (ushort)theByte;
            return dword;
        }

        /// <summary>
        /// Get the lo byte (8 bit) in a dword (32 bit)
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static short endian_32lo8(long dword)
        {
            return (short)(dword & 0xff);
        }

        /// <summary>
        /// Set the hi byte (8 bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <param name="theByte"></param>
        /// <returns></returns>
        public static long endian_32hi8(long dword, short theByte)
        {
            dword &= (long)0xffff00ff;
            dword |= (long)theByte << 8;
            return dword;
        }

        /// <summary>
        /// Get the hi byte (8 bit) in a dword (32 bit)
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        public static short endian_32hi8(long dword)
        {
            return (short)((dword >> 8) & 0xff);
        }

        /// <summary>
        /// Swap hi and lo words endian in 32 bit dword
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        public static long endian_32swap16(long dword)
        {
            int lo = endian_32lo16(dword);
            int hi = endian_32hi16(dword);
            dword = 0;
            dword |= endian_32lo16(dword, hi);
            dword |= endian_32hi16(dword, lo);
            return dword;
        }

        /// <summary>
        /// Swap word endian
        /// </summary>
        /// <param name="dword"></param>
        /// <returns></returns>
        public static long endian_32swap8(long dword)
        {
            int lo, hi;
            lo = endian_32lo16(dword);
            hi = endian_32hi16(dword);
            lo = endian_16swap8(lo);
            hi = endian_16swap8(hi);
            dword = 0;
            dword |= endian_32lo16(dword, hi);
            dword |= endian_32hi16(dword, lo);
            return dword;
        }

        /// <summary>
        /// Convert high-byte and low-byte to 32-bit word
        /// </summary>
        /// <param name="hihi"></param>
        /// <param name="hilo"></param>
        /// <param name="hi"></param>
        /// <param name="lo"></param>
        /// <returns></returns>
        public static long endian_32(short hihi, short hilo, short hi, short lo)
        {
            long dword = 0;
            int word = 0;
            dword = endian_32lo8(dword, lo);
            dword |= endian_32hi8(dword, hi);
            word = endian_16lo8(word, hilo);
            word |= endian_16hi8(word, hihi);
            dword |= endian_32hi16(dword, word);
            return dword;
        }

        /// <summary>
        /// Convert high-byte and low-byte to 32-bit little endian word
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static long endian_little32(short[] ptr, int pos)
        {
            return endian_32(ptr[pos + 3], ptr[pos + 2], ptr[pos + 1], ptr[pos + 0]);
        }

        /// <summary>
        /// Write a little-endian 32-bit word to four bytes in memory
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <param name="dword"></param>
        public static void endian_little32(short[] ptr, int pos, long dword)
        {
            int word = 0;
            ptr[pos + 0] = endian_32lo8(dword);
            ptr[pos + 1] = endian_32hi8(dword);
            word = endian_32hi16(dword);
            ptr[pos + 2] = endian_16lo8(word);
            ptr[pos + 3] = endian_16hi8(word);
        }

        /// <summary>
        /// Convert high-byte and low-byte to 32-bit big endian word
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static long endian_big32(short[] ptr, int pos)
        {
            return endian_32(ptr[pos + 0], ptr[pos + 1], ptr[pos + 2], ptr[pos + 3]);
        }

        /// <summary>
        /// Write a big-endian 32-bit word to four bytes in memory
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="pos"></param>
        /// <param name="dword"></param>
        public static void endian_big32(short[] ptr, int pos, long dword)
        {
            int word = 0;
            word = endian_32hi16(dword);
            ptr[pos + 1] = endian_16lo8(word);
            ptr[pos + 0] = endian_16hi8(word);
            ptr[pos + 2] = endian_32hi8(dword);
            ptr[pos + 3] = endian_32lo8(dword);
        }
    }
}