using System;
using System.IO;

namespace SharpSid
{
    public class NullSID : SIDEmu
    {
        public NullSID()
            : base()
        {
        }

        // Standard component functions

        public override void reset()
        {
            base.reset();
        }

        public override void reset(short volume)
        {
        }

        public override short read(short addr)
        {
            return 0;
        }

        public override void write(short addr, short data)
        {
        }

        /*
        public string error()
        {
            return string.Empty;
        }
        */

        // Standard SID functions

        public override long output(short volume)
        {
            return 0;
        }

        public override void voice(short num, short vol, bool mute)
        {
        }

        public override void gain(short percent)
        {
        }

        public override SIDEmu.SIDEmuType GetEmuType()
        {
            return SIDEmuType.nullsid;
        }

        public override void SaveToWriter(BinaryWriter writer)
        {
        }
        protected override void LoadFromReader(EventScheduler context, BinaryReader reader)
        {
        }

    }
}