using System;
using System.IO;

namespace SharpSid
{
    public abstract class MOS6526
    {
        public const int INTERRUPT_TA = 1 << 0;
        public const int INTERRUPT_TB = 1 << 1;
        public const int INTERRUPT_ALARM = 1 << 2;
        public const int INTERRUPT_SP = 1 << 3;
        public const int INTERRUPT_FLAG = 1 << 4;
        public const int INTERRUPT_REQUEST = 1 << 7;

        public const int PRA = 0;
        public const int PRB = 1;
        public const int DDRA = 2;
        public const int DDRB = 3;
        public const int TAL = 4;
        public const int TAH = 5;
        public const int TBL = 6;
        public const int TBH = 7;
        public const int TOD_TEN = 8;
        public const int TOD_SEC = 9;
        public const int TOD_MIN = 10;
        public const int TOD_HR = 11;
        public const int SDR = 12;
        public const int ICR = 13;
        public const int IDR = 13;
        public const int CRA = 14;
        public const int CRB = 15;


        protected EventScheduler event_context;


        // Optional information

        protected short[] regs = new short[0x10];

        protected bool cnt_high;

        // Timer A

        protected short cra, cra_latch, dpa;

        protected int ta, ta_latch;

        protected bool ta_underflow;

        // Timer B

        protected short crb;

        protected int tb, tb_latch;

        protected bool tb_underflow;

        // Serial Data Registers

        protected short sdr_out;

        protected bool sdr_buffered;

        protected int sdr_count;

        protected short icr, idr; // Interrupt Control Register

        protected long m_accessClk;

        protected event_phase_t m_phase;

        protected bool m_todlatched;

        protected bool m_todstopped;

        protected short[] m_todclock = new short[4];
        protected short[] m_todalarm = new short[4];
        protected short[] m_todlatch = new short[4];

        long m_todCycles, m_todPeriod;

        internal EventTa event_ta;
        internal int event_ta_id;

        internal EventTb event_tb;
        internal int event_tb_id;

        internal EventTod event_tod;
        internal int event_tod_id;

        private static int id = 0;

        public int m_id;


        protected MOS6526(EventScheduler context)
        {
            m_id = id++;
            idr = 0;
            event_context = context;
            m_phase = event_phase_t.EVENT_CLOCK_PHI1;
            m_todPeriod = ~0 & 0xffffffffL;
            event_ta = new EventTa(this);
            event_tb = new EventTb(this);
            event_tod = new EventTod(this);
            reset();
        }
        // only used for deserializing
        protected MOS6526(EventScheduler context, BinaryReader reader, EventList events)
        {
            event_context = context;

            LoadFromReader(reader);

            event_ta = events.GetEventById(event_ta_id) as EventTa;
            event_tb = events.GetEventById(event_tb_id) as EventTb;
            event_tod = events.GetEventById(event_tod_id) as EventTod;

#if DEBUG
            if (event_ta == null)
            {
                throw new Exception("MOS6526: EventTa not found");
            }
            if (event_tb == null)
            {
                throw new Exception("MOS6526: EventTb not found");
            }
            if (event_tod == null)
            {
                throw new Exception("MOS6526: EventTod not found");
            }
#endif

            event_ta.m_cia = this;
            event_tb.m_cia = this;
            event_tod.m_cia = this;
        }

        internal void ta_event()
        {
            // Timer Modes
            long cycles;
            short mode = (short)(cra & 0x21);

            if (mode == 0x21)
            {
                if ((ta--) != 0)
                {
                    return;
                }
            }

            cycles = event_context.getTime(m_accessClk, m_phase);
            m_accessClk += cycles;

            ta = ta_latch;
            ta_underflow ^= true; // toggle flipflop
            if ((cra & 0x08) != 0)
            {
                // one shot, stop timer A
                cra &= (~0x01 & 0xff);
            }
            else if (mode == 0x01)
            {
                // Reset event
                event_context.schedule(event_ta, (long)ta + 1, m_phase);
            }
            trigger(INTERRUPT_TA);

            // Handle serial port
            if ((cra & 0x40) != 0)
            {
                if (sdr_count != 0)
                {
                    if ((--sdr_count) == 0)
                    {
                        trigger(INTERRUPT_SP);
                    }
                }
                if ((sdr_count == 0) && sdr_buffered)
                {
                    sdr_out = regs[SDR];
                    sdr_buffered = false;
                    sdr_count = 16; // Output rate 8 bits at ta / 2
                }
            }

            switch (crb & 0x61)
            {
                case 0x01:
                    tb -= (int)cycles;
                    break;
                case 0x41:
                case 0x61:
                    tb_event();
                    break;
            }
        }

