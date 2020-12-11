using System;
using System.IO;

namespace SharpSid
{
    public class Channel
    {
        private const int FM_NONE = 0;
        private const int FM_HUELS = 1;
        private const int FM_GALWAY = 2;

        public const int SO_LOWHIGH = 0;
        public const int SO_HIGHLOW = 1;


        // general

        private EventScheduler m_context;

        private XSID m_xsid;


        private string m_name;


        internal SampleEvent sampleEvent;
        internal int sampleEvent_id;

        internal GalwayEvent galwayEvent;
        internal int galwayEvent_id;


        private event_phase_t m_phase;

        private short[] reg = new short[0x10];

        private int mode;

        private bool active;

        private int address;

        /// <summary>
        /// Counts to zero and triggers!
        /// </summary>
        private int cycleCount;

        private short volShift;

        private short sampleLimit;

        private byte sample;

        // Sample Section

        private short samRepeat;

        private short samScale;

        private short samOrder;

        private short samNibble;

        private int samEndAddr;

        private int samRepeatAddr;

        private int samPeriod;

        // Galway Section

        private short galTones;

        private short galInitLength;

        private short galLength;

        private short galVolume;

        private short galLoopWait;

        private short galNullWait;

        // For Debugging

        private long cycles;

        private long outputs;


        internal static int id = 0;

        public int m_id;


        internal Channel(string name, EventScheduler context, XSID xsid)
        {
            m_id = id++;
            m_name = name;
            m_context = context;
            m_phase = event_phase_t.EVENT_CLOCK_PHI1;
            m_xsid = xsid;
            sampleEvent = new SampleEvent(this);
            galwayEvent = new GalwayEvent(this);
            for (int i = 0; i < reg.Length; i++)
            {
                reg[i] = 0;
            }
            active = true;
            reset();
        }
        // only used for deserializing
        internal Channel(EventScheduler context, XSID xsid, BinaryReader reader, EventList events)
        {
            m_context = context;
            m_xsid = xsid;
            LoadFromReader(reader);

            sampleEvent = events.GetEventById(sampleEvent_id) as SampleEvent;
            galwayEvent = events.GetEventById(galwayEvent_id) as GalwayEvent;

#if DEBUG
            if (sampleEvent_id > -1 && sampleEvent == null)
            {
                throw new Exception("Channel: sampleEvent not found: " + sampleEvent_id.ToString());
            }
            if (galwayEvent_id > -1 && galwayEvent == null)
            {
                throw new Exception("Channel: galwayEvent not found: " + galwayEvent_id.ToString());
            }
#endif

            sampleEvent.m_ch = this;
            galwayEvent.m_ch = this;
        }

        private void free()
        {
            active = false;
            cycleCount = 0;
            sampleLimit = 0;
            // Set XSID to stopped state
            reg[convertAddr(0x1d)] = 0;
            silence();
        }

        private void silence()
        {
            sample = 0;
            m_context.cancel(sampleEvent);
            m_context.cancel(galwayEvent);
            m_context.schedule(m_xsid.xsidEvent, 0, m_phase);
        }

        private void sampleInit()
        {
            if (active && (mode == FM_GALWAY))
            {
                return;
            }

            // Check all important parameters are legal
            short r = convertAddr(0x1d);
            volShift = (short)((0 - reg[r]) >> 1);
            reg[r] = 0;

            r = convertAddr(0x1e);
            address = SIDEndian.endian_16(reg[r + 1], reg[r]);
            r = convertAddr(0x3d);
            samEndAddr = SIDEndian.endian_16(reg[r + 1], reg[r]);
            if (samEndAddr <= address)
            {
                return;
            }
            samScale = reg[convertAddr(0x5f)];
            r = convertAddr(0x5d);
            samPeriod = SIDEndian.endian_16(reg[r + 1], reg[r]) >> samScale;
            if (samPeriod == 0)
            {
                // Stop this channel
                reg[convertAddr(0x1d)] = 0xfd;
                checkForInit();
                return;
            }

            // Load the other parameters
            samNibble = 0;
            samRepeat = reg[convertAddr(0x3f)];
            samOrder = reg[convertAddr(0x7d)];
            r = convertAddr(0x7e);
            samRepeatAddr = SIDEndian.endian_16(reg[r + 1], reg[r]);
            cycleCount = samPeriod;

            // Support Galway Samples, but that
            // mode is setup only when a Galway
            // Noise sequence begins
            if (mode == FM_NONE)
            {
                mode = FM_HUELS;
            }

            active = true;
            cycles = 0;
            outputs = 0;

            sampleLimit = (short)(8 >> volShift);
            sample = sampleCalculate();

            // Calculate the sample offset
            m_xsid.sampleOffsetCalc();

            // Schedule a sample update
            m_context.schedule(m_xsid.xsidEvent, 0, m_phase);
            m_context.schedule(sampleEvent, cycleCount, m_phase);
        }

