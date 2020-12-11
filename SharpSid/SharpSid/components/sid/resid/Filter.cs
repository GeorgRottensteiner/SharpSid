﻿using System;
using System.IO;

namespace SharpSid
{
    /// <summary>
    /// The SID filter is modeled with a two-integrator-loop biquadratic filter,
    /// which has been confirmed by Bob Yannes to be the actual circuit used in the
    /// SID chip.
    /// 
    /// Measurements show that excellent emulation of the SID filter is achieved,
    /// except when high resonance is combined with high sustain levels. In this case
    /// the SID op-amps are performing less than ideally and are causing some
    /// peculiar behavior of the SID filter. This however seems to have more effect
    /// on the overall amplitude than on the color of the sound.
    /// 
    /// The theory for the filter circuit can be found in "Microelectric Circuits" by
    /// Adel S. Sedra and Kenneth C. Smith. The circuit is modeled based on the
    /// explanation found there except that an additional inverter is used in the
    /// feedback from the bandpass output, allowing the summer op-amp to operate in
    /// single-ended mode. This yields inverted filter outputs with levels
    /// independent of Q, which corresponds with the results obtained from a real
    /// SID.
    /// 
    /// We have been able to model the summer and the two integrators of the circuit
    /// to form components of an IIR filter. vhp is the output of the summer, vbp is
    /// the output of the first integrator, and vlp is the output of the second
    /// integrator in the filter circuit.
    /// 
    /// According to Bob Yannes, the active stages of the SID filter are not really
    /// op-amps. Rather, simple NMOS inverters are used. By biasing an inverter into
    /// its region of quasi-linear operation using a feedback resistor from input to
    /// output, a MOS inverter can be made to act like an op-amp for small signals
    /// centered around the switching threshold.
    /// 
    /// Qualified guesses at SID filter schematics are depicted below.
    /// 
    /// SID filter
    /// ----------
    /// 
    ///     -----------------------------------------------
    ///    |                                               |
    ///    |            ---Rq--                            |
    ///    |           |       |                           |
    ///    |  ------------<A]-----R1---------              |
    ///    | |                               |             |
    ///    | |                        ---C---|      ---C---|
    ///    | |                       |       |     |       |
    ///    |  --R1--    ---R1--      |---Rs--|     |---Rs--| 
    ///    |        |  |       |     |       |     |       |
    ///     ----R1--|-----[A>--|--R-----[A>--|--R-----[A>--|
    ///             |          |             |             |
    /// vi -----R1--           |             |             |
    /// 
    ///                       vhp           vbp           vlp
    /// 
    /// 
    /// vi  - input voltage
    /// vhp - highpass output
    /// vbp - bandpass output
    /// vlp - lowpass output
    /// [A> - op-amp
    /// R1  - summer resistor
    /// Rq  - resistor array controlling resonance (4 resistors)
    /// R   - NMOS FET voltage controlled resistor controlling cutoff frequency
    /// Rs  - shunt resitor
    /// C   - capacitor
    /// 
    /// 
    /// 
    /// SID integrator
    /// --------------
    /// 
    ///                                   V+
    /// 
    ///                                   |
    ///                                   |
    ///                              -----|
    ///                             |     |
    ///                             | ||--
    ///                              -||
    ///                   ---C---     ||->
    ///                  |       |        |
    ///                  |---Rs-----------|---- vo
    ///                  |                |
    ///                  |            ||--
    /// vi ----     -----|------------||
    ///        |   ^     |            ||->
    ///        |___|     |                |
    ///        -----     |                |
    ///          |       |                |
    ///          |---R2--                 |
    ///          |
    ///          R1                       V-
    ///          |
    ///          |
    /// 
    ///          Vw
    /// ----------------------------------------------------------------------------
    /// 
    /// @author Ken Händel
    /// </summary>
    public class Filter
    {
        /// <summary>
        /// Filter enabled
        /// </summary>
        protected bool enabled;

        /// <summary>
        /// Filter cutoff frequency
        /// </summary>
        internal int fc;

        /// <summary>
        /// Filter resonance
        /// </summary>
        internal int res;

        /// <summary>
        /// Selects which inputs to route through filter
        /// </summary>
        internal int filt;

        /// <summary>
        /// Switch voice 3 off
        /// </summary>
        internal int voice3off;

        /// <summary>
        /// Highpass, bandpass, and lowpass filter modes
        /// </summary>
        internal int hp_bp_lp;

        /// <summary>
        /// Output master volume
        /// </summary>
        internal int vol;

        /// <summary>
        /// Mixer DC offset
        /// </summary>
        protected int mixer_DC;

        /// <summary>
        /// State of filter. highpass
        /// </summary>
        protected int vhp;

        /// <summary>
        /// State of filter. bandpass
        /// </summary>
        protected int vbp;

        /// <summary>
        /// State of filter. lowpass
        /// </summary>
        protected int vlp;

        /// <summary>
        /// State of filter. not filtered
        /// </summary>
        protected int vnf;

        // when to begin, how fast it grows
        int DLthreshold, DLsteepness;
        int DHthreshold, DHsteepness;
        int DLlp, DLbp, DLhp; // coefficients, 256 = 1.0
        int DHlp, DHbp, DHhp;

        // Cutoff frequency, resonance.
        protected int w0, w0_ceil_1, w0_ceil_dt;

        protected int _1024_div_Q;

        protected int f0_count;

        /// <summary>
        /// Cutoff frequency tables. FC is an 11 bit register
        /// </summary>
        protected int[] f0_6581 = new int[2048];

