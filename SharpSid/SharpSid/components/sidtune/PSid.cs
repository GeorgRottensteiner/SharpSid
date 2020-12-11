using System;
using System.IO;

namespace SharpSid
{
    public class PSid
    {
        public const int PSID_ID = 0x50534944;
        public const int RSID_ID = 0x52534944;

        // PSID_SPECIFIC and PSID_BASIC are mutually exclusive

        public const int PSID_MUS = 1 << 0;
        public const int PSID_SPECIFIC = 1 << 1;
        public const int PSID_BASIC = 1 << 1;
        public const int PSID_CLOCK = 3 << 2;
        public const int PSID_SIDMODEL = 3 << 4;

        // These are also used in the emulator engine!

        public const int PSID_CLOCK_UNKNOWN = 0;
        public const int PSID_CLOCK_PAL = 1 << 2;
        public const int PSID_CLOCK_NTSC = 1 << 3;
        public const int PSID_CLOCK_ANY = (PSID_CLOCK_PAL | PSID_CLOCK_NTSC);

        // SID model

        public const int PSID_SIDMODEL_UNKNOWN = 0;
        public const int PSID_SIDMODEL_6581 = 1 << 4;
        public const int PSID_SIDMODEL_8580 = 1 << 5;
        public const int PSID_SIDMODEL_ANY = PSID_SIDMODEL_6581 | PSID_SIDMODEL_8580;

        // sidtune format errors
        /*
        const string _sidtune_format_psid = "PlaySID one-file format (PSID)";
        const string _sidtune_format_rsid = "Real C64 one-file format (RSID)";
        const string _sidtune_unknown_psid = "Unsupported PSID version";
        const string _sidtune_unknown_rsid = "Unsupported RSID version";
        const string _sidtune_truncated = "ERROR: File is most likely truncated";
        const string _sidtune_invalid = "ERROR: File contains invalid data";
        */

        const int _sidtune_psid_maxStrLen = 31;

        private SidTune sidtune;

        private SidTuneInfo info;

        public PSid(SidTune sidtune)
        {
            this.sidtune = sidtune;
            this.info = sidtune.info;
        }

