using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// @author Ken Händel
    /// 
    /// Private Event Context object (The scheduler)
    /// </summary>
    public class EventScheduler : Event
    {
        public const int EVENT_TIMEWARP_COUNT = 0x0FFFFF;

        private long m_absClk;

        private long m_events;

        internal EventTimeWarp m_timeWarp;
        internal int m_timeWarp_id;

        internal InternalPlayer m_player;

        /// <summary>
        /// Used to prevent overflowing by timewarping the event clocks
        /// </summary>
        public override void _event()
        {
            Event e = m_next;

            m_absClk += m_clk;
            while (e.m_pending)
            {
                e.m_clk -= m_clk;
                e = e.m_next;
            }
            m_clk = 0;

            // Re-schedule the next timeWarp
            schedule(m_timeWarp, EVENT_TIMEWARP_COUNT, event_phase_t.EVENT_CLOCK_PHI1);
        }

        private void dispatch(Event e)
        {
            cancelPending(e);
            e._event();
        }

        private void cancelPending(Event _event)
        {
            _event.m_pending = false;
            _event.m_prev.m_next = _event.m_next;
            _event.m_next.m_prev = _event.m_prev;
            m_events--;
        }

        public EventScheduler(string name)
            : base(name)
        {
            m_events = 0;
            m_timeWarp = new EventTimeWarp(this);
            m_next = this;
            m_prev = this;
            reset();
        }
        // only used for deserializing
        public EventScheduler(InternalPlayer player, BinaryReader reader, int id)
            : base(string.Empty)
        {
            m_id = id;
            m_player = player;
            LoadFromReader(reader);
        }

        /// <summary>
        /// Cancel a pending event
        /// </summary>
        /// <param name="_event"></param>
        public void cancel(Event _event)
        {
            if (_event.m_pending)
            {
                cancelPending(_event);
            }
        }

        public void reset()
        {
            // Remove all events
            Event e = m_next;

            m_pending = false;
            while (e.m_pending)
            {
                e.m_pending = false;
                e = e.m_next;
            }
            m_next = this;
            m_prev = this;
            m_clk = m_absClk = 0;
            m_events = 0;
            _event();
        }

        /// <summary>
        /// Add event to ordered pending queue
        /// </summary>
        /// <param name="_event"></param>
        /// <param name="cycles"></param>
        /// <param name="phase"></param>
        public void schedule(Event _event, long cycles, event_phase_t phase)
        {
            if (!_event.m_pending)
            {
                long clk = m_clk + (cycles << 1);
                clk += (((m_absClk + clk) & 1) ^ (phase == event_phase_t.EVENT_CLOCK_PHI1 ? 0 : 1));

                // Now put in the correct place so we don't need to keep searching the list later.
                Event e = m_next;
                long count = m_events;
                while ((count-- != 0) && (e.m_clk <= clk))
                {
                    e = e.m_next;
                }

                _event.m_next = e;
                _event.m_prev = e.m_prev;
                e.m_prev.m_next = _event;
                e.m_prev = _event;
                _event.m_pending = true;
                _event.m_clk = clk;
                m_events++;
            }
            else
            {
                cancelPending(_event);
                schedule(_event, cycles, phase);
            }
        }

        public void clock()
        {
            m_clk = m_next.m_clk;
            dispatch(m_next);
        }

        /// <summary>
        /// Get time with respect to a specific clock phase
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        public long getTime(event_phase_t phase)
        {
            return (m_absClk + m_clk + (((phase == event_phase_t.EVENT_CLOCK_PHI1) ? 0 : 1) ^ 1)) >> 1;
        }

        public long getTime(long clock, event_phase_t phase)
        {
            return ((getTime(phase) - clock) << 1) >> 1; // 31 bit res.
        }

        public event_phase_t phase
        {
            get
            {
                return ((m_absClk + m_clk) & 1) == 0 ? event_phase_t.EVENT_CLOCK_PHI1 : event_phase_t.EVENT_CLOCK_PHI2;
            }
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);

            writer.Write(Event.id);

            writer.Write(m_absClk);
            writer.Write(m_events);

            EventList.SaveEvent2Writer(m_timeWarp, writer);
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);

            Event.id = reader.ReadInt32();

            m_absClk = reader.ReadInt64();
            m_events = reader.ReadInt64();

            m_timeWarp_id = reader.ReadInt32();
        }

        internal override Event.EventType GetEventType()
        {
            return EventType.schedEvt;
        }
    }
}