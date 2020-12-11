using System;
using System.IO;

namespace SharpSid
{
    public class MOS6510
    {
        public const int MOS6510_INTERRUPT_DELAY = 2;

        // Status Register flag definitions

        public const int SR_NEGATIVE = 7;

        public const int SR_OVERFLOW = 6;

        public const int SR_NOTUSED = 5;

        public const int SR_BREAK = 4;

        public const int SR_DECIMAL = 3;

        public const int SR_INTERRUPT = 2;

        public const int SR_ZERO = 1;

        public const int SR_CARRY = 0;

        //

        public const short SP_PAGE = 0x01;

        // Interrupt Routines

        public const int iIRQSMAX = 3;

        public const int oNONE = 255;

        public const int oRST = 0;

        public const int oNMI = 1;

        public const int oIRQ = 2;

        public const int iNONE = 0;

        public const int iRST = 1 << oRST;

        public const int iNMI = 1 << oNMI;

        public const int iIRQ = 1 << oIRQ;


        #region CHR$ conversion table

        /// <summary>
        /// CHR$ conversion table (0x01 = no output)
        /// </summary>
        private static char[] _sidtune_CHRtab = {
            (char)0x0, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1,
            (char)0xd, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1,
            (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x20, (char)0x21, (char)0x1, (char)0x23, (char)0x24, (char)0x25,
            (char)0x26, (char)0x27, (char)0x28, (char)0x29, (char)0x2a, (char)0x2b, (char)0x2c, (char)0x2d, (char)0x2e, (char)0x2f, (char)0x30,
            (char)0x31, (char)0x32, (char)0x33, (char)0x34, (char)0x35, (char)0x36, (char)0x37, (char)0x38, (char)0x39, (char)0x3a, (char)0x3b,
            (char)0x3c, (char)0x3d, (char)0x3e, (char)0x3f, (char)0x40, (char)0x41, (char)0x42, (char)0x43, (char)0x44, (char)0x45, (char)0x46,
            (char)0x47, (char)0x48, (char)0x49, (char)0x4a, (char)0x4b, (char)0x4c, (char)0x4d, (char)0x4e, (char)0x4f, (char)0x50, (char)0x51,
            (char)0x52, (char)0x53, (char)0x54, (char)0x55, (char)0x56, (char)0x57, (char)0x58, (char)0x59, (char)0x5a, (char)0x5b, (char)0x24,
            (char)0x5d, (char)0x20, (char)0x20,
            // alternative: CHR$(92=0x5c) => ISO Latin-1(0xa3)
            (char)0x2d, (char)0x23, (char)0x7c, (char)0x2d, (char)0x2d, (char)0x2d, (char)0x2d, (char)0x7c, (char)0x7c, (char)0x5c, (char)0x5c,
            (char)0x2f, (char)0x5c, (char)0x5c, (char)0x2f, (char)0x2f, (char)0x5c, (char)0x23, (char)0x5f, (char)0x23, (char)0x7c, (char)0x2f,
            (char)0x58, (char)0x4f, (char)0x23, (char)0x7c, (char)0x23, (char)0x2b, (char)0x7c, (char)0x7c, (char)0x26, (char)0x5c,
            // 0x80-0xFF
            (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1,
            (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1,
            (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x1, (char)0x20, (char)0x7c, (char)0x23, (char)0x2d, (char)0x2d, (char)0x7c,
            (char)0x23, (char)0x7c, (char)0x23, (char)0x2f, (char)0x7c, (char)0x7c, (char)0x2f, (char)0x5c, (char)0x5c, (char)0x2d, (char)0x2f,
            (char)0x2d, (char)0x2d, (char)0x7c, (char)0x7c, (char)0x7c, (char)0x7c, (char)0x2d, (char)0x2d, (char)0x2d, (char)0x2f, (char)0x5c,
            (char)0x5c, (char)0x2f, (char)0x2f, (char)0x23, (char)0x2d, (char)0x23, (char)0x7c, (char)0x2d, (char)0x2d, (char)0x2d, (char)0x2d,
            (char)0x7c, (char)0x7c, (char)0x5c, (char)0x5c, (char)0x2f, (char)0x5c, (char)0x5c, (char)0x2f, (char)0x2f, (char)0x5c, (char)0x23,
            (char)0x5f, (char)0x23, (char)0x7c, (char)0x2f, (char)0x58, (char)0x4f, (char)0x23, (char)0x7c, (char)0x23, (char)0x2b, (char)0x7c,
            (char)0x7c, (char)0x26, (char)0x5c, (char)0x20, (char)0x7c, (char)0x23, (char)0x2d, (char)0x2d, (char)0x7c, (char)0x23, (char)0x7c,
            (char)0x23, (char)0x2f, (char)0x7c, (char)0x7c, (char)0x2f, (char)0x5c, (char)0x5c, (char)0x2d, (char)0x2f, (char)0x2d, (char)0x2d,
            (char)0x7c, (char)0x7c, (char)0x7c, (char)0x7c, (char)0x2d, (char)0x2d, (char)0x2d, (char)0x2f, (char)0x5c, (char)0x5c, (char)0x2f,
            (char)0x2f, (char)0x23 };

        #endregion

        private static char[] filetmp = new char[0x100];

        // External signals

        /// <summary>
        /// Address Controller, blocks reads
        /// </summary>
        protected bool aec;

        protected bool m_blocked;

        protected long m_stealingClk;

        protected long m_dbgClk;

        internal EventScheduler eventContext;

        internal InternalPlayer player;

        /// <summary>
        /// Clock phase in use by the processor
        /// </summary>
        internal event_phase_t m_phase;

        /// <summary>
        /// Clock phase when external events appear
        /// </summary>
        protected event_phase_t m_extPhase;

        protected ProcessorCycle fetchCycle = new ProcessorCycle();

        protected ProcessorCycle[] procCycle;
        protected int procCycle_id;

        protected ProcessorOperations[] instrTable = new ProcessorOperations[0x100];

        protected ProcessorOperations[] interruptTable = new ProcessorOperations[3];

        protected ProcessorOperations instrCurrent;
        protected int lastInstrCurrent;

        protected int instrStartPC;

        protected short instrOpcode;

        protected byte lastAddrCycle;

        protected sbyte cycleCount;

        // Pointers to the current instruction cycle

        protected int Cycle_EffectiveAddress;

        protected short Cycle_Data;

        protected int Cycle_Pointer;

        protected short Register_Accumulator;

        protected short Register_X;

        protected short Register_Y;

        protected long Register_ProgramCounter;

        protected short Register_Status;

        protected short Register_c_Flag;

        protected short Register_n_Flag;

        protected short Register_v_Flag;

        protected short Register_z_Flag;

        protected int Register_StackPointer;

        // Interrupts

        protected short interrupts_pending;

        protected short interrupts_irqs;

        protected long interrupts_nmiClk;

        protected long interrupts_irqClk;

        protected bool interrupts_irqRequest;

        protected bool interrupts_irqLatch;


        /// <summary>
        /// Resolve multiple inheritance
        /// </summary>
        internal CPUEvent cpuEvent;
        internal int cpuEvent_id;


        /// <summary>
        /// Emulate One Complete Cycle
        /// </summary>
        internal void clock()
        {
            sbyte i = cycleCount++;
            if (procCycle[i].nosteal || aec)
            {
                procCycle[i].func();
                return;
            }
            else if (!m_blocked)
            {
                m_blocked = true;
                m_stealingClk = eventContext.getTime(m_phase);
            }
            cycleCount--;
            eventContext.cancel(cpuEvent);
        }

        /// <summary>
        /// Initialize CPU Emulation (Registers)
        /// </summary>
        protected void Initialise()
        {
            // Reset stack
            Register_StackPointer = SIDEndian.endian_16(SP_PAGE, (short)0xFF);

            // Reset Cycle Count
            cycleCount = 0;
            procCycle = new ProcessorCycle[] { fetchCycle };

            // Reset Status Register
            Register_Status = (1 << SR_NOTUSED) | (1 << SR_BREAK);
            // FLAGS are set from data directly and do not require
            // being calculated first before setting. E.g. if you used
            // SetFlags (0), N flag would = 0, and Z flag would = 1.
            setFlagsNZ((short)1);
            setFlagC((short)0);
            setFlagV((short)0);

            // Set PC to some value
            Register_ProgramCounter = 0;
            // IRQs pending check
            interrupts_irqLatch = false;
            interrupts_irqRequest = false;
            if ((interrupts_irqs) != 0)
            {
                interrupts_irqRequest = true;
            }

            // Signals
            aec = true;

            m_blocked = false;
            eventContext.schedule(cpuEvent, 0, m_phase);
        }

        // Declare Interrupt Routines

        internal void RSTRequest()
        {
            player.Reset(true);
        }

        internal void NMIRequest()
        {
            Cycle_EffectiveAddress = SIDEndian.endian_16lo8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFA));
        }

        internal void NMI1Request()
        {
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFB));
            Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
        }

        internal void IRQRequest()
        {
            PushSR(false);
            setFlagI((short)1);
            interrupts_irqRequest = false;
        }

