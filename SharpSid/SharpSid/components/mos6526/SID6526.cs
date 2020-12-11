using System;
using System.IO;

namespace SharpSid
{
    public class SID6526
    {
        private InternalPlayer m_player;

        private EventScheduler m_eventContext;

        private long m_accessClk;

        private event_phase_t m_phase;

        private short[] regs = new short[0x10];

        /// <summary>
        /// Timer A Control Register
        /// </summary>
        private short cra;

        private int ta_latch;

        /// <summary>
        /// Current count (reduces to zero)
        /// </summary>
        private int ta;

        private long rnd;

        private int m_count;

        /// <summary>
        /// Prevent code changing CIA
        /// </summary>
        private bool locked;

        internal CiaEvent m_taEvent;
        internal int m_taEvent_id;


        public SID6526(InternalPlayer player)
        {
            m_player = player;
            m_eventContext = m_player.m_scheduler;
            m_phase = event_phase_t.EVENT_CLOCK_PHI1;
            rnd = 0;
            m_taEvent = new CiaEvent(this);
            clock(0xffff);
            reset(false);
        }
        // only used for deserializing
        public SID6526(InternalPlayer player, BinaryReader reader, EventList events)
        {
            m_player = player;
            m_eventContext = m_player.m_scheduler;

            LoadFromReader(reader);

            m_taEvent = events.GetEventById(m_taEvent_id) as CiaEvent;
            m_taEvent.m_cia = this;

#if DEBUG
            if (m_taEvent == null)
            {
                throw new Exception("SID6526: CiaEvent not found");
            }
#endif
        }

        // Common

        public void reset()
        {
            reset(false);
        }

        public void reset(bool seed)
        {
            locked = false;
            ta = ta_latch = m_count;
            cra = 0;
            // Initialise random number generator
            if (seed)
            {
                rnd = 0;
            }
            else
            {
#if DEBUG
                rnd = 1;
#else
                rnd += DateTime.Now.Millisecond & 0xff;
#endif
            }
            m_accessClk = 0;
            // Remove outstanding events
            m_eventContext.cancel(m_taEvent);
        }

        public short read(short addr)
        {
            if (addr > 0x0f)
            {
                return 0;
            }

            switch (addr)
            {
                case 0x04:
                case 0x05:
                case 0x11:
                case 0x12:
                    rnd = rnd * 13 + 1;
                    return (short)(rnd >> 3);
                default:
                    return regs[addr];
            }
        }

        public void write(short addr, short data)
        {
            if (addr > 0x0f)
            {
                return;
            }

            regs[addr] = data;

            if (locked)
            {
                return; // Stop program changing time interval
            }

            // Sync up timer
            long cycles;
            cycles = m_eventContext.getTime(m_accessClk, m_phase);
            m_accessClk += cycles;
            ta -= (int)cycles;
            if (ta == 0)
            {
                _event();
            }

            switch (addr)
            {
                case 0x4:
                    ta_latch = SIDEndian.endian_16lo8(ta_latch, data);
                    break;
                case 0x5:
                    ta_latch = SIDEndian.endian_16hi8(ta_latch, data);
                    if ((cra & 0x01) == 0) // Reload timer if stopped
                    {
                        ta = ta_latch;
                    }
                    break;
                case 0x0e:
                    cra = (short)(data | 0x01);
                    if ((data & 0x10) != 0)
                    {
                        cra &= (~0x10 & 0xff);
                        ta = ta_latch;
                    }
                    m_eventContext.schedule(m_taEvent, (long)ta + 1, m_phase);
                    break;
                default:
                    break;
            }
        }

        // Specific

        public void _event()
        {
            // Timer Modes
            m_accessClk = m_eventContext.getTime(m_phase);
            ta = ta_latch;
            m_eventContext.schedule(m_taEvent, (long)ta + 1, m_phase);
            m_player.interruptIRQ(true);
        }

        public void clock(int count)
        {
            m_count = count;
        }

        public void _lock()
        {
            locked = true;
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            EventList.SaveEvent2Writer(m_taEvent, writer);

            writer.Write(m_accessClk);
            writer.Write((short)m_phase);
            for (int i = 0; i < 0x10; i++)
            {
                writer.Write(regs[i]);
            }
            writer.Write(cra);
            writer.Write(ta_latch);
            writer.Write(ta);
            writer.Write(rnd);
            writer.Write(m_count);
            writer.Write(locked);
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            m_taEvent_id = reader.ReadInt32();

            m_accessClk = reader.ReadInt64();
            m_phase = (event_phase_t)reader.ReadInt16();
            for (int i = 0; i < 0x10; i++)
            {
                regs[i] = reader.ReadInt16();
            }
            cra = reader.ReadInt16();
            ta_latch = reader.ReadInt32();
            ta = reader.ReadInt32();
            rnd = reader.ReadInt64();
            m_count = reader.ReadInt32();
            locked = reader.ReadBoolean();
        }
    }
}