        internal void sampleClock()
        {
            cycleCount = samPeriod;
            if (address >= samEndAddr)
            {
                if (samRepeat != 0xFF)
                {
                    if (samRepeat != 0)
                    {
                        samRepeat--;
                    }
                    else
                    {
                        samRepeatAddr = address;
                    }
                }

                address = samRepeatAddr;
                if (address >= samEndAddr)
                {
                    // The sequence has completed
                    short r = convertAddr(0x1d);
                    short status = reg[r];
                    if (status == 0)
                    {
                        reg[r] = 0xfd;
                    }
                    if (status != 0xfd)
                    {
                        active = false;
                    }

                    checkForInit();
                    return;
                }
            }

            // We have reached the required sample
            // So now we need to extract the right nibble
            sample = sampleCalculate();
            cycles += cycleCount;
            // Schedule a sample update
            m_context.schedule(sampleEvent, cycleCount, m_phase);
            m_context.schedule(m_xsid.xsidEvent, 0, m_phase);
        }

        private void galwayInit()
        {
            if (active)
            {
                return;
            }

            // Check all important parameters are legal
            short r = convertAddr(0x1d);
            galTones = reg[r];
            reg[r] = 0;
            galInitLength = reg[convertAddr(0x3d)];
            if (galInitLength == 0)
            {
                return;
            }
            galLoopWait = reg[convertAddr(0x3f)];
            if (galLoopWait == 0)
            {
                return;
            }
            galNullWait = reg[convertAddr(0x5d)];
            if (galNullWait == 0)
            {
                return;
            }

            // Load the other parameters
            r = convertAddr(0x1e);
            address = SIDEndian.endian_16(reg[r + 1], reg[r]);
            volShift = (short)(reg[convertAddr(0x3e)] & 0x0f);
            mode = FM_GALWAY;
            active = true;
            cycles = 0;
            outputs = 0;

            sampleLimit = 8;
            sample = (byte)(galVolume - 8);
            galwayTonePeriod();

            // Calculate the sample offset
            m_xsid.sampleOffsetCalc();

            // Schedule a sample update
            m_context.schedule(m_xsid.xsidEvent, 0, m_phase);
            m_context.schedule(galwayEvent, cycleCount, m_phase);
        }

        internal void galwayClock()
        {
            if (--galLength != 0)
            {
                cycleCount = samPeriod;
            }
            else if (galTones == 0xff)
            {
                // The sequence has completed
                int r = convertAddr(0x1d);
                short status = reg[r];
                if (status == 0)
                {
                    reg[r] = 0xfd;
                }
                if (status != 0xfd)
                {
                    active = false;
                }

                checkForInit();
                return;
            }
            else
            {
                galwayTonePeriod();
            }

            // See Galway Example...
            galVolume += volShift;
            galVolume &= 0x0f;
            sample = (byte)(galVolume - 8);
            cycles += cycleCount;
            m_context.schedule(galwayEvent, cycleCount, m_phase);
            m_context.schedule(m_xsid.xsidEvent, 0, m_phase);
        }

        /// <summary>
        /// Compress address to not leave so many spaces
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private short convertAddr(int addr)
        {
            return (short)(((addr) & 0x3) | ((addr) >> 3) & 0x0c);
        }

        internal void reset()
        {
            galVolume = 0; // This is left to free run until reset
            mode = FM_NONE;
            free();
            // Remove outstanding events
            m_context.cancel(m_xsid.xsidEvent);
            m_context.cancel(sampleEvent);
            m_context.cancel(galwayEvent);
        }

        /// <summary>
        /// Unused method. Modifier set from private to public!
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public short read(short addr)
        {
            return reg[convertAddr(addr)];
        }

        internal void write(short addr, short data)
        {
            reg[convertAddr(addr)] = data;
        }

        internal byte output()
        {
            outputs++;
            return sample;
        }

