using System;
using System.IO;

namespace SharpSid
{
    public class CiaEvent : Event
    {
        internal SID6526 m_cia;

        public override void _event()
        {
            m_cia._event();
        }

        public CiaEvent(SID6526 cia)
            : base("CIA Timer A")
        {
            m_cia = cia;
        }
        // only used for deserializing
        public CiaEvent(EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
        }

        internal override EventType GetEventType()
        {
            return EventType.ciaEvt;
        }
    }
}