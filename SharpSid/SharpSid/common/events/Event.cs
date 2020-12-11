using System;
using System.IO;

namespace SharpSid
{
    public abstract class Event
    {
        public enum EventType
        {
            none = 0,
            ciaEvt = 1,
            cpuEvt = 2,
            mixerEvt = 3,
            RtcEvt = 4,
            TaEvt = 5,
            TbEvt = 6,
            TimeWarpEvt = 7,
            TodEvt = 8,
            GalwayEvt = 9,
            SampleEvt = 10,
            xSidEvt = 11,
            vicEvt = 12,
            schedEvt = 13
        }

        public static int id = 0;

        public const int EVENT_CONTEXT_MAX_PENDING_EVENTS = 0x100;

        public int m_id;

        public string m_name;

        public long m_clk;

        internal EventScheduler m_context;

        /// <summary>
        /// This variable is set by the event context when it is scheduled
        /// </summary>
        public bool m_pending;

        /// <summary>
        /// Link to the next and previous events in the list.
        /// </summary>
        public Event m_next, m_prev;
        public int m_next_id, m_prev_id;

        public Event(string name)
        {
            m_name = name;
            m_pending = false;
            m_id = id++;
        }

        // only used for deserializing
        public Event(EventScheduler context, BinaryReader reader, int newId)
        {
            m_context = context;

            m_id = newId;

            LoadFromReader(reader);
        }

        public abstract void _event();

        public bool pending()
        {
            return m_pending;
        }

        internal abstract EventType GetEventType();

        // serializing
        public virtual void SaveToWriter(BinaryWriter writer)
        {
            writer.Write(m_id);
            writer.Write((short)GetEventType());
            writer.Write(m_name);
            writer.Write(m_clk);
            writer.Write(m_pending);

            if (m_prev == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(m_prev.m_id);
            }

            if (m_next == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(m_next.m_id);
            }
        }
        // deserializing
        protected virtual void LoadFromReader(BinaryReader reader)
        {
            m_name = reader.ReadString();
            m_clk = reader.ReadInt64();
            m_pending = reader.ReadBoolean();

            m_prev_id = reader.ReadInt32();
            m_next_id = reader.ReadInt32();
        }
    }
}