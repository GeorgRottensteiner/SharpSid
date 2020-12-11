using System;
using System.IO;

namespace SharpSid
{
    public abstract class SIDEmu
    {
        public enum SIDEmuType
        {
            nullsid,
            xsid,
            resid
        }

        public SIDEmu()
        {
        }

        // Standard component functions

        public virtual void reset()
        {
            reset(0);
        }

        public abstract void reset(short volume);

        public abstract short read(short addr);

        public abstract void write(short addr, short data);

        // Standard SID functions

        public abstract long output(short bits);

        public abstract void voice(short num, short vol, bool mute);

        public abstract void gain(short precent);

        public virtual void optimisation(byte level)
        {
        }

        public abstract SIDEmuType GetEmuType();

        // serializing
        public abstract void SaveToWriter(BinaryWriter writer);
        // deserializing
        protected abstract void LoadFromReader(EventScheduler context, BinaryReader reader);
    }
}