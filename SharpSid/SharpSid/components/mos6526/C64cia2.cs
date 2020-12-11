using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// CIA 2 specifics: Generates NMIs
    /// </summary>
    public class C64cia2 : MOS6526
    {
        private InternalPlayer m_player;

        public override void portA()
        {
        }

        public override void portB()
        {
        }

        public override void interrupt(bool state)
        {
            if (state)
            {
                m_player.interruptNMI();
            }
        }

        public C64cia2(InternalPlayer player)
            : base(player.m_scheduler)
        {
            m_player = player;
        }
        // only used for deserializing
        public C64cia2(InternalPlayer player, BinaryReader reader, EventList events)
            : base(player.m_scheduler, reader, events)
        {
            m_player = player;
        }
    }
}