        internal SidTune.LoadStatus PSID_fileSupport(Buffer_sidtt dataBuf)
        {
            short clock, compatibility;
            long speed;
            int bufLen = dataBuf.bufLen;
#if SIDTUNE_PSID2NG
            clock = SidTune.SIDTUNE_CLOCK_UNKNOWN;
#else
            clock = info.clockSpeed;
#endif
            compatibility = SidTune.SIDTUNE_COMPATIBILITY_C64;

            // Require minimum size to allow access to the first few bytes.
            // Require a valid ID and version number.
            PHeader pHeader = new PHeader(dataBuf.buf, 0);

            // File format check
            if (bufLen < 6)
            {
                return SidTune.LoadStatus.LOAD_NOT_MINE;
            }
            if (SIDEndian.endian_big32((short[])pHeader.id, 0) == PSID_ID)
            {
                switch (SIDEndian.endian_big16(pHeader.version, 0))
                {
                    case 1:
                        compatibility = SidTune.SIDTUNE_COMPATIBILITY_PSID;
                        // Deliberate run on
                        break;
                    case 2:
                        break;
                    default:
                        //info.formatstring = _sidtune_unknown_psid;
                        return SidTune.LoadStatus.LOAD_ERROR;
                }
                //info.formatstring = _sidtune_format_psid;
            }
            else if (SIDEndian.endian_big32((short[])pHeader.id, 0) == RSID_ID)
            {
                if (SIDEndian.endian_big16(pHeader.version, 0) != 2)
                {
                    //info.formatstring = _sidtune_unknown_rsid;
                    return SidTune.LoadStatus.LOAD_ERROR;
                }
                //info.formatstring = _sidtune_format_rsid;
                compatibility = SidTune.SIDTUNE_COMPATIBILITY_R64;
            }
            else
            {
                return SidTune.LoadStatus.LOAD_NOT_MINE;
            }

            // Due to security concerns, input must be at least as long as version 1
            // header plus 16-bit C64 load address. That is the area which will be
            // accessed.
            if (bufLen < (PHeader.SIZE + 2))
            {
                //info.formatstring = _sidtune_truncated;
                return SidTune.LoadStatus.LOAD_ERROR;
            }

            sidtune.fileOffset = SIDEndian.endian_big16(pHeader.data, 0);
            info.loadAddr = SIDEndian.endian_big16(pHeader.load, 0);
            info.initAddr = SIDEndian.endian_big16(pHeader.init, 0);
            info.playAddr = SIDEndian.endian_big16(pHeader.play, 0);
            info.songs = SIDEndian.endian_big16(pHeader.songs, 0);
            info.startSong = SIDEndian.endian_big16(pHeader.start, 0);
            info.sidChipBase1 = 0xd400;
            info.sidChipBase2 = 0;
            info.compatibility = compatibility;
            speed = SIDEndian.endian_big32(pHeader.speed, 0);

            if (info.songs > SidTune.SIDTUNE_MAX_SONGS)
            {
                info.songs = SidTune.SIDTUNE_MAX_SONGS;
            }

            info.musPlayer = false;
            info.sidModel = SidTune.SIDTUNE_SIDMODEL_UNKNOWN;
            info.relocPages = 0;
            info.relocStartPage = 0;
            if (SIDEndian.endian_big16(pHeader.version, 0) >= 2)
            {
                int flags = SIDEndian.endian_big16(pHeader.flags, 0);
                if ((flags & PSID_MUS) != 0)
                {
                    // MUS tunes run at any speed
                    clock = SidTune.SIDTUNE_CLOCK_ANY;
                    info.musPlayer = true;
                }

#if SIDTUNE_PSID2NG
                // This flags is only available for the appropriate file formats
                switch (compatibility)
                {
                    case SidTune.SIDTUNE_COMPATIBILITY_C64:
                        if ((flags & PSID_SPECIFIC) != 0)
                        {
                            info.compatibility = SidTune.SIDTUNE_COMPATIBILITY_PSID;
                        }
                        break;
                    case SidTune.SIDTUNE_COMPATIBILITY_R64:
                        if ((flags & PSID_BASIC) != 0)
                        {
                            info.compatibility = SidTune.SIDTUNE_COMPATIBILITY_BASIC;
                        }
                        break;
                }

                if ((flags & PSID_CLOCK_PAL) != 0)
                {
                    clock |= SidTune.SIDTUNE_CLOCK_PAL;
                }
                if ((flags & PSID_CLOCK_NTSC) != 0)
                {
                    clock |= SidTune.SIDTUNE_CLOCK_NTSC;
                }
                info.clockSpeed = clock;

                info.sidModel = SidTune.SIDTUNE_SIDMODEL_UNKNOWN;
                if ((flags & PSID_SIDMODEL_6581) != 0)
                {
                    info.sidModel |= SidTune.SIDTUNE_SIDMODEL_6581;
                }
                if ((flags & PSID_SIDMODEL_8580) != 0)
                {
                    info.sidModel |= SidTune.SIDTUNE_SIDMODEL_8580;
                }

                info.relocStartPage = pHeader.relocStartPage;
                info.relocPages = pHeader.relocPages;
#endif
            }

            // Check reserved fields to force real c64 compliance
            // as required by the RSID specification
            if (compatibility == SidTune.SIDTUNE_COMPATIBILITY_R64)
            {
                if ((info.loadAddr != 0) || (info.playAddr != 0) || (speed != 0))
                {
                    //info.formatstring = _sidtune_invalid;
                    return SidTune.LoadStatus.LOAD_ERROR;
                }
                // Real C64 tunes appear as CIA
                speed = ~0;
            }
            // Create the speed/clock setting table.
            sidtune.convertOldStyleSpeedToTables(speed, clock);

            // Copy info strings, so they will not get lost.
            info.numberOfInfostrings = 3;

            // Name
            int i;
            for (i = 0; i < pHeader.name.Length; i++)
            {
                if (pHeader.name[i] == 0)
                {
                    break;
                }
            }
            info.infostring[0] = sidtune.infostring[0] = new string(pHeader.name, 0, Math.Min(i, _sidtune_psid_maxStrLen));

            // Author
            for (i = 0; i < pHeader.author.Length; i++)
            {
                if (pHeader.author[i] == 0)
                {
                    break;
                }
            }
            info.infostring[1] = sidtune.infostring[1] = new string(pHeader.author, 0, Math.Min(i, _sidtune_psid_maxStrLen));

            // Released
            for (i = 0; i < pHeader.released.Length; i++)
            {
                if (pHeader.released[i] == 0)
                {
                    break;
                }
            }
            info.infostring[2] = sidtune.infostring[2] = new string(pHeader.released, 0, Math.Min(i, _sidtune_psid_maxStrLen));

            return SidTune.LoadStatus.LOAD_OK;
        }