        /// <summary>
        /// Cutoff frequency tables. FC is an 11 bit register
        /// </summary>
        protected int[] f0_8580 = new int[2048];

        protected int[] f0;

        protected int[][] f0_points;




        /// <summary>
        /// Maximum cutoff frequency is specified as FCmax = 2.6e-5/C =
        /// 2.6e-5/2200e-12 = 11818.
        /// 
        /// Measurements indicate a cutoff frequency range of approximately 220Hz -
        /// 18kHz on a MOS6581 fitted with 470pF capacitors. The function mapping FC
        /// to cutoff frequency has the shape of the tanh function, with a
        /// discontinuity at FCHI = 0x80. In contrast, the MOS8580 almost perfectly
        /// corresponds with the specification of a linear mapping from 30Hz to
        /// 12kHz.
        /// 
        /// The mappings have been measured by feeding the SID with an external
        /// signal since the chip itself is incapable of generating waveforms of
        /// higher fundamental frequency than 4kHz. It is best to use the bandpass
        /// output at full resonance to pick out the cutoff frequency at any given FC
        /// setting.
        /// 
        /// The mapping function is specified with spline interpolation points and
        /// the function values are retrieved via table lookup.
        /// 
        /// NB! Cutoff frequency characteristics may vary, we have modeled two
        /// particular Commodore 64s.
        /// </summary>
        protected static int[][] f0_points_6581 = {
                // -----FC----f-------FCHI-FCLO
                // ----------------------------
                new int[] { 0, 220 }, // 0x00 - repeated end point
                new int[] { 0, 220 }, // 0x00
                new int[] { 128, 230 }, // 0x10
                new int[] { 256, 250 }, // 0x20
                new int[] { 384, 300 }, // 0x30
                new int[] { 512, 420 }, // 0x40
                new int[] { 640, 780 }, // 0x50
                new int[] { 768, 1600 }, // 0x60
                new int[] { 832, 2300 }, // 0x68
                new int[] { 896, 3200 }, // 0x70
                new int[] { 960, 4300 }, // 0x78
                new int[] { 992, 5000 }, // 0x7c
                new int[] { 1008, 5400 }, // 0x7e
                new int[] { 1016, 5700 }, // 0x7f
                new int[] { 1023, 6000 }, // 0x7f 0x07
                new int[] { 1023, 6000 }, // 0x7f 0x07 - discontinuity
                new int[] { 1024, 4600 }, // 0x80 -
                new int[] { 1024, 4600 }, // 0x80
                new int[] { 1032, 4800 }, // 0x81
                new int[] { 1056, 5300 }, // 0x84
                new int[] { 1088, 6000 }, // 0x88
                new int[] { 1120, 6600 }, // 0x8c
                new int[] { 1152, 7200 }, // 0x90
                new int[] { 1280, 9500 }, // 0xa0
                new int[] { 1408, 12000 }, // 0xb0
                new int[] { 1536, 14500 }, // 0xc0
                new int[] { 1664, 16000 }, // 0xd0
                new int[] { 1792, 17100 }, // 0xe0
                new int[] { 1920, 17700 }, // 0xf0
                new int[] { 2047, 18000 }, // 0xff 0x07
                new int[] { 2047, 18000 } // 0xff 0x07 - repeated end point
        };

        /// <summary>
        /// Maximum cutoff frequency is specified as FCmax = 2.6e-5/C =
        /// 2.6e-5/2200e-12 = 11818.
        /// 
        /// Measurements indicate a cutoff frequency range of approximately 220Hz -
        /// 18kHz on a MOS6581 fitted with 470pF capacitors. The function mapping FC
        /// to cutoff frequency has the shape of the tanh function, with a
        /// discontinuity at FCHI = 0x80. In contrast, the MOS8580 almost perfectly
        /// corresponds with the specification of a linear mapping from 30Hz to
        /// 12kHz.
        /// 
        /// The mappings have been measured by feeding the SID with an external
        /// signal since the chip itself is incapable of generating waveforms of
        /// higher fundamental frequency than 4kHz. It is best to use the bandpass
        /// output at full resonance to pick out the cutoff frequency at any given FC
        /// setting.
        /// 
        /// The mapping function is specified with spline interpolation points and
        /// the function values are retrieved via table lookup.
        /// 
        /// NB! Cutoff frequency characteristics may vary, we have modeled two
        /// particular Commodore 64s.
        /// </summary>
        protected static int[][] f0_points_8580 = {
                // -----FC----f-------FCHI-FCLO
                // ----------------------------
                new int[] { 0, 0 }, // 0x00 - repeated end point
                new int[] { 0, 0 }, // 0x00
                new int[] { 128, 800 }, // 0x10
                new int[] { 256, 1600 }, // 0x20
                new int[] { 384, 2500 }, // 0x30
                new int[] { 512, 3300 }, // 0x40
                new int[] { 640, 4100 }, // 0x50
                new int[] { 768, 4800 }, // 0x60
                new int[] { 896, 5600 }, // 0x70
                new int[] { 1024, 6500 }, // 0x80
                new int[] { 1152, 7500 }, // 0x90
                new int[] { 1280, 8400 }, // 0xa0
                new int[] { 1408, 9200 }, // 0xb0
                new int[] { 1536, 9800 }, // 0xc0
                new int[] { 1664, 10500 }, // 0xd0
                new int[] { 1792, 11000 }, // 0xe0
                new int[] { 1920, 11700 }, // 0xf0
                new int[] { 2047, 12500 }, // 0xff 0x07
                new int[] { 2047, 12500 } // 0xff 0x07 - repeated end point
        };