        internal void tb_event()
        {
            // Timer Modes
            short mode = (short)(crb & 0x61);
            switch (mode)
            {
                case 0x01:
                    break;

                case 0x21:
                case 0x41:
                    if ((tb--) != 0)
                    {
                        return;
                    }
                    break;

                case 0x61:
                    if (cnt_high)
                    {
                        if ((tb--) != 0)
                        {
                            return;
                        }
                    }
                    break;

                default:
                    return;
            }

            m_accessClk = event_context.getTime(m_phase);
            tb = tb_latch;
            tb_underflow ^= true; // toggle flipflop
            if ((crb & 0x08) != 0)
            {
                // one shot, stop timer A
                crb &= (~0x01 & 0xff);
            }
            else if (mode == 0x01)
            {
                // Reset event
                event_context.schedule(event_tb, (long)tb + 1, m_phase);
            }
            trigger(INTERRUPT_TB);
        }

        // TOD implementation taken from Vice

        private static short byte2bcd(short thebyte)
        {
            return (short)((((thebyte / 10) << 4) + (thebyte % 10)) & 0xff);
        }

        private static short bcd2byte(short bcd)
        {
            return (short)(((10 * ((bcd & 0xf0) >> 4)) + (bcd & 0xf)) & 0xff);
        }

        internal void tod_event()
        {
            // Reload divider according to 50/60 Hz flag
            // Only performed on expiry according to Frodo
            if ((cra & 0x80) != 0)
            {
                m_todCycles += (m_todPeriod * 5);
            }
            else
            {
                m_todCycles += (m_todPeriod * 6);
            }

            // Fixed precision 25.7
            event_context.schedule(event_tod, m_todCycles >> 7, m_phase);
            m_todCycles &= 0x7F; // Just keep the decimal part

            if (!m_todstopped)
            {
                // inc timer
                short[] tod = m_todclock;
                int todPos = 0;
                short t = (short)(bcd2byte(tod[todPos]) + 1);
                tod[todPos++] = byte2bcd((short)(t % 10));
                if (t >= 10)
                {
                    t = (short)(bcd2byte(tod[todPos]) + 1);
                    tod[todPos++] = byte2bcd((short)(t % 60));
                    if (t >= 60)
                    {
                        t = (short)(bcd2byte(tod[todPos]) + 1);
                        tod[todPos++] = byte2bcd((short)(t % 60));
                        if (t >= 60)
                        {
                            short pm = (short)(tod[todPos] & 0x80);
                            t = (short)(tod[todPos] & 0x1f);
                            if (t == 0x11)
                            {
                                pm ^= 0x80; // toggle am/pm on 0:59->1:00 hr
                            }
                            if (t == 0x12)
                            {
                                t = 1;
                            }
                            else if (++t == 10)
                            {
                                t = 0x10; // increment, adjust bcd
                            }
                            t &= 0x1f;
                            tod[todPos] = (short)(t | pm);
                        }
                    }
                }
                // check alarm
                if (!memcmp(m_todalarm, m_todclock, m_todalarm.Length))
                {
                    trigger(INTERRUPT_ALARM);
                }
            }
        }

        protected void trigger(int irq)
        {
            if (irq == 0)
            {
                // Clear any requested IRQs
                if ((idr & INTERRUPT_REQUEST) != 0)
                {
                    interrupt(false);
                }
                idr = 0;
                return;
            }

            idr |= (short)irq;
            if ((icr & idr) != 0)
            {
                if ((idr & INTERRUPT_REQUEST) == 0)
                {
                    idr |= INTERRUPT_REQUEST;
                    interrupt(true);
                }
            }
        }

        // Environment Interface

        public abstract void interrupt(bool state);

        public abstract void portA();

