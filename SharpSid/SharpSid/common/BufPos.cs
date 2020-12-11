using System;

namespace SharpSid
{
    public class BufPos
    {
        public BufPos(short[] buf, int pos, int size)
        {
            this.fBuf = buf;
            this.fPos = pos;
            this.fSize = size;
        }

        internal short[] fBuf;

        internal int fPos;

        internal int fSize;
    }
}