        /// <summary>
        /// SID clocking - 1 cycle
        /// </summary>
        /// <param name="voice1"></param>
        /// <param name="voice2"></param>
        /// <param name="voice3"></param>
        /// <param name="ext_in"></param>
        public void clock(int voice1, int voice2, int voice3, int ext_in)
        {
            // Scale each voice down from 20 to 13 bits.
            voice1 >>= 7;
            voice2 >>= 7;

            // NB! Voice 3 is not silenced by voice3off if it is routed through the filter
            if ((voice3off != 0) && ((filt & 0x04) == 0))
            {
                voice3 = 0;
            }
            else
            {
                voice3 >>= 7;
            }

            ext_in >>= 7;

            // This is handy for testing.
            if (!enabled)
            {
                vnf = voice1 + voice2 + voice3 + ext_in;
                vhp = vbp = vlp = 0;
                return;
            }

            int Vi = vnf = 0;
            // Route voices into or around filter.

#if ANTTI_LANKILA_PATCH

            if ((filt & 1) != 0)
            {
                Vi += voice1;
            }
            else
            {
                vnf += voice1;
            }
            if ((filt & 2) != 0)
            {
                Vi += voice2;
            }
            else
            {
                vnf += voice2;
            }
            if ((filt & 4) != 0)
            {
                Vi += voice3;
            }
            else
            {
                vnf += voice3;
            }
            if ((filt & 8) != 0)
            {
                Vi += ext_in;
            }
            else
            {
                vnf += ext_in;
            }
#else
            // The code below is expanded to a switch for faster execution.
            // (filt1 ? Vi : vnf) += voice1;
            // (filt2 ? Vi : vnf) += voice2;
            // (filt3 ? Vi : vnf) += voice3;

            switch (filt)
            {
                default:
                case 0x0:
                    Vi = 0;
                    vnf = voice1 + voice2 + voice3 + ext_in;
                    break;
                case 0x1:
                    Vi = voice1;
                    vnf = voice2 + voice3 + ext_in;
                    break;
                case 0x2:
                    Vi = voice2;
                    vnf = voice1 + voice3 + ext_in;
                    break;
                case 0x3:
                    Vi = voice1 + voice2;
                    vnf = voice3 + ext_in;
                    break;
                case 0x4:
                    Vi = voice3;
                    vnf = voice1 + voice2 + ext_in;
                    break;
                case 0x5:
                    Vi = voice1 + voice3;
                    vnf = voice2 + ext_in;
                    break;
                case 0x6:
                    Vi = voice2 + voice3;
                    vnf = voice1 + ext_in;
                    break;
                case 0x7:
                    Vi = voice1 + voice2 + voice3;
                    vnf = ext_in;
                    break;
                case 0x8:
                    Vi = ext_in;
                    vnf = voice1 + voice2 + voice3;
                    break;
                case 0x9:
                    Vi = voice1 + ext_in;
                    vnf = voice2 + voice3;
                    break;
                case 0xa:
                    Vi = voice2 + ext_in;
                    vnf = voice1 + voice3;
                    break;
                case 0xb:
                    Vi = voice1 + voice2 + ext_in;
                    vnf = voice3;
                    break;
                case 0xc:
                    Vi = voice3 + ext_in;
                    vnf = voice1 + voice2;
                    break;
                case 0xd:
                    Vi = voice1 + voice3 + ext_in;
                    vnf = voice2;
                    break;
                case 0xe:
                    Vi = voice2 + voice3 + ext_in;
                    vnf = voice1;
                    break;
                case 0xf:
                    Vi = voice1 + voice2 + voice3 + ext_in;
                    vnf = 0;
                    break;
            }
#endif

            // delta_t = 1 is converted to seconds given a 1MHz clock by dividing with 1 000 000.

#if ANTTI_LANKILA_PATCH

            int Vi_peak_bp = ((vlp * DHlp + vbp * DHbp + vhp * DHhp) >> 8) + Vi;
            if (Vi_peak_bp < DHthreshold)
            {
                Vi_peak_bp = DHthreshold;
            }
            int Vi_peak_lp = ((vlp * DLlp + vbp * DLbp + vhp * DLhp) >> 8) + Vi;
            if (Vi_peak_lp < DLthreshold)
            {
                Vi_peak_lp = DLthreshold;
            }
            int w0_eff_bp = w0 + w0 * ((Vi_peak_bp - DHthreshold) >> 4) / DHsteepness;
            int w0_eff_lp = w0 + w0 * ((Vi_peak_lp - DLthreshold) >> 4) / DLsteepness;
            // we need to ensure filter's stability
            if (w0_eff_bp > w0_ceil_1)
            {
                w0_eff_bp = w0_ceil_1;
            }
            if (w0_eff_lp > w0_ceil_1)
            {
                w0_eff_lp = w0_ceil_1;
            }

            vhp = (vbp * _1024_div_Q >> 10) - vlp - Vi;
            vlp -= w0_eff_lp * vbp >> 20;
            vbp -= w0_eff_bp * vhp >> 20;
#else
            // Calculate filter outputs.
            // vhp = vbp/Q - vlp - Vi;
            // dVbp = -w0*vhp*dt;
            // dVlp = -w0*vbp*dt;

            int dVbp = (w0_ceil_1 * vhp >> 20);
            int dVlp = (w0_ceil_1 * vbp >> 20);
            vbp -= dVbp;
            vlp -= dVlp;
            vhp = (vbp * _1024_div_Q >> 10) - vlp - Vi;
#endif
        }