        public abstract void portB();

        // Component Standard Calls

        public virtual void reset()
        {
            ta = ta_latch = 0xffff;
            tb = tb_latch = 0xffff;
            ta_underflow = tb_underflow = false;
            cra = crb = sdr_out = 0;
            sdr_count = 0;
            sdr_buffered = false;
            // Clear off any IRQs
            trigger(0);
            cnt_high = true;
            icr = idr = 0;
            m_accessClk = 0;
            dpa = 0xf0;
            for (int i = 0; i < regs.Length; i++)
            {
                regs[i] = 0;
            }

            // Reset tod
            for (int i = 0; i < m_todclock.Length; i++)
            {
                m_todclock[i] = 0;
            }
            for (int i = 0; i < m_todalarm.Length; i++)
            {
                m_todalarm[i] = 0;
            }
            for (int i = 0; i < m_todlatch.Length; i++)
            {
                m_todlatch[i] = 0;
            }

            m_todlatched = false;
            m_todstopped = true;
            m_todclock[TOD_HR - TOD_TEN] = 1; // the most common value
            m_todCycles = 0;

            // Remove outstanding events
            event_context.cancel(event_ta);
            event_context.cancel(event_tb);
            event_context.schedule(event_tod, 0, m_phase);
        }

        public short read(short addr)
        {
            long cycles;

            if (addr > 0x0f)
            {
                return 0;
            }

            bool ta_pulse = false, tb_pulse = false;

            cycles = event_context.getTime(m_accessClk, event_context.phase);
            m_accessClk += cycles;

            // Sync up timers
            if ((cra & 0x21) == 0x01)
            {
                ta -= (int)cycles;
                if (ta == 0)
                {
                    ta_event();
                    ta_pulse = true;
                }
            }
            if ((crb & 0x61) == 0x01)
            {
                tb -= (int)cycles;
                if (tb == 0)
                {
                    tb_event();
                    tb_pulse = true;
                }
            }

            switch (addr)
            {
                case PRA: // Simulate a serial port
                    return (short)(regs[PRA] | (short)(~regs[DDRA] & 0xff));
                case PRB:
                    {
                        short data = (short)(regs[PRB] | (short)(~regs[DDRB] & 0xff));
                        // Timers can appear on the port
                        if ((cra & 0x02) != 0)
                        {
                            data &= 0xbf;
                            if ((cra & 0x04) != 0 ? ta_underflow : ta_pulse)
                            {
                                data |= 0x40;
                            }
                        }
                        if ((crb & 0x02) != 0)
                        {
                            data &= 0x7f;
                            if ((crb & 0x04) != 0 ? tb_underflow : tb_pulse)
                            {
                                data |= 0x80;
                            }
                        }
                        return data;
                    }
                case TAL:
                    return SIDEndian.endian_16lo8(ta);
                case TAH:
                    return SIDEndian.endian_16hi8(ta);
                case TBL:
                    return SIDEndian.endian_16lo8(tb);
                case TBH:
                    return SIDEndian.endian_16hi8(tb);

                // TOD implementation taken from Vice
                // TOD clock is latched by reading Hours, and released
                // upon reading Tenths of Seconds. The counter itself
                // keeps ticking all the time.
                // Also note that this latching is different from the input one.
                case TOD_TEN: // Time Of Day clock 1/10 s
                case TOD_SEC: // Time Of Day clock sec
                case TOD_MIN: // Time Of Day clock min
                case TOD_HR:  // Time Of Day clock hour
                    if (!m_todlatched)
                    {
                        for (int i = 0; i < m_todlatch.Length; i++)
                        {
                            m_todlatch[i] = m_todclock[i];
                        }
                    }
                    if (addr == TOD_TEN)
                    {
                        m_todlatched = false;
                    }
                    if (addr == TOD_HR)
                    {
                        m_todlatched = true;
                    }
                    return m_todlatch[addr - TOD_TEN];

                case IDR:
                    {
                        // Clear IRQs, and return interrupt data register
                        short ret = idr;
                        trigger(0);
                        return ret;
                    }

                case CRA:
                    return cra;
                case CRB:
                    return crb;
                default:
                    return regs[addr];
            }
        }

