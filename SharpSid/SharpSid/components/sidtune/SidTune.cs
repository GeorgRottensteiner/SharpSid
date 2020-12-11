using System;
using System.Text;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// @author Ken Händel
    /// </summary>
    public class SidTune
    {
        /// <summary>
        /// Also PSID file format limit
        /// </summary>
        public const int SIDTUNE_MAX_SONGS = 256;

        public const int SIDTUNE_MAX_CREDIT_stringS = 10;

        /// <summary>
        /// 80 characters plus terminating zero
        /// </summary>
        public const int SIDTUNE_MAX_CREDIT_STRLEN = 80 + 1;

        /// <summary>
        /// C64KB
        /// </summary>
        public const int SIDTUNE_MAX_MEMORY = 65536;

        /// <summary>
        /// C64KB+LOAD+PSID
        /// </summary>
        public const int SIDTUNE_MAX_FILELEN = 65536 + 2 + 0x7C;

        /// <summary>
        /// Vertical-Blanking-Interrupt
        /// </summary>
        public const int SIDTUNE_SPEED_VBI = 0;

        /// <summary>
        /// CIA 1 Timer A
        /// </summary>
        public const int SIDTUNE_SPEED_CIA_1A = 60;

        public const int SIDTUNE_CLOCK_UNKNOWN = 0x00;
        public const int SIDTUNE_CLOCK_PAL = 0x01;
        public const int SIDTUNE_CLOCK_NTSC = 0x02;
        public const int SIDTUNE_CLOCK_ANY = (SIDTUNE_CLOCK_PAL | SIDTUNE_CLOCK_NTSC);

        public const int SIDTUNE_SIDMODEL_UNKNOWN = 0x00;
        public const int SIDTUNE_SIDMODEL_6581 = 0x01;
        public const int SIDTUNE_SIDMODEL_8580 = 0x02;
        public const int SIDTUNE_SIDMODEL_ANY = (SIDTUNE_SIDMODEL_6581 | SIDTUNE_SIDMODEL_8580);

        public const int SIDTUNE_R64_MIN_LOAD_ADDR = 0x07e8;

        /// <summary>
        /// File is C64 compatible
        /// </summary>
        public const int SIDTUNE_COMPATIBILITY_C64 = 0x00;

        /// <summary>
        /// File is PSID specific
        /// </summary>
        public const int SIDTUNE_COMPATIBILITY_PSID = 0x01;

        /// <summary>
        /// File is Real C64 only
        /// </summary>
        public const int SIDTUNE_COMPATIBILITY_R64 = 0x02;

        /// <summary>
        /// File requires C64 Basic
        /// </summary>
        public const int SIDTUNE_COMPATIBILITY_BASIC = 0x03;

        public enum LoadStatus
        {
            LOAD_NOT_MINE,
            LOAD_OK,
            LOAD_ERROR
        }


        internal SidTuneInfo info = new SidTuneInfo();

        internal bool status;

        internal short[] songSpeed = new short[SIDTUNE_MAX_SONGS];
        internal short[] clockSpeed = new short[SIDTUNE_MAX_SONGS];
        internal short[] songLength = new short[SIDTUNE_MAX_SONGS];

        /// <summary>
        /// holds text info from the format headers etc
        /// </summary>
        internal string[] infostring = new string[SIDTUNE_MAX_CREDIT_stringS];

        /// <summary>
        /// For files with header: offset to real data
        /// </summary>
        internal int fileOffset;

        /// <summary>
        /// Needed for MUS/STR player installation
        /// </summary>
        internal int musDataLen;

        protected Buffer_sidtt cache = new Buffer_sidtt();


        /// <summary>
        /// Load a sidtune from a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNameExt"></param>
        public SidTune(Stream stream)
        {
            init();
            getFromFiles(stream);
        }
        // only used for deserializing
        public SidTune(BinaryReader reader)
        {
            LoadFromReader(reader);
        }

        /// <summary>
        /// Select sub-song (0 = default starting song) and return active song number
        /// out of [1,2,..,SIDTUNE_MAX_SONGS]
        /// </summary>
        /// <param name="selectedSong"></param>
        /// <returns></returns>
        public int selectSong(int selectedSong)
        {
            if (!status)
            {
                return 0;
            }
            else
            {
                //info.statusstring = txt_noErrors;
            }

            int song = selectedSong;
            // Determine and set starting song number.
            if (selectedSong == 0)
            {
                song = info.startSong;
            }
            if (selectedSong > info.songs || selectedSong > SIDTUNE_MAX_SONGS)
            {
                song = info.startSong;
                //info.statusstring = txt_songNumberExceed;
            }
            info.currentSong = song;
            //info.songLength = songLength[song - 1];
            // Retrieve song speed definition.
            if (info.compatibility == SIDTUNE_COMPATIBILITY_R64)
            {
                info.songSpeed = SIDTUNE_SPEED_CIA_1A;
            }
            else
            {
                info.songSpeed = songSpeed[song - 1];
            }
            info.clockSpeed = clockSpeed[song - 1];
            // Assign song speed description string depending on clock speed.
            // speed description is available only after song init.
            /*
            if (info.songSpeed == SIDTUNE_SPEED_VBI)
            {
                info.speedstring = txt_VBI;
            }
            else
            {
                info.speedstring = txt_CIA;
            }
            */
            return info.currentSong;
        }

        /// <summary>
        /// Retrieve sub-song specific information
        /// </summary>
        /// <returns></returns>
        public SidTuneInfo Info
        {
            get
            {
                return info;
            }
        }

        /// <summary>
        /// Determine current state of object (true = okay, false = error). Upon
        /// error condition use SidTuneInfo.statusstring
        /// </summary>
        /// <returns></returns>
        public bool StatusOk
        {
            get
            {
                return status;
            }
        }

        /// <summary>
        /// Whether sidtune uses two SID chips
        /// </summary>
        /// <returns></returns>
        public bool isStereo
        {
            get
            {
                return (info.sidChipBase1 != 0 && info.sidChipBase2 != 0);
            }
        }

        /// <summary>
        /// Copy sidtune into C64 memory (64 KB)
        /// </summary>
        /// <param name="c64buf"></param>
        /// <returns></returns>
        public bool placeSidTuneInC64mem(short[] c64buf)
        {
            if (status && c64buf != null)
            {
                int endPos = info.loadAddr + info.c64dataLen;
                if (endPos <= SIDTUNE_MAX_MEMORY)
                {
                    // Copy data from cache to the correct destination.
                    Array.Copy(cache.buf, fileOffset, c64buf, info.loadAddr, info.c64dataLen);
                    //info.statusstring = txt_noErrors;
                }
                else
                {
                    // Security - cut data which would exceed the end of the C64 memory
                    Array.Copy(cache.buf, fileOffset, c64buf, info.loadAddr, info.c64dataLen - (endPos - SIDTUNE_MAX_MEMORY));
                    //info.statusstring = txt_dataTooLong;
                }
            }
            return (status && c64buf != null);
        }

        /// <summary>
        /// Does not affect status of object, and therefore can be used to load
        /// files. Error string is put into info.statusstring, though
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="bufferRef"></param>
        /// <returns></returns>
        public bool loadFile(Stream stream, Buffer_sidtt bufferRef)
        {
            Buffer_sidtt fileBuf = new Buffer_sidtt();
            int fileLen = 0;

            try
            {
                using (BinaryReader myIn = new BinaryReader(stream))
                {
                    fileLen = (int)stream.Length;
                    if (!fileBuf.assign(new short[fileLen], fileLen))
                    {
                        //info.statusstring = txt_notEnoughMemory;
                        return false;
                    }
                    int restFileLen = fileLen;
                    if (restFileLen > 0)
                    {
                        for (int i = 0; i < fileLen; i++)
                        {
                            fileBuf.buf[i] = (short)myIn.ReadByte();
                        }
                    }
                }
            }
            catch
            {
                //info.statusstring = txt_cantLoadFile;
                return false;
            }

            if (fileLen == 0)
            {
                //info.statusstring = txt_empty;
                return false;
            }

            if (decompressPP20(fileBuf) < 0)
            {
                return false;
            }

            bufferRef.assign(fileBuf.xferPtr(), fileBuf.xferLen());
            return true;
        }

        /// <summary>
        /// Convert 32-bit PSID-style speed word to internal tables
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="clock"></param>
        internal void convertOldStyleSpeedToTables(long speed, short clock)
        {
            // Create the speed/clock setting tables.
            //
            // This does not take into account the PlaySID bug upon evaluating the
            // SPEED field. It would most likely break compatibility to lots of
            // sidtunes, which have been converted from .SID format and vice versa.
            // The .SID format does the bit-wise/song-wise evaluation of the SPEED
            // value correctly, like it is described in the PlaySID documentation.

            int toDo = ((info.songs <= SIDTUNE_MAX_SONGS) ? info.songs : SIDTUNE_MAX_SONGS);
            for (int s = 0; s < toDo; s++)
            {
                clockSpeed[s] = clock;
                if (((speed >> (s & 31)) & 1) == 0)
                {
                    songSpeed[s] = SIDTUNE_SPEED_VBI;
                }
                else
                {
                    songSpeed[s] = SIDTUNE_SPEED_CIA_1A;
                }
            }
        }

        internal int convertPetsciiToAscii(SmartPtr_sidtt spPet, StringBuilder dest)
        {
            int count = 0;
            short c;
            if (dest != null)
            {
                do
                {
                    c = _sidtune_CHRtab[spPet.operatorMal()]; // ASCII CHR$
                    // conversion
                    if ((c >= 0x20) && (count <= 31))
                    {
                        dest.Length = count + 1;
                        dest[count++] = (char)c; // copy to info string
                    }
                    // if character is 0x9d (left arrow key) then move back.
                    if ((spPet.operatorMal() == 0x9d) && (count >= 0))
                    {
                        count--;
                    }
                    spPet.operatorPlusPlus();
                } while (!((c == 0x0D) || (c == 0x00) || spPet.fail));
            }
            else
            {
                // Just find end of string
                do
                {
                    c = _sidtune_CHRtab[spPet.operatorMal()]; // ASCII CHR$
                    // conversion
                    spPet.operatorPlusPlus();
                } while (!((c == 0x0D) || (c == 0x00) || spPet.fail));
            }
            return count;
        }

        /// <summary>
        /// Check compatibility details are sensible
        /// </summary>
        /// <returns></returns>
        protected bool checkCompatibility()
        {
            switch (info.compatibility)
            {
                case SIDTUNE_COMPATIBILITY_R64:
                    // Check valid init address
                    switch (info.initAddr >> 12)
                    {
                        case 0x0F:
                        case 0x0E:
                        case 0x0D:
                        case 0x0B:
                        case 0x0A:
                            //info.statusstring = txt_badAddr;
                            return false;
                        default:
                            if ((info.initAddr < info.loadAddr) || (info.initAddr > (info.loadAddr + info.c64dataLen - 1)))
                            {
                                //info.statusstring = txt_badAddr;
                                return false;
                            }
                            break;
                    }
                    // fall through
                    // Check tune is loadable on a real C64
                    if (info.loadAddr < SIDTUNE_R64_MIN_LOAD_ADDR)
                    {
                        //info.statusstring = txt_badAddr;
                        return false;
                    }
                    break;
                case SIDTUNE_COMPATIBILITY_BASIC:
                    // Check tune is loadable on a real C64
                    if (info.loadAddr < SIDTUNE_R64_MIN_LOAD_ADDR)
                    {
                        //info.statusstring = txt_badAddr;
                        return false;
                    }
                    break;
            }
            return true;
        }

        /// <summary>
        /// Check for valid relocation information
        /// </summary>
        /// <returns></returns>
        protected bool checkRelocInfo()
        {
            short startp, endp;

            // Fix relocation information
            if (info.relocStartPage == 0xFF)
            {
                info.relocPages = 0;
                return true;
            }
            else if (info.relocPages == 0)
            {
                info.relocStartPage = 0;
                return true;
            }

            // Calculate start/end page
            startp = info.relocStartPage;
            endp = (short)((startp + info.relocPages - 1) & 0xff);
            if (endp < startp)
            {
                //info.statusstring = txt_badReloc;
                return false;
            }

            // Check against load range
            short startlp, endlp;
            startlp = (short)(info.loadAddr >> 8);
            endlp = startlp;
            endlp += (short)((info.c64dataLen - 1) >> 8);

            if (((startp <= startlp) && (endp >= startlp)) || ((startp <= endlp) && (endp >= endlp)))
            {
                //info.statusstring = txt_badReloc;
                return false;
            }

            // Check that the relocation information does not use the following
            // memory areas: 0x0000-0x03FF, 0xA000-0xBFFF and 0xD000-0xFFFF
            if ((startp < 0x04) || ((0xa0 <= startp) && (startp <= 0xbf)) || (startp >= 0xd0) || ((0xa0 <= endp) && (endp <= 0xbf)) || (endp >= 0xd0))
            {
                //info.statusstring = txt_badReloc;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Common address resolution procedure
        /// </summary>
        /// <param name="c64data"></param>
        /// <param name="fileOffset2"></param>
        /// <returns></returns>
        protected bool resolveAddrs(short[] c64data, int fileOffset2)
        {
            // Originally used as a first attempt at an RSID
            // style format. Now reserved for future use
            if (info.playAddr == 0xffff)
            {
                info.playAddr = 0;
            }

            // loadAddr = 0 means, the address is stored in front of the C64 data.
            if (info.loadAddr == 0)
            {
                if (info.c64dataLen < 2)
                {
                    //info.statusstring = txt_corrupt;
                    return false;
                }
                info.loadAddr = SIDEndian.endian_16(c64data[fileOffset + 1], c64data[fileOffset + 0]);
                fileOffset += 2;
                // c64data += 2;
                info.c64dataLen -= 2;
            }

            if (info.compatibility == SIDTUNE_COMPATIBILITY_BASIC)
            {
                if (info.initAddr != 0)
                {
                    //info.statusstring = txt_badAddr;
                    return false;
                }
            }
            else if (info.initAddr == 0)
            {
                info.initAddr = info.loadAddr;
            }
            return true;
        }

        // Support for various file formats.

        private PSid psid;

        protected LoadStatus PSID_fileSupport(Buffer_sidtt dataBuf)
        {
            return psid.PSID_fileSupport(dataBuf);
        }

        protected LoadStatus SID_fileSupport(Buffer_sidtt dataBuf, Buffer_sidtt sidBuf)
        {
            return LoadStatus.LOAD_NOT_MINE;
        }

        protected bool SID_fileSupportSave(Stream toFile)
        {
            return true;
        }

        // Error and status message strings.
        /*
        protected const string txt_songNumberExceed = "SIDTUNE WARNING: Selected song number was too high";
        protected const string txt_empty = "SIDTUNE ERROR: No data to load";
        protected const string txt_unrecognizedFormat = "SIDTUNE ERROR: Could not determine file format";
        protected const string txt_noDataFile = "SIDTUNE ERROR: Did not find the corresponding data file";
        protected const string txt_notEnoughMemory = "SIDTUNE ERROR: Not enough free memory";
        protected const string txt_cantLoadFile = "SIDTUNE ERROR: Could not load input file";
        protected const string txt_cantOpenFile = "SIDTUNE ERROR: Could not open file for binary input";
        protected const string txt_fileTooLong = "SIDTUNE ERROR: Input data too long";
        protected const string txt_dataTooLong = "SIDTUNE ERROR: Size of music data exceeds C64 memory";
        protected const string txt_cantCreateFile = "SIDTUNE ERROR: Could not create output file";
        protected const string txt_fileIoError = "SIDTUNE ERROR: File I/O error";
        protected const string txt_badAddr = "SIDTUNE ERROR: Bad address data";
        protected const string txt_badReloc = "SIDTUNE ERROR: Bad reloc data";
        protected const string txt_corrupt = "SIDTUNE ERROR: File is incomplete or corrupt";
        protected const string txt_noErrors = "No errors";
        
        protected const string txt_VBI = "VBI";
        protected const string txt_CIA = "CIA 1 Timer A";
        protected const string txt_na = "N/A";
        */

        /// <summary>
        /// Petscii to Ascii conversion table.
        /// 
        /// CHR$ conversion table (0x01 = no output)
        /// </summary>
        private static short[] _sidtune_CHRtab = { 0x0, 0x1, 0x1, 0x1, 0x1,
            0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0xd, 0x1, 0x1, 0x1, 0x1,
            0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1,
            0x1, 0x20, 0x21, 0x1, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
            0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0x34,
            0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a,
            0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55,
            0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x24, 0x5d, 0x20, 0x20,
            // alternative: CHR$(92=0x5c) => ISO Latin-1(0xa3)
            0x2d, 0x23, 0x7c, 0x2d, 0x2d, 0x2d, 0x2d, 0x7c, 0x7c, 0x5c, 0x5c,
            0x2f, 0x5c, 0x5c, 0x2f, 0x2f, 0x5c, 0x23, 0x5f, 0x23, 0x7c, 0x2f,
            0x58, 0x4f, 0x23, 0x7c, 0x23, 0x2b, 0x7c, 0x7c, 0x26, 0x5c,
            // 0x80-0xFF
            0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1,
            0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1,
            0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x20, 0x7c, 0x23, 0x2d, 0x2d, 0x7c,
            0x23, 0x7c, 0x23, 0x2f, 0x7c, 0x7c, 0x2f, 0x5c, 0x5c, 0x2d, 0x2f,
            0x2d, 0x2d, 0x7c, 0x7c, 0x7c, 0x7c, 0x2d, 0x2d, 0x2d, 0x2f, 0x5c,
            0x5c, 0x2f, 0x2f, 0x23, 0x2d, 0x23, 0x7c, 0x2d, 0x2d, 0x2d, 0x2d,
            0x7c, 0x7c, 0x5c, 0x5c, 0x2f, 0x5c, 0x5c, 0x2f, 0x2f, 0x5c, 0x23,
            0x5f, 0x23, 0x7c, 0x2f, 0x58, 0x4f, 0x23, 0x7c, 0x23, 0x2b, 0x7c,
            0x7c, 0x26, 0x5c, 0x20, 0x7c, 0x23, 0x2d, 0x2d, 0x7c, 0x23, 0x7c,
            0x23, 0x2f, 0x7c, 0x7c, 0x2f, 0x5c, 0x5c, 0x2d, 0x2f, 0x2d, 0x2d,
            0x7c, 0x7c, 0x7c, 0x7c, 0x2d, 0x2d, 0x2d, 0x2f, 0x5c, 0x5c, 0x2f,
            0x2f, 0x23 };

        private void init()
        {
            psid = new PSid(this);

            // Initialize the object with some safe defaults.
            status = false;

            //info.statusstring = txt_na;
            info.path = info.infoFileName = info.dataFileName = null;
            info.dataFileLen = info.c64dataLen = 0;
            //info.formatstring = txt_na;
            //info.speedstring = txt_na;
            info.loadAddr = (info.initAddr = (info.playAddr = 0));
            info.songs = (info.startSong = (info.currentSong = 0));
            info.sidChipBase1 = 0xd400;
            info.sidChipBase2 = 0;
            info.musPlayer = false;
            info.fixLoad = false;
            info.songSpeed = SIDTUNE_SPEED_VBI;
#if SIDTUNE_PSID2NG
            info.clockSpeed = SIDTUNE_CLOCK_UNKNOWN;
            info.sidModel = SIDTUNE_SIDMODEL_UNKNOWN;
#else
            info.clockSpeed = SIDTUNE_CLOCK_PAL;
            info.sidModel = SIDTUNE_SIDMODEL_6581;
#endif
            info.compatibility = SIDTUNE_COMPATIBILITY_C64;
            //info.songLength = 0;
            info.relocStartPage = 0;
            info.relocPages = 0;

            for (int si = 0; si < SIDTUNE_MAX_SONGS; si++)
            {
                songSpeed[si] = info.songSpeed;
                clockSpeed[si] = info.clockSpeed;
                songLength[si] = 0;
            }

            fileOffset = 0;
            musDataLen = 0;

            for (int sNum = 0; sNum < SIDTUNE_MAX_CREDIT_stringS; sNum++)
            {
                infostring[sNum] = null;
            }
            info.numberOfInfostrings = 0;
        }

        /// <summary>
        /// Initializing the object based upon what we find in the specified file
        /// </summary>
        /// <param name="fileName"></param>
        private void getFromFiles(Stream stream)
        {
            // Assume a failure, so we can simply return.
            status = false;

            Buffer_sidtt fileBuf1 = new Buffer_sidtt(), fileBuf2 = new Buffer_sidtt();
            StringBuilder fileName2 = new StringBuilder();

            // Try to load the single specified file. The original method didn't
            // quite work that well, so instead we now let the support files take
            // ownership of a known file and don't assume we should just
            // continue searching when an error is found.
            if (loadFile(stream, fileBuf1))
            {
                LoadStatus ret;

                // File loaded. Now check if it is in a valid single-file-format.
                ret = PSID_fileSupport(fileBuf1);
                if (ret != LoadStatus.LOAD_NOT_MINE)
                {
                    if (ret == LoadStatus.LOAD_OK)
                    {
                        status = acceptSidTune(fileBuf1);
                    }
                }
            } 
        }

        /// <summary>
        /// Support for OR-ing two LoadStatus enums
        /// </summary>
        /// <param name="support"></param>
        /// <param name="support2"></param>
        /// <returns></returns>
        /*
        private LoadStatus orStatus(LoadStatus support, LoadStatus support2)
        {
            int val1 = (support == LoadStatus.LOAD_NOT_MINE) ? 0 : (support == LoadStatus.LOAD_OK) ? 1 : 2;
            int val2 = (support2 == LoadStatus.LOAD_NOT_MINE) ? 0 : (support2 == LoadStatus.LOAD_OK) ? 1 : 2;
            int erg = val1 | val2;
            return (erg == 0) ? LoadStatus.LOAD_NOT_MINE : (erg == 1) ? LoadStatus.LOAD_OK : LoadStatus.LOAD_ERROR;
        }
        */

        /// <summary>
        /// Cache the data of a single-file or two-file sidtune and its corresponding file names
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        private bool acceptSidTune(Buffer_sidtt buf)
        {
            // @FIXME@ - MUS
            if (info.numberOfInfostrings == 3)
            {
                // Add <?> (HVSC standard) to
                // missing title, author,
                // release fields
                for (int i = 0; i < 3; i++)
                {
                    if (infostring[i].Length == 0)
                    {
                        infostring[i] = "<?>";
                        info.infostring[i] = infostring[i];
                    }
                }
            }

            // Fix bad sidtune set up.
            if (info.songs > SIDTUNE_MAX_SONGS)
            {
                info.songs = SIDTUNE_MAX_SONGS;
            }
            else if (info.songs == 0)
            {
                info.songs++;
            }
            if (info.startSong > info.songs)
            {
                info.startSong = 1;
            }
            else if (info.startSong == 0)
            {
                info.startSong++;
            }

            info.dataFileLen = buf.bufLen;
            info.c64dataLen = buf.bufLen - fileOffset;

            // Calculate any remaining addresses and then
            // confirm all the file details are correct
            if (resolveAddrs(buf.buf, fileOffset) == false)
            {
                return false;
            }
            if (!checkRelocInfo())
            {
                return false;
            }
            if (!checkCompatibility())
            {
                return false;
            }

            if (info.dataFileLen >= 2)
            {
                // We only detect an offset of two. Some position independent
                // sidtunes contain a load address of 0xE000, but are loaded
                // to 0x0FFE and call player at 0x1000.
                info.fixLoad = (SIDEndian.endian_little16(buf.buf, fileOffset) == (info.loadAddr + 2));
            }

            // Check the size of the data.
            if (info.c64dataLen > SIDTUNE_MAX_MEMORY)
            {
                //info.statusstring = txt_dataTooLong;
                return false;
            }
            else if (info.c64dataLen == 0)
            {
                //info.statusstring = txt_empty;
                return false;
            }

            cache.assign(buf.xferPtr(), buf.xferLen());

            //info.statusstring = txt_noErrors;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <returns>0 for no decompression (buf unchanged), 1 for decompression and -1 for error</returns>
        private int decompressPP20(Buffer_sidtt buf)
        {
            // Check for PowerPacker compression: load and decompress, if PP20 file.
            PP20 myPP = new PP20();
            int fileLen;
            if (myPP.isCompressed(buf.buf, buf.bufLen))
            {
                PP20.Decompressed decomp = new PP20.Decompressed();
                if (0 == (fileLen = myPP.decompress(buf.buf, buf.bufLen, decomp)))
                {
                    //info.statusstring = myPP.getStatusstring();
                    return -1;
                }
                else
                {
                    //info.statusstring = myPP.getStatusstring();
                    // Replace compressed buffer with uncompressed buffer.
                    buf.assign(decomp.destBufRef, fileLen);
                }
                return 1;
            }
            return 0;
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            info.SaveToWriter(writer);
            writer.Write(status);

            writer.Write(songSpeed.Length);
            for (int i = 0; i < songSpeed.Length; i++)
            {
                writer.Write(songSpeed[i]);
            }

            writer.Write(clockSpeed.Length);
            for (int i = 0; i < clockSpeed.Length; i++)
            {
                writer.Write(clockSpeed[i]);
            }

            writer.Write(songLength.Length);
            for (int i = 0; i < songLength.Length; i++)
            {
                writer.Write(songLength[i]);
            }

            writer.Write(infostring.Length);
            for (int i = 0; i < infostring.Length; i++)
            {
                writer.Write(SID2Types.StringNotNull(infostring[i]));
            }

            writer.Write(fileOffset);
            writer.Write(musDataLen);
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            info = new SidTuneInfo(reader);

            status = reader.ReadBoolean();

            int count = reader.ReadInt32();
            songSpeed = new short[count];
            for (int i = 0; i < songSpeed.Length; i++)
            {
                songSpeed[i] = reader.ReadInt16();
            }

            count = reader.ReadInt32();
            clockSpeed = new short[count];
            for (int i = 0; i < clockSpeed.Length; i++)
            {
                clockSpeed[i] = reader.ReadInt16();
            }

            count = reader.ReadInt32();
            songLength = new short[count];
            for (int i = 0; i < songLength.Length; i++)
            {
                songLength[i] = reader.ReadInt16();
            }

            count = reader.ReadInt32();
            infostring = new string[count];
            for (int i = 0; i < infostring.Length; i++)
            {
                infostring[i] = reader.ReadString();
            }

            fileOffset = reader.ReadInt32();
            musDataLen = reader.ReadInt32();
        }
    }
}