        /// <summary>
        /// SID clocking - delta_t cycles
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="voice1"></param>
        /// <param name="voice2"></param>
        /// <param name="voice3"></param>
        /// <param name="ext_in"></param>
        public void clock(int delta_t, int voice1, int voice2, int voice3, int ext_in)
        {
            // Scale each voice down from 20 to 13 bits.
            voice1 >>= 7;
            voice2 >>= 7;

            // NB! Voice 3 is not silenced by voice3off if it is routed through the filter.
            if ((voice3off != 0) && ((filt & 0x04) == 0))
            {
                voice3 = 0;
            }
            else
            {
                voice3 >>= 7;
            }

            ext_in >>= 7;

            // Enable filter on/off.
            // This is not really part of SID, but is useful for testing.
            // On slow CPUs it may be necessary to bypass the filter to lower the CPU load.
            if (!enabled)
            {
                vnf = voice1 + voice2 + voice3 + ext_in;
                vhp = vbp = vlp = 0;
                return;
            }

            int Vi = vnf = 0;

            // Route voices into or around filter.
            // The code below is expanded to a switch for faster execution.

#if ANTTI_LANKILA_PATCH

            switch (filt)
            {
                default:
                case 0x0:
                    Vi = 0;
                    vnf = voice1 + voice2 + voice3 + ext_in;
                    break;
                case 0x1:
                    Vi = voice1;
                    vnf = voice2 + voice3 + ext_in;
                    break;
                case 0x2:
                    Vi = voice2;
                    vnf = voice1 + voice3 + ext_in;
                    break;
                case 0x3:
                    Vi = voice1 + voice2;
                    vnf = voice3 + ext_in;
                    break;
                case 0x4:
                    Vi = voice3;
                    vnf = voice1 + voice2 + ext_in;
                    break;
                case 0x5:
                    Vi = voice1 + voice3;
                    vnf = voice2 + ext_in;
                    break;
                case 0x6:
                    Vi = voice2 + voice3;
                    vnf = voice1 + ext_in;
                    break;
                case 0x7:
                    Vi = voice1 + voice2 + voice3;
                    vnf = ext_in;
                    break;
                case 0x8:
                    Vi = ext_in;
                    vnf = voice1 + voice2 + voice3;
                    break;
                case 0x9:
                    Vi = voice1 + ext_in;
                    vnf = voice2 + voice3;
                    break;
                case 0xa:
                    Vi = voice2 + ext_in;
                    vnf = voice1 + voice3;
                    break;
                case 0xb:
                    Vi = voice1 + voice2 + ext_in;
                    vnf = voice3;
                    break;
                case 0xc:
                    Vi = voice3 + ext_in;
                    vnf = voice1 + voice2;
                    break;
                case 0xd:
                    Vi = voice1 + voice3 + ext_in;
                    vnf = voice2;
                    break;
                case 0xe:
                    Vi = voice2 + voice3 + ext_in;
                    vnf = voice1;
                    break;
                case 0xf:
                    Vi = voice1 + voice2 + voice3 + ext_in;
                    vnf = 0;
                    break;
            }
#else
            if ((filt & 1) != 0)
            {
                Vi += voice1;
            }
            else
            {
                vnf += voice1;
            }
            if ((filt & 2) != 0)
            {
                Vi += voice2;
            }
            else
            {
                vnf += voice2;
            }
            if ((filt & 4) != 0)
            {
                Vi += voice3;
            }
            else
            {
                vnf += voice3;
            }
            if ((filt & 8) != 0)
            {
                Vi += ext_in;
            }
            else
            {
                vnf += ext_in;
            }
#endif
            // Maximum delta cycles for the filter to work satisfactorily under
            // current cutoff frequency and resonance constraints is approximately 8.
            int delta_t_flt = 8;

            while (delta_t != 0)
            {
                if (delta_t < delta_t_flt)
                {
                    delta_t_flt = delta_t;
                }

                // delta_t is converted to seconds given a 1MHz clock by dividing
                // with 1 000 000. This is done in two operations to avoid int
                // multiplication overflow.

                // Calculate filter outputs.
                // vhp = vbp/Q - vlp - Vi;
                // dVbp = -w0*vhp*dt;
                // dVlp = -w0*vbp*dt;
                int w0_delta_t = w0_ceil_dt * delta_t_flt >> 6;

                int dVbp = (w0_delta_t * vhp >> 14);
                int dVlp = (w0_delta_t * vbp >> 14);
                vbp -= dVbp;
                vlp -= dVlp;
                vhp = (vbp * _1024_div_Q >> 10) - vlp - Vi;

                delta_t -= delta_t_flt;
            }
        }