        internal void IRQ1Request()
        {
            Cycle_EffectiveAddress = SIDEndian.endian_16lo8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFE));
        }

        internal void IRQ2Request()
        {
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFF));
            Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
        }

        private byte[] offTable = { oNONE, oRST, oNMI, oRST, oIRQ, oRST, oNMI, oRST };
        protected bool interruptPending()
        {
            byte offset, pending;

            // Update IRQ pending
            if (!interrupts_irqLatch)
            {
                interrupts_pending &= ~iIRQ & 0xff;
                if (interrupts_irqRequest)
                {
                    interrupts_pending |= iIRQ;
                }
            }

            pending = (byte)interrupts_pending;

            // MOS6510_interruptPending_check: 
            while (true)
            {
                // Service the highest priority interrupt
                offset = offTable[pending];
                switch (offset)
                {
                    case oNONE:
                        return false;

                    case oNMI:
                        {
                            // Try to determine if we should be processing the NMI yet
                            long cycles = eventContext.getTime(interrupts_nmiClk, m_extPhase);
                            if (cycles >= MOS6510_INTERRUPT_DELAY)
                            {
                                interrupts_pending &= ~iNMI & 0xff;
                                break;
                            }

                            // NMI delayed so check for other interrupts
                            pending &= ~iNMI & 0xff;
                            continue; // MOS6510_interruptPending_check;
                        }

                    case oIRQ:
                        {
                            // Try to determine if we should be processing the IRQ yet
                            long cycles = eventContext.getTime(interrupts_irqClk, m_extPhase);
                            if (cycles >= MOS6510_INTERRUPT_DELAY)
                            {
                                break;
                            }

                            // NMI delayed so check for other interrupts
                            pending &= ~iIRQ & 0xff;
                            continue; // MOS6510_interruptPending_check;
                        }

                    case oRST:
                        break;
                }

                // END PSEUDO LOOP
                break; //MOS6510_interruptPending_check;
            }

            // Start the interrupt
            instrCurrent = interruptTable[offset];
            lastInstrCurrent = -(offset + 1);
            procCycle = instrCurrent.cycle;
            cycleCount = 0;
            clock();
            return true;
        }

        // Declare Instruction Routines

        //
        // Common Instruction Addressing Routines
        // Addressing operations as described in 64doc by John West and
        // Marko Makela
        //

        /// <summary>
        /// Fetch opcode, increment PC
        /// 
        /// Addressing Modes: All
        /// </summary>
        internal virtual void FetchOpcode()
        {
            // On new instruction all interrupt delays are reset
            interrupts_irqLatch = false;

            instrStartPC = SIDEndian.endian_32lo16(Register_ProgramCounter++);
            instrOpcode = player.mem_readMemByte(instrStartPC);

            // Convert opcode to pointer in instruction table
            instrCurrent = instrTable[instrOpcode];
            lastInstrCurrent = instrOpcode;
            procCycle = instrCurrent.cycle;
            cycleCount = 0;

#if DEBUG_CPU
            counter++;
            if (counter >= 0 && counter <= 1000)
            {
                Debug.WriteLine(counter.ToString() + " $" + (Register_ProgramCounter - 1).ToString("X4") + " " + Disassembler.Opcode2String(instrOpcode, (int)Register_ProgramCounter - 1, Register_X, Register_Y, Register_Accumulator, this)+" "+Cycle_Data.ToString("X2"));
            }
            else if (counter > 1000)
            {
                Debugger.Break();
            }
#endif
        }
#if DEBUG_CPU
        private static long counter = 0;
#endif

        internal void NextInstr()
        {
            if (!interruptPending())
            {
                cycleCount = 0;
                procCycle = new ProcessorCycle[] { fetchCycle };
                clock();
            }
        }

        /// <summary>
        /// Fetch value, increment PC
        /// 
        /// Addressing Modes:
        /// - Immediate
        /// - Relative
        /// </summary>
        internal void FetchDataByte()
        {
            // Get data byte from memory
            Cycle_Data = player.mem_readMemByte(SIDEndian.endian_32lo16(Register_ProgramCounter));
            Register_ProgramCounter++;
        }

        /// <summary>
        /// Fetch low address byte, increment PC
        /// 
        /// Addressing Modes:
        /// - Stack Manipulation
        /// - Absolute
        /// - Zero Page
        /// - Zero Page Indexed
        /// - Absolute Indexed
        /// - Absolute Indirect
        /// </summary>
        internal void FetchLowAddr()
        {
            Cycle_EffectiveAddress = player.mem_readMemByte(SIDEndian.endian_32lo16(Register_ProgramCounter));
            Register_ProgramCounter++;
        }

        /// <summary>
        /// Read from address, add index register X to it
        /// 
        /// Addressing Modes:
        /// - Zero Page Indexed
        /// </summary>
        internal void FetchLowAddrX()
        {
            FetchLowAddr();
            Cycle_EffectiveAddress = (Cycle_EffectiveAddress + Register_X) & 0xFF;
        }

        /// <summary>
        /// Read from address, add index register Y to it
        /// 
        /// Addressing Modes:
        /// - Zero Page Indexed
        /// </summary>
        internal void FetchLowAddrY()
        {
            FetchLowAddr();
            Cycle_EffectiveAddress = (Cycle_EffectiveAddress + Register_Y) & 0xFF;
        }

        /// <summary>
        /// Fetch high address byte, increment PC (Absolute Addressing)
        /// Low byte must have been obtained first!
        /// 
        /// Addressing Modes:
        /// - Absolute
        /// </summary>
        internal void FetchHighAddr()
        {
            // Get the high byte of an address from memory
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemByte(SIDEndian.endian_32lo16(Register_ProgramCounter)));
            Register_ProgramCounter++;
        }

        /// <summary>
        /// Fetch high byte of address, add index register X to low address byte, increment PC
        /// 
        /// Addressing Modes:
        /// - Absolute Indexed
        /// </summary>
        internal void FetchHighAddrX()
        {
            short page;
            // Rev 1.05 (saw) - Call base Function
            FetchHighAddr();
            page = SIDEndian.endian_16hi8(Cycle_EffectiveAddress);
            Cycle_EffectiveAddress += Register_X;

#if MOS6510_ACCURATE_CYCLES
            // Handle page boundary crossing
            if (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) == page)
            {
                cycleCount++;
            }
#endif
        }

        /// <summary>
        /// Same as above except dosen't worry about page crossing
        /// </summary>
        internal void FetchHighAddrX2()
        {
            FetchHighAddr();
            Cycle_EffectiveAddress += Register_X;
        }

        /// <summary>
        /// Fetch high byte of address, add index register Y to low address byte, increment PC
        /// 
        /// Addressing Modes:
        /// - Absolute Indexed
        /// </summary>
        internal void FetchHighAddrY()
        {
            short page;
            // Rev 1.05 (saw) - Call base Function
            FetchHighAddr();
            page = SIDEndian.endian_16hi8(Cycle_EffectiveAddress);
            Cycle_EffectiveAddress += Register_Y;

#if MOS6510_ACCURATE_CYCLES
            // Handle page boundary crossing
            if (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) == page)
            {
                cycleCount++;
            }
#endif
        }

        /// <summary>
        /// Same as above except doesn't worry about page crossing
        /// </summary>
        internal void FetchHighAddrY2()
        {
            FetchHighAddr();
            Cycle_EffectiveAddress += Register_Y;
        }

        /// <summary>
        /// Fetch effective address low
        /// 
        /// Addressing Modes:
        /// - Indirect
        /// - Indexed Indirect (pre X)
        /// - Indirect indexed (post Y)
        /// </summary>
        internal void FetchLowEffAddr()
        {
            Cycle_EffectiveAddress = player.mem_readMemDataByte(Cycle_Pointer);
        }

        /// <summary>
        /// Fetch effective address high
        /// 
        /// Addressing Modes:
        /// - Indirect
        /// - Indexed Indirect (pre X)
        /// </summary>
        internal void FetchHighEffAddr()
        {
            // Rev 1.03 (Mike) - Extra +1 removed
            Cycle_Pointer = SIDEndian.endian_16lo8(Cycle_Pointer, (short)((Cycle_Pointer + 1) & 0xff));
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemDataByte(Cycle_Pointer));
        }

        /// <summary>
        /// Fetch effective address high, add Y to low byte of effective address
        /// 
        /// Addressing Modes:
        /// - Indirect indexed (post Y)
        /// </summary>
        internal void FetchHighEffAddrY()
        {
            short page;
            // Rev 1.05 (saw) - Call base Function
            FetchHighEffAddr();
            page = SIDEndian.endian_16hi8(Cycle_EffectiveAddress);
            Cycle_EffectiveAddress += Register_Y;

#if MOS6510_ACCURATE_CYCLES
            // Handle page boundary crossing
            if (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) == page)
            {
                cycleCount++;
            }