        /*
        internal bool PSID_fileSupportSave(BinaryWriter fMyOut, short[] dataBuffer)
        {
            try
            {
                PHeader myHeader = new PHeader();
                SIDEndian.endian_big32((short[])myHeader.id, 0, PSID_ID);
                SIDEndian.endian_big16(myHeader.version, 0, 2);
                SIDEndian.endian_big16(myHeader.data, 0, PHeader.SIZE);
                SIDEndian.endian_big16(myHeader.songs, 0, info.songs);
                SIDEndian.endian_big16(myHeader.start, 0, info.startSong);

                short speed = 0, check = 0;
                int maxBugSongs = ((info.songs <= 32) ? info.songs : 32);
                for (int s = 0; s < maxBugSongs; s++)
                {
                    if (sidtune.songSpeed[s] == SidTune.SIDTUNE_SPEED_CIA_1A)
                    {
                        speed |= (short)(1 << s);
                    }
                    check |= (short)(1 << s);
                }
                SIDEndian.endian_big32(myHeader.speed, 0, speed);

                int tmpFlags = 0;
                if (info.musPlayer)
                {
                    SIDEndian.endian_big16(myHeader.load, 0, 0);
                    SIDEndian.endian_big16(myHeader.init, 0, 0);
                    SIDEndian.endian_big16(myHeader.play, 0, 0);
                    myHeader.relocStartPage = 0;
                    myHeader.relocPages = 0;
                    tmpFlags |= PSID_MUS;
                }
                else
                {
                    SIDEndian.endian_big16(myHeader.load, 0, 0);
                    SIDEndian.endian_big16(myHeader.init, 0, info.initAddr);
                    myHeader.relocStartPage = info.relocStartPage;
                    myHeader.relocPages = info.relocPages;

                    switch (info.compatibility)
                    {
                        case SidTune.SIDTUNE_COMPATIBILITY_BASIC:
                            tmpFlags |= PSID_BASIC;
                            // fall-through?
                            SIDEndian.endian_big32((short[])myHeader.id, 0, RSID_ID);
                            SIDEndian.endian_big16(myHeader.play, 0, 0);
                            SIDEndian.endian_big32(myHeader.speed, 0, 0);
                            break;
                        case SidTune.SIDTUNE_COMPATIBILITY_R64:
                            SIDEndian.endian_big32((short[])myHeader.id, 0, RSID_ID);
                            SIDEndian.endian_big16(myHeader.play, 0, 0);
                            SIDEndian.endian_big32(myHeader.speed, 0, 0);
                            break;
                        case SidTune.SIDTUNE_COMPATIBILITY_PSID:
                            tmpFlags |= PSID_SPECIFIC;
                            // fall-through?
                            SIDEndian.endian_big16(myHeader.play, 0, info.playAddr);
                            break;
                        default:
                            SIDEndian.endian_big16(myHeader.play, 0, info.playAddr);
                            break;
                    }
                }

                for (int i = 0; i < 32; i++)
                {
                    myHeader.name[i] = (char)0;
                    myHeader.author[i] = (char)0;
                    myHeader.released[i] = (char)0;
                }

                // @FIXME@ Need better solution. Make it possible to override MUS strings
                if (info.numberOfInfostrings == 3)
                {
                    Array.Copy(info.infostring[0].ToCharArray(), 0, myHeader.name, 0, Math.Min(info.infostring[0].Length, _sidtune_psid_maxStrLen));
                    Array.Copy(info.infostring[1].ToCharArray(), 0, myHeader.author, 0, Math.Min(info.infostring[1].Length, _sidtune_psid_maxStrLen));
                    Array.Copy(info.infostring[2].ToCharArray(), 0, myHeader.released, 0, Math.Min(info.infostring[2].Length, _sidtune_psid_maxStrLen));
                }

                tmpFlags |= (info.clockSpeed << 2);
                tmpFlags |= (info.sidModel << 4);
                SIDEndian.endian_big16(myHeader.flags, 0, tmpFlags);
                SIDEndian.endian_big16(myHeader.reserved, 0, 0);

                write(fMyOut, myHeader.getArray(), 0, PHeader.SIZE);

                if (info.musPlayer)
                {
                    write(fMyOut, dataBuffer, 0, info.dataFileLen);
                }
                else
                {
                    // Save C64 lo/hi load address (little-endian).
                    short[] saveAddr = new short[2];
                    saveAddr[0] = (short)(info.loadAddr & 255);
                    saveAddr[1] = (short)(info.loadAddr >> 8);
                    write(fMyOut, saveAddr, 0, 2);

                    // Data starts at: bufferaddr + fileoffset
                    // Data length: datafilelen - fileoffset
                    write(fMyOut, dataBuffer, sidtune.fileOffset, info.dataFileLen - sidtune.fileOffset);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void write(BinaryWriter myOut, short[] dataBuffer, int offset, int length)
        {
            for (int j = offset; j < length; j++)
            {
                myOut.Write(dataBuffer[j]);
            }
        }
        */
    }
}