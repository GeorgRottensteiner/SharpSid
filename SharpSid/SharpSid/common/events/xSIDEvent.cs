using System;
using System.IO;

namespace SharpSid
{
    public class xSIDEvent : Event
    {
        internal XSID owner;

        public xSIDEvent(XSID owner)
            : base("xSID")
        {
            this.owner = owner;
        }

        /// <summary>
        /// Resolve multiple inheritance
        /// </summary>
        public override void _event()
        {
            if (owner.ch4.isOk || owner.ch5.isOk)
            {
                owner.setSidData0x18();
                owner.wasRunning = true;
            }
            else if (owner.wasRunning)
            {
                owner.recallSidData0x18();
                owner.wasRunning = false;
            }
        }
        // only used for deserializing
        public xSIDEvent(EventScheduler context, BinaryReader reader, int newId)
            : base(context, reader, newId)
        {
        }

        internal override EventType GetEventType()
        {
            return EventType.xSidEvt;
        }
    }
}