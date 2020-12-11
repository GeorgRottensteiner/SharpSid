using System;
using System.IO;

namespace SharpSid
{
    public class EventRTC : Event
    {
        private EventScheduler m_eventContext;

        private long m_seconds;

        private long m_period;

        public bool saved = false;

        public override void _event()
        {
            long cycles;
            m_clk += m_period;
            cycles = m_clk >> 7;
            m_clk &= 0x7F;
            m_seconds++;
            m_eventContext.schedule(this, cycles, event_phase_t.EVENT_CLOCK_PHI1);
        }

        public EventRTC(EventScheduler context)
            : base("RTC")
        {
            m_eventContext = context;
            m_seconds = (0);
        }
        // only used for deserializing
        public EventRTC(EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
            m_eventContext = context;
        }

        public long getTime()
        {
            return m_seconds;
        }

        public void reset()
        {
            m_seconds = 0;
            m_clk = m_period & 0x7F;
            m_eventContext.schedule(this, m_period >> 7, event_phase_t.EVENT_CLOCK_PHI1);
        }

        public void clock(double period)
        {
            m_period = (long)(period / 10.0 * (double)(1 << 7));
            reset();
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);

            writer.Write(m_seconds);
            writer.Write(m_period);

            saved = true;
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);

            m_seconds = reader.ReadInt64();
            m_period = reader.ReadInt64();
        }

        internal override EventType GetEventType()
        {
            return EventType.RtcEvt;
        }
    }
}