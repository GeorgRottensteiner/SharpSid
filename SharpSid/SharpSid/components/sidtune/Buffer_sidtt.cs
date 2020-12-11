using System;

namespace SharpSid
{
    public class Buffer_sidtt
    {
        internal short[] buf;

        internal int bufLen;

        private short dummy;


        public Buffer_sidtt()
        {
            dummy = 0;
            kill();
        }

        public Buffer_sidtt(short[] inBuf, int inLen)
        {
            dummy = 0;
            kill();
            if (inBuf != null && inLen != 0)
            {
                buf = inBuf;
                bufLen = inLen;
            }
        }


        public bool assign(short[] newBuf, int newLen)
        {
            erase();
            buf = newBuf;
            bufLen = newLen;
            return (buf != null);
        }

        public short[] xferPtr()
        {
            short[] tmpBuf = buf;
            buf = null;
            return tmpBuf;
        }

        public int xferLen()
        {
            int tmpBufLen = bufLen;
            bufLen = 0;
            return tmpBufLen;
        }

        public short opAt(int index)
        {
            if (index < bufLen)
                return buf[index];
            else
                return dummy;
        }

        public bool isEmpty()
        {
            return (buf == null);
        }

        public void erase()
        {
            if (buf != null && bufLen != 0)
            {
                buf = null;
            }
            kill();
        }

        private void kill()
        {
            buf = null;
            bufLen = 0;
        }
    }
}