        public void write(short addr, short data)
        {
            long cycles;

            if (addr > 0x0f)
            {
                return;
            }

            regs[addr] = data;
            cycles = event_context.getTime(m_accessClk, event_context.phase);

            if (cycles != 0)
            {
                m_accessClk += cycles;
                // Sync up timers
                if ((cra & 0x21) == 0x01)
                {
                    ta -= (int)cycles;
                    if (ta == 0)
                    {
                        ta_event();
                    }
                }
                if ((crb & 0x61) == 0x01)
                {
                    tb -= (int)cycles;
                    if (tb == 0)
                    {
                        tb_event();
                    }
                }
            }

            switch (addr)
            {
                case PRA:
                case DDRA:
                    portA();
                    break;
                case PRB:
                case DDRB:
                    portB();
                    break;
                case TAL:
                    ta_latch = SIDEndian.endian_16lo8(ta_latch, data);
                    break;
                case TAH:
                    ta_latch = SIDEndian.endian_16hi8(ta_latch, data);
                    if ((cra & 0x01) == 0) // Reload timer if stopped
                    {
                        ta = ta_latch;
                    }
                    break;

                case TBL:
                    tb_latch = SIDEndian.endian_16lo8(tb_latch, data);
                    break;
                case TBH:
                    tb_latch = SIDEndian.endian_16hi8(tb_latch, data);
                    if ((crb & 0x01) == 0) // Reload timer if stopped
                    {
                        tb = tb_latch;
                    }
                    break;

                // TOD implementation taken from Vice
                case TOD_HR: // Time Of Day clock hour
                    // Flip AM/PM on hour 12
                    // Flip AM/PM only when writing time, not when writing alarm
                    data &= 0x9f;
                    if ((data & 0x1f) == 0x12 && ((crb & 0x80) == 0))
                    {
                        data ^= 0x80;
                    }
                    // deliberate run on
                    if ((crb & 0x80) != 0)
                    {
                        m_todalarm[addr - TOD_TEN] = data;
                    }
                    else
                    {
                        if (addr == TOD_TEN)
                        {
                            m_todstopped = false;
                        }
                        if (addr == TOD_HR)
                        {
                            m_todstopped = true;
                        }
                        m_todclock[addr - TOD_TEN] = data;
                    }
                    // check alarm
                    if (!m_todstopped && !memcmp(m_todalarm, m_todclock, m_todalarm.Length))
                    {
                        trigger(INTERRUPT_ALARM);
                    }
                    break;
                case TOD_TEN: // Time Of Day clock 1/10 s
                case TOD_SEC: // Time Of Day clock sec
                case TOD_MIN: // Time Of Day clock min
                    if ((crb & 0x80) != 0)
                    {
                        m_todalarm[addr - TOD_TEN] = data;
                    }
                    else
                    {
                        if (addr == TOD_TEN)
                        {
                            m_todstopped = false;
                        }
                        if (addr == TOD_HR)
                        {
                            m_todstopped = true;
                        }
                        m_todclock[addr - TOD_TEN] = data;
                    }
                    // check alarm
                    if (!m_todstopped && !memcmp(m_todalarm, m_todclock, m_todalarm.Length))
                    {
                        trigger(INTERRUPT_ALARM);
                    }
                    break;

                case SDR:
                    if ((cra & 0x40) != 0)
                    {
                        sdr_buffered = true;
                    }
                    break;

                case ICR:
                    if ((data & 0x80) != 0)
                    {
                        icr |= (short)(data & 0x1f);
                    }
                    else
                    {
                        icr &= (short)(~data & 0xff);
                    }
                    trigger(idr);
                    break;

                case CRA:
                    // Reset the underflow flipflop for the data port
                    if (((data & 1) != 0) && ((cra & 1) == 0))
                    {
                        ta = ta_latch;
                        ta_underflow = true;
                    }
                    cra = data;

                    // Check for forced load
                    if ((data & 0x10) != 0)
                    {
                        cra &= (~0x10 & 0xff);
                        ta = ta_latch;
                    }

                    if ((data & 0x21) == 0x01)
                    {
                        // Active
                        event_context.schedule(event_ta, (long)ta + 1, m_phase);
                    }
                    else
                    {
                        // Inactive
                        event_context.cancel(event_ta);
                    }
                    break;

                case CRB:
                    // Reset the underflow flipflop for the data port
                    if (((data & 1) != 0) && ((crb & 1) == 0))
                    {
                        tb = tb_latch;
                        tb_underflow = true;
                    }
                    // Check for forced load
                    crb = data;
                    if ((data & 0x10) != 0)
                    {
                        crb &= (~0x10 & 0xff);
                        tb = tb_latch;
                    }

                    if ((data & 0x61) == 0x01)
                    {
                        // Active
                        event_context.schedule(event_tb, (long)tb + 1, m_phase);
                    }
                    else
                    {
                        // Inactive
                        event_context.cancel(event_tb);
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This is not correct! There should be muliple schedulers running
        /// at different rates that are passed into different function calls.
        /// This is the same as have different clock freqs connected to pins on the IC.
        /// </summary>
        /// <param name="clock"></param>
        public void clock(double clock)
        {
            m_todPeriod = (long)(clock * (double)(1 << 7));
        }

        private bool memcmp(short[] m_todalarm2, short[] m_todclock2, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (m_todalarm2[i] != m_todclock2[i])
                {
                    return true;
                }
            }
            return false;
        }

        // serializing
        public virtual void SaveToWriter(BinaryWriter writer)
        {
            EventList.SaveEvent2Writer(event_ta, writer);
            EventList.SaveEvent2Writer(event_tb, writer);
            EventList.SaveEvent2Writer(event_tod, writer);

            for (int i = 0; i < 0x10; i++)
            {
                writer.Write(regs[i]);
            }
            writer.Write(cnt_high);
            writer.Write(cra);
            writer.Write(cra_latch);
            writer.Write(dpa);
            writer.Write(ta);
            writer.Write(ta_latch);
            writer.Write(ta_underflow);
            writer.Write(crb);
            writer.Write(tb);
            writer.Write(tb_latch);
            writer.Write(tb_underflow);
            writer.Write(sdr_out);
            writer.Write(sdr_buffered);
            writer.Write(sdr_count);
            writer.Write(icr);
            writer.Write(idr);
            writer.Write(m_accessClk);
            writer.Write((short)m_phase);
            writer.Write(m_todlatched);
            writer.Write(m_todstopped);
            for (int i = 0; i < 4; i++)
            {
                writer.Write(m_todclock[i]);
                writer.Write(m_todalarm[i]);
                writer.Write(m_todlatch[i]);
            }
            writer.Write(m_todCycles);
            writer.Write(m_todPeriod);
        }
        // deserializing
        protected virtual void LoadFromReader(BinaryReader reader)
        {
            event_ta_id = reader.ReadInt32();
            event_tb_id = reader.ReadInt32();
            event_tod_id = reader.ReadInt32();

            for (int i = 0; i < 0x10; i++)
            {
                regs[i] = reader.ReadInt16();
            }
            cnt_high = reader.ReadBoolean();
            cra = reader.ReadInt16();
            cra_latch = reader.ReadInt16();
            dpa = reader.ReadInt16();
            ta = reader.ReadInt32();
            ta_latch = reader.ReadInt32();
            ta_underflow = reader.ReadBoolean();
            crb = reader.ReadInt16();
            tb = reader.ReadInt32();
            tb_latch = reader.ReadInt32();
            tb_underflow = reader.ReadBoolean();
            sdr_out = reader.ReadInt16();
            sdr_buffered = reader.ReadBoolean();
            sdr_count = reader.ReadInt32();
            icr = reader.ReadInt16();
            idr = reader.ReadInt16();
            m_accessClk = reader.ReadInt64();
            m_phase = (event_phase_t)reader.ReadInt16();
            m_todlatched = reader.ReadBoolean();
            m_todstopped = reader.ReadBoolean();
            for (int i = 0; i < 4; i++)
            {
                m_todclock[i] = reader.ReadInt16();
                m_todalarm[i] = reader.ReadInt16();
                m_todlatch[i] = reader.ReadInt16();
            }
            m_todCycles = reader.ReadInt64();
            m_todPeriod = reader.ReadInt64();
        }
    }
}