#endif
        }

        /// <summary>
        /// Same as above except doesn't worry about page crossing
        /// </summary>
        internal void FetchHighEffAddrY2()
        {
            FetchHighEffAddr();
            Cycle_EffectiveAddress += Register_Y;
        }

        /// <summary>
        /// Fetch pointer address low, increment PC
        /// 
        /// Addressing Modes:
        /// - Absolute Indirect
        /// - Indirect indexed (post Y)
        /// </summary>
        internal void FetchLowPointer()
        {
            Cycle_Pointer = player.mem_readMemByte(SIDEndian.endian_32lo16(Register_ProgramCounter));
            Register_ProgramCounter++;
        }

        /// <summary>
        /// Read pointer from the address and add X to it
        /// 
        /// Addressing Modes:
        /// - Indexed Indirect (pre X)
        /// </summary>
        internal void FetchLowPointerX()
        {
            Cycle_Pointer = SIDEndian.endian_16hi8(Cycle_Pointer, player.mem_readMemDataByte(Cycle_Pointer));
            // Page boundary crossing is not handled
            Cycle_Pointer = (Cycle_Pointer + Register_X) & 0xFF;
        }

        /// <summary>
        /// Fetch pointer address high, increment PC
        /// 
        /// Addressing Modes:
        /// - Absolute Indirect
        /// </summary>
        internal void FetchHighPointer()
        {
            Cycle_Pointer = SIDEndian.endian_16hi8(Cycle_Pointer, player.mem_readMemByte(SIDEndian.endian_32lo16(Register_ProgramCounter)));
            Register_ProgramCounter++;
        }

        // Common Data Accessing Routines
        // Data Accessing operations as described in 64doc by John West and Marko Makela

        internal void FetchEffAddrDataByte()
        {
            Cycle_Data = player.mem_readMemDataByte(Cycle_EffectiveAddress);
        }

        internal void PutEffAddrDataByte()
        {
            player.mem_writeMemByte(Cycle_EffectiveAddress, Cycle_Data);
        }

        /// <summary>
        /// Push Program Counter Low Byte on stack, decrement S
        /// </summary>
        internal void PushLowPC()
        {
            int addr;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            player.mem_writeMemByte(addr, SIDEndian.endian_32lo8(Register_ProgramCounter));
            Register_StackPointer--;
        }

        /// <summary>
        /// Push Program Counter High Byte on stack, decrement S
        /// </summary>
        internal void PushHighPC()
        {
            int addr;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            player.mem_writeMemByte(addr, SIDEndian.endian_32hi8(Register_ProgramCounter));
            Register_StackPointer--;
        }

        /// <summary>
        /// Push P on stack, decrement S
        /// </summary>
        /// <param name="b_flag"></param>
        internal void PushSR(bool b_flag)
        {
            int addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            Register_Status &= ((1 << SR_NOTUSED) | (1 << SR_INTERRUPT) | (1 << SR_DECIMAL) | (1 << SR_BREAK));
            Register_Status |= (short)((getFlagN() ? 1 : 0) << SR_NEGATIVE);
            Register_Status |= (short)((getFlagV() ? 1 : 0) << SR_OVERFLOW);
            Register_Status |= (short)((getFlagZ() ? 1 : 0) << SR_ZERO);
            Register_Status |= (short)((getFlagC() ? 1 : 0) << SR_CARRY);
            player.mem_writeMemByte(addr, (short)(Register_Status & (~(((!b_flag) ? 1 : 0) << SR_BREAK) & 0xff)));
            Register_StackPointer--;
        }

        internal void PushSR()
        {
            PushSR(true);
        }

        /// <summary>
        /// Increment stack and pull program counter low byte from stack
        /// </summary>
        internal void PopLowPC()
        {
            int addr;
            Register_StackPointer++;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            Cycle_EffectiveAddress = SIDEndian.endian_16lo8(Cycle_EffectiveAddress, player.mem_readMemDataByte(addr));
        }

        /// <summary>
        /// Increment stack and pull program counter high byte from stack
        /// </summary>
        internal void PopHighPC()
        {
            int addr;
            Register_StackPointer++;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemDataByte(addr));
        }

        /// <summary>
        /// increment S, Pop P off stack
        /// </summary>
        internal void PopSR()
        {
            bool newFlagI, oldFlagI;
            oldFlagI = getFlagI();

            // Get status register off stack
            Register_StackPointer++;

            int addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            Register_Status = player.mem_readMemDataByte(addr);

            Register_Status |= ((1 << SR_NOTUSED) | (1 << SR_BREAK));
            setFlagN(Register_Status);
            setFlagV((short)(Register_Status & (1 << SR_OVERFLOW)));
            setFlagZ((short)(((Register_Status & (1 << SR_ZERO)) == 0) ? 1 : 0));
            setFlagC((short)(Register_Status & (1 << SR_CARRY)));

            // I flag change is delayed by 1 instruction
            newFlagI = getFlagI();
            interrupts_irqLatch = oldFlagI ^ newFlagI;
            // Check to see if interrupts got re-enabled
            if (!newFlagI && (interrupts_irqs != 0))
            {
                interrupts_irqRequest = true;
            }
        }

        internal void WasteCycle()
        {
#if !MOS6510_ACCURATE_CYCLES
            clock();
#endif
        }

        // Generic Instruction Addressing Routines

        // Generic Instruction Undocumented Opcodes
        // See documented 6502-nmo.opc by Adam Vardy for more details

        // Generic Instruction Opcodes
        // See a 6510 Assembly Book for more information on these instructions

        internal void adc_instr()
        {
            Perform_ADC();
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of the A register with an
        /// immediate value and then LSRs the result
        /// </summary>
        internal void alr_instr()
        {
            Register_Accumulator &= Cycle_Data;
            setFlagC((short)(Register_Accumulator & 0x01));
            setFlagsNZ(Register_Accumulator >>= 1);
            clock();
        }

        /// <summary>
        /// Undocumented - ANC ANDs the contents of the A register with an immediate
        /// value and then moves bit 7 of A into the Carry flag. This opcode works
        /// basically identically to AND #immed. except that the Carry flag is set to
        /// the same state that the Negative flag is set to
        /// </summary>
        internal void anc_instr()
        {
            setFlagsNZ(Register_Accumulator &= Cycle_Data);
            setFlagC((short)(getFlagN() ? 1 : 0));
            clock();
        }

        internal void and_instr()
        {
            setFlagsNZ(Register_Accumulator &= Cycle_Data);
            clock();
        }

        internal void ane_instr()
        {
            setFlagsNZ(Register_Accumulator = (short)((Register_Accumulator | 0xee) & Register_X & Cycle_Data));
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of the A register with an
        /// immediate value and then RORs the result
        /// (Implementation based on that of Frodo C64 Emulator)
        /// </summary>
        internal void arr_instr()
        {
            short data = (short)(Cycle_Data & Register_Accumulator);
            Register_Accumulator = (short)(data >> 1);
            if (getFlagC())
            {
                Register_Accumulator |= 0x80;
            }

            if (getFlagD())
            {
                setFlagN((short)0);
                if (getFlagC())
                {
                    setFlagN((short)(1 << SR_NEGATIVE));
                }
                setFlagZ(Register_Accumulator);
                setFlagV((short)((data ^ Register_Accumulator) & 0x40));

                if ((data & 0x0f) + (data & 0x01) > 5)
                {
                    Register_Accumulator = (short)(Register_Accumulator & 0xf0 | (Register_Accumulator + 6) & 0x0f);
                }
                setFlagC((short)(((data + (data & 0x10)) & 0x1f0) > 0x50 ? 1 : 0));
                if (getFlagC())
                {
                    Register_Accumulator += 0x60;
                }
            }
            else
            {
                setFlagsNZ(Register_Accumulator);
                setFlagC((short)(Register_Accumulator & 0x40));
                setFlagV((short)((Register_Accumulator & 0x40) ^ ((Register_Accumulator & 0x20) << 1)));
            }
            clock();
        }

        internal void asl_instr()
        {
            PutEffAddrDataByte();
            setFlagC((short)(Cycle_Data & 0x80));
            setFlagsNZ(Cycle_Data = (short)((Cycle_Data << 1) & 0xff));
        }

        internal void asla_instr()
        {
            setFlagC((short)(Register_Accumulator & 0x80));
            setFlagsNZ(Register_Accumulator = (short)((Register_Accumulator << 1) & 0xff));
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ASLs the contents of a memory location and
        /// then ORs the result with the accumulator
        /// </summary>
        internal void aso_instr()
        {
            PutEffAddrDataByte();
            setFlagC((short)(Cycle_Data & 0x80));
            Cycle_Data = (short)((Cycle_Data << 1) & 0xff);
            setFlagsNZ(Register_Accumulator |= Cycle_Data);
        }

        /// <summary>
        /// Undocumented - This opcode stores the result of A AND X AND the high byte
        /// of the target address of the operand +1 in memory.
        /// </summary>
        internal void axa_instr()
        {
            Cycle_Data = (short)(Register_X & Register_Accumulator & (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) + 1));
            PutEffAddrDataByte();
        }

        /// <summary>
        /// Undocumented - AXS ANDs the contents of the A and X registers (without
        /// changing the contents of either register) and stores the result in
        /// memory. AXS does not affect any flags in the processor status register
        /// </summary>
        internal void axs_instr()
        {
            Cycle_Data = (short)(Register_Accumulator & Register_X);
            PutEffAddrDataByte();
        }

        internal void bcc_instr()
        {
            branch_instr(!getFlagC());
        }

        internal void bcs_instr()
        {
            branch_instr(getFlagC());
        }

        internal void beq_instr()
        {
            branch_instr(getFlagZ());
        }

        internal void bit_instr()
        {
            setFlagZ((short)(Register_Accumulator & Cycle_Data));
            setFlagN(Cycle_Data);
            setFlagV((short)(Cycle_Data & 0x40));
            clock();
        }

        internal void bmi_instr()
        {
            branch_instr(getFlagN());
        }

        internal void bne_instr()
        {
            branch_instr(!getFlagZ());
        }

        internal void branch_instr(bool condition)
        {
#if MOS6510_ACCURATE_CYCLES
            if (condition)
            {
                short page;
                page = SIDEndian.endian_32hi8(Register_ProgramCounter);
                Register_ProgramCounter += (sbyte)Cycle_Data;

                // Handle page boundary crossing
                if (SIDEndian.endian_32hi8(Register_ProgramCounter) != page)
                {
                    cycleCount++;
                }
            }
            else
            {
                cycleCount += 2;
                clock();
            }
#else
            if (condition)
            {
                Register_ProgramCounter += (sbyte)Cycle_Data;
            }
#endif
        }

        internal void branch2_instr()
        {
            // This only gets processed when page boundary is not crossed.
            // This causes pending interrupts to be delayed by a cycle
            interrupts_irqClk++;
            interrupts_nmiClk++;
            cycleCount++;
            clock();
        }

        internal void bpl_instr()
        {
            branch_instr(!getFlagN());
        }

        internal void brk_instr()
        {
            PushSR();
            setFlagI((short)1);
            interrupts_irqRequest = false;

            // Check for an NMI, and switch over if pending
            if ((interrupts_pending & iNMI) != 0)
            {
                long cycles = eventContext.getTime(interrupts_nmiClk, m_extPhase);
                if (cycles > MOS6510_INTERRUPT_DELAY)
                {
                    interrupts_pending &= ~iNMI & 0xff;
                    instrCurrent = interruptTable[oNMI];
                    lastInstrCurrent = -(oNMI + 1);
                    procCycle = instrCurrent.cycle;
                }
            }
        }

        internal void bvc_instr()
        {
            branch_instr(!getFlagV());
        }

        internal void bvs_instr()
        {
            branch_instr(getFlagV());
        }

        internal void clc_instr()
        {
            setFlagC((short)0);
            clock();
        }

        internal void cld_instr()
        {
            setFlagD((short)0);
            clock();
        }

        internal virtual void cli_instr()
        {
            bool oldFlagI = getFlagI();
            setFlagI((short)0);
            // I flag change is delayed by 1 instruction
            interrupts_irqLatch = oldFlagI ^ getFlagI();
            // Check to see if interrupts got re-enabled
            if ((interrupts_irqs) != 0)
            {
                interrupts_irqRequest = true;
            }
            clock();
        }

        internal void clv_instr()
        {
            setFlagV((short)0);
            clock();
        }

        internal void cmp_instr()
        {
            int tmp = (int)Register_Accumulator - Cycle_Data & 0xffff;
            setFlagsNZ((short)tmp);
            setFlagC((short)((tmp < 0x100) ? 1 : 0));
            clock();
        }

        internal void cpx_instr()
        {
            int tmp = (int)Register_X - Cycle_Data & 0xffff;
            setFlagsNZ((short)tmp);
            setFlagC((short)((tmp < 0x100) ? 1 : 0));
            clock();
        }

        internal void cpy_instr()
        {
            int tmp = (int)Register_Y - Cycle_Data & 0xffff;
            setFlagsNZ((short)tmp);
            setFlagC((short)((tmp < 0x100) ? 1 : 0));
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode DECs the contents of a memory location and
        /// then CMPs the result with the A register
        /// </summary>
        internal void dcm_instr()
        {
            int tmp;
            PutEffAddrDataByte();
            Cycle_Data = (short)((Cycle_Data - 1) & 0xff);
            tmp = (int)Register_Accumulator - Cycle_Data;
            setFlagsNZ((short)tmp);
            setFlagC((short)((tmp < 0x100) ? 1 : 0));
        }

        internal void dec_instr()
        {
            PutEffAddrDataByte();
            setFlagsNZ(Cycle_Data = (short)((Cycle_Data - 1) & 0xff));
        }

        internal void dex_instr()
        {
            setFlagsNZ(Register_X = (short)((Register_X - 1) & 0xff));
            clock();
        }

        internal void dey_instr()
        {
            setFlagsNZ(Register_Y = (short)((Register_Y - 1) & 0xff));
            clock();
        }

        internal void eor_instr()
        {
            setFlagsNZ(Register_Accumulator ^= Cycle_Data);
            clock();
        }

        internal void inc_instr()
        {
            PutEffAddrDataByte();
            setFlagsNZ(Cycle_Data = (short)((Cycle_Data + 1) & 0xff));
        }

        /// <summary>
        /// Undocumented - This opcode INCs the contents of a memory location and
        /// then SBCs the result from the A register
        /// </summary>
        internal void ins_instr()
        {
            PutEffAddrDataByte();
            Cycle_Data++;
            Perform_SBC();
        }

        internal void inx_instr()
        {
            setFlagsNZ(Register_X = (short)((Register_X + 1) & 0xff));
            clock();
        }

        internal void iny_instr()
        {
            setFlagsNZ(Register_Y = (short)((Register_Y + 1) & 0xff));
            clock();
        }

        internal virtual void jmp_instr()
        {
            Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
            clock();
        }

        internal void jsr_instr()
        {
            // JSR uses absolute addressing in this emulation,
            // hence the -1. The real SID does not use this addressing mode.
            Register_ProgramCounter--;
            PushHighPC();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of a memory location with
        /// the contents of the stack pointer register and stores the result in the
        /// accumulator, the X register, and the stack pointer. Affected flags: N Z
        /// </summary>
        internal void las_instr()
        {
            setFlagsNZ(Cycle_Data &= SIDEndian.endian_16lo8(Register_StackPointer));
            Register_Accumulator = Cycle_Data;
            Register_X = Cycle_Data;
            Register_StackPointer = Cycle_Data;
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode loads both the accumulator and the X register
        /// with the contents of a memory location
        /// </summary>
        internal void lax_instr()
        {
            setFlagsNZ(Register_Accumulator = Register_X = Cycle_Data);
            clock();
        }

        internal void lda_instr()
        {
            setFlagsNZ(Register_Accumulator = Cycle_Data);
            clock();
        }

        internal void ldx_instr()
        {
            setFlagsNZ(Register_X = Cycle_Data);
            clock();
        }

        internal void ldy_instr()
        {
            setFlagsNZ(Register_Y = Cycle_Data);
            clock();
        }

        /// <summary>
        /// Undocumented - LSE LSRs the contents of a memory location and then EORs
        /// the result with the accumulator
        /// </summary>
        internal void lse_instr()
        {
            PutEffAddrDataByte();
            setFlagC((short)(Cycle_Data & 0x01));
            Cycle_Data >>= 1;
            setFlagsNZ(Register_Accumulator ^= Cycle_Data);
        }

        internal void lsr_instr()
        {
            PutEffAddrDataByte();
            setFlagC((short)(Cycle_Data & 0x01));
            setFlagsNZ(Cycle_Data >>= 1);
        }

        internal void lsra_instr()
        {
            setFlagC((short)(Register_Accumulator & 0x01));
            setFlagsNZ(Register_Accumulator >>= 1);
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ORs the A register with #xx, ANDs the result
        /// with an immediate value, and then stores the result in both A and X. xx
        /// may be EE,EF,FE, OR FF, but most emulators seem to use EE
        /// </summary>
        internal void oal_instr()
        {
            setFlagsNZ(Register_X = (Register_Accumulator = (short)(Cycle_Data & (Register_Accumulator | 0xee))));
            clock();
        }

        internal void ora_instr()
        {
            setFlagsNZ(Register_Accumulator |= Cycle_Data);
            clock();
        }

        internal void pha_instr()
        {
            int addr;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            player.mem_writeMemByte(addr, Register_Accumulator);
            Register_StackPointer--;
        }

        internal void pla_instr()
        {
            int addr;
            Register_StackPointer++;
            addr = Register_StackPointer;
            addr = SIDEndian.endian_16hi8(addr, SP_PAGE);
            setFlagsNZ(Register_Accumulator = player.mem_readMemDataByte(addr));
        }

        /// <summary>
        /// Undocumented - RLA ROLs the contents of a memory location and then ANDs
        /// the result with the accumulator
        /// </summary>
        internal void rla_instr()
        {
            short tmp = (short)(Cycle_Data & 0x80);
            PutEffAddrDataByte();
            Cycle_Data = (short)((Cycle_Data << 1) & 0xff);
            if (getFlagC())
            {
                Cycle_Data |= 0x01;
            }
            setFlagC(tmp);
            setFlagsNZ(Register_Accumulator &= Cycle_Data);
        }

        internal void rol_instr()
        {
            short tmp = (short)(Cycle_Data & 0x80);
            PutEffAddrDataByte();
            Cycle_Data = (short)((Cycle_Data << 1) & 0xff);
            if (getFlagC())
            {
                Cycle_Data |= 0x01;
            }
            setFlagsNZ(Cycle_Data);
            setFlagC(tmp);
        }

        internal void rola_instr()
        {
            short tmp = (short)(Register_Accumulator & 0x80);
            Register_Accumulator = (short)((Register_Accumulator << 1) & 0xff);
            if (getFlagC())
            {
                Register_Accumulator |= 0x01;
            }
            setFlagsNZ(Register_Accumulator);
            setFlagC(tmp);
            clock();
        }

        internal void ror_instr()
        {
            short tmp = (short)(Cycle_Data & 0x01);
            PutEffAddrDataByte();
            Cycle_Data >>= 1;
            if (getFlagC())
            {
                Cycle_Data |= 0x80;
            }
            setFlagsNZ(Cycle_Data);
            setFlagC(tmp);
        }

        internal void rora_instr()
        {
            short tmp = (short)(Register_Accumulator & 0x01);
            Register_Accumulator >>= 1;
            if (getFlagC())
            {
                Register_Accumulator |= 0x80;
            }
            setFlagsNZ(Register_Accumulator);
            setFlagC(tmp);
            clock();
        }

        /// <summary>
        /// Undocumented - RRA RORs the contents of a memory location and then ADCs
        /// the result with the accumulator
        /// </summary>
        internal void rra_instr()
        {
            short tmp = (short)(Cycle_Data & 0x01);
            PutEffAddrDataByte();
            Cycle_Data >>= 1;
            if (getFlagC())
            {
                Cycle_Data |= 0x80;
            }
            setFlagC(tmp);
            Perform_ADC();
        }

        /// <summary>
        /// RTI does not delay the IRQ I flag change as it is set 3 cycles before the
        /// end of the opcode, and thus the 6510 has enough time to call the
        /// interrupt routine as soon as the opcode ends, if necessary
        /// </summary>
        internal void rti_instr()
        {
            Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
            interrupts_irqLatch = false;
            clock();
        }

        internal void rts_instr()
        {
            Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
            Register_ProgramCounter++;
        }

        internal void sbx_instr()
        {
            long tmp = (Register_X & Register_Accumulator) - Cycle_Data;
            setFlagsNZ((Register_X = (short)(tmp & 0xff)));
            setFlagC((short)((tmp < 0x100) ? 1 : 0));
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of the Y register with
        /// (ab+1) and stores the result in memory
        /// </summary>
        internal void say_instr()
        {
            Cycle_Data = (short)(Register_Y & (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) + 1));
            PutEffAddrDataByte();
        }

        internal void sbc_instr()
        {
            Perform_SBC();
            clock();
        }

        internal void sec_instr()
        {
            setFlagC((short)1);
            clock();
        }

        internal void sed_instr()
        {
            setFlagD((short)1);
            clock();
        }

        internal void sei_instr()
        {
            bool oldFlagI = getFlagI();
            setFlagI((short)1);
            // I flag change is delayed by 1 instruction
            interrupts_irqLatch = oldFlagI ^ getFlagI();
            interrupts_irqRequest = false;
            clock();
        }

        /// <summary>
        /// Generic Instruction Undocumented Opcodes See documented 6502-nmo.opc by
        /// Adam Vardy for more details
        /// </summary>
        internal void shs_instr()
        {
            Register_StackPointer = SIDEndian.endian_16lo8(Register_StackPointer, (short)(Register_Accumulator & Register_X));
            Cycle_Data = (short)((SIDEndian.endian_16hi8(Cycle_EffectiveAddress) + 1) & Register_StackPointer);
            PutEffAddrDataByte();
        }

        internal void sta_instr()
        {
            Cycle_Data = Register_Accumulator;
            PutEffAddrDataByte();
        }

        internal void stx_instr()
        {
            Cycle_Data = Register_X;
            PutEffAddrDataByte();
        }

        internal void sty_instr()
        {
            Cycle_Data = Register_Y;
            PutEffAddrDataByte();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of the A and X registers
        /// (without changing the contents of either register) and transfers the
        /// result to the stack pointer. It then ANDs that result with the contents
        /// of the high byte of the target address of the operand +1 and stores that
        /// result in memory
        /// </summary>
        internal void tas_instr()
        {
            Register_StackPointer = SIDEndian.endian_16lo8(Register_StackPointer, (short)(Register_Accumulator & Register_X));
            int tmp = Register_StackPointer & (Cycle_EffectiveAddress + 1);
            Cycle_Data = SIDEndian.endian_16lo8(tmp);
        }

        internal void tax_instr()
        {
            setFlagsNZ(Register_X = Register_Accumulator);
            clock();
        }

        internal void tay_instr()
        {
            setFlagsNZ(Register_Y = Register_Accumulator);
            clock();
        }

        internal void tsx_instr()
        {
            setFlagsNZ(Register_X = SIDEndian.endian_16lo8(Register_StackPointer));
            clock();
        }

        internal void txa_instr()
        {
            setFlagsNZ(Register_Accumulator = Register_X);
            clock();
        }

        internal void txs_instr()
        {
            Register_StackPointer = SIDEndian.endian_16lo8(Register_StackPointer, Register_X);
            clock();
        }

        internal void tya_instr()
        {
            setFlagsNZ(Register_Accumulator = Register_Y);
            clock();
        }

        /// <summary>
        /// Undocumented - This opcode ANDs the contents of the X register with
        /// (ab+1) and stores the result in memory
        /// </summary>
        internal void xas_instr()
        {
            Cycle_Data = (short)(Register_X & (SIDEndian.endian_16hi8(Cycle_EffectiveAddress) + 1));
            PutEffAddrDataByte();
        }

        internal virtual void illegal_instr()
        {
            // Perform Environment Reset
            player.Reset(true);
        }

        // Generic Binary Coded Decimal Correction

        protected void Perform_ADC()
        {
            int C = getFlagC() ? 1 : 0;
            int A = Register_Accumulator;
            int s = Cycle_Data;
            int regAC2 = A + s + C;

            if (getFlagD())
            {
                // BCD mode
                int lo = (A & 0x0f) + (s & 0x0f) + C;
                int hi = (A & 0xf0) + (s & 0xf0);
                if (lo > 0x09)
                {
                    lo += 0x06;
                }
                if (lo > 0x0f)
                {
                    hi += 0x10;
                }

                setFlagZ((short)regAC2);
                setFlagN((short)hi);
                setFlagV((((hi ^ A) & 0x80) != 0) && (((A ^ s) & 0x80) == 0) ? (short)1 : (short)0);
                if (hi > 0x90)
                {
                    hi += 0x60;
                }

                setFlagC((hi > 0xff) ? (short)1 : (short)0);
                Register_Accumulator = (short)(hi | (lo & 0x0f));
            }
            else
            {
                // Binary mode
                setFlagC((regAC2 > 0xff) ? (short)1 : (short)0);
                setFlagV((((regAC2 ^ A) & 0x80) != 0) && (((A ^ s) & 0x80) == 0) ? (short)1 : (short)0);
                setFlagsNZ(Register_Accumulator = (short)(regAC2 & 0xff));
            }
        }

        protected void Perform_SBC()
        {
            int C = getFlagC() ? 0 : 1;
            int A = Register_Accumulator;
            int s = Cycle_Data;
            int regAC2 = A - s - C & 0xffff;

            setFlagC((regAC2 < 0x100) ? (short)1 : (short)0);
            setFlagV(((((regAC2 ^ A) & 0x80) != 0) && (((A ^ s) & 0x80) != 0)) ? (short)1 : (short)0);
            setFlagsNZ((short)regAC2);

            if (getFlagD())
            {
                // BCD mode
                int lo = (A & 0x0f) - (s & 0x0f) - C;
                int hi = (A & 0xf0) - (s & 0xf0);
                if ((lo & 0x10) != 0)
                {
                    lo -= 0x06;
                    hi -= 0x10;
                }
                if ((hi & 0x100) != 0)
                {
                    hi -= 0x60;
                }
                Register_Accumulator = (short)(hi | (lo & 0x0f));
            }
            else
            {
                // Binary mode
                Register_Accumulator = (short)(regAC2 & 0xff);
            }
        }

        /// <summary>
        /// Overridden in the Sub-class SID6510 for Sidplay compatibility
        /// </summary>
        internal virtual void IRQRequest_sidplay_irq()
        {
            IRQRequest();
        }

        /// <summary>
        /// Overridden in the Sub-class SID6510 for Sidplay compatibility
        /// </summary>
        internal virtual void PushHighPC_sidplay_brk()
        {
            PushHighPC();
        }

        /// <summary>
        /// Overridden in the Sub-class SID6510 for Sidplay compatibility
        /// </summary>
        internal virtual void PopSR_sidplay_rti()
        {
            PopSR();
        }

        /// <summary>
        /// Initialize and create CPU Chip
        /// </summary>
        /// <param name="context"></param>
        public MOS6510(EventScheduler context, InternalPlayer owner)
        {
            cpuEvent = new CPUEvent(this);
            eventContext = context;
            player = owner;
            m_phase = event_phase_t.EVENT_CLOCK_PHI2;
            m_extPhase = event_phase_t.EVENT_CLOCK_PHI1;

            BuildInstructions();

            // Initialize Processor Registers
            Register_Accumulator = 0;
            Register_X = 0;
            Register_Y = 0;

            Cycle_EffectiveAddress = 0;
            Cycle_Data = 0;

            Initialise();
        }
        // only used for deserializing
        public MOS6510(EventScheduler context, InternalPlayer owner, BinaryReader reader, EventList events)
        {
            eventContext = context;
            player = owner;
            LoadFromReader(reader);
            BuildInstructions();

            if (lastInstrCurrent >= 0)
            {
                instrCurrent = instrTable[lastInstrCurrent];
            }
            else
            {
                instrCurrent = interruptTable[-lastInstrCurrent - 1];
            }
            if (procCycle == null)
            {
                procCycle = instrCurrent.cycle;
            }

            if (cpuEvent_id == -1)
            {
                cpuEvent = null;
            }
            else
            {
                cpuEvent = events.GetEventById(cpuEvent_id) as CPUEvent;
                cpuEvent.owner = this;

#if DEBUG
                if (cpuEvent == null)
                {
                    throw new Exception("MOS6510: CPUEvent not found");
                }
#endif
            }
        }

        private void BuildInstructions()
        {
            ProcessorOperations instr;
            bool legalMode = true;
            bool legalInstr = true;

            ProcessorCycle[] procCycle_tmp = null;

            #region Function Delegates
            ProcessorCycle.FunctionDelegate dlg_WasteCycle = new ProcessorCycle.FunctionDelegate(WasteCycle);
            ProcessorCycle.FunctionDelegate dlg_FetchLowAddr = new ProcessorCycle.FunctionDelegate(FetchLowAddr);
            ProcessorCycle.FunctionDelegate dlg_FetchLowAddrX = new ProcessorCycle.FunctionDelegate(FetchLowAddrX);
            ProcessorCycle.FunctionDelegate dlg_FetchLowAddrY = new ProcessorCycle.FunctionDelegate(FetchLowAddrY);
            ProcessorCycle.FunctionDelegate dlg_FetchEffAddrDataByte = new ProcessorCycle.FunctionDelegate(FetchEffAddrDataByte);
            ProcessorCycle.FunctionDelegate dlg_FetchHighAddr = new ProcessorCycle.FunctionDelegate(FetchHighAddr);
            ProcessorCycle.FunctionDelegate dlg_FetchHighAddrX = new ProcessorCycle.FunctionDelegate(FetchHighAddrX);
            ProcessorCycle.FunctionDelegate dlg_FetchHighAddrX2 = new ProcessorCycle.FunctionDelegate(FetchHighAddrX2);
            ProcessorCycle.FunctionDelegate dlg_FetchHighAddrY = new ProcessorCycle.FunctionDelegate(FetchHighAddrY);
            ProcessorCycle.FunctionDelegate dlg_FetchHighAddrY2 = new ProcessorCycle.FunctionDelegate(FetchHighAddrY2);
            ProcessorCycle.FunctionDelegate dlg_FetchLowPointer = new ProcessorCycle.FunctionDelegate(FetchLowPointer);
            ProcessorCycle.FunctionDelegate dlg_FetchLowPointerX = new ProcessorCycle.FunctionDelegate(FetchLowPointerX);
            ProcessorCycle.FunctionDelegate dlg_FetchLowEffAddr = new ProcessorCycle.FunctionDelegate(FetchLowEffAddr);
            ProcessorCycle.FunctionDelegate dlg_FetchHighEffAddr = new ProcessorCycle.FunctionDelegate(FetchHighEffAddr);
            ProcessorCycle.FunctionDelegate dlg_FetchHighEffAddrY = new ProcessorCycle.FunctionDelegate(FetchHighEffAddrY);
            ProcessorCycle.FunctionDelegate dlg_FetchHighEffAddrY2 = new ProcessorCycle.FunctionDelegate(FetchHighEffAddrY2);
            ProcessorCycle.FunctionDelegate dlg_PutEffAddrDataByte = new ProcessorCycle.FunctionDelegate(PutEffAddrDataByte);
            ProcessorCycle.FunctionDelegate dlg_branch2_instr = new ProcessorCycle.FunctionDelegate(branch2_instr);
            ProcessorCycle.FunctionDelegate dlg_PushLowPC = new ProcessorCycle.FunctionDelegate(PushLowPC);
            ProcessorCycle.FunctionDelegate dlg_PushHighPC = new ProcessorCycle.FunctionDelegate(PushHighPC);
            ProcessorCycle.FunctionDelegate dlg_PopLowPC = new ProcessorCycle.FunctionDelegate(PopLowPC);
            ProcessorCycle.FunctionDelegate dlg_PopHighPC = new ProcessorCycle.FunctionDelegate(PopHighPC);
            ProcessorCycle.FunctionDelegate dlg_FetchOpcode = new ProcessorCycle.FunctionDelegate(FetchOpcode);
            #endregion

            // ----------------------------------------------------------------------
            // Build up the processor instruction table
            for (int i = 0; i < instrTable.Length; i++)
            {
                // Pass 1 allocates the memory, Pass 2 builds the instruction
                instr = instrTable[i] = new ProcessorOperations();

                for (int pass = 0; pass < 2; pass++)
                {
                    int WRITE = 0;
                    int READ = 1;
                    int access = WRITE;
                    int cycleCounter = -1;
                    legalMode = true;
                    legalInstr = true;

                    switch (i)
                    {
                        // Accumulator or Implied addressing
                        case OpCode.ASLn:
                        case OpCode.CLCn:
                        case OpCode.CLDn:
                        case OpCode.CLIn:
                        case OpCode.CLVn:
                        case OpCode.DEXn:
                        case OpCode.DEYn:
                        case OpCode.INXn:
                        case OpCode.INYn:
                        case OpCode.LSRn:
                        case OpCode.NOPn:
                        case OpCode.NOPn_1:
                        case OpCode.NOPn_2:
                        case OpCode.NOPn_3:
                        case OpCode.NOPn_4:
                        case OpCode.NOPn_5:
                        case OpCode.NOPn_6:
                        case OpCode.PHAn:
                        case OpCode.PHPn:
                        case OpCode.PLAn:
                        case OpCode.PLPn:
                        case OpCode.ROLn:
                        case OpCode.RORn:
                        case OpCode.RTIn:
                        case OpCode.RTSn:
                        case OpCode.SECn:
                        case OpCode.SEDn:
                        case OpCode.SEIn:
                        case OpCode.TAXn:
                        case OpCode.TAYn:
                        case OpCode.TSXn:
                        case OpCode.TXAn:
                        case OpCode.TXSn:
                        case OpCode.TYAn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            break;

                        // Immediate and Relative Addressing Mode Handler
                        case OpCode.ADCb:
                        case OpCode.ANDb:
                        case OpCode.ANCb:
                        case OpCode.ANCb_1:
                        case OpCode.ANEb:
                        case OpCode.ASRb:
                        case OpCode.ARRb:
                        case OpCode.BCCr:
                        case OpCode.BCSr:
                        case OpCode.BEQr:
                        case OpCode.BMIr:
                        case OpCode.BNEr:
                        case OpCode.BPLr:
                        case OpCode.BRKn:
                        case OpCode.BVCr:
                        case OpCode.BVSr:
                        case OpCode.CMPb:
                        case OpCode.CPXb:
                        case OpCode.CPYb:
                        case OpCode.EORb:
                        case OpCode.LDAb:
                        case OpCode.LDXb:
                        case OpCode.LDYb:
                        case OpCode.LXAb:
                        case OpCode.NOPb:
                        case OpCode.NOPb_1:
                        case OpCode.NOPb_2:
                        case OpCode.NOPb_3:
                        case OpCode.NOPb_4:
                        case OpCode.ORAb:
                        case OpCode.SBCb:
                        case OpCode.SBCb_1:
                        case OpCode.SBXb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(FetchDataByte);
                            }
                            break;

                        // Zero Page Addressing Mode Handler - Read & RMW
                        case OpCode.ADCz:
                        case OpCode.ANDz:
                        case OpCode.BITz:
                        case OpCode.CMPz:
                        case OpCode.CPXz:
                        case OpCode.CPYz:
                        case OpCode.EORz:
                        case OpCode.LAXz:
                        case OpCode.LDAz:
                        case OpCode.LDXz:
                        case OpCode.LDYz:
                        case OpCode.ORAz:
                        case OpCode.NOPz:
                        case OpCode.NOPz_1:
                        case OpCode.NOPz_2:
                        case OpCode.SBCz:
                        case OpCode.ASLz:
                        case OpCode.DCPz:
                        case OpCode.DECz:
                        case OpCode.INCz:
                        case OpCode.ISBz:
                        case OpCode.LSRz:
                        case OpCode.ROLz:
                        case OpCode.RORz:
                        case OpCode.SREz:
                        case OpCode.SLOz:
                        case OpCode.RLAz:
                        case OpCode.RRAz:
                            access++;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;
                        case OpCode.SAXz:
                        case OpCode.STAz:
                        case OpCode.STXz:
                        case OpCode.STYz:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Zero Page with X Offset Addressing Mode Handler
                        case OpCode.ADCzx:
                        case OpCode.ANDzx:
                        case OpCode.CMPzx:
                        case OpCode.EORzx:
                        case OpCode.LDAzx:
                        case OpCode.LDYzx:
                        case OpCode.NOPzx:
                        case OpCode.NOPzx_1:
                        case OpCode.NOPzx_2:
                        case OpCode.NOPzx_3:
                        case OpCode.NOPzx_4:
                        case OpCode.NOPzx_5:
                        case OpCode.ORAzx:
                        case OpCode.SBCzx:
                        case OpCode.ASLzx:
                        case OpCode.DCPzx:
                        case OpCode.DECzx:
                        case OpCode.INCzx:
                        case OpCode.ISBzx:
                        case OpCode.LSRzx:
                        case OpCode.RLAzx:
                        case OpCode.ROLzx:
                        case OpCode.RORzx:
                        case OpCode.RRAzx:
                        case OpCode.SLOzx:
                        case OpCode.SREzx:
                            access++;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddrX;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;
                        case OpCode.STAzx:
                        case OpCode.STYzx:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddrX;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Zero Page with Y Offset Addressing Mode Handler
                        case OpCode.LDXzy:
                        case OpCode.LAXzy:
                            access = READ;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddrY;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;
                        case OpCode.STXzy:
                        case OpCode.SAXzy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddrY;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Absolute Addressing Mode Handler
                        case OpCode.ADCa:
                        case OpCode.ANDa:
                        case OpCode.BITa:
                        case OpCode.CMPa:
                        case OpCode.CPXa:
                        case OpCode.CPYa:
                        case OpCode.EORa:
                        case OpCode.LAXa:
                        case OpCode.LDAa:
                        case OpCode.LDXa:
                        case OpCode.LDYa:
                        case OpCode.NOPa:
                        case OpCode.ORAa:
                        case OpCode.SBCa:
                        case OpCode.ASLa:
                        case OpCode.DCPa:
                        case OpCode.DECa:
                        case OpCode.INCa:
                        case OpCode.ISBa:
                        case OpCode.LSRa:
                        case OpCode.ROLa:
                        case OpCode.RORa:
                        case OpCode.SLOa:
                        case OpCode.SREa:
                        case OpCode.RLAa:
                        case OpCode.RRAa:
                            access++;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;
                        case OpCode.JMPw:
                        case OpCode.JSRw:
                        case OpCode.SAXa:
                        case OpCode.STAa:
                        case OpCode.STXa:
                        case OpCode.STYa:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Absolute With X Offset Addressing Mode Handler (Read)
                        case OpCode.ADCax:
                        case OpCode.ANDax:
                        case OpCode.CMPax:
                        case OpCode.EORax:
                        case OpCode.LDAax:
                        case OpCode.LDYax:
                        case OpCode.NOPax:
                        case OpCode.NOPax_1:
                        case OpCode.NOPax_2:
                        case OpCode.NOPax_3:
                        case OpCode.NOPax_4:
                        case OpCode.NOPax_5:
                        case OpCode.ORAax:
                        case OpCode.SBCax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrX;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;

                        // Absolute X (No page crossing handled)
                        case OpCode.ASLax:
                        case OpCode.DCPax:
                        case OpCode.DECax:
                        case OpCode.INCax:
                        case OpCode.ISBax:
                        case OpCode.LSRax:
                        case OpCode.RLAax:
                        case OpCode.ROLax:
                        case OpCode.RORax:
                        case OpCode.RRAax:
                        case OpCode.SLOax:
                        case OpCode.SREax:
                            access = READ;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrX2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;
                        case OpCode.SHYax:
                        case OpCode.STAax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrX2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Absolute With Y Offset Addressing Mode Handler (Read)
                        case OpCode.ADCay:
                        case OpCode.ANDay:
                        case OpCode.CMPay:
                        case OpCode.EORay:
                        case OpCode.LASay:
                        case OpCode.LAXay:
                        case OpCode.LDAay:
                        case OpCode.LDXay:
                        case OpCode.ORAay:
                        case OpCode.SBCay:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrY;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;

                        // Absolute Y (No page crossing handled)
                        case OpCode.DCPay:
                        case OpCode.ISBay:
                        case OpCode.RLAay:
                        case OpCode.RRAay:
                        case OpCode.SLOay:
                        case OpCode.SREay:
                            access = READ;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrY2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;
                        case OpCode.SHAay:
                        case OpCode.SHSay:
                        case OpCode.SHXay:
                        case OpCode.STAay:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighAddrY2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Absolute Indirect Addressing Mode Handler
                        case OpCode.JMPi:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(FetchHighPointer);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddr;
                            }
                            break;

                        // Indexed with X Preinc Addressing Mode Handler
                        case OpCode.ADCix:
                        case OpCode.ANDix:
                        case OpCode.CMPix:
                        case OpCode.EORix:
                        case OpCode.LAXix:
                        case OpCode.LDAix:
                        case OpCode.ORAix:
                        case OpCode.SBCix:
                        case OpCode.DCPix:
                        case OpCode.ISBix:
                        case OpCode.SLOix:
                        case OpCode.SREix:
                        case OpCode.RLAix:
                        case OpCode.RRAix:
                            access++;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointerX;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;
                        case OpCode.SAXix:
                        case OpCode.STAix:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointerX;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddr;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        // Indexed with Y Postinc Addressing Mode Handler (Read)
                        case OpCode.ADCiy:
                        case OpCode.ANDiy:
                        case OpCode.CMPiy:
                        case OpCode.EORiy:
                        case OpCode.LAXiy:
                        case OpCode.LDAiy:
                        case OpCode.ORAiy:
                        case OpCode.SBCiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddrY;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;

                        // Indexed Y (No page crossing handled)
                        case OpCode.DCPiy:
                        case OpCode.ISBiy:
                        case OpCode.RLAiy:
                        case OpCode.RRAiy:
                        case OpCode.SLOiy:
                        case OpCode.SREiy:
                            access = READ;
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddrY2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                            }
                            break;
                        case OpCode.SHAiy:
                        case OpCode.STAiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowPointer;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchLowEffAddr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchHighEffAddrY2;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            if (access == READ)
                            {
                                cycleCounter++;
                                if (pass != 0)
                                {
                                    procCycle_tmp[cycleCounter].func = dlg_FetchEffAddrDataByte;
                                }
                            }
                            break;

                        default:
                            legalMode = false;
                            break;
                    }

                    if (pass != 0)
                    {
                        // Everything up to now is reads and can
                        // therefore be blocked through cycle stealing
                        for (int c = -1; c < cycleCounter; )
                        {
                            procCycle_tmp[++c].nosteal = false;
                        }
                    }

                    // ---------------------------------------------------------------------------------------
                    // Addressing Modes Finished, other cycles are instruction
                    // dependent
                    switch ((int)i)
                    {
                        case OpCode.ADCz:
                        case OpCode.ADCzx:
                        case OpCode.ADCa:
                        case OpCode.ADCax:
                        case OpCode.ADCay:
                        case OpCode.ADCix:
                        case OpCode.ADCiy:
                        case OpCode.ADCb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(adc_instr);
                            }
                            break;

                        case OpCode.ANCb:
                        case OpCode.ANCb_1:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(anc_instr);
                            }
                            break;

                        case OpCode.ANDz:
                        case OpCode.ANDzx:
                        case OpCode.ANDa:
                        case OpCode.ANDax:
                        case OpCode.ANDay:
                        case OpCode.ANDix:
                        case OpCode.ANDiy:
                        case OpCode.ANDb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(and_instr);
                            }
                            break;

                        case OpCode.ANEb: // also known as XAA
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ane_instr);
                            }
                            break;

                        case OpCode.ARRb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(arr_instr);
                            }
                            break;

                        case OpCode.ASLn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(asla_instr);
                            }
                            break;

                        case OpCode.ASLz:
                        case OpCode.ASLzx:
                        case OpCode.ASLa:
                        case OpCode.ASLax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(asl_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.ASRb: // also known as ALR
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(alr_instr);
                            }
                            break;

                        case OpCode.BCCr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bcc_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BCSr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bcs_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }

                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BEQr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(beq_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BITz:
                        case OpCode.BITa:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bit_instr);
                            }
                            break;

                        case OpCode.BMIr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bmi_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BNEr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bne_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BPLr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bpl_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BRKn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(PushHighPC_sidplay_brk);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushLowPC;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(brk_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQ1Request);
                            }
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQ2Request);
                            }
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchOpcode;
                            }
                            break;

                        case OpCode.BVCr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bvc_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.BVSr:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(bvs_instr);
                            }
