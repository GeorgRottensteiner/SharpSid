using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// References below are from:
    /// The MOS 6567/6569 video controller (VIC-II) and its application in the
    /// Commodore 64 http://www.uni-mainz.de/~bauec002/VIC-Article.gz
    /// 
    /// @author Ken Händel
    /// </summary>
    public abstract class MOS656X : Event
    {
        public enum mos656x_model_t
        {
            MOS6567R56A, // OLD NTSC CHIP
            MOS6567R8,   // NTSC
            MOS6569,     // PAL
        }

        public const int MOS6567R56A_SCREEN_HEIGHT = 262;
        public const int MOS6567R56A_SCREEN_WIDTH = 64;
        public const int MOS6567R56A_FIRST_DMA_LINE = 0x30;
        public const int MOS6567R56A_LAST_DMA_LINE = 0xf7;

        public const int MOS6567R8_SCREEN_HEIGHT = 263;
        public const int MOS6567R8_SCREEN_WIDTH = 65;
        public const int MOS6567R8_FIRST_DMA_LINE = 0x30;
        public const int MOS6567R8_LAST_DMA_LINE = 0xf7;

        public const int MOS6569_SCREEN_HEIGHT = 312;
        public const int MOS6569_SCREEN_WIDTH = 63;
        public const int MOS6569_FIRST_DMA_LINE = 0x30;
        public const int MOS6569_LAST_DMA_LINE = 0xf7;


        protected EventScheduler event_context;

        // Optional information

        protected short[] regs = new short[0x40];

        protected short icr, idr, ctrl1;

        protected int yrasters, xrasters, raster_irq;

        protected int raster_x, raster_y;

        protected int first_dma_line, last_dma_line, y_scroll;

        protected bool bad_lines_enabled, bad_line;

        protected bool vblanking;

        protected bool lp_triggered;

        protected short lpx, lpy;

        protected short sprite_dma, sprite_expand_y;

        protected short[] sprite_mc_base = new short[8];

        protected long m_rasterClk;

        protected event_phase_t m_phase;

        protected MOS656X(EventScheduler context)
            : base("VIC Raster")
        {
            event_context = context;
            m_phase = event_phase_t.EVENT_CLOCK_PHI1;
            chip(mos656x_model_t.MOS6569);
        }
        // only used for deserializing
        protected MOS656X(EventScheduler context, BinaryReader reader, int newid)
            : base(context, reader, newid)
        {
            event_context = context;
        }

        public override void _event()
        {
            long cycles = event_context.getTime(m_rasterClk, event_context.phase);

            // Cycle already executed check
            if (cycles == 0)
            {
                return;
            }

            long delay = 1;
            int cycle;

            // Update x raster
            m_rasterClk += cycles;
            raster_x += (int)cycles;
            cycle = (raster_x + 9) % xrasters;
            raster_x %= xrasters;

            switch (cycle)
            {
                case 0:
                    {
                        // Calculate sprite DMA
                        short y = (short)(raster_y & 0xff);
                        short mask = 1;
                        sprite_expand_y ^= regs[0x17];
                        for (int i = 1; i < 0x10; i += 2, mask <<= 1)
                        {
                            if (((regs[0x15] & mask) != 0) && (y == regs[i]))
                            {
                                sprite_dma |= mask;
                                sprite_mc_base[i >> 1] = 0;
                                sprite_expand_y &= (short)(~(regs[0x17] & mask) & 0xff);
                            }
                        }

                        delay = 2;
                        if ((sprite_dma & 0x01) != 0)
                        {
                            addrctrl(false);
                        }
                        else
                        {
                            addrctrl(true);
                            // No sprites before next compulsory cycle
                            if ((sprite_dma & 0x1f) == 0)
                            {
                                delay = 9;
                            }
                        }
                        break;
                    }

                case 1:
                    break;

                case 2:
                    if ((sprite_dma & 0x02) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 3:
                    if ((sprite_dma & 0x03) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 4:
                    if ((sprite_dma & 0x04) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 5:
                    if ((sprite_dma & 0x06) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 6:
                    if ((sprite_dma & 0x08) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 7:
                    if ((sprite_dma & 0x0c) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 8:
                    if ((sprite_dma & 0x10) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 9: // IRQ occurred (xraster != 0)
                    if (raster_y == (yrasters - 1))
                    {
                        vblanking = true;
                    }
                    else
                    {
                        raster_y++;
                        // Trigger raster IRQ if IRQ line reached
                        if (raster_y == raster_irq)
                        {
                            trigger(MOS656X_INTERRUPT_RST);
                        }
                    }
                    if ((sprite_dma & 0x18) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 10: // Vertical blank (line 0)
                    if (vblanking)
                    {
                        vblanking = lp_triggered = false;
                        raster_y = 0;
                        // Trigger raster IRQ if IRQ in line 0
                        if (raster_irq == 0)
                        {
                            trigger(MOS656X_INTERRUPT_RST);
                        }
                    }
                    if ((sprite_dma & 0x20) != 0)
                    {
                        addrctrl(false);
                    }
                    // No sprites before next compulsory cycle
                    else if ((sprite_dma & 0xf8) == 0)
                    {
                        delay = 10;
                    }
                    break;

                case 11:
                    if ((sprite_dma & 0x30) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 12:
                    if ((sprite_dma & 0x40) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 13:
                    if ((sprite_dma & 0x60) == 0)
                    {
                        addrctrl(true);
                    }
                    break;

                case 14:
                    if ((sprite_dma & 0x80) != 0)
                    {
                        addrctrl(false);
                    }
                    break;

                case 15:
                    delay = 2;
                    if ((sprite_dma & 0xc0) == 0)
                    {
                        addrctrl(true);
                        delay = 5;
                    }
                    break;

                case 16:
                    break;

                case 17:
                    delay = 2;
                    if ((sprite_dma & 0x80) == 0)
                    {
                        addrctrl(true);
                        delay = 3;
                    }
                    break;

                case 18:
                    break;

                case 19:
                    addrctrl(true);
                    break;

                case 20: // Start bad line
                    {
                        // In line $30, the DEN bit controls if Bad Lines can occur
                        if (raster_y == first_dma_line)
                        {
                            bad_lines_enabled = (ctrl1 & 0x10) != 0;
                        }

                        // Test for bad line condition
                        bad_line = (raster_y >= first_dma_line) && (raster_y <= last_dma_line) && ((raster_y & 7) == y_scroll) && bad_lines_enabled;

                        if (bad_line)
                        {
                            // DMA starts on cycle 23
                            addrctrl(false);
                        }
                        delay = 3;
                        break;
                    }

                case 23:
                    {
                        for (int i = 0; i < sprite_mc_base.Length; i++)//for (int i = 0; i < 8; i++)
                        {
                            if ((sprite_expand_y & (1 << i)) != 0)
                            {
                                sprite_mc_base[i] += 2;
                            }
                        }
                        break;
                    }

                case 24:
                    {
                        short mask = 1;
                        for (int i = 0; i < sprite_mc_base.Length; i++, mask <<= 1)//for (int i = 0; i < 8; i++, mask <<= 1)
                        {
                            if ((sprite_expand_y & mask) != 0)
                            {
                                sprite_mc_base[i]++;
                            }
                            if ((sprite_mc_base[i] & 0x3f) == 0x3f)
                            {
                                sprite_dma &= (short)(~mask & 0xff);
                            }
                        }
                        delay = 39;
                        break;
                    }

                case 63: // End DMA - Only get here for non PAL
                    addrctrl(true);
                    delay = xrasters - cycle;
                    break;

                default:
                    if (cycle < 23)
                    {
                        delay = 23 - cycle;
                    }
                    else if (cycle < 63)
                    {
                        delay = 63 - cycle;
                    }
                    else
                    {
                        delay = xrasters - cycle;
                    }
                    break;
            }

            event_context.schedule(this, delay - (event_context.phase == event_phase_t.EVENT_CLOCK_PHI1 ? 0 : 1), m_phase);
        }

        protected void trigger(int irq)
        {
            if (irq == 0)
            {
                // Clear any requested IRQs
                if ((idr & MOS656X_INTERRUPT_REQUEST) != 0)
                {
                    interrupt(false);
                }
                idr = 0;
                return;
            }

            idr |= (short)irq;
            if ((icr & idr) != 0)
            {
                if ((idr & MOS656X_INTERRUPT_REQUEST) == 0)
                {
                    idr |= MOS656X_INTERRUPT_REQUEST;
                    interrupt(true);
                }
            }
        }

        // Environment Interface

        protected abstract void interrupt(bool state);

        protected abstract void addrctrl(bool state);

        public void chip(mos656x_model_t model)
        {
            switch (model)
            {
                // Seems to be an older NTSC chip
                case mos656x_model_t.MOS6567R56A:
                    yrasters = MOS6567R56A_SCREEN_HEIGHT;
                    xrasters = MOS6567R56A_SCREEN_WIDTH;
                    first_dma_line = MOS6567R56A_FIRST_DMA_LINE;
                    last_dma_line = MOS6567R56A_LAST_DMA_LINE;
                    break;

                // NTSC Chip
                case mos656x_model_t.MOS6567R8:
                    yrasters = MOS6567R8_SCREEN_HEIGHT;
                    xrasters = MOS6567R8_SCREEN_WIDTH;
                    first_dma_line = MOS6567R8_FIRST_DMA_LINE;
                    last_dma_line = MOS6567R8_LAST_DMA_LINE;
                    break;

                // PAL Chip
                case mos656x_model_t.MOS6569:
                    yrasters = MOS6569_SCREEN_HEIGHT;
                    xrasters = MOS6569_SCREEN_WIDTH;
                    first_dma_line = MOS6569_FIRST_DMA_LINE;
                    last_dma_line = MOS6569_LAST_DMA_LINE;
                    break;
            }

            reset();
        }

        /// <summary>
        /// Handle light pen trigger
        /// </summary>
        public void lightpen()
        {
            // Synchronise simulation
            _event();

            if (!lp_triggered)
            {
                // Latch current coordinates
                lpx = (short)(raster_x << 2);
                lpy = (short)(raster_y & 0xff);
                trigger(MOS656X_INTERRUPT_LP);
            }
        }

        // Component Standard Calls

        public void reset()
        {
            icr = idr = ctrl1 = 0;
            raster_irq = 0;
            y_scroll = 0;
            raster_y = yrasters - 1;
            raster_x = 0;
            bad_lines_enabled = false;
            m_rasterClk = 0;
            vblanking = lp_triggered = false;
            lpx = lpy = 0;
            sprite_dma = 0;
            sprite_expand_y = 0xff;
            for (int i = 0; i < regs.Length; i++)
            {
                regs[i] = 0;
            }
            for (int i = 0; i < sprite_mc_base.Length; i++)
            {
                sprite_mc_base[i] = 0;
            }
            event_context.schedule(this, 0, m_phase);
        }

        public short read(short addr)
        {
            if (addr > 0x3f)
            {
                return 0;
            }
            if (addr > 0x2e)
            {
                return 0xff;
            }

            // Sync up timers
            _event();

            switch (addr)
            {
                case 0x11: // Control register 1
                    return (short)((ctrl1 & 0x7f) | ((raster_y & 0x100) >> 1));
                case 0x12: // Raster counter
                    return (short)(raster_y & 0xFF);
                case 0x13:
                    return lpx;
                case 0x14:
                    return lpy;
                case 0x19: // IRQ flags
                    return idr;
                case 0x1a: // IRQ mask
                    return (short)(icr | 0xf0);
                default:
                    return regs[addr];
            }
        }

        public void write(short addr, short data)
        {
            if (addr > 0x3f)
            {
                return;
            }

            regs[addr] = data;

            // Sync up timers
            _event();

            switch (addr)
            {
                case 0x11: // Control register 1
                    {
                        raster_irq = SIDEndian.endian_16hi8(raster_irq, (short)(data >> 7));
                        ctrl1 = data;
                        y_scroll = data & 7;

                        if (raster_x < 11)
                        {
                            break;
                        }

                        // In line $30, the DEN bit controls if Bad Lines can occur
                        if ((raster_y == first_dma_line) && ((data & 0x10) != 0))
                        {
                            bad_lines_enabled = true;
                        }

                        // Bad Line condition?
                        bad_line = (raster_y >= first_dma_line) && (raster_y <= last_dma_line) && ((raster_y & 7) == y_scroll) && bad_lines_enabled;

                        // Start bad dma line now
                        if (bad_line && (raster_x < 53))
                        {
                            addrctrl(false);
                        }
                        break;
                    }

                case 0x12: // Raster counter
                    raster_irq = SIDEndian.endian_16lo8(raster_irq, data);
                    break;

                case 0x17:
                    sprite_expand_y |= (short)(~data & 0xff);
                    break;

                case 0x19: // IRQ flags
                    idr &= (short)((~data & 0x0f) | 0x80);
                    if (idr == 0x80)
                    {
                        trigger(0);
                    }
                    break;

                case 0x1a: // IRQ mask
                    icr = (short)(data & 0x0f);
                    trigger(icr & idr);
                    break;
            }
        }

        // ----------------------------------------------------------------------------
        // Inline functions.
        // ----------------------------------------------------------------------------

        public const int MOS656X_INTERRUPT_RST = 1 << 0;

        public const int MOS656X_INTERRUPT_LP = 1 << 3;

        public const int MOS656X_INTERRUPT_REQUEST = 1 << 7;

        // ----------------------------------------------------------------------------
        // END Inline functions.
        // ----------------------------------------------------------------------------

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            base.SaveToWriter(writer);

            for (int i = 0; i < 0x40; i++)
            {
                writer.Write(regs[i]);
            }
            writer.Write(icr);
            writer.Write(idr);
            writer.Write(ctrl1);
            writer.Write(yrasters);
            writer.Write(xrasters);
            writer.Write(raster_irq);
            writer.Write(raster_x);
            writer.Write(raster_y);
            writer.Write(first_dma_line);
            writer.Write(last_dma_line);
            writer.Write(y_scroll);
            writer.Write(bad_lines_enabled);
            writer.Write(bad_line);
            writer.Write(vblanking);
            writer.Write(lp_triggered);
            writer.Write(lpx);
            writer.Write(lpy);
            writer.Write(sprite_dma);
            writer.Write(sprite_expand_y);
            for (int i = 0; i < 8; i++)
            {
                writer.Write(sprite_mc_base[i]);
            }
            writer.Write(m_rasterClk);
            writer.Write((short)m_phase);
        }
        // deserializing
        protected override void LoadFromReader(BinaryReader reader)
        {
            base.LoadFromReader(reader);

            for (int i = 0; i < 0x40; i++)
            {
                regs[i] = reader.ReadInt16();
            }

            icr = reader.ReadInt16();
            idr = reader.ReadInt16();
            ctrl1 = reader.ReadInt16();
            yrasters = reader.ReadInt32();
            xrasters = reader.ReadInt32();
            raster_irq = reader.ReadInt32();
            raster_x = reader.ReadInt32();
            raster_y = reader.ReadInt32();
            first_dma_line = reader.ReadInt32();
            last_dma_line = reader.ReadInt32();
            y_scroll = reader.ReadInt32();
            bad_lines_enabled = reader.ReadBoolean();
            bad_line = reader.ReadBoolean();
            vblanking = reader.ReadBoolean();
            lp_triggered = reader.ReadBoolean();
            lpx = reader.ReadInt16();
            lpy = reader.ReadInt16();
            sprite_dma = reader.ReadInt16();
            sprite_expand_y = reader.ReadInt16();
            for (int i = 0; i < 8; i++)
            {
                sprite_mc_base[i] = reader.ReadInt16();
            }
            m_rasterClk = reader.ReadInt64();
            m_phase = (event_phase_t)reader.ReadInt16();
        }
    }
}