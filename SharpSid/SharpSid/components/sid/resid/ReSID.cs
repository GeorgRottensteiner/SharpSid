using System;
using System.IO;

namespace SharpSid
{
    public class ReSID : SIDEmu
    {
        private EventScheduler m_context;

        private event_phase_t m_phase;

        private SID m_sid;

        private long m_accessClk;

        private long m_gain;

        //private string m_error;

        private bool m_status;

        private bool m_locked;

        private byte m_optimisation;


        public ReSID()
            : base()
        {
            m_context = null;
            m_phase = event_phase_t.EVENT_CLOCK_PHI1;
            m_sid = new SID();
            m_gain = 100;
            m_status = true;
            m_locked = false;
            m_optimisation = 0;

            reset((short)0);
        }
        // only used for deserializing
        public ReSID(EventScheduler context, BinaryReader reader)
            : base()
        {
            m_context = context;
            LoadFromReader(context, reader);
        }


        // Standard component functions

        public override void reset()
        {
            base.reset();
        }

        public override void reset(short volume)
        {
            m_accessClk = 0;
            m_sid.reset();
            m_sid.write(0x18, volume);
        }

        public override short read(short addr)
        {
            long cycles = m_context.getTime(m_accessClk, m_phase);

            // > not for debug
            m_accessClk += cycles;
            if (m_optimisation != 0)
            {
                if (cycles != 0)
                {
                    m_sid.clock((int)cycles);
                }
            }
            else
            {
                while ((cycles--) != 0)
                {
                    m_sid.clock();
                }
            }
            // <
            return (short)m_sid.read(addr);
        }

        public override void write(short addr, short data)
        {
            long cycles = m_context.getTime(m_accessClk, m_phase);
            m_accessClk += cycles;
            if (m_optimisation != 0)
            {
                if (cycles != 0)
                {
                    m_sid.clock((int)cycles);
                }
            }
            else
            {
                while ((cycles--) != 0)
                {
                    m_sid.clock();
                }

            }
            m_sid.write(addr, data);
        }

        /*
        public string error()
        {
            return m_error;
        }
         */

        // Standard SID functions

        public override long output(short bits)
        {
            long cycles = m_context.getTime(m_accessClk, m_phase);
            m_accessClk += cycles;
            if (m_optimisation != 0)
            {
                if (cycles != 0)
                {
                    m_sid.clock((int)cycles);
                }
            }
            else
            {
                while ((cycles--) != 0)
                {
                    m_sid.clock();
                }

            }
            return m_sid.output(bits) * m_gain / 100;
        }

        public void filter(bool enable)
        {
            m_sid.enable_filter(enable);
        }

        public override void voice(short num, short volume, bool mute)
        {
            // At this time only mute is supported
            m_sid.mute(num, mute);
        }

        public override void gain(short percent)
        {
            // 0 to 99 is loss, 101 - 200 is gain
            m_gain = percent;
            m_gain += 100;
            if (m_gain > 200)
            {
                m_gain = 200;
            }
        }

        /// <summary>
        /// Set optimisation level
        /// </summary>
        /// <param name="level"></param>
        public override void optimisation(byte level)
        {
            m_optimisation = level;
        }

        public bool isOk
        {
            get
            {
                return m_status;
            }
        }

        // Specific to ReSID

        public void sampling(long freq)
        {
            m_sid.set_sampling_parameters(1000000, SIDDefs.sampling_method.SAMPLE_FAST, freq, -1, 0.79);
        }

        public bool filter(sid_filter_t filter)
        {
            int[][] fc = new int[0x802][];
            for (int i = 0; i <= fc.GetLength(0); i++)
            {
                fc[i] = new int[2];
            }

            int[][] f0 = fc;
            int points = 0;

            if (filter == null)
            {
                // Select default filter
                // m_sid.fc_default(f0, points);
                FCPoints fcp = new FCPoints();
                m_sid.fc_default(fcp);
                fc = fcp.points;
                points = fcp.count;
            }
            else
            {
                // Make sure there are enough filter points and they are legal
                points = filter.points;
                if ((points < 2) || (points > 0x800))
                {
                    return false;
                }

                {
                    int[] fstart = { -1, 0 };
                    int[] fprev = fstart;
                    int fin = 0;
                    int fout = 0;
                    // Last check, make sure they are list in numerical order for both axis
                    while (points-- > 0)
                    {
                        if ((fprev)[0] >= filter.cutoff[fin][0])
                        {
                            return false;
                        }
                        fout++;
                        fc[fout][0] = filter.cutoff[fin][0];
                        fc[fout][1] = filter.cutoff[fin][1];
                        fprev = filter.cutoff[fin++];
                    }
                    // Updated ReSID interpolate requires we repeat the end points
                    fc[fout + 1][0] = fc[fout][0];
                    fc[fout + 1][1] = fc[fout][1];
                    fc[0][0] = fc[1][0];
                    fc[0][1] = fc[1][1];
                    points = filter.points + 2;
                }
            }

            // function from reSID
            points--;
            m_sid.filter.interpolate(f0, 0, points, m_sid.fc_plotter(), 1.0);

            if (filter != null && filter.Lthreshold != 0)
            {
                m_sid.set_distortion_properties(filter.Lthreshold, filter.Lsteepness, filter.Llp, filter.Lbp, filter.Lhp, filter.Hthreshold, filter.Hsteepness, filter.Hlp, filter.Hbp, filter.Hhp);
            }

            return true;
        }

        /// <summary>
        /// Set the emulated SID model
        /// </summary>
        /// <param name="model"></param>
        public void model(SID2Types.sid2_model_t model)
        {
            if (model == SID2Types.sid2_model_t.SID2_MOS8580)
            {
                m_sid.set_chip_model(SIDDefs.chip_model.MOS8580);
            }
            else
            {
                m_sid.set_chip_model(SIDDefs.chip_model.MOS6581);
            }
        }

        // Must lock the SID before using the standard functions

        /// <summary>
        /// Set execution environment and lock sid to it
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool _lock(InternalPlayer player)
        {
            if (player == null)
            {
                if (!m_locked)
                {
                    return false;
                }
                m_locked = false;
                m_context = null;
            }
            else
            {
                if (m_locked)
                {
                    return false;
                }
                m_locked = true;
                m_context = player.m_scheduler;
            }
            return true;
        }


        public override SIDEmu.SIDEmuType GetEmuType()
        {
            return SIDEmuType.resid;
        }

        // serializing
        public override void SaveToWriter(BinaryWriter writer)
        {
            writer.Write((short)m_phase);
            writer.Write(m_accessClk);
            writer.Write(m_gain);
            writer.Write(m_status);
            writer.Write(m_locked);
            writer.Write(m_optimisation);
            m_sid.SaveToWriter(writer);
        }
        // deserializing
        protected override void LoadFromReader(EventScheduler context, BinaryReader reader)
        {
            m_phase = (event_phase_t)reader.ReadInt16();
            m_accessClk = reader.ReadInt64();
            m_gain = reader.ReadInt64();
            m_status = reader.ReadBoolean();
            m_locked = reader.ReadBoolean();
            m_optimisation = reader.ReadByte();
            m_sid = new SID(reader);
        }
    }
}