#if MOS6510_ACCURATE_CYCLES
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_branch2_instr;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
#endif
                            break;

                        case OpCode.CLCn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(clc_instr);
                            }
                            break;

                        case OpCode.CLDn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(cld_instr);
                            }
                            break;

                        case OpCode.CLIn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(cli_instr);
                            }
                            break;

                        case OpCode.CLVn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(clv_instr);
                            }
                            break;

                        case OpCode.CMPz:
                        case OpCode.CMPzx:
                        case OpCode.CMPa:
                        case OpCode.CMPax:
                        case OpCode.CMPay:
                        case OpCode.CMPix:
                        case OpCode.CMPiy:
                        case OpCode.CMPb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(cmp_instr);
                            }
                            break;

                        case OpCode.CPXz:
                        case OpCode.CPXa:
                        case OpCode.CPXb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(cpx_instr);
                            }
                            break;

                        case OpCode.CPYz:
                        case OpCode.CPYa:
                        case OpCode.CPYb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(cpy_instr);
                            }
                            break;

                        case OpCode.DCPz:
                        case OpCode.DCPzx:
                        case OpCode.DCPa:
                        case OpCode.DCPax:
                        case OpCode.DCPay:
                        case OpCode.DCPix:
                        case OpCode.DCPiy: // also known as DCM
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(dcm_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.DECz:
                        case OpCode.DECzx:
                        case OpCode.DECa:
                        case OpCode.DECax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(dec_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.DEXn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(dex_instr);
                            }
                            break;

                        case OpCode.DEYn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(dey_instr);
                            }
                            break;

                        case OpCode.EORz:
                        case OpCode.EORzx:
                        case OpCode.EORa:
                        case OpCode.EORax:
                        case OpCode.EORay:
                        case OpCode.EORix:
                        case OpCode.EORiy:
                        case OpCode.EORb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(eor_instr);
                            }
                            break;

                        /*
                         * HLT // also known as JAM 
                         */

                        case OpCode.INCz:
                        case OpCode.INCzx:
                        case OpCode.INCa:
                        case OpCode.INCax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(inc_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.INXn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(inx_instr);
                            }
                            break;

                        case OpCode.INYn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(iny_instr);
                            }
                            break;

                        case OpCode.ISBz:
                        case OpCode.ISBzx:
                        case OpCode.ISBa:
                        case OpCode.ISBax:
                        case OpCode.ISBay:
                        case OpCode.ISBix:
                        case OpCode.ISBiy: // also known as INS
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ins_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.JSRw:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(jsr_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushLowPC;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            // fall-through
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(jmp_instr);
                            }
                            break;
                        case OpCode.JMPw:
                        case OpCode.JMPi:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(jmp_instr);
                            }
                            break;

                        case OpCode.LASay:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(las_instr);
                            }
                            break;

                        case OpCode.LAXz:
                        case OpCode.LAXzy:
                        case OpCode.LAXa:
                        case OpCode.LAXay:
                        case OpCode.LAXix:
                        case OpCode.LAXiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(lax_instr);
                            }
                            break;

                        case OpCode.LDAz:
                        case OpCode.LDAzx:
                        case OpCode.LDAa:
                        case OpCode.LDAax:
                        case OpCode.LDAay:
                        case OpCode.LDAix:
                        case OpCode.LDAiy:
                        case OpCode.LDAb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(lda_instr);
                            }
                            break;

                        case OpCode.LDXz:
                        case OpCode.LDXzy:
                        case OpCode.LDXa:
                        case OpCode.LDXay:
                        case OpCode.LDXb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ldx_instr);
                            }
                            break;

                        case OpCode.LDYz:
                        case OpCode.LDYzx:
                        case OpCode.LDYa:
                        case OpCode.LDYax:
                        case OpCode.LDYb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ldy_instr);
                            }
                            break;

                        case OpCode.LSRn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(lsra_instr);
                            }
                            break;

                        case OpCode.LSRz:
                        case OpCode.LSRzx:
                        case OpCode.LSRa:
                        case OpCode.LSRax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(lsr_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.NOPn:
                        case OpCode.NOPn_1:
                        case OpCode.NOPn_2:
                        case OpCode.NOPn_3:
                        case OpCode.NOPn_4:
                        case OpCode.NOPn_5:
                        case OpCode.NOPn_6:
                        case OpCode.NOPb:
                        case OpCode.NOPb_1:
                        case OpCode.NOPb_2:
                        case OpCode.NOPb_3:
                        case OpCode.NOPb_4:
                        case OpCode.NOPz:
                        case OpCode.NOPz_1:
                        case OpCode.NOPz_2:
                        case OpCode.NOPzx:
                        case OpCode.NOPzx_1:
                        case OpCode.NOPzx_2:
                        case OpCode.NOPzx_3:
                        case OpCode.NOPzx_4:
                        case OpCode.NOPzx_5:
                        case OpCode.NOPa:
                        case OpCode.NOPax:
                        case OpCode.NOPax_1:
                        case OpCode.NOPax_2:
                        case OpCode.NOPax_3:
                        case OpCode.NOPax_4:
                        case OpCode.NOPax_5:
                            // NOPb NOPz NOPzx - also known as SKBn
                            // NOPa NOPax - also known as SKWn
                            break;

                        case OpCode.LXAb: // also known as OAL
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(oal_instr);
                            }
                            break;

                        case OpCode.ORAz:
                        case OpCode.ORAzx:
                        case OpCode.ORAa:
                        case OpCode.ORAax:
                        case OpCode.ORAay:
                        case OpCode.ORAix:
                        case OpCode.ORAiy:
                        case OpCode.ORAb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ora_instr);
                            }
                            break;

                        case OpCode.PHAn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(pha_instr);
                            }
                            break;

                        case OpCode.PHPn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(PushSR);
                            }
                            break;

                        case OpCode.PLAn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(pla_instr);
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            break;

                        case OpCode.PLPn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(PopSR);
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            break;

                        case OpCode.RLAz:
                        case OpCode.RLAzx:
                        case OpCode.RLAix:
                        case OpCode.RLAa:
                        case OpCode.RLAax:
                        case OpCode.RLAay:
                        case OpCode.RLAiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rla_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.ROLn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rola_instr);
                            }
                            break;

                        case OpCode.ROLz:
                        case OpCode.ROLzx:
                        case OpCode.ROLa:
                        case OpCode.ROLax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rol_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.RORn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rora_instr);
                            }
                            break;

                        case OpCode.RORz:
                        case OpCode.RORzx:
                        case OpCode.RORa:
                        case OpCode.RORax:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(ror_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.RRAa:
                        case OpCode.RRAax:
                        case OpCode.RRAay:
                        case OpCode.RRAz:
                        case OpCode.RRAzx:
                        case OpCode.RRAix:
                        case OpCode.RRAiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rra_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.RTIn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(PopSR_sidplay_rti);
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PopLowPC;
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PopHighPC;
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rti_instr);
                            }
                            break;

                        case OpCode.RTSn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PopLowPC;
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PopHighPC;
                                procCycle_tmp[cycleCounter].nosteal = false;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(rts_instr);
                            }
                            break;

                        case OpCode.SAXz:
                        case OpCode.SAXzy:
                        case OpCode.SAXa:
                        case OpCode.SAXix: // also known as AXS
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(axs_instr);
                            }
                            break;

                        case OpCode.SBCz:
                        case OpCode.SBCzx:
                        case OpCode.SBCa:
                        case OpCode.SBCax:
                        case OpCode.SBCay:
                        case OpCode.SBCix:
                        case OpCode.SBCiy:
                        case OpCode.SBCb:
                        case OpCode.SBCb_1:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sbc_instr);
                            }
                            break;

                        case OpCode.SBXb:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sbx_instr);
                            }
                            break;

                        case OpCode.SECn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sec_instr);
                            }
                            break;

                        case OpCode.SEDn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sed_instr);
                            }
                            break;

                        case OpCode.SEIn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sei_instr);
                            }
                            break;

                        case OpCode.SHAay:
                        case OpCode.SHAiy: // also known as AXA
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(axa_instr);
                            }
                            break;

                        case OpCode.SHSay: // also known as TAS
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(shs_instr);
                            }
                            break;

                        case OpCode.SHXay: // also known as XAS
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(xas_instr);
                            }
                            break;

                        case OpCode.SHYax: // also known as SAY
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(say_instr);
                            }
                            break;

                        case OpCode.SLOz:
                        case OpCode.SLOzx:
                        case OpCode.SLOa:
                        case OpCode.SLOax:
                        case OpCode.SLOay:
                        case OpCode.SLOix:
                        case OpCode.SLOiy: // also known as ASO
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(aso_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.SREz:
                        case OpCode.SREzx:
                        case OpCode.SREa:
                        case OpCode.SREax:
                        case OpCode.SREay:
                        case OpCode.SREix:
                        case OpCode.SREiy: // also known as LSE
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(lse_instr);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PutEffAddrDataByte;
                            }
                            break;

                        case OpCode.STAz:
                        case OpCode.STAzx:
                        case OpCode.STAa:
                        case OpCode.STAax:
                        case OpCode.STAay:
                        case OpCode.STAix:
                        case OpCode.STAiy:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sta_instr);
                            }
                            break;

                        case OpCode.STXz:
                        case OpCode.STXzy:
                        case OpCode.STXa:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(stx_instr);
                            }
                            break;

                        case OpCode.STYz:
                        case OpCode.STYzx:
                        case OpCode.STYa:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(sty_instr);
                            }
                            break;

                        case OpCode.TAXn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(tax_instr);
                            }
                            break;

                        case OpCode.TAYn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(tay_instr);
                            }
                            break;

                        case OpCode.TSXn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(tsx_instr);
                            }
                            break;

                        case OpCode.TXAn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(txa_instr);
                            }
                            break;

                        case OpCode.TXSn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(txs_instr);
                            }
                            break;

                        case OpCode.TYAn:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(tya_instr);
                            }
                            break;

                        default:
                            legalInstr = false;
                            break;
                    }

                    if (!(legalMode || legalInstr))
                    {
                        cycleCounter++;
                        if (pass != 0)
                        {
                            procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(illegal_instr);
                        }
                    }
