using System;

namespace SharpSid
{
    /// <summary>
    /// Header has been extended for 'RSID' format
    /// 
    /// The following changes are present:
    /// - id = 'RSID'
    /// - version = 2 only
    /// - play, load and speed reserved 0
    /// - psid specific flag reserved 0
    /// - init cannot be under ROMS/IO-
    ///  load cannot be less than 0x0801 (start of basic)
    ///  
    /// all values big-endian
    /// 
    /// @author Ken Händel
    /// </summary>
    public class PHeader
    {
        public const int SIZE = 124;

        public PHeader(short[] s, int offset)
        {
            for (int i = 0; i < id.Length; i++)
            {
                id[i] = s[offset++];
            }
            for (int i = 0; i < version.Length; i++)
            {
                version[i] = s[offset++];
            }
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = s[offset++];
            }
            for (int i = 0; i < load.Length; i++)
            {
                load[i] = s[offset++];
            }
            for (int i = 0; i < init.Length; i++)
            {
                init[i] = s[offset++];
            }
            for (int i = 0; i < play.Length; i++)
            {
                play[i] = s[offset++];
            }
            for (int i = 0; i < songs.Length; i++)
            {
                songs[i] = s[offset++];
            }
            for (int i = 0; i < start.Length; i++)
            {
                start[i] = s[offset++];
            }
            for (int i = 0; i < speed.Length; i++)
            {
                speed[i] = s[offset++];
            }
            for (int i = 0; i < name.Length; i++)
            {
                name[i] = (char)s[offset++];
            }
            for (int i = 0; i < author.Length; i++)
            {
                author[i] = (char)s[offset++];
            }
            for (int i = 0; i < released.Length; i++)
            {
                released[i] = (char)s[offset++];
            }
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = s[offset++];
            }
            relocStartPage = s[offset++];
            relocPages = s[offset++];
            for (int i = 0; i < reserved.Length; i++)
            {
                reserved[i] = s[offset++];
            }
        }

        public PHeader()
        {
        }

        /// <summary>
        /// 'PSID' (ASCII)
        /// </summary>
        public short[] id = new short[4];

        /// <summary>
        /// 0x0001 or 0x0002
        /// </summary>
        public short[] version = new short[2];

        /// <summary>
        /// 16-bit offset to binary data in file
        /// </summary>
        public short[] data = new short[2];

        /// <summary>
        /// 16-bit C64 address to load file to
        /// </summary>
        public short[] load = new short[2];

        /// <summary>
        /// 16-bit C64 address of init subroutine
        /// </summary>
        public short[] init = new short[2];

        /// <summary>
        /// 16-bit C64 address of play subroutine
        /// </summary>
        public short[] play = new short[2];

        /// <summary>
        /// number of songs
        /// </summary>
        public short[] songs = new short[2];

        /// <summary>
        /// start song out of [1..256]
        /// </summary>
        public short[] start = new short[2];

        /// <summary>
        /// 32-bit speed info:
        /// bit: 0=50 Hz, 1=CIA 1 Timer A (default: 60 Hz)
        /// </summary>
        public short[] speed = new short[4];

        /// <summary>
        /// ASCII strings, 31 characters long and terminated by a trailing zero
        /// </summary>
        public char[] name = new char[32];

        /// <summary>
        /// ASCII strings, 31 characters long and terminated by a trailing zero
        /// </summary>
        public char[] author = new char[32];

        /// <summary>
        /// ASCII strings, 31 characters long and terminated by a trailing zero
        /// </summary>
        public char[] released = new char[32];

        /// <summary>
        /// only version 0x0002
        /// </summary>
        public short[] flags = new short[2];

        /// <summary>
        /// only version 0x0002B
        /// </summary>
        public short relocStartPage;

        /// <summary>
        /// only version 0x0002B
        /// </summary>
        public short relocPages;

        /// <summary>
        /// only version 0x0002
        /// </summary>
        public short[] reserved = new short[2];

        public short[] getArray()
        {
            return new short[] {
                    id[0], id[1], id[2], id[3], version[0], version[1],
                    data[0], data[1], load[0], load[1], init[0], init[1],
                    play[0], play[1], songs[0], songs[1], start[0], start[1],
                    speed[0], speed[1], speed[2], speed[3], (short) name[0],
                    (short) name[1], (short) name[2], (short) name[3],
                    (short) name[4], (short) name[5], (short) name[6],
                    (short) name[7], (short) name[8], (short) name[9],
                    (short) name[10], (short) name[11], (short) name[12],
                    (short) name[13], (short) name[14], (short) name[13],
                    (short) name[16], (short) name[17], (short) name[18],
                    (short) name[19], (short) name[20], (short) name[21],
                    (short) name[22], (short) name[23], (short) name[24],
                    (short) name[25], (short) name[26], (short) name[27],
                    (short) name[28], (short) name[29], (short) name[30],
                    (short) name[31],

                    (short) author[0], (short) author[1], (short) author[2],
                    (short) author[3], (short) author[4], (short) author[5],
                    (short) author[6], (short) author[7], (short) author[8],
                    (short) author[9], (short) author[10], (short) author[11],
                    (short) author[12], (short) author[13], (short) author[14],
                    (short) author[13], (short) author[16], (short) author[17],
                    (short) author[18], (short) author[19], (short) author[20],
                    (short) author[21], (short) author[22], (short) author[23],
                    (short) author[24], (short) author[25], (short) author[26],
                    (short) author[27], (short) author[28], (short) author[29],
                    (short) author[30], (short) author[31],

                    (short) released[0], (short) released[1],
                    (short) released[2], (short) released[3],
                    (short) released[4], (short) released[5],
                    (short) released[6], (short) released[7],
                    (short) released[8], (short) released[9],
                    (short) released[10], (short) released[11],
                    (short) released[12], (short) released[13],
                    (short) released[14], (short) released[13],
                    (short) released[16], (short) released[17],
                    (short) released[18], (short) released[19],
                    (short) released[20], (short) released[21],
                    (short) released[22], (short) released[23],
                    (short) released[24], (short) released[25],
                    (short) released[26], (short) released[27],
                    (short) released[28], (short) released[29],
                    (short) released[30], (short) released[31],

                    flags[0], flags[1], relocStartPage, relocPages,
                    reserved[0], reserved[1], };
        }
    }
}