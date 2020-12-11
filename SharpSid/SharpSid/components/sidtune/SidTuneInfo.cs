using System;
using System.IO;
using System.Text;

namespace SharpSid
{
    /// <summary>
    /// An instance of this structure is used to transport values to and from SidTune
    /// objects. You must read (i.e. activate) sub-song specific information via:
    /// 
    /// SidTuneInfo tuneInfo = SidTune[songNumber];
    /// SidTuneInfo tuneInfo = SidTune.getInfo();
    /// void SidTune.getInfo(tuneInfo);
    /// 
    /// Consider the following fields as read-only, because the SidTune class does
    /// not provide an implementation of:
    /// 
    /// bool setInfo(SidTuneInfo)
    /// 
    /// Currently, the only way to get the class to accept values which are written
    /// to these fields is by creating a derived class.
    /// 
    /// @author Ken Händel
    /// </summary>
    public class SidTuneInfo
    {
        /// <summary>
        /// the name of the identified file format
        /// </summary>
        //public string formatstring;

        /// <summary>
        /// error/status message of last operation
        /// </summary>
        //public string statusstring;

        /// <summary>
        /// describing the speed a song is running at
        /// </summary>
        public string speedstring;

        public int loadAddr;

        public int initAddr;

        public int playAddr;

        public int songs;

        public int startSong;

        /// <summary>
        /// The SID chip base address used by the sidtune.
        /// 
        /// 0xD400 (normal, 1st SID)
        /// </summary>
        public int sidChipBase1;

        /// <summary>
        /// The SID chip base address used by the sidtune.
        /// 
        /// 0xD?00 (2nd SID) or 0 (no 2nd SID)
        /// </summary>
        public int sidChipBase2;

        //
        // Available after song initialization
        //

        /// <summary>
        /// the one that has been initialized
        /// </summary>
        public int currentSong { get; set; }

        public int songCount
        {
            get
            {
                return songs;
            }
        }

        /// <summary>
        /// intended speed, see top
        /// </summary>
        public short songSpeed;

        /// <summary>
        /// intended speed, see top
        /// </summary>
        public short clockSpeed;

        /// <summary>
        /// First available page for relocation
        /// </summary>
        public short relocStartPage;

        /// <summary>
        /// Number of pages available for relocation
        /// </summary>
        public short relocPages;

        /// <summary>
        /// whether Sidplayer routine has been installed
        /// </summary>
        public bool musPlayer;

        /// <summary>
        /// Sid Model required for this sid
        /// </summary>
        public int sidModel;

        /// <summary>
        /// compatibility requirements
        /// </summary>
        public int compatibility;

        /// <summary>
        /// whether load address might be duplicate
        /// </summary>
        internal bool fixLoad;

        /// <summary>
        /// Song title, credits, ... 0 = Title, 1 = Author, 2 = Copyright/Publisher
        /// 
        /// the number of available text info lines
        /// </summary>
        public short numberOfInfostrings;

        /// <summary>
        /// holds text info from the format headers etc
        /// </summary>
        public string[] infostring = new string[SidTune.SIDTUNE_MAX_CREDIT_stringS];

        public string InfoString1
        {
            get
            {
                if (infostring != null && infostring.Length > 0)
                {
                    return infostring[0];
                }
                return string.Empty;
            }
        }
        public string InfoString2
        {
            get
            {
                if (infostring != null && infostring.Length > 1)
                {
                    return infostring[1];
                }
                return string.Empty;
            }
        }
        public string InfoString3
        {
            get
            {
                if (infostring != null && infostring.Length > 2)
                {
                    return infostring[2];
                }
                return string.Empty;
            }
        }
        public string SongEmulation
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                switch (sidModel)
                {
                    case SidTune.SIDTUNE_SIDMODEL_6581:
                        sb.Append("6581");
                        break;
                    case SidTune.SIDTUNE_SIDMODEL_8580:
                        sb.Append("8580");
                        break;
                    default:
                        sb.Append("unknown");
                        break;
                }
                sb.Append(" / ");
                sb.Append(speedstring);

                return sb.ToString();
            }
        }

        /// <summary>
        /// length of single-file sidtune file
        /// </summary>
        public int dataFileLen;

        /// <summary>
        /// length of raw C64 data without load address
        /// </summary>
        public int c64dataLen;

        /// <summary>
        /// path to sidtune files
        /// </summary>
        internal string path;

        /// <summary>
        /// a first file: e.g. "foo.c64"; "", if none
        /// </summary>
        public string dataFileName;

        /// <summary>
        /// a second file: e.g. "foo.sid"; "", if none
        /// </summary>
        public string infoFileName;


        public SidTuneInfo()
        {
        }
        public SidTuneInfo(BinaryReader reader)
        {
            LoadFromReader(reader);
        }

        public void SaveToWriter(BinaryWriter writer)
        {
            //writer.Write(SID2Types.StringNotNull(formatstring));
            //writer.Write(SID2Types.StringNotNull(statusstring));
            writer.Write(SID2Types.StringNotNull(speedstring));
            writer.Write(loadAddr);
            writer.Write(initAddr);
            writer.Write(playAddr);
            writer.Write(songs);
            writer.Write(startSong);
            writer.Write(sidChipBase1);
            writer.Write(sidChipBase2);
            writer.Write(currentSong);
            writer.Write(songSpeed);
            writer.Write(clockSpeed);
            writer.Write(relocStartPage);
            writer.Write(relocPages);
            writer.Write(musPlayer);
            writer.Write(sidModel);
            writer.Write(compatibility);
            writer.Write(fixLoad);
            writer.Write(numberOfInfostrings);

            writer.Write(infostring.Length);
            for (int i = 0; i < infostring.Length; i++)
            {
                writer.Write(SID2Types.StringNotNull(infostring[i]));
            }

            writer.Write(dataFileLen);
            writer.Write(c64dataLen);
            writer.Write(SID2Types.StringNotNull(path));
            writer.Write(SID2Types.StringNotNull(dataFileName));
            writer.Write(SID2Types.StringNotNull(infoFileName));
        }
        protected void LoadFromReader(BinaryReader reader)
        {
            //formatstring = reader.ReadString();
            //statusstring = reader.ReadString();
            speedstring = reader.ReadString();
            loadAddr = reader.ReadInt32();
            initAddr = reader.ReadInt32();
            playAddr = reader.ReadInt32();
            songs = reader.ReadInt32();
            startSong = reader.ReadInt32();
            sidChipBase1 = reader.ReadInt32();
            sidChipBase2 = reader.ReadInt32();
            currentSong = reader.ReadInt32();
            songSpeed = reader.ReadInt16();
            clockSpeed = reader.ReadInt16();
            relocStartPage = reader.ReadInt16();
            relocPages = reader.ReadInt16();
            musPlayer = reader.ReadBoolean();
            sidModel = reader.ReadInt32();
            compatibility = reader.ReadInt32();
            fixLoad = reader.ReadBoolean();
            numberOfInfostrings = reader.ReadInt16();

            int count = reader.ReadInt32();
            infostring = new string[count];
            for (int i = 0; i < infostring.Length; i++)
            {
                infostring[i] = reader.ReadString();
            }

            dataFileLen = reader.ReadInt32();
            c64dataLen = reader.ReadInt32();
            path = reader.ReadString();
            dataFileName = reader.ReadString();
            infoFileName = reader.ReadString();
        }
    }
}