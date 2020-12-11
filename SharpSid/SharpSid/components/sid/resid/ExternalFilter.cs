using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// The audio output stage in a Commodore 64 consists of two STC networks, a
    /// low-pass filter with 3-dB frequency 16kHz followed by a high-pass filter with
    /// 3-dB frequency 16Hz (the latter provided an audio equipment input impedance
    /// of 1kOhm).
    /// 
    /// The STC networks are connected with a BJT supposedly meant to act as a unity
    /// gain buffer, which is not really how it works. A more elaborate model would
    /// include the BJT, however DC circuit analysis yields BJT base-emitter and
    /// emitter-base impedances sufficiently low to produce additional low-pass and
    /// high-pass 3dB-frequencies in the order of hundreds of kHz. This calls for a
    /// sampling frequency of several MHz, which is far too high for practical use.
    /// 
    /// @author Ken Händel
    /// </summary>
    public class ExternalFilter
    {
        /// <summary>
        /// Filter enabled
        /// </summary>
        protected bool enabled;

        /// <summary>
        /// Maximum mixer DC offset
        /// </summary>
        protected int mixer_DC;

        /// <summary>
        /// State of filters. lowpass
        /// </summary>
        protected int vlp;

        /// <summary>
        /// State of filters. highpass
        /// </summary>
        protected int vhp;

        /// <summary>
        /// State of filters
        /// </summary>
        internal int vo;

        /// <summary>
        /// Cutoff frequencies
        /// </summary>
        protected int w0lp;

        /// <summary>
        /// Cutoff frequencies
        /// </summary>
        protected int w0hp;

        /// <summary>
        /// SID clocking - 1 cycle
        /// </summary>
        /// <param name="Vi"></param>
        public void clock(int Vi)
        {
            // This is handy for testing.
            if (!enabled)
            {
                // Remove maximum DC level since there is no filter to do it.
                vlp = vhp = 0;
                vo = Vi - mixer_DC;
                return;
            }

            // delta_t is converted to seconds given a 1MHz clock by dividing
            // with 1 000 000.

            // Calculate filter outputs.
            // vo = vlp - vhp;
            // vlp = vlp + w0lp*(Vi - vlp)*delta_t;
            // vhp = vhp + w0hp*(vlp - vhp)*delta_t;

            int dVlp = (w0lp >> 8) * (Vi - vlp) >> 12;
            int dVhp = w0hp * (vlp - vhp) >> 20;
            vo = vlp - vhp;
            vlp += dVlp;
            vhp += dVhp;
        }

        /// <summary>
        /// SID clocking - delta_t cycles
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="Vi"></param>
        public void clock(int delta_t, int Vi)
        {
            // This is handy for testing.
            if (!enabled)
            {
                // Remove maximum DC level since there is no filter to do it.
                vlp = vhp = 0;
                vo = Vi - mixer_DC;
                return;
            }

            // Maximum delta cycles for the external filter to work satisfactorily
            // is approximately 8.
            int delta_t_flt = 8;

            while (delta_t != 0)
            {
                if (delta_t < delta_t_flt)
                {
                    delta_t_flt = delta_t;
                }

                // delta_t is converted to seconds given a 1MHz clock by dividing
                // with 1 000 000.

                // Calculate filter outputs.
                // vo = vlp - vhp;
                // vlp = vlp + w0lp*(Vi - vlp)*delta_t;
                // vhp = vhp + w0hp*(vlp - vhp)*delta_t;

                int dVlp = (w0lp * delta_t_flt >> 8) * (Vi - vlp) >> 12;
                int dVhp = w0hp * delta_t_flt * (vlp - vhp) >> 20;
                vo = vlp - vhp;
                vlp += dVlp;
                vhp += dVhp;

                delta_t -= delta_t_flt;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExternalFilter()
        {
            reset();
            enable_filter(true);
            set_sampling_parameter(15915.6);
            set_chip_model(SIDDefs.chip_model.MOS6581);
        }
        // only used for deserializing
        public ExternalFilter(BinaryReader reader)
        {
            LoadFromReader(reader);
        }


        /// <summary>
        /// Enable filter
        /// </summary>
        /// <param name="enable"></param>
        public void enable_filter(bool enable)
        {
            enabled = enable;
        }

        /// <summary>
        /// Setup of the external filter sampling parameters
        /// </summary>
        /// <param name="pass_freq"></param>
        public void set_sampling_parameter(double pass_freq)
        {
            double pi = 3.1415926535897932385;

            // Low-pass: R = 10kOhm, C = 1000pF; w0l = 1/RC = 1/(1e4*1e-9) = 100000
            // High-pass: R = 1kOhm, C = 10uF; w0h = 1/RC = 1/(1e3*1e-5) = 100
            // Multiply with 1.048576 to facilitate division by 1 000 000 by right-shifting 20 times (2 ^ 20 = 1048576).

            w0hp = 105;
            w0lp = (int)(pass_freq * (2.0 * pi * 1.048576));
            if (w0lp > 104858)
            {
                w0lp = 104858;
            }
        }

        public void set_chip_model(SIDDefs.chip_model model)
        {
            if (model == SIDDefs.chip_model.MOS6581)
            {
                // Maximum mixer DC output level; to be removed if the external
                // filter is turned off: ((wave DC + voice DC) * voices + mixer DC) * volume
                // See Voice.cs and Filter.cs for an explanation of the values.
                mixer_DC = ((((0x800 - 0x380) + 0x800) * 0xff * 3 - 0xfff * 0xff / 18) >> 7) * 0x0f;
            }
            else
            {
                // No DC offsets in the MOS8580.
                mixer_DC = 0;
            }
        }

        public void reset()
        {
            // State of filter.
            vlp = 0;
            vhp = 0;
            vo = 0;
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            writer.Write(enabled);
            writer.Write(mixer_DC);
            writer.Write(vlp);
            writer.Write(vhp);
            writer.Write(vo);
            writer.Write(w0lp);
            writer.Write(w0hp);
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            enabled = reader.ReadBoolean();
            mixer_DC = reader.ReadInt32();
            vlp = reader.ReadInt32();
            vhp = reader.ReadInt32();
            vo = reader.ReadInt32();
            w0lp = reader.ReadInt32();
            w0hp = reader.ReadInt32();
        }
    }
}