using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// Effectively there is only 1 channel, which can either perform Galway Noise or
    /// Sampling. However, to achieve all the effects on a C64, 2 sampling channels
    /// are required. No divide by 2 is required and is compensated for automatically
    /// in the C64 machine code.
    /// 
    /// Confirmed by Warren Pilkington using the tune Turbo Outrun: A new sample must
    /// interrupt an existing sample running on the same channel.
    /// 
    /// Confirmed by Michael Schwendt and Antonia Vera using the tune Game Over: A
    /// Galway Sample or Noise sequence cannot interrupt any other. However the last
    /// of these new requested sequences will be played after the current sequence
    /// ends.
    /// 
    /// Lastly playing samples through the SIDs volume is not as clean as playing
    /// them on their own channel. Playing through the SID will effect the volume of
    /// the other channels and this will be most noticable at low frequencies. These
    /// effects are however present in the original SID music.
    /// 
    /// Some SIDs put values directly into the volume register. Others play samples
    /// with respect to the current volume. We can't for definate know which the
    /// author has chosen originally. We must just make a guess based on what the
    /// volume is initially at the start of a sample sequence and from the details
    /// xSID has been programmed with.
    /// 
    /// @author Ken Händel
    /// </summary>
    public class XSID : SIDEmu
    {
        // Convert from 4 bit resolution to 8 bits
        private static short[] sampleConvertTable = { 0x80, 0x94, 0xa9, 0xbc, 0xce, 0xe1, 0xf2, 0x03, 0x1b, 0x2a, 0x3b, 0x49, 0x58, 0x66, 0x73, 0x7f };

        private InternalPlayer m_player;

        private SIDEmu m_sid;

        private long m_gain;

        internal Channel ch4;

        internal Channel ch5;

        private bool muted;

        private bool suppressed;

        private short sidData0x18;

        private bool _sidSamples;

        private short sampleOffset;

        internal bool wasRunning;

        private EventList events;

        /// <summary>
        /// Resolve multiple inheritance. XSID event
        /// </summary>
        internal xSIDEvent xsidEvent;
        internal int xsidEvent_id;


        internal void setSidData0x18()
        {
            if (!_sidSamples || muted)
            {
                return;
            }

            short data = (short)(sidData0x18 & 0xf0);
            data |= (short)((sampleOffset + sampleOutput()) & 0x0f);

            writeMemByte(data);
        }

        internal void recallSidData0x18()
        {
            // Rev 2.0.5 (saw) - Changed to recall volume differently depending on mode
            // Normally after samples volume should be restored to half volume,
            // however, Galway Tunes sound horrible and seem to require setting back
            // to the original volume. Setting back to the original volume for
            // normal samples can have nasty pulsing effects
            if (ch4.isGalway)
            {
                if (_sidSamples && !muted)
                {
                    writeMemByte(sidData0x18);
                }
            }
            else
            {
                setSidData0x18();
            }
        }

        private byte sampleOutput()
        {
            byte sample;
            sample = ch4.output();
            sample += ch5.output();
            // Automatically compensated for by C64 code
            // return (sample >> 1);
            return sample;
        }

        internal void sampleOffsetCalc()
        {
            // Try to determine a sensible offset between voice and sample volumes.
            short lower = (short)(ch4.limit() + ch5.limit());
            short upper;

            // Both channels seem to be off. Keep current offset!
            if (lower == 0)
            {
                return;
            }

            sampleOffset = (short)(sidData0x18 & 0x0f);

            // Is possible to compensate for both channels
            // set to 4 bits here, but should never happen.
            if (lower > 8)
            {
                lower >>= 1;
            }
            upper = (short)(0x0f - lower + 1);

            // Check against limits
            if (sampleOffset < lower)
            {
                sampleOffset = lower;
            }
            else if (sampleOffset > upper)
            {
                sampleOffset = upper;
            }
        }

        internal short readMemByte(int addr)
        {
            short data = m_player.readMemRamByte(addr);
            m_player.sid2crc(data);
            return data;
        }

        protected void writeMemByte(short data)
        {
            m_sid.write((short)0x18, data);
        }

        public XSID(InternalPlayer player, SIDEmu sid)
            : base()
        {
            xsidEvent = new xSIDEvent(this);
            ch4 = new Channel("CH4", player.m_scheduler, this);
            ch5 = new Channel("CH5", player.m_scheduler, this);
            muted = (false);
            suppressed = (false);
            wasRunning = (false);
            sidSamples(true);

            m_player = player;
            m_sid = sid;
            m_gain = 100;
        }
        // only used for deserializing
        public XSID(InternalPlayer player, BinaryReader reader, EventList events)
            : base()
        {
            this.events = events;

            this.m_player = player;

            LoadFromReader(player.m_scheduler, reader);

            if (xsidEvent_id == -1)
            {
                xsidEvent = null;
            }
            else
            {
                xsidEvent = events.GetEventById(xsidEvent_id) as xSIDEvent;

#if DEBUG
                if (xsidEvent == null)
                {
                    throw new Exception("XSID: xSIDEvent not found");
                }
#endif
            }
        }

        // Standard Calls

        public override void reset()
        {
            base.reset();
        }

        public override void reset(short volume)
        {
            ch4.reset();
            ch5.reset();
            suppressed = false;
            wasRunning = false;

            m_sid.reset(volume);
        }

        public override short read(short addr)
        {
            return m_sid.read(addr);
        }

        public override void write(short addr, short data)
        {
            if (addr == 0x18)
            {
                storeSidData0x18(data);
            }
            else
            {
                m_sid.write(addr, data);
            }
        }

        public void write16(int addr, short data)
        {
            write(addr, data);
        }

        // Specialist Calls

        public short read(int addr)
        {
            return 0;
        }

        public void write(int addr, short data)
        {
            Channel ch;
            short tempAddr;

            // Make sure address is legal
            if (((addr & 0xfe8c) ^ 0x000c) != 0)
            {
                return;
            }

            ch = ch4;
            if ((addr & 0x0100) != 0)
            {
                ch = ch5;
            }

            tempAddr = (short)addr;
            ch.write(tempAddr, data);

            if (tempAddr == 0x1d)
            {
                if (suppressed)
                {
                    return;
                }
                ch.checkForInit();
            }
        }

        public override long output(short bits)
        {
            long op;

            if (_sidSamples || muted)
            {
                op = 0;
            }
            else
            {
                long sample = sampleConvertTable[sampleOutput() + 8];
                op = sample << (bits - 8);
            }

            return m_sid.output(bits) + (op * m_gain / 100);
        }

        public override void voice(short num, short vol, bool doMute)
        {
            if (num == 3)
            {
                mute(doMute);
            }
            else
            {
                m_sid.voice(num, vol, doMute);
            }
        }

        public override void gain(short percent)
        {
            // 0 to 99 is loss, 101 - 200 is gain
            m_gain = percent;
            m_gain += 100;
            if (m_gain > 200)
            {
                m_gain = 200;
            }
        }

        /// <summary>
        /// By muting samples they will start and play the at the appropriate time
        /// but no sound is produced. Un-muting will cause sound output from the
        /// current play position
        /// </summary>
        /// <param name="enable"></param>
        public void mute(bool enable)
        {
            if (!muted && enable && wasRunning)
            {
                recallSidData0x18();
            }
            muted = enable;
        }

        public bool isMuted
        {
            get
            {
                return muted;
            }
        }

        public void emulation(SIDEmu sid)
        {
            m_sid = sid;
        }

        public SIDEmu emulation()
        {
            return m_sid;
        }

        /// <summary>
        /// Use Suppress to delay the samples and start them later. Effectivly allows
        /// running samples in a frame based mode
        /// </summary>
        /// <param name="enable"></param>
        public void suppress(bool enable)
        {
            // @FIXME@: Mute Temporary Hack
            suppressed = enable;
            if (!suppressed)
            {
                // Get the channels running

                ch4.checkForInit();
                ch5.checkForInit();
            }
            else
            {
            }
        }

        public void sidSamples(bool enable)
        {
            _sidSamples = enable;
        }

        /// <summary>
        /// Return whether we care it was changed
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool storeSidData0x18(short data)
        {
            sidData0x18 = data;
            if (ch4.isOk || ch5.isOk)
            {
                // Force volume to be changed at next clock
                sampleOffsetCalc();
                if (_sidSamples)
                {
                    return true;
                }
            }
            writeMemByte(sidData0x18);
            return false;
        }

        public override SIDEmu.SIDEmuType GetEmuType()
        {
            return SIDEmuType.xsid;
        }
        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            EventList.SaveEvent2Writer(xsidEvent, writer);

            writer.Write(Channel.id);
            ch4.SaveToWriter(writer);
            ch5.SaveToWriter(writer);
            writer.Write(muted);
            writer.Write(suppressed);
            writer.Write(sidData0x18);
            writer.Write(_sidSamples);
            writer.Write(sampleOffset);
            writer.Write(wasRunning);

            writer.Write(m_gain);
            m_sid.SaveToWriter(writer);
        }
        // deserializing
        protected override void LoadFromReader(EventScheduler context, BinaryReader reader)
        {
            xsidEvent_id = reader.ReadInt32();

            Channel.id = reader.ReadInt32();
            ch4 = new Channel(context, this, reader, events);
            ch5 = new Channel(context, this, reader, events);
            muted = reader.ReadBoolean();
            suppressed = reader.ReadBoolean();
            sidData0x18 = reader.ReadInt16();
            _sidSamples = reader.ReadBoolean();
            sampleOffset = reader.ReadInt16();
            wasRunning = reader.ReadBoolean();

            m_gain = reader.ReadInt64();
            m_sid = new ReSID(context, reader);
        }
    }
}