using System;
using System.IO;

namespace SharpSid
{
    public class EventMixer : Event
    {
        private InternalPlayer m_player;

        public override void _event()
        {
            m_player.mixer();
        }

        public EventMixer(InternalPlayer player)
            : base("Mixer")
        {
            m_player = player;
        }
        // only used for deserializing
        public EventMixer(InternalPlayer player, EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
            m_player = player;
        }

        internal override EventType GetEventType()
        {
            return EventType.mixerEvt;
        }
    }
}