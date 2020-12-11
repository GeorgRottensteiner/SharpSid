using System;
using System.IO;

namespace SharpSid
{
    public class EventTa : Event
    {
        internal MOS6526 m_cia;

        internal int ciaId;

        public override void _event()
        {
            m_cia.ta_event();
        }

        public EventTa(MOS6526 cia)
            : base("CIA Timer A")
        {
            m_cia = cia;
        }
        // only used for deserializing
        public EventTa(EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);
            writer.Write(m_cia.m_id);
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);
            ciaId = reader.ReadInt32();
        }

        internal override EventType GetEventType()
        {
            return EventType.TaEvt;
        }
    }
}