using System;

namespace SharpSid
{
    public class SmartPtr_sidtt : SmartPtrBase_sidtt
    {
        public SmartPtr_sidtt(short[] buffer, int bufferLen, bool bufOwner)
            : base(buffer, bufferLen, bufOwner)
        {
        }

        public SmartPtr_sidtt()
            : base(null, 0, false)
        {
        }

        public SmartPtr_sidtt(short[] buffer, int fileOffset, int bufferLen)
            : base(buffer, bufferLen - fileOffset, false)
        {
            pBufCurrent = fileOffset;
        }

        public void setBuffer(short[] buffer, int bufferLen)
        {
            if (bufferLen >= 1)
            {
                pBufCurrent = 0;
                bufEnd = bufferLen;
                bufLen = bufferLen;
                status = true;
            }
            else
            {
                pBufCurrent = bufEnd = 0;
                bufLen = 0;
                status = false;
            }
        }
    }
}