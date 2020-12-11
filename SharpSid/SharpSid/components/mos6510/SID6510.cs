using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// Sidplay Specials
    /// 
    /// @author Ken Händel
    /// </summary>
    public class SID6510 : MOS6510
    {
        private bool m_sleeping;

        private SID2Types.sid2_env_t m_mode;

        private long m_delayClk;

        private bool m_framelock;

        internal ProcessorCycle delayCycle = new ProcessorCycle();

        public SID6510(EventScheduler context, InternalPlayer owner)
            : base(context, owner)
        {
            m_mode = SID2Types.sid2_env_t.sid2_envR;
            m_framelock = false;

            // The hacks for de.quippy.sidplay.sidplay are done with overridden methods of MOS6510

            // Used to insert busy delays into the CPU emulation
            delayCycle.func = new ProcessorCycle.FunctionDelegate(sid_delay);
        }

        public SID6510(EventScheduler context, InternalPlayer owner, BinaryReader reader, EventList events)
            : base(context, owner, reader, events)
        {
            delayCycle.func = new ProcessorCycle.FunctionDelegate(sid_delay);

            if (procCycle_id == 2)
            {
                procCycle = new ProcessorCycle[] { delayCycle };
            }
        }

        // Standard Functions

        public override void reset()
        {
            m_sleeping = false;
            // Call inherited reset
            base.reset();
        }

        public void reset(int pc, short a, short x, short y)
        {
            // Reset the processor
            reset();

            // Registers not touched by a reset
            Register_Accumulator = a;
            Register_X = x;
            Register_Y = y;
            Register_ProgramCounter = pc;
        }

        public void environment(SID2Types.sid2_env_t mode)
        {
            m_mode = mode;
        }

        // Sidplay compatibility interrupts. Basically wakes CPU if it is m_sleeping

        public override void triggerRST()
        {
            // All modes
            base.triggerRST();
            if (m_sleeping)
            {
                m_sleeping = false;
                eventContext.schedule(cpuEvent, (eventContext.phase == m_phase) ? 1 : 0, m_phase);
            }
        }

        public override void triggerNMI()
        {
            // Only in Real C64 mode
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                base.triggerNMI();
                if (m_sleeping)
                {
                    m_sleeping = false;
                    eventContext.schedule(cpuEvent, (eventContext.phase == m_phase) ? 1 : 0, m_phase);
                }
            }
        }

        public override void triggerIRQ()
        {
            switch (m_mode)
            {
                default:
                    return;
                case SID2Types.sid2_env_t.sid2_envR:
                    base.triggerIRQ();
                    if (m_sleeping)
                    {
                        // Simulate busy loop
                        m_sleeping = !(interrupts_irqRequest || (interrupts_pending != 0));
                        if (!m_sleeping)
                        {
                            eventContext.schedule(cpuEvent, (eventContext.phase == m_phase) ? 1 : 0, m_phase);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Send CPU is about to sleep. Only a reset or interrupt will wake up the processor
        /// </summary>
        public void sleep()
        {
            // Simulate a delay for JMPw
            m_delayClk = m_stealingClk = eventContext.getTime(m_phase);
            procCycle = new ProcessorCycle[] { delayCycle };
            cycleCount = 0;
            m_sleeping = !(interrupts_irqRequest || (interrupts_pending != 0));
            player.Sleep();
        }

        // Ok start all the hacks for de.quippy.sidplay.sidplay. This prevents
        // execution of code in roms. For real c64 emulation
        // create object from base class! Also stops code
        // rom execution when bad code switches roms in over
        // itself.

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: Suppresses Illegal Instructions
        /// </summary>
        internal override void illegal_instr()
        {
            sid_illegal();
        }

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: Stop jumps into ROM code
        /// </summary>
        internal override void jmp_instr()
        {
            sid_jmp();
        }

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: No overlapping IRQs allowed
        /// </summary>
        internal override void cli_instr()
        {
            sid_cli();
        }

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: 
        /// Since no real IRQs, all RTIs mapped to RTS Required for fix bad tunes in old modes
        /// </summary>
        internal override void PopSR_sidplay_rti()
        {
            sid_rti();
        }

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: Support of sidplays BRK functionality
        /// </summary>
        internal override void PushHighPC_sidplay_brk()
        {
            sid_brk();
        }

        /// <summary>
        /// Hack for de.quippy.sidplay.sidplay: RTI behaves like RTI in sidplay1 modes
        /// </summary>
        internal override void IRQRequest_sidplay_irq()
        {
            sid_irq();
        }

        internal override void FetchOpcode()
        {
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                base.FetchOpcode();
                return;
            }

            // Sid tunes end by wrapping the stack. For compatibility it has to be handled.
            m_sleeping |= (SIDEndian.endian_16hi8(Register_StackPointer) != SP_PAGE);
            m_sleeping |= (SIDEndian.endian_32hi16(Register_ProgramCounter) != 0);
            if (!m_sleeping)
            {
                base.FetchOpcode();
            }

            if (m_framelock == false)
            {
                int timeout = 6000000;
                m_framelock = true;
                // Simulate sidplay1 frame based execution
                while (!m_sleeping && (timeout != 0))
                {
                    base.clock();
                    timeout--;
                }
                if (timeout == 0)
                {
                    player.Reset(true);
                }
                sleep();
                m_framelock = false;
            }
        }

        // For de.quippy.sidplay.sidplay compatibility implement those instructions which don't behave properly

        /// <summary>
        /// Sidplay Suppresses Illegal Instructions
        /// </summary>
        private void sid_illegal()
        {
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                base.illegal_instr();
                return;
            }
        }

        internal void sid_delay()
        {
            long stolen = eventContext.getTime(m_stealingClk, m_phase);
            long delayed = eventContext.getTime(m_delayClk, m_phase);

            // Check for stealing. The relative clock cycle
            // differences are compared here rather than the
            // clocks directly. This means we don't have to
            // worry about the clocks wrapping
            if (delayed > stolen)
            {
                // No longer stealing so adjust clock
                delayed -= stolen;
                m_delayClk += stolen;
                m_stealingClk = m_delayClk;
            }

            cycleCount--;
            // Woken from sleep just to handle the stealing release
            if (m_sleeping)
            {
                eventContext.cancel(cpuEvent);
            }
            else
            {
                long cycle = delayed % 3;
                if (cycle == 0)
                {
                    if (interruptPending())
                    {
                        return;
                    }
                }
                eventContext.schedule(cpuEvent, 3 - cycle, m_phase);
            }
        }

        private void sid_brk()
        {
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                base.PushHighPC();
                return;
            }

            sei_instr();
#if !NO_RTS_UPON_BRK
            sid_rts();
#endif
            FetchOpcode();
        }

        private void sid_jmp()
        {
            // For de.quippy.sidplay.sidplay compatibility, inherited from environment
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                // If a busy loop then just sleep
                if (Cycle_EffectiveAddress == instrStartPC)
                {
                    Register_ProgramCounter = SIDEndian.endian_32lo16(Register_ProgramCounter, Cycle_EffectiveAddress);
                    if (!interruptPending())
                    {
                        this.sleep();
                    }
                }
                else
                {
                    base.jmp_instr();
                }
                return;
            }

            if (player.CheckBankJump(Cycle_EffectiveAddress))
            {
                base.jmp_instr();
            }
            else
            {
                sid_rts();
            }
        }

        /// <summary>
        /// Will do a full rts in 1 cycle, to destroy current function and quit
        /// </summary>
        private void sid_rts()
        {
            PopLowPC();
            PopHighPC();
            rts_instr();
        }

        private void sid_cli()
        {
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                base.cli_instr();
            }
        }

        private void sid_rti()
        {
            if (m_mode == SID2Types.sid2_env_t.sid2_envR)
            {
                PopSR();
                return;
            }

            // Fake RTS
            sid_rts();
            FetchOpcode();
        }

        private void sid_irq()
        {
            base.IRQRequest();
            if (m_mode != SID2Types.sid2_env_t.sid2_envR)
            {
                // RTI behaves like RTI in sidplay1 modes
                Register_StackPointer++;
            }
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer, ProcessorCycle sid_delay)
        {
            base.SaveToWriter(writer, sid_delay);

            writer.Write(m_sleeping);
            writer.Write((short)m_mode);
            writer.Write(m_delayClk);
            writer.Write(m_framelock);
        }
        // deserializing
        public override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);

            m_sleeping = reader.ReadBoolean();
            m_mode = (SID2Types.sid2_env_t)reader.ReadInt16();
            m_delayClk = reader.ReadInt64();
            m_framelock = reader.ReadBoolean();
        }
    }
}