#if DEBUG
                    else if (!(legalMode && legalInstr))
                    {
                        throw new Exception("MOS6510 ERROR: no legal mode nor legal instruction");
                    }
#endif

                    cycleCounter++;
                    if (pass != 0)
                    {
                        procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(NextInstr);
                    }
                    cycleCounter++;
                    if (pass == 0)
                    {
                        // Pass 1 - Allocate Memory
                        if ((cycleCounter) != 0)
                        {
                            instr.cycle = new ProcessorCycle[cycleCounter];
                            procCycle_tmp = instr.cycle;

                            int c = cycleCounter;
                            while (c > 0)
                            {
                                procCycle_tmp[--c] = new ProcessorCycle();
                                procCycle_tmp[c].nosteal = true;
                            }
                        }
                    }
                }
            }

            // ----------------------------------------------------------------------
            // Build interrupts
            for (int i = 0; i < 3; i++)
            {
                // Pass 1 allocates the memory, Pass 2 builds the interrupt
                instr = interruptTable[i] = new ProcessorOperations();
                instr.cycle = null;

                for (int pass = 0; pass < 2; pass++)
                {
                    int cycleCounter = -1;
                    if (pass != 0)
                    {
                        procCycle_tmp = instr.cycle;
                    }

                    switch (i)
                    {
                        case oRST:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(RSTRequest);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchOpcode;
                            }
                            break;

                        case oNMI:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushHighPC;
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushLowPC;
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQRequest);
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(NMIRequest);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(NMI1Request);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchOpcode;
                            }
                            break;

                        case oIRQ:
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_WasteCycle;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushHighPC;
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_PushLowPC;
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQRequest_sidplay_irq);
                                procCycle_tmp[cycleCounter].nosteal = true;
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQ1Request);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = new ProcessorCycle.FunctionDelegate(IRQ2Request);
                            }
                            cycleCounter++;
                            if (pass != 0)
                            {
                                procCycle_tmp[cycleCounter].func = dlg_FetchOpcode;
                            }
                            break;
                    }

                    cycleCounter++;
                    if (pass == 0)
                    {
                        // Pass 1 - Allocate Memory
                        if (cycleCounter != 0)
                        {
                            instr.cycle = new ProcessorCycle[cycleCounter];
                            procCycle_tmp = instr.cycle;
                            for (int c = 0; c < cycleCounter; c++)
                            {
                                procCycle_tmp[c] = new ProcessorCycle();
                                procCycle_tmp[c].nosteal = false;
                            }
                        }
                    }

                }
            }

            fetchCycle.func = dlg_FetchOpcode;
        }

        /// <summary>
        /// Reset CPU Emulation
        /// </summary>
        public virtual void reset()
        {
            // Reset Interrupts
            interrupts_pending = 0;
            interrupts_irqs = 0;

            // Internal Stuff
            Initialise();

            // Requires External Bits
            // Read from reset vector for program entry point
            Cycle_EffectiveAddress = SIDEndian.endian_16lo8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFC));
            Cycle_EffectiveAddress = SIDEndian.endian_16hi8(Cycle_EffectiveAddress, player.mem_readMemDataByte(0xFFFD));
            Register_ProgramCounter = Cycle_EffectiveAddress;
        }

        /// <summary>
        /// Handle bus access signals
        /// </summary>
        /// <param name="state"></param>
        public void aecSignal(bool state)
        {
            if (aec != state)
            {
                long clock = eventContext.getTime(m_extPhase);

                // If the CPU blocked waiting for the bus then schedule a retry.
                aec = state;
                if (state && m_blocked)
                {
                    // Correct IRQs that appeared before the steal
                    long stolen = clock - m_stealingClk;
                    interrupts_nmiClk += stolen;
                    interrupts_irqClk += stolen;
                    // IRQs that appeared during the steal must have their clocks corrected
                    if (interrupts_nmiClk > clock)
                    {
                        interrupts_nmiClk = clock - 1;
                    }
                    if (interrupts_irqClk > clock)
                    {
                        interrupts_irqClk = clock - 1;
                    }
                    m_blocked = false;
                }

                eventContext.schedule(cpuEvent, (eventContext.phase == m_phase ? 1 : 0), m_phase);
            }
        }

        // Non-standard functions

        public virtual void triggerRST()
        {
            interrupts_pending |= iRST;
        }

        public virtual void triggerNMI()
        {
            interrupts_pending |= iNMI;
            interrupts_nmiClk = eventContext.getTime(m_extPhase);
        }

        /// <summary>
        /// Level triggered interrupt
        /// </summary>
        public virtual void triggerIRQ()
        {
            // IRQ Suppressed
            if (!getFlagI())
            {
                interrupts_irqRequest = true;
            }
            if ((interrupts_irqs++ == 0))
            {
                interrupts_irqClk = eventContext.getTime(m_extPhase);
            }

#if DEBUG
            if (interrupts_irqs > iIRQSMAX)
            {
                throw new Exception("MOS6510 Error: too many IRQs");
            }
#endif
        }

        public void clearIRQ()
        {
            if (interrupts_irqs > 0)
            {
                if ((--interrupts_irqs) == 0)
                {
                    // Clear off the interrupts
                    interrupts_irqRequest = false;
                }
            }
        }

        // Status Register Routines
        // Set N and Z flags according to byte

        internal void setFlagsNZ(short x)
        {
            Register_z_Flag = (Register_n_Flag = x);
        }

        internal void setFlagN(short x)
        {
            Register_n_Flag = x;
        }

        internal void setFlagV(short x)
        {
            Register_v_Flag = x;
        }

        internal void setFlagD(short x)
        {
            Register_Status = (short)((Register_Status & (~(1 << SR_DECIMAL) & 0xff)) | ((((x) != 0) ? 1 : 0) << SR_DECIMAL));
        }

        internal void setFlagI(short x)
        {
            Register_Status = (short)((Register_Status & (~(1 << SR_INTERRUPT) & 0xff)) | ((((x) != 0) ? 1 : 0) << SR_INTERRUPT));
        }

        internal void setFlagZ(short x)
        {
            Register_z_Flag = x;
        }

        internal void setFlagC(short x)
        {
            Register_c_Flag = x;
        }

        internal bool getFlagN()
        {
            return (Register_n_Flag & (1 << SR_NEGATIVE)) != 0;
        }

        internal bool getFlagV()
        {
            return Register_v_Flag != 0;
        }

        internal bool getFlagD()
        {
            return (Register_Status & (1 << SR_DECIMAL)) != 0;
        }

        internal bool getFlagI()
        {
            return (Register_Status & (1 << SR_INTERRUPT)) != 0;
        }

        internal bool getFlagZ()
        {
            return Register_z_Flag == 0;
        }

        internal bool getFlagC()
        {
            return Register_c_Flag != 0;
        }

        // serializing
        public virtual void SaveToWriter(BinaryWriter writer, ProcessorCycle sid_event)
        {
            EventList.SaveEvent2Writer(cpuEvent, writer);

            writer.Write(aec);
            writer.Write(m_blocked);
            writer.Write(m_stealingClk);
            writer.Write(m_dbgClk);
            writer.Write((short)m_phase);
            writer.Write((short)m_extPhase);
            writer.Write(instrStartPC);
            writer.Write(instrOpcode);
            writer.Write(lastInstrCurrent);
            writer.Write(lastAddrCycle);
            writer.Write(cycleCount);
            writer.Write(Cycle_EffectiveAddress);
            writer.Write(Cycle_Data);
            writer.Write(Cycle_Pointer);
            writer.Write(Register_Accumulator);
            writer.Write(Register_X);
            writer.Write(Register_Y);
            writer.Write(Register_ProgramCounter);
            writer.Write(Register_Status);
            writer.Write(Register_c_Flag);
            writer.Write(Register_n_Flag);
            writer.Write(Register_v_Flag);
            writer.Write(Register_z_Flag);
            writer.Write(Register_StackPointer);
            writer.Write(interrupts_pending);
            writer.Write(interrupts_irqs);
            writer.Write(interrupts_nmiClk);
            writer.Write(interrupts_irqClk);
            writer.Write(interrupts_irqRequest);
            writer.Write(interrupts_irqLatch);

            if (procCycle.Length == 1)
            {
                if (procCycle[0] == fetchCycle)
                {
                    writer.Write((int)1);
                }
                else if (sid_event != null && procCycle[0] == sid_event)
                {
                    writer.Write((int)2);
                }
            }
            else
            {
                writer.Write((int)0);
            }
        }
        // deserializing
        public virtual void LoadFromReader(BinaryReader reader)
        {
            cpuEvent_id = reader.ReadInt32();

            aec = reader.ReadBoolean();
            m_blocked = reader.ReadBoolean();
            m_stealingClk = reader.ReadInt64();
            m_dbgClk = reader.ReadInt64();
            m_phase = (event_phase_t)reader.ReadInt16();
            m_extPhase = (event_phase_t)reader.ReadInt16();
            instrStartPC = reader.ReadInt32();
            instrOpcode = reader.ReadInt16();
            lastInstrCurrent = reader.ReadInt32();
            lastAddrCycle = reader.ReadByte();
            cycleCount = reader.ReadSByte();
            Cycle_EffectiveAddress = reader.ReadInt32();
            Cycle_Data = reader.ReadInt16();
            Cycle_Pointer = reader.ReadInt32();
            Register_Accumulator = reader.ReadInt16();
            Register_X = reader.ReadInt16();
            Register_Y = reader.ReadInt16();
            Register_ProgramCounter = reader.ReadInt64();
            Register_Status = reader.ReadInt16();
            Register_c_Flag = reader.ReadInt16();
            Register_n_Flag = reader.ReadInt16();
            Register_v_Flag = reader.ReadInt16();
            Register_z_Flag = reader.ReadInt16();
            Register_StackPointer = reader.ReadInt32();
            interrupts_pending = reader.ReadInt16();
            interrupts_irqs = reader.ReadInt16();
            interrupts_nmiClk = reader.ReadInt64();
            interrupts_irqClk = reader.ReadInt64();
            interrupts_irqRequest = reader.ReadBoolean();
            interrupts_irqLatch = reader.ReadBoolean();

            procCycle_id = reader.ReadInt32();
            if (procCycle_id == 1)
            {
                procCycle = new ProcessorCycle[] { fetchCycle };
            }
            else
            {
                procCycle = null;
            }

        }
    }
}