        internal bool isGalway
        {
            get
            {
                return mode == FM_GALWAY;
            }
        }

        internal short limit()
        {
            return sampleLimit;
        }

        internal void checkForInit()
        {
            // Check to see mode of operation
            // See xsid documentation
            switch (reg[convertAddr(0x1d)])
            {
                case 0xFF:
                case 0xFE:
                case 0xFC:
                    sampleInit();
                    break;
                case 0xFD:
                    if (!active)
                        return;
                    free(); // Stop
                    // Calculate the sample offset
                    m_xsid.sampleOffsetCalc();
                    break;
                case 0x00:
                    break;
                default:
                    galwayInit();
                    break;
            }
        }

        private byte sampleCalculate()
        {
            short tempSample = m_xsid.readMemByte(address);
            if (samOrder == SO_LOWHIGH)
            {
                if (samScale == 0)
                {
                    if (samNibble != 0)
                    {
                        tempSample >>= 4;
                    }
                }
                // AND 15 further below.
            }
            else // if (samOrder == SO_HIGHLOW)
            {
                if (samScale == 0)
                {
                    if (samNibble == 0)
                    {
                        tempSample >>= 4;
                    }
                }
                else
                {
                    // if (samScale != 0)
                    tempSample >>= 4;
                }
                // AND 15 further below.
            }

            // Move to next address
            address += samNibble;
            samNibble ^= 1;
            return (byte)(((tempSample & 0x0f) - 0x08) >> volShift);
        }

        private void galwayTonePeriod()
        {
            // Calculate the number of cycles over which sample should last
            galLength = galInitLength;
            samPeriod = m_xsid.readMemByte(address + galTones);
            samPeriod *= galLoopWait;
            samPeriod += galNullWait;
            cycleCount = samPeriod;

            galTones--;
        }

        /// <summary>
        /// Used to indicate if channel is running
        /// </summary>
        /// <returns></returns>
        internal bool isOk
        {
            get
            {
                return active;
            }
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            writer.Write(m_id);
            writer.Write(m_name);

            EventList.SaveEvent2Writer(sampleEvent, writer);
            EventList.SaveEvent2Writer(galwayEvent, writer);

            writer.Write((short)m_phase);
            for (int i = 0; i < 0x10; i++)
            {
                writer.Write(reg[i]);
            }
            writer.Write(mode);
            writer.Write(active);
            writer.Write(address);
            writer.Write(cycleCount);
            writer.Write(volShift);
            writer.Write(sampleLimit);
            writer.Write(sample);
            writer.Write(samRepeat);
            writer.Write(samScale);
            writer.Write(samOrder);
            writer.Write(samNibble);
            writer.Write(samEndAddr);
            writer.Write(samRepeatAddr);
            writer.Write(samPeriod);
            writer.Write(galTones);
            writer.Write(galInitLength);
            writer.Write(galLength);
            writer.Write(galVolume);
            writer.Write(galLoopWait);
            writer.Write(galNullWait);
            writer.Write(cycles);
            writer.Write(outputs);
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            m_id = reader.ReadInt32();
            m_name = reader.ReadString();

            sampleEvent_id = reader.ReadInt32();
            galwayEvent_id = reader.ReadInt32();

            m_phase = (event_phase_t)reader.ReadInt16();
            for (int i = 0; i < 0x10; i++)
            {
                reg[i] = reader.ReadInt16();
            }
            mode = reader.ReadInt32();
            active = reader.ReadBoolean();
            address = reader.ReadInt32();
            cycleCount = reader.ReadInt32();
            volShift = reader.ReadInt16();
            sampleLimit = reader.ReadInt16();
            sample = reader.ReadByte();
            samRepeat = reader.ReadInt16();
            samScale = reader.ReadInt16();
            samOrder = reader.ReadInt16();
            samNibble = reader.ReadInt16();
            samEndAddr = reader.ReadInt32();
            samRepeatAddr = reader.ReadInt32();
            samPeriod = reader.ReadInt32();
            galTones = reader.ReadInt16();
            galInitLength = reader.ReadInt16();
            galLength = reader.ReadInt16();
            galVolume = reader.ReadInt16();
            galLoopWait = reader.ReadInt16();
            galNullWait = reader.ReadInt16();
            cycles = reader.ReadInt64();
            outputs = reader.ReadInt64();
        }
    }
}