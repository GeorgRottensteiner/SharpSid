using System;
using System.IO;

namespace SharpSid
{
    public class GalwayEvent : Event
    {
        internal Channel m_ch;

        public int chId;

        public override void _event()
        {
            m_ch.galwayClock();
        }

        public GalwayEvent(Channel ch)
            : base("xSID Galway")
        {
            m_ch = ch;
        }
        // only used for deserializing
        public GalwayEvent(EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);
            writer.Write(m_ch.m_id);
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);
            chId = reader.ReadInt32();
        }

        internal override EventType GetEventType()
        {
            return EventType.GalwayEvt;
        }
    }
}