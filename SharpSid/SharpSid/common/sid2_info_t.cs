using System;
using System.IO;

namespace SharpSid
{
    public class sid2_info_t
    {
        public string[] credits;

        public int channels;

        public int driverAddr;

        public int driverLength;

        /// <summary>
        /// load, config and stop calls will reset this and remove all pending events! 10th sec resolution.
        /// </summary>
        public EventScheduler eventContext;

        public int maxsids;

        public SID2Types.sid2_env_t environment;

        public int powerOnDelay;

        public long sid2crc;

        /// <summary>
        /// Number of sid writes forming crc
        /// </summary>
        public long sid2crcCount;


        public sid2_info_t()
        {
        }
        // only used for deserializing
        public sid2_info_t(BinaryReader reader)
        {
            LoadFromReader(reader);
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            if (credits == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(credits.Length);
                for (int i = 0; i < credits.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(credits[i]))
                    {
                        writer.Write(credits[i]);
                    }
                    else
                    {
                        writer.Write(string.Empty);
                    }
                }
            }

            writer.Write(channels);
            writer.Write(driverAddr);
            writer.Write(driverLength);
            writer.Write(maxsids);
            writer.Write((short)environment);
            writer.Write(powerOnDelay);
            writer.Write(sid2crc);
            writer.Write(sid2crcCount);
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count == -1)
            {
                credits = null;
            }
            else
            {
                credits = new string[count];
                for (int i = 0; i < credits.Length; i++)
                {
                    credits[i] = reader.ReadString();
                }
            }

            channels = reader.ReadInt32();
            driverAddr = reader.ReadInt32();
            driverLength = reader.ReadInt32();
            maxsids = reader.ReadInt32();
            environment = (SID2Types.sid2_env_t)reader.ReadInt16();
            powerOnDelay = reader.ReadInt32();
            sid2crc = reader.ReadInt64();
            sid2crcCount = reader.ReadInt64();
        }
    }
}