        /// <summary>
        /// SID audio output (16 bits). SID audio output (20 bits)
        /// </summary>
        /// <returns></returns>
        public int output
        {
            get
            {
                // This is handy for testing.
                if (!enabled)
                {
                    return (vnf + mixer_DC) * (vol);
                }

#if ANTTI_LANKILA_PATCH

                // Mix highpass, bandpass, and lowpass outputs. The sum is not
                // weighted, this can be confirmed by sampling sound output for
                // e.g. bandpass, lowpass, and bandpass+lowpass from a SID chip.

                int Vf;

                switch (hp_bp_lp)
                {
                    default:
                    case 0x0:
                        Vf = 0;
                        break;
                    case 0x1:
                        Vf = vlp;
                        break;
                    case 0x2:
                        Vf = vbp;
                        break;
                    case 0x3:
                        Vf = vlp + vbp;
                        break;
                    case 0x4:
                        Vf = vhp;
                        break;
                    case 0x5:
                        Vf = vlp + vhp;
                        break;
                    case 0x6:
                        Vf = vbp + vhp;
                        break;
                    case 0x7:
                        Vf = vlp + vbp + vhp;
                        break;
                }

                // Sum non-filtered and filtered output.
                // Multiply the sum with volume.
                return (vnf + Vf + mixer_DC) * (vol);
#else
            int Vf = 0;
            if ((hp_bp_lp & 1) != 0)
            {
                Vf += vlp;
            }
            if ((hp_bp_lp & 2) != 0)
            {
                Vf += vbp;
            }
            if ((hp_bp_lp & 4) != 0)
            {
                Vf += vhp;
            }

            // Sum non-filtered and filtered output.
            // Multiply the sum with volume.
            return (vnf + Vf + mixer_DC) * (vol);
#endif
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Filter()
        {
            fc = 0;

            res = 0;

            filt = 0;

            voice3off = 0;

            hp_bp_lp = 0;

            vol = 0;

            // State of filter.
            vhp = 0;
            vbp = 0;
            vlp = 0;
            vnf = 0;

            enable_filter(true);

            // Create mappings from FC to cutoff frequency.
            interpolate(f0_points_6581, 0, f0_points_6581.Length - 1, new PointPlotter(f0_6581), 1.0);
            interpolate(f0_points_8580, 0, f0_points_8580.Length - 1, new PointPlotter(f0_8580), 1.0);

            set_chip_model(SIDDefs.chip_model.MOS6581);

            /* no distortion by default */
            set_distortion_properties(999999, 999999, 0, 0, 0, 999999, 999999, 0, 0, 0);
        }
        // only used for deserializing
        public Filter(BinaryReader reader)
        {
            LoadFromReader(reader);
        }


        public void enable_filter(bool enable)
        {
            enabled = enable;
        }

        public void set_chip_model(SIDDefs.chip_model model)
        {
            if (model == SIDDefs.chip_model.MOS6581)
            {
                // The mixer has a small input DC offset. This is found as follows:
                //
                // The "zero" output level of the mixer measured on the SID audio
                // output pin is 5.50V at zero volume, and 5.44 at full
                // volume. This yields a DC offset of (5.44V - 5.50V) = -0.06V.
                //
                // The DC offset is thus -0.06V/1.05V ~ -1/18 of the dynamic range
                // of one voice. See Voice.java for measurement of the dynamic
                // range.

                mixer_DC = -0xfff * 0xff / 18 >> 7;

                f0 = f0_6581;
                f0_points = f0_points_6581;
                f0_count = f0_points_6581.Length;
            }
            else
            {
                // No DC offsets in the MOS8580.
                mixer_DC = 0;

                f0 = f0_8580;
                f0_points = f0_points_8580;
                f0_count = f0_points_8580.Length;
            }

            set_w0();
            set_Q();
        }

        internal void set_distortion_properties(int Lthreshold, int Lsteepness, int Llp, int Lbp, int Lhp, int Hthreshold, int Hsteepness, int Hlp, int Hbp, int Hhp)
        {
            DLthreshold = Lthreshold;
            if (Lsteepness < 16)
            {
                Lsteepness = 16; // avoid division by zero
            }
            DLsteepness = Lsteepness >> 4;
            DLlp = Llp;
            DLbp = Lbp;
            DLhp = Lhp;

            DHthreshold = Hthreshold;
            if (Hsteepness < 16)
            {
                Hsteepness = 16;
            }
            DHsteepness = Hsteepness >> 4;
            DHlp = Hlp;
            DHbp = Hbp;
            DHhp = Hhp;
        }

        public void reset()
        {
            fc = 0;

            res = 0;

            filt = 0;

            voice3off = 0;

            hp_bp_lp = 0;

            vol = 0;

            // State of filter.
            vhp = 0;
            vbp = 0;
            vlp = 0;
            vnf = 0;

            set_w0();
            set_Q();
        }

        /// <summary>
        /// Register functions
        /// </summary>
        /// <param name="fc_lo"></param>
        public void writeFC_LO(int fc_lo)
        {
            fc = fc & 0x7f8 | fc_lo & 0x007;
            set_w0();
        }

        /// <summary>
        /// Register functions
        /// </summary>
        /// <param name="fc_hi"></param>
        public void writeFC_HI(int fc_hi)
        {
            fc = (fc_hi << 3) & 0x7f8 | fc & 0x007;
            set_w0();
        }

        /// <summary>
        /// Register functions
        /// </summary>
        /// <param name="res_filt"></param>
        public void writeRES_FILT(int res_filt)
        {
            res = (res_filt >> 4) & 0x0f;
            set_Q();

            filt = res_filt & 0x0f;
        }

        /// <summary>
        /// Register functions
        /// </summary>
        /// <param name="mode_vol"></param>
        public void writeMODE_VOL(int mode_vol)
        {
            voice3off = mode_vol & 0x80;

            hp_bp_lp = (mode_vol >> 4) & 0x07;

            vol = mode_vol & 0x0f;
        }

        // Set filter cutoff frequency.
        protected void set_w0()
        {
            double pi = 3.1415926535897932385;

            // Multiply with 1.048576 to facilitate division by 1 000 000 by right-
            // shifting 20 times (2 ^ 20 = 1048576).
            w0 = (int)(2 * pi * f0[fc] * 1.048576);

#if ANTTI_LANKILA_PATCH

            // Set the static limit to the dynamic, distortion-driven filter.
            // I need a few kHz headroom at least to be even half certain that
            // the filter will not drive itself to oblivion.
            w0_ceil_1 = (int)(2 * pi * 18000 * 1.048576);
#else
            // Limit f0 to 16kHz to keep 1 cycle filter stable.
            int w0_max_1 = (int)(2 * pi * 16000 * 1.048576);
            w0_ceil_1 = w0 <= w0_max_1 ? w0 : w0_max_1;
#endif

            // Limit f0 to 4kHz to keep delta_t cycle filter stable.
            int w0_max_dt = (int)(2 * pi * 4000 * 1.048576);
            w0_ceil_dt = w0 <= w0_max_dt ? w0 : w0_max_dt;
        }

        /// <summary>
        /// Set filter resonance
        /// </summary>
        protected void set_Q()
        {
            // Q is controlled linearly by res. Q has approximate range [0.707, 1.7].
            // As resonance is increased, the filter must be clocked more often to
            // keep stable.

            // The coefficient 1024 is dispensed of later by right-shifting 10 times (2 ^ 10 = 1024).
            _1024_div_Q = (int)(1024.0 / (0.707 + 1.0 * res / 0x0f));
        }

        // ----------------------------------------------------------------------------
        // Spline functions.
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Return the array of spline interpolation points used to map the FC
        /// register to filter cutoff frequency.
        /// </summary>
        /// <param name="fcp">IN/OUT parameter points and count</param>
        public void fc_default(FCPoints fcp)
        {
            fcp.points = f0_points;
            fcp.count = f0_count;
        }

        // ----------------------------------------------------------------------------
        // Given an array of interpolation points p with n points, the following
        // statement will specify a new FC mapping:
        // interpolate(p, p + n - 1, filter.fc_plotter(), 1.0);
        // Note that the x range of the interpolation points *must* be [0, 2047],
        // and that additional end points *must* be present since the end points
        // are not interpolated.
        // ----------------------------------------------------------------------------
        public PointPlotter fc_plotter()
        {
            return new PointPlotter(f0);
        }

        // Our objective is to construct a smooth interpolating single-valued
        // function
        // y = f(x).
        //
        // Catmull-Rom splines are widely used for interpolation, however these are
        // parametric curves [x(t) y(t) ...] and can not be used to directly
        // calculate
        // y = f(x).
        // For a discussion of Catmull-Rom splines see Catmull, E., and R. Rom,
        // "A Class of Local Interpolating Splines", Computer Aided Geometric
        // Design.
        //
        // Natural cubic splines are single-valued functions, and have been used in
        // several applications e.g. to specify gamma curves for image display.
        // These splines do not afford local control, and a set of linear equations
        // including all interpolation points must be solved before any point on the
        // curve can be calculated. The lack of local control makes the splines
        // more difficult to handle than e.g. Catmull-Rom splines, and real-time
        // interpolation of a stream of data points is not possible.
        // For a discussion of natural cubic splines, see e.g. Kreyszig, E.,
        // "Advanced
        // Engineering Mathematics".
        //
        // Our approach is to approximate the properties of Catmull-Rom splines for
        // piecewice cubic polynomials f(x) = ax^3 + bx^2 + cx + d as follows:
        // Each curve segment is specified by four interpolation points,
        // p0, p1, p2, p3.
        // The curve between p1 and p2 must interpolate both p1 and p2, and in
        // addition
        // f'(p1.x) = k1 = (p2.y - p0.y)/(p2.x - p0.x) and
        // f'(p2.x) = k2 = (p3.y - p1.y)/(p3.x - p1.x).
        //
        // The constraints are expressed by the following system of linear equations
        //
        // [ 1 xi xi^2 xi^3 ] [ d ] [ yi ]
        // [ 1 2*xi 3*xi^2 ] * [ c ] = [ ki ]
        // [ 1 xj xj^2 xj^3 ] [ b ] [ yj ]
        // [ 1 2*xj 3*xj^2 ] [ a ] [ kj ]
        //
        // Solving using Gaussian elimination and back substitution, setting
        // dy = yj - yi, dx = xj - xi, we get
        //	 
        // a = ((ki + kj) - 2*dy/dx)/(dx*dx);
        // b = ((kj - ki)/dx - 3*(xi + xj)*a)/2;
        // c = ki - (3*xi*a + 2*b)*xi;
        // d = yi - ((xi*a + b)*xi + c)*xi;
        //
        // Having calculated the coefficients of the cubic polynomial we have the
        // choice of evaluation by brute force
        //
        // for (x = x1; x <= x2; x += res) {
        // y = ((a*x + b)*x + c)*x + d;
        // plot(x, y);
        // }
        //
        // or by forward differencing
        //
        // y = ((a*x1 + b)*x1 + c)*x1 + d;
        // dy = (3*a*(x1 + res) + 2*b)*x1*res + ((a*res + b)*res + c)*res;
        // d2y = (6*a*(x1 + res) + 2*b)*res*res;
        // d3y = 6*a*res*res*res;
        //	     
        // for (x = x1; x <= x2; x += res) {
        // plot(x, y);
        // y += dy; dy += d2y; d2y += d3y;
        // }
        //
        // See Foley, Van Dam, Feiner, Hughes, "Computer Graphics, Principles and
        // Practice" for a discussion of forward differencing.
        //
        // If we have a set of interpolation points p0, ..., pn, we may specify
        // curve segments between p0 and p1, and between pn-1 and pn by using the
        // following constraints:
        // f''(p0.x) = 0 and
        // f''(pn.x) = 0.
        //
        // Substituting the results for a and b in
        //
        // 2*b + 6*a*xi = 0
        //
        // we get
        //
        // ki = (3*dy/dx - kj)/2;
        //
        // or by substituting the results for a and b in
        //
        // 2*b + 6*a*xj = 0
        //
        // we get
        //
        // kj = (3*dy/dx - ki)/2;
        //
        // Finally, if we have only two interpolation points, the cubic polynomial
        // will degenerate to a straight line if we set
        //
        // ki = kj = dy/dx;
        //

        /// <summary>
        /// Calculation of coefficients
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="k1"></param>
        /// <param name="k2"></param>
        /// <param name="coeff"></param>
        protected void cubic_coefficients(double x1, double y1, double x2, double y2, double k1, double k2, Coefficients coeff)
        {
            double dx = x2 - x1, dy = y2 - y1;

            coeff.a = ((k1 + k2) - 2 * dy / dx) / (dx * dx);
            coeff.b = ((k2 - k1) / dx - 3 * (x1 + x2) * coeff.a) / 2;
            coeff.c = k1 - (3 * x1 * coeff.a + 2 * coeff.b) * x1;
            coeff.d = y1 - ((x1 * coeff.a + coeff.b) * x1 + coeff.c) * x1;
        }

        /// <summary>
        /// Evaluation of cubic polynomial by brute force
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="k1"></param>
        /// <param name="k2"></param>
        /// <param name="plotter"></param>
        /// <param name="res"></param>
        protected void interpolate_brute_force(double x1, double y1, double x2, double y2, double k1, double k2, PointPlotter plotter, double res)
        {
            Coefficients coeff = new Coefficients();
            cubic_coefficients(x1, y1, x2, y2, k1, k2, coeff);

            // Calculate each point
            for (double x = x1; x <= x2; x += res)
            {
                double y = ((coeff.a * x + coeff.b) * x + coeff.c) * x + coeff.d;
                plotter.plot(x, y);
            }
        }

        /// <summary>
        /// Evaluation of cubic polynomial by forward differencing
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="k1"></param>
        /// <param name="k2"></param>
        /// <param name="plotter"></param>
        /// <param name="res"></param>
        protected void interpolate_forward_difference(double x1, double y1, double x2, double y2, double k1, double k2, PointPlotter plotter, double res)
        {
            Coefficients coeff = new Coefficients();
            cubic_coefficients(x1, y1, x2, y2, k1, k2, coeff);

            double y = ((coeff.a * x1 + coeff.b) * x1 + coeff.c) * x1 + coeff.d;
            double dy = (3 * coeff.a * (x1 + res) + 2 * coeff.b) * x1 * res + ((coeff.a * res + coeff.b) * res + coeff.c) * res;
            double d2y = (6 * coeff.a * (x1 + res) + 2 * coeff.b) * res * res;
            double d3y = 6 * coeff.a * res * res * res;

            // Calculate each point
            for (double x = x1; x <= x2; x += res)
            {
                plotter.plot(x, y);
                y += dy;
                dy += d2y;
                d2y += d3y;
            }
        }

        protected double x(int[][] f0_base, int p)
        {
            return (f0_base[p])[0];
        }

        protected double y(int[][] f0_base, int p)
        {
            return (f0_base[p])[1];
        }

        /// <summary>
        /// Evaluation of complete interpolating function. Note that since each curve
        /// segment is controlled by four points, the end points will not be
        /// interpolated. If extra control points are not desirable, the end points
        /// can simply be repeated to ensure interpolation. Note also that points of
        /// non-differentiability and discontinuity can be introduced by repeating
        /// points
        /// </summary>
        /// <param name="f0_base"></param>
        /// <param name="p0"></param>
        /// <param name="pn"></param>
        /// <param name="plotter"></param>
        /// <param name="res"></param>
        public void interpolate(int[][] f0_base, int p0, int pn, PointPlotter plotter, double res)
        {
            double k1, k2;

            // Set up points for first curve segment.
            int p1 = p0;
            ++p1;
            int p2 = p1;
            ++p2;
            int p3 = p2;
            ++p3;

            // Draw each curve segment.
            for (; p2 != pn; ++p0, ++p1, ++p2, ++p3)
            {
                // p1 and p2 equal; single point.
                if (x(f0_base, p1) == x(f0_base, p2))
                {
                    continue;
                }
                // Both end points repeated; straight line.
                if (x(f0_base, p0) == x(f0_base, p1) && x(f0_base, p2) == x(f0_base, p3))
                {
                    k1 = k2 = (y(f0_base, p2) - y(f0_base, p1)) / (x(f0_base, p2) - x(f0_base, p1));
                }
                // p0 and p1 equal; use f''(x1) = 0.
                else if (x(f0_base, p0) == x(f0_base, p1))
                {
                    k2 = (y(f0_base, p3) - y(f0_base, p1)) / (x(f0_base, p3) - x(f0_base, p1));
                    k1 = (3 * (y(f0_base, p2) - y(f0_base, p1)) / (x(f0_base, p2) - x(f0_base, p1)) - k2) / 2;
                }
                // p2 and p3 equal; use f''(x2) = 0.
                else if (x(f0_base, p2) == x(f0_base, p3))
                {
                    k1 = (y(f0_base, p2) - y(f0_base, p0)) / (x(f0_base, p2) - x(f0_base, p0));
                    k2 = (3 * (y(f0_base, p2) - y(f0_base, p1)) / (x(f0_base, p2) - x(f0_base, p1)) - k1) / 2;
                }
                // Normal curve.
                else
                {
                    k1 = (y(f0_base, p2) - y(f0_base, p0)) / (x(f0_base, p2) - x(f0_base, p0));
                    k2 = (y(f0_base, p3) - y(f0_base, p1)) / (x(f0_base, p3) - x(f0_base, p1));
                }

#if SPLINE_BRUTE_FORCE
                {
                    interpolate_brute_force(x(f0_base, p1), y(f0_base, p1), x(f0_base, p2), y(f0_base, p2), k1, k2, plotter, res);
                }
#else
                {
                    interpolate_forward_difference(x(f0_base, p1), y(f0_base, p1), x(f0_base, p2), y(f0_base, p2), k1, k2, plotter, res);
                }
#endif
            }
        }

        // ----------------------------------------------------------------------------
        // END Spline functions.
        // ----------------------------------------------------------------------------

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            writer.Write(enabled);
            writer.Write(fc);
            writer.Write(res);
            writer.Write(filt);
            writer.Write(voice3off);
            writer.Write(hp_bp_lp);
            writer.Write(vol);
            writer.Write(mixer_DC);
            writer.Write(vhp);
            writer.Write(vbp);
            writer.Write(vlp);
            writer.Write(vnf);
            writer.Write(DLthreshold);
            writer.Write(DLsteepness);
            writer.Write(DHthreshold);
            writer.Write(DHsteepness);
            writer.Write(DLlp);
            writer.Write(DLbp);
            writer.Write(DLhp);
            writer.Write(DHlp);
            writer.Write(DHbp);
            writer.Write(DHhp);
            writer.Write(w0);
            writer.Write(w0_ceil_1);
            writer.Write(w0_ceil_dt);
            writer.Write(_1024_div_Q);
            writer.Write(f0_count);

            if (f0_6581 == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(f0_6581.Length);
                for (int i = 0; i < f0_6581.Length; i++)
                {
                    writer.Write(f0_6581[i]);
                }
            }

            if (f0_8580 == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(f0_8580.Length);
                for (int i = 0; i < f0_8580.Length; i++)
                {
                    writer.Write(f0_8580[i]);
                }
            }

            if (f0 == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(f0.Length);
                for (int i = 0; i < f0.Length; i++)
                {
                    writer.Write(f0[i]);
                }
            }

            if (f0_points == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(f0_points.Length);
                for (int i = 0; i < f0_points.Length; i++)
                {
                    if (f0_points[i] == null)
                    {
                        writer.Write((int)-1);
                    }
                    else
                    {
                        writer.Write(f0_points[i].Length);
                        for (int j = 0; j < f0_points[i].Length; j++)
                        {
                            writer.Write(f0_points[i][j]);
                        }
                    }
                }
            }
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            enabled = reader.ReadBoolean();
            fc = reader.ReadInt32();
            res = reader.ReadInt32();
            filt = reader.ReadInt32();
            voice3off = reader.ReadInt32();
            hp_bp_lp = reader.ReadInt32();
            vol = reader.ReadInt32();
            mixer_DC = reader.ReadInt32();
            vhp = reader.ReadInt32();
            vbp = reader.ReadInt32();
            vlp = reader.ReadInt32();
            vnf = reader.ReadInt32();
            DLthreshold = reader.ReadInt32();
            DLsteepness = reader.ReadInt32();
            DHthreshold = reader.ReadInt32();
            DHsteepness = reader.ReadInt32();
            DLlp = reader.ReadInt32();
            DLbp = reader.ReadInt32();
            DLhp = reader.ReadInt32();
            DHlp = reader.ReadInt32();
            DHbp = reader.ReadInt32();
            DHhp = reader.ReadInt32();
            w0 = reader.ReadInt32();
            w0_ceil_1 = reader.ReadInt32();
            w0_ceil_dt = reader.ReadInt32();
            _1024_div_Q = reader.ReadInt32();
            f0_count = reader.ReadInt32();

            int count;

            count = reader.ReadInt32();
            if (count == -1)
            {
                f0_6581 = null;
            }
            else
            {
                f0_6581 = new int[count];
                for (int i = 0; i < f0_6581.Length; i++)
                {
                    f0_6581[i] = reader.ReadInt32();
                }
            }

            count = reader.ReadInt32();
            if (count == -1)
            {
                f0_8580 = null;
            }
            else
            {
                f0_8580 = new int[count];
                for (int i = 0; i < f0_8580.Length; i++)
                {
                    f0_8580[i] = reader.ReadInt32();
                }
            }

            count = reader.ReadInt32();
            if (count == -1)
            {
                f0 = null;
            }
            else
            {
                f0 = new int[count];
                for (int i = 0; i < f0.Length; i++)
                {
                    f0[i] = reader.ReadInt32();
                }
            }

            count = reader.ReadInt32();
            if (count == -1)
            {
                f0_points = null;
            }
            else
            {
                f0_points = new int[count][];
                for (int i = 0; i < f0_points.Length; i++)
                {
                    count = reader.ReadInt32();
                    if (count == -1)
                    {
                        f0_points[i] = null;
                    }
                    else
                    {
                        f0_points[i] = new int[count];
                        for (int j = 0; j < f0_points[i].Length; j++)
                        {
                            f0_points[i][j] = reader.ReadInt32();
                        }
                    }
                }
            }
        }
    }
}