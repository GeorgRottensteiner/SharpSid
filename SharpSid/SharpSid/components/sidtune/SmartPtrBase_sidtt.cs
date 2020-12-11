using System;

namespace SharpSid
{
    public class SmartPtrBase_sidtt
    {
        public SmartPtrBase_sidtt(short[] buffer, int bufferLen, bool bufOwner)
        {
            dummy = (0);
            doFree = bufOwner;
            if (bufferLen >= 1)
            {
                bufBegin = buffer;
                pBufCurrent = 0;
                bufEnd = bufferLen;
                bufLen = bufferLen;
                status = true;
            }
            else
            {
                bufBegin = null;
                pBufCurrent = (bufEnd = 0);
                bufLen = 0;
                status = false;
            }
        }

        public short[] tellBegin()
        {
            return bufBegin;
        }

        public int tellLength()
        {
            return bufLen;
        }

        public int tellPos()
        {
            return (int)(pBufCurrent);
        }

        public bool checkIndex(int index)
        {
            return ((pBufCurrent + index) < bufEnd);
        }

        public bool reset()
        {
            if (bufLen >= 1)
            {
                pBufCurrent = 0;
                return (status = true);
            }
            else
            {
                return (status = false);
            }
        }

        public bool isOk
        {
            get
            {
                return (pBufCurrent < bufEnd);
            }
        }

        public bool fail
        {
            get
            {
                return (pBufCurrent == bufEnd);
            }
        }

        public void operatorPlusPlus()
        {
            if (isOk)
            {
                pBufCurrent++;
            }
            else
            {
                status = false;
            }
        }

        public void operatorMinusMinus()
        {
            if (!fail)
            {
                pBufCurrent--;
            }
            else
            {
                status = false;
            }
        }

        public void operatorPlusGleich(int offset)
        {
            if (checkIndex(offset))
            {
                pBufCurrent += offset;
            }
            else
            {
                status = false;
            }
        }

        public void operatorMinusGleich(int offset)
        {
            if ((pBufCurrent - offset) >= 0)
            {
                pBufCurrent -= offset;
            }
            else
            {
                status = false;
            }
        }

        public short operatorMal()
        {
            if (isOk)
            {
                return bufBegin[pBufCurrent];
            }
            else
            {
                status = false;
                return dummy;
            }
        }

        public short operatorAt(int index)
        {
            if (checkIndex(index))
            {
                return bufBegin[pBufCurrent + index];
            }
            else
            {
                status = false;
                return dummy;
            }
        }

        public bool operatorBool()
        {
            return status;
        }

        protected short[] bufBegin;

        protected int bufEnd;

        protected int pBufCurrent;

        protected int bufLen;

        protected bool status;

        protected bool doFree;

        protected short dummy;
    }
}