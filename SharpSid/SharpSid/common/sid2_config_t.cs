using System;
using System.IO;

namespace SharpSid
{
    public class sid2_config_t
    {
        /// <summary>
        /// Intended tune speed when unknown
        /// </summary>
        public SID2Types.sid2_clock_t clockDefault;

        public bool clockForced;

        /// <summary>
        /// User requested emulation speed
        /// </summary>
        public SID2Types.sid2_clock_t clockSpeed;

        public SID2Types.sid2_env_t environment;

        public bool forceDualSids;

        public long frequency;

        public byte optimisation;

        public SID2Types.sid2_playback_t playback;

        public int precision;

        /// <summary>
        /// Intended sid model when unknown
        /// </summary>
        public SID2Types.sid2_model_t sidDefault;

        /// <summary>
        /// User requested sid model
        /// </summary>
        public SID2Types.sid2_model_t sidModel;

        public bool sidSamples;

        public long volume;

        public SID2Types.sid2_sample_t sampleFormat;

        public int powerOnDelay;

        /// <summary>
        /// Max sid writes to form crc
        /// </summary>
        public long sid2crcCount;


        public sid2_config_t()
        {
        }
        // only used for deserializing
        public sid2_config_t(BinaryReader reader)
        {
            LoadFromReader(reader);
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            writer.Write((short)clockDefault);
            writer.Write(clockForced);
            writer.Write((short)clockSpeed);
            writer.Write((short)environment);
            writer.Write(forceDualSids);
            writer.Write(frequency);
            writer.Write(optimisation);
            writer.Write((short)playback);
            writer.Write(precision);
            writer.Write((short)sidDefault);
            writer.Write((short)sidModel);
            writer.Write(sidSamples);
            writer.Write(volume);
            writer.Write((short)sampleFormat);
            writer.Write(powerOnDelay);
            writer.Write(sid2crcCount);
        }
        // deserializing
        public void LoadFromReader(BinaryReader reader)
        {
            clockDefault = (SID2Types.sid2_clock_t)reader.ReadInt16();
            clockForced = reader.ReadBoolean();
            clockSpeed = (SID2Types.sid2_clock_t)reader.ReadInt16();
            environment = (SID2Types.sid2_env_t)reader.ReadInt16();
            forceDualSids = reader.ReadBoolean();
            frequency = reader.ReadInt64();
            optimisation = reader.ReadByte();
            playback = (SID2Types.sid2_playback_t)reader.ReadInt16();
            precision = reader.ReadInt32();
            sidDefault = (SID2Types.sid2_model_t)reader.ReadInt16();
            sidModel = (SID2Types.sid2_model_t)reader.ReadInt16();
            sidSamples = reader.ReadBoolean();
            volume = reader.ReadInt64();
            sampleFormat = (SID2Types.sid2_sample_t)reader.ReadInt16();
            powerOnDelay = reader.ReadInt32();
            sid2crcCount = reader.ReadInt64();
        }
    }
}