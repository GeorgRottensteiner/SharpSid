using System;
using System.IO;

namespace SharpSid
{
    public class SID
    {
        /// <summary>
        /// Resampling constants. The error in interpolated lookup is bounded by
        /// 1.234/L^2, while the error in non-interpolated lookup is bounded by
        /// 0.7854/L + 0.4113/L^2, see
        /// http://www-ccrma.stanford.edu/~jos/resample/Choice_Table_Size.html For a
        /// resolution of 16 bits this yields L >= 285 and L >= 51473, respectively
        /// </summary>
        /// 
        protected const int FIR_N = 125;
        protected const int FIR_RES_INTERPOLATE = 285;
        protected const int FIR_RES_FAST = 51473;
        protected const int FIR_SHIFT = 15;

        protected const int RINGSIZE = 16384;

        // Fixpoint constants (16.16 bits)

        protected const int FIXP_SHIFT = 16;
        protected const int FIXP_MASK = 0xffff;



        protected Voice voice0, voice1, voice2;

        public Filter filter;

        protected ExternalFilter extfilt;


        protected int bus_value;

        protected int bus_value_ttl;

        protected double clock_frequency;

        /// <summary>
        /// External audio input
        /// </summary>
        protected int ext_in;



        // Sampling variables

        protected SIDDefs.sampling_method sampling;

        protected int cycles_per_sample;

        protected int sample_offset;

        protected int sample_index;

        protected short sample_prev;

        protected int fir_N;

        protected int fir_RES;

        /// <summary>
        /// Ring buffer with overflow for contiguous storage of RINGSIZE samples
        /// </summary>
        protected short[] sample;

        /// <summary>
        /// FIR_RES filter tables (FIR_N*FIR_RES)
        /// </summary>
        protected short[] fir;


        /// <summary>
        /// Constructor
        /// </summary>
        public SID()
        {
            voice0 = new Voice();
            voice1 = new Voice();
            voice2 = new Voice();

            filter = new Filter();
            extfilt = new ExternalFilter();

            // Initialize pointers.
            sample = null;
            fir = null;

            voice0.set_sync_source(voice2);
            voice1.set_sync_source(voice0);
            voice2.set_sync_source(voice1);

            set_sampling_parameters(985248, SIDDefs.sampling_method.SAMPLE_FAST, 44100, -1, 0.97);

            bus_value = 0;
            bus_value_ttl = 0;

            ext_in = 0;
        }
        // only used for deserializing
        public SID(BinaryReader reader)
        {
            voice0 = new Voice();
            voice1 = new Voice();
            voice2 = new Voice();

            LoadFromReader(reader);

            voice0.set_sync_source(voice2);
            voice1.set_sync_source(voice0);
            voice2.set_sync_source(voice1);
        }


        public void set_chip_model(SIDDefs.chip_model model)
        {
            voice0.set_chip_model(model);
            voice1.set_chip_model(model);
            voice2.set_chip_model(model);

            filter.set_chip_model(model);
            extfilt.set_chip_model(model);
        }

        public void set_distortion_properties(int Lt, int Ls, int Ll, int Lb, int Lh, int Ht, int Hs, int Hl, int Hb, int Hh)
        {
            filter.set_distortion_properties(Lt, Ls, Ll, Lb, Lh, Ht, Hs, Hl, Hb, Hh);
        }

        public void reset()
        {
            voice0.reset();
            voice1.reset();
            voice2.reset();

            filter.reset();
            extfilt.reset();

            bus_value = 0;
            bus_value_ttl = 0;
        }

        /// <summary>
        /// 16-bit input (EXT IN). Write 16-bit sample to audio input. NB! The caller
        /// is responsible for keeping the value within 16 bits. Note that to mix in
        /// an external audio signal, the signal should be resampled to 1MHz first to
        /// avoid sampling noise
        /// </summary>
        /// <param name="sample"></param>
        public void input(int sample)
        {
            // Voice outputs are 20 bits. Scale up to match three voices in order
            // to facilitate simulation of the MOS8580 "digi boost" hardware hack.
            ext_in = (sample << 4) * 3;
        }

        /// <summary>
        /// 16-bit output (AUDIO OUT). Read sample from audio output. Both 16-bit and
        /// n-bit output is provided
        /// </summary>
        /// <returns></returns>
        public int output()
        {
            int range = 1 << 16;
            int half = range >> 1;
            int sample = extfilt.vo / ((4095 * 255 >> 7) * 3 * 15 * 2 / range);
            if (sample >= half)
            {
                return half - 1;
            }
            if (sample < -half)
            {
                return -half;
            }
            return sample;
        }

        /// <summary>
        /// n-bit output
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public int output(int bits)
        {
            int range = 1 << bits;
            int half = range >> 1;
            int sample = extfilt.vo / ((4095 * 255 >> 7) * 3 * 15 * 2 / range);
            if (sample >= half)
            {
                return half - 1;
            }
            if (sample < -half)
            {
                return -half;
            }
            return sample;
        }

        /// <summary>
        /// Read registers.
        /// Reading a write only register returns the last byte written to any SID
        /// register. The individual bits in this value start to fade down towards
        /// zero after a few cycles. All bits reach zero within approximately $2000 -
        /// $4000 cycles. It has been claimed that this fading happens in an orderly
        /// fashion, however sampling of write only registers reveals that this is
        /// not the case. NB! This is not correctly modeled. The actual use of write
        /// only registers has largely been made in the belief that all SID registers
        /// are readable. To support this belief the read would have to be done
        /// immediately after a write to the same register (remember that an
        /// intermediate write to another register would yield that value instead).
        /// With this in mind we return the last value written to any SID register
        /// for $2000 cycles without modeling the bit fading
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public int read(int offset)
        {
            switch (offset)
            {
                case 0x19:
                    return 0xff; // potX
                case 0x1a:
                    return 0xff; // potY
                case 0x1b:
                    return voice2.wave.OSC;
                case 0x1c:
                    return voice2.envelope.envelope_counter;
                default:
                    return bus_value;
            }
        }

        /// <summary>
        /// Write registers
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public void write(int offset, int value)
        {
            bus_value = value;
            bus_value_ttl = 0x2000;

            switch (offset)
            {
                case 0x00:
                    voice0.wave.writeFREQ_LO(value);
                    break;
                case 0x01:
                    voice0.wave.writeFREQ_HI(value);
                    break;
                case 0x02:
                    voice0.wave.writePW_LO(value);
                    break;
                case 0x03:
                    voice0.wave.writePW_HI(value);
                    break;
                case 0x04:
                    voice0.writeCONTROL_REG(value);
                    break;
                case 0x05:
                    voice0.envelope.writeATTACK_DECAY(value);
                    break;
                case 0x06:
                    voice0.envelope.writeSUSTAIN_RELEASE(value);
                    break;
                case 0x07:
                    voice1.wave.writeFREQ_LO(value);
                    break;
                case 0x08:
                    voice1.wave.writeFREQ_HI(value);
                    break;
                case 0x09:
                    voice1.wave.writePW_LO(value);
                    break;
                case 0x0a:
                    voice1.wave.writePW_HI(value);
                    break;
                case 0x0b:
                    voice1.writeCONTROL_REG(value);
                    break;
                case 0x0c:
                    voice1.envelope.writeATTACK_DECAY(value);
                    break;
                case 0x0d:
                    voice1.envelope.writeSUSTAIN_RELEASE(value);
                    break;
                case 0x0e:
                    voice2.wave.writeFREQ_LO(value);
                    break;
                case 0x0f:
                    voice2.wave.writeFREQ_HI(value);
                    break;
                case 0x10:
                    voice2.wave.writePW_LO(value);
                    break;
                case 0x11:
                    voice2.wave.writePW_HI(value);
                    break;
                case 0x12:
                    voice2.writeCONTROL_REG(value);
                    break;
                case 0x13:
                    voice2.envelope.writeATTACK_DECAY(value);
                    break;
                case 0x14:
                    voice2.envelope.writeSUSTAIN_RELEASE(value);
                    break;
                case 0x15:
                    filter.writeFC_LO(value);
                    break;
                case 0x16:
                    filter.writeFC_HI(value);
                    break;
                case 0x17:
                    filter.writeRES_FILT(value);
                    break;
                case 0x18:
                    filter.writeMODE_VOL(value);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// SID voice muting
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="enable"></param>
        public void mute(int channel, bool enable)
        {
            switch (channel)
            {
                case 0:
                    voice0.mute(enable);
                    return;
                case 1:
                    voice1.mute(enable);
                    return;
                case 2:
                    voice2.mute(enable);
                    return;
            }
        }

        public InternalState read_state()
        {
            InternalState state = new InternalState();
            int j = 0;

            WaveformGenerator wave = voice0.wave;
            EnvelopeGenerator envelope = voice0.envelope;
            state.sid_register[j + 0] = (char)(wave.freq & 0xff);
            state.sid_register[j + 1] = (char)(wave.freq >> 8);
            state.sid_register[j + 2] = (char)(wave.pw & 0xff);
            state.sid_register[j + 3] = (char)(wave.pw >> 8);
            state.sid_register[j + 4] = (char)((wave.waveform << 4) | ((wave.test != 0) ? 0x08 : 0) | ((wave.ring_mod != 0) ? 0x04 : 0) | ((wave.sync != 0) ? 0x02 : 0) | ((envelope.gate != 0) ? 0x01 : 0));
            state.sid_register[j + 5] = (char)((envelope.attack << 4) | envelope.decay);
            state.sid_register[j + 6] = (char)((envelope.sustain << 4) | envelope.release);

            j++;
            wave = voice1.wave;
            envelope = voice1.envelope;
            state.sid_register[j + 0] = (char)(wave.freq & 0xff);
            state.sid_register[j + 1] = (char)(wave.freq >> 8);
            state.sid_register[j + 2] = (char)(wave.pw & 0xff);
            state.sid_register[j + 3] = (char)(wave.pw >> 8);
            state.sid_register[j + 4] = (char)((wave.waveform << 4) | ((wave.test != 0) ? 0x08 : 0) | ((wave.ring_mod != 0) ? 0x04 : 0) | ((wave.sync != 0) ? 0x02 : 0) | ((envelope.gate != 0) ? 0x01 : 0));
            state.sid_register[j + 5] = (char)((envelope.attack << 4) | envelope.decay);
            state.sid_register[j + 6] = (char)((envelope.sustain << 4) | envelope.release);

            j++;
            wave = voice2.wave;
            envelope = voice2.envelope;
            state.sid_register[j + 0] = (char)(wave.freq & 0xff);
            state.sid_register[j + 1] = (char)(wave.freq >> 8);
            state.sid_register[j + 2] = (char)(wave.pw & 0xff);
            state.sid_register[j + 3] = (char)(wave.pw >> 8);
            state.sid_register[j + 4] = (char)((wave.waveform << 4) | ((wave.test != 0) ? 0x08 : 0) | ((wave.ring_mod != 0) ? 0x04 : 0) | ((wave.sync != 0) ? 0x02 : 0) | ((envelope.gate != 0) ? 0x01 : 0));
            state.sid_register[j + 5] = (char)((envelope.attack << 4) | envelope.decay);
            state.sid_register[j + 6] = (char)((envelope.sustain << 4) | envelope.release);

            state.sid_register[j++] = (char)(filter.fc & 0x007);
            state.sid_register[j++] = (char)(filter.fc >> 3);
            state.sid_register[j++] = (char)((filter.res << 4) | filter.filt);
            state.sid_register[j++] = (char)(((filter.voice3off != 0) ? 0x80 : 0) | (filter.hp_bp_lp << 4) | filter.vol);

            // These registers are superfluous, but included for completeness.
            for (; j < 0x1d; j++)
            {
                state.sid_register[j] = (char)(read(j));
            }
            for (; j < 0x20; j++)
            {
                state.sid_register[j] = (char)0;
            }

            state.bus_value = bus_value;
            state.bus_value_ttl = bus_value_ttl;

            state.accumulator0 = voice0.wave.accumulator;
            state.shift_register0 = voice0.wave.shift_register;
            state.rate_counter0 = voice0.envelope.rate_counter;
            state.rate_counter_period0 = voice0.envelope.rate_period;
            state.exponential_counter0 = voice0.envelope.exponential_counter;
            state.exponential_counter_period0 = voice0.envelope.exponential_counter_period;
            state.envelope_counter0 = voice0.envelope.envelope_counter;
            state.envelope_state0 = voice0.envelope.state;
            state.hold_zero0 = voice0.envelope.hold_zero;

            state.accumulator1 = voice1.wave.accumulator;
            state.shift_register1 = voice1.wave.shift_register;
            state.rate_counter1 = voice1.envelope.rate_counter;
            state.rate_counter_period1 = voice1.envelope.rate_period;
            state.exponential_counter1 = voice1.envelope.exponential_counter;
            state.exponential_counter_period1 = voice1.envelope.exponential_counter_period;
            state.envelope_counter1 = voice1.envelope.envelope_counter;
            state.envelope_state1 = voice1.envelope.state;
            state.hold_zero1 = voice1.envelope.hold_zero;

            state.accumulator2 = voice2.wave.accumulator;
            state.shift_register2 = voice2.wave.shift_register;
            state.rate_counter2 = voice2.envelope.rate_counter;
            state.rate_counter_period2 = voice2.envelope.rate_period;
            state.exponential_counter2 = voice2.envelope.exponential_counter;
            state.exponential_counter_period2 = voice2.envelope.exponential_counter_period;
            state.envelope_counter2 = voice2.envelope.envelope_counter;
            state.envelope_state2 = voice2.envelope.state;
            state.hold_zero2 = voice2.envelope.hold_zero;

            return state;
        }

        public void write_state(InternalState state)
        {
            int i;

            for (i = 0; i <= 0x18; i++)
            {
                write(i, state.sid_register[i]);
            }

            bus_value = state.bus_value;
            bus_value_ttl = state.bus_value_ttl;

            voice0.wave.accumulator = state.accumulator0;
            voice0.wave.shift_register = state.shift_register0;
            voice0.envelope.rate_counter = state.rate_counter0;
            voice0.envelope.rate_period = state.rate_counter_period0;
            voice0.envelope.exponential_counter = state.exponential_counter0;
            voice0.envelope.exponential_counter_period = state.exponential_counter_period0;
            voice0.envelope.envelope_counter = state.envelope_counter0;
            voice0.envelope.state = state.envelope_state0;
            voice0.envelope.hold_zero = state.hold_zero0;

            voice1.wave.accumulator = state.accumulator1;
            voice1.wave.shift_register = state.shift_register1;
            voice1.envelope.rate_counter = state.rate_counter1;
            voice1.envelope.rate_period = state.rate_counter_period1;
            voice1.envelope.exponential_counter = state.exponential_counter1;
            voice1.envelope.exponential_counter_period = state.exponential_counter_period1;
            voice1.envelope.envelope_counter = state.envelope_counter1;
            voice1.envelope.state = state.envelope_state1;
            voice1.envelope.hold_zero = state.hold_zero1;

            voice2.wave.accumulator = state.accumulator2;
            voice2.wave.shift_register = state.shift_register2;
            voice2.envelope.rate_counter = state.rate_counter2;
            voice2.envelope.rate_period = state.rate_counter_period2;
            voice2.envelope.exponential_counter = state.exponential_counter2;
            voice2.envelope.exponential_counter_period = state.exponential_counter_period2;
            voice2.envelope.envelope_counter = state.envelope_counter2;
            voice2.envelope.state = state.envelope_state2;
            voice2.envelope.hold_zero = state.hold_zero2;
        }

        public void enable_filter(bool enable)
        {
            filter.enable_filter(enable);
        }

        public void enable_external_filter(bool enable)
        {
            extfilt.enable_filter(enable);
        }

        /// <summary>
        /// I0() computes the 0th order modified Bessel function of the first kind.
        /// This function is originally from resample-1.5/filterkit.c by J. O. Smith
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        protected double I0(double x)
        {
            // Max error acceptable in I0.
            double I0e = 1e-6;

            double sum, u, halfx, temp;
            int n;

            sum = u = n = 1;
            halfx = x / 2.0;

            do
            {
                temp = halfx / n++;
                u *= temp * temp;
                sum += u;
            } while (u >= I0e * sum);

            return sum;
        }

        /// <summary>
        /// Setting of SID sampling parameters.
        /// Use a clock freqency of 985248Hz for PAL C64, 1022730Hz for NTSC C64. The
        /// default end of passband frequency is pass_freq = 0.9*sample_freq/2 for
        /// sample frequencies up to ~ 44.1kHz, and 20kHz for higher sample
        /// frequencies.
        /// 
        /// For resampling, the ratio between the clock frequency and the sample
        /// frequency is limited as follows: 125*clock_freq/sample_freq < 16384 E.g.
        /// provided a clock frequency of ~ 1MHz, the sample frequency can not be set
        /// lower than ~ 8kHz. A lower sample frequency would make the resampling
        /// code overfill its 16k sample ring buffer.
        /// 
        /// The end of passband frequency is also limited: pass_freq <=
        /// 0.9*sample_freq/2
        /// 
        /// E.g. for a 44.1kHz sampling rate the end of passband frequency is limited
        /// to slightly below 20kHz. This constraint ensures that the FIR table is
        /// not overfilled.
        /// </summary>
        /// <param name="clock_freq"></param>
        /// <param name="method"></param>
        /// <param name="sample_freq"></param>
        /// <param name="pass_freq"></param>
        /// <param name="filter_scale"></param>
        /// <returns></returns>
        public bool set_sampling_parameters(double clock_freq, SIDDefs.sampling_method method, double sample_freq, double pass_freq, double filter_scale)
        {
            // Check resampling constraints
            if (method == SIDDefs.sampling_method.SAMPLE_RESAMPLE_INTERPOLATE || method == SIDDefs.sampling_method.SAMPLE_RESAMPLE_FAST)
            {
                // Check whether the sample ring buffer would overfill.
                if (FIR_N * clock_freq / sample_freq >= RINGSIZE)
                {
                    return false;
                }
            }
            // The default passband limit is 0.9*sample_freq/2 for sample
            // frequencies below ~ 44.1kHz, and 20kHz for higher sample
            // frequencies
            if (pass_freq < 0)
            {
                pass_freq = 20000;
                if (2 * pass_freq / sample_freq >= 0.9)
                {
                    pass_freq = 0.9 * sample_freq / 2;
                }
            }
            // Check whether the FIR table would overfill
            else if (pass_freq > 0.9 * sample_freq / 2)
            {
                return false;
            }

            // The filter scaling is only included to avoid clipping, so keep it sane.
            if (filter_scale < 0.9 || filter_scale > 1.0)
            {
                return false;
            }

            // Set the external filter to the pass freq
            extfilt.set_sampling_parameter(pass_freq);
            clock_frequency = clock_freq;
            sampling = method;

            cycles_per_sample = (int)(clock_freq / sample_freq * (1 << FIXP_SHIFT) + 0.5);

            sample_offset = 0;
            sample_prev = 0;

            // FIR initialization is only necessary for resampling
            if (method != SIDDefs.sampling_method.SAMPLE_RESAMPLE_INTERPOLATE && method != SIDDefs.sampling_method.SAMPLE_RESAMPLE_FAST)
            {
                sample = null;
                fir = null;
                return true;
            }

            double pi = 3.1415926535897932385;

            // 16 bits -> -96dB stopband attenuation
            double A = -20 * Math.Log10(1.0 / (1 << 16));
            // A fraction of the bandwidth is allocated to the transition band,
            double dw = (1 - 2 * pass_freq / sample_freq) * pi;
            // The cutoff frequency is midway through the transition band.
            double wc = (2 * pass_freq / sample_freq + 1) * pi / 2;

            // For calculation of beta and N see the reference for the kaiserord
            // function in the MATLAB Signal Processing Toolbox:
            // http://www.mathworks.com/access/helpdesk/help/toolbox/signal/kaiserord.html
            double beta = 0.1102 * (A - 8.7);
            double I0beta = I0(beta);

            // The filter order will maximally be 124 with the current constraints.
            // N >= (96.33 - 7.95)/(2.285*0.1*pi) -> N >= 123
            // The filter order is equal to the number of zero crossings, i.e.
            // it should be an even number (sinc is symmetric about x = 0).
            int N = (int)((A - 7.95) / (2.285 * dw) + 0.5);
            N += N & 1;

            double f_samples_per_cycle = sample_freq / clock_freq;
            double f_cycles_per_sample = clock_freq / sample_freq;

            // The filter length is equal to the filter order + 1.
            // The filter length must be an odd number (sinc is symmetric about x =
            // 0).
            fir_N = (int)(N * f_cycles_per_sample) + 1;
            fir_N |= 1;

            // We clamp the filter table resolution to 2^n, making the fixpoint
            // sample_offset a whole multiple of the filter table resolution.
            int res = method == SIDDefs.sampling_method.SAMPLE_RESAMPLE_INTERPOLATE ? FIR_RES_INTERPOLATE : FIR_RES_FAST;
            int n = (int)Math.Ceiling(Math.Log(res / f_cycles_per_sample) / Math.Log((double)2));
            fir_RES = 1 << n;

            // Allocate memory for FIR tables.
            fir = null;
            fir = new short[fir_N * fir_RES];

            // Calculate fir_RES FIR tables for linear interpolation.
            for (int i = 0; i < fir_RES; i++)
            {
                int fir_offset = i * fir_N + fir_N / 2;
                double j_offset = (double)(i) / fir_RES;
                // Calculate FIR table. This is the sinc function, weighted by the
                // Kaiser window.
                for (int j = -fir_N / 2; j <= fir_N / 2; j++)
                {
                    double jx = j - j_offset;
                    double wt = wc * jx / f_cycles_per_sample;
                    double temp = jx / ((double)fir_N / 2d);
                    double Kaiser = Math.Abs(temp) <= 1 ? I0(beta * Math.Sqrt(1 - temp * temp)) / I0beta : 0;
                    double sincwt = Math.Abs(wt) >= 1e-6 ? Math.Sin(wt) / wt : 1; double val = (1 << FIR_SHIFT) * filter_scale * f_samples_per_cycle * wc / pi * sincwt * Kaiser;
                    fir[fir_offset + j] = (short)(val + 0.5);
                }
            }

            // Allocate sample buffer.
            if ((sample == null))
            {
                sample = new short[RINGSIZE * 2];
            }
            // Clear sample buffer.
            for (int j = 0; j < RINGSIZE * 2; j++)
            {
                sample[j] = 0;
            }
            sample_index = 0;

            return true;
        }

        /// <summary>
        /// Adjustment of SID sampling frequency.
        /// In some applications, e.g. a C64 emulator, it can be desirable to
        /// synchronize sound with a timer source. This is supported by adjustment of
        /// the SID sampling frequency.
        /// <P>
        /// NB! Adjustment of the sampling frequency may lead to noticeable shifts in
        /// frequency, and should only be used for interactive applications. Note
        /// also that any adjustment of the sampling frequency will change the
        /// characteristics of the resampling filter, since the filter is not
        /// rebuilt
        /// </summary>
        /// <param name="sample_freq"></param>
        public void adjust_sampling_frequency(double sample_freq)
        {
            cycles_per_sample = (int)(clock_frequency / sample_freq * (1 << FIXP_SHIFT) + 0.5);
        }

        public void fc_default(FCPoints fcp)
        {
            filter.fc_default(fcp);
        }

        /// <summary>
        /// Return FC spline plotter object
        /// </summary>
        /// <returns></returns>
        public PointPlotter fc_plotter()
        {
            return filter.fc_plotter();
        }

        /// <summary>
        /// SID clocking - 1 cycle
        /// </summary>
        public void clock()
        {
            // Age bus value.
            if (--bus_value_ttl <= 0)
            {
                bus_value = 0;
                bus_value_ttl = 0;
            }

            // Clock amplitude modulators.
            voice0.envelope.clock();
            voice1.envelope.clock();
            voice2.envelope.clock();

            // Clock oscillators.
            voice0.wave.clock();
            voice1.wave.clock();
            voice2.wave.clock();

            // Synchronize oscillators.
            voice0.wave.synchronize();
            voice1.wave.synchronize();
            voice2.wave.synchronize();

            // Clock filter.
            filter.clock(voice0.output, voice1.output, voice2.output, ext_in);

            // Clock external filter.
            extfilt.clock(filter.output);
        }

        /// <summary>
        /// SID clocking - delta_t cycles
        /// </summary>
        /// <param name="delta_t"></param>
        public void clock(int delta_t)
        {
            if (delta_t <= 0)
            {
                return;
            }

            // Age bus value.
            bus_value_ttl -= delta_t;
            if (bus_value_ttl <= 0)
            {
                bus_value = 0;
                bus_value_ttl = 0;
            }

            // Clock amplitude modulators.
            voice0.envelope.clock(delta_t);
            voice1.envelope.clock(delta_t);
            voice2.envelope.clock(delta_t);

            // Clock and synchronize oscillators.
            // Loop until we reach the current cycle.
            int delta_t_osc = delta_t;
            while (delta_t_osc != 0)
            {
                int delta_t_min = delta_t_osc;

                // Find minimum number of cycles to an oscillator accumulator MSB toggle.
                // We have to clock on each MSB on / MSB off for hard sync to operate correctly.
                WaveformGenerator wave = voice0.wave;

                // It is only necessary to clock on the MSB of an oscillator
                // that is a sync source and has freq != 0.
                if ((wave.sync_dest.sync != 0) && (wave.freq != 0))
                {
                    int freq = wave.freq;
                    int accumulator = wave.accumulator;

                    // Clock on MSB off if MSB is on, clock on MSB on if MSB is off.
                    int delta_accumulator = ((accumulator & 0x800000) != 0 ? 0x1000000 : 0x800000) - accumulator;

                    int delta_t_next = (delta_accumulator / freq);
                    if ((delta_accumulator % freq) != 0)
                    {
                        ++delta_t_next;
                    }

                    if (delta_t_next < delta_t_min)
                    {
                        delta_t_min = delta_t_next;
                    }
                }

                wave = voice1.wave;

                // It is only necessary to clock on the MSB of an oscillator
                // that is a sync source and has freq != 0.
                if ((wave.sync_dest.sync != 0) && (wave.freq != 0))
                {
                    int freq = wave.freq;
                    int accumulator = wave.accumulator;

                    // Clock on MSB off if MSB is on, clock on MSB on if MSB is off.
                    int delta_accumulator = ((accumulator & 0x800000) != 0 ? 0x1000000 : 0x800000) - accumulator;

                    int delta_t_next = (delta_accumulator / freq);
                    if ((delta_accumulator % freq) != 0)
                    {
                        ++delta_t_next;
                    }

                    if (delta_t_next < delta_t_min)
                    {
                        delta_t_min = delta_t_next;
                    }
                }

                wave = voice2.wave;

                // It is only necessary to clock on the MSB of an oscillator
                // that is a sync source and has freq != 0.
                if ((wave.sync_dest.sync != 0) && (wave.freq != 0))
                {
                    int freq = wave.freq;
                    int accumulator = wave.accumulator;

                    // Clock on MSB off if MSB is on, clock on MSB on if MSB is off.
                    int delta_accumulator = ((accumulator & 0x800000) != 0 ? 0x1000000 : 0x800000) - accumulator;

                    int delta_t_next = (delta_accumulator / freq);
                    if ((delta_accumulator % freq) != 0)
                    {
                        ++delta_t_next;
                    }

                    if (delta_t_next < delta_t_min)
                    {
                        delta_t_min = delta_t_next;
                    }
                }

                // Clock oscillators.
                voice0.wave.clock(delta_t_min);
                voice1.wave.clock(delta_t_min);
                voice2.wave.clock(delta_t_min);

                // Synchronize oscillators.
                voice0.wave.synchronize();
                voice1.wave.synchronize();
                voice2.wave.synchronize();

                delta_t_osc -= delta_t_min;
            }

            // Clock filter.
            filter.clock(delta_t, voice0.output, voice1.output, voice2.output, ext_in);

            // Clock external filter.
            extfilt.clock(delta_t, filter.output);
        }

        /// <summary>
        /// SID clocking with audio sampling
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <param name="interleave"></param>
        /// <returns></returns>
        public int clock(CycleCount delta_t, short[] buf, int n, int interleave)
        {
            switch (sampling)
            {
                default:
                case SIDDefs.sampling_method.SAMPLE_FAST:
                    return clock_fast(delta_t, buf, n, interleave);
                case SIDDefs.sampling_method.SAMPLE_INTERPOLATE:
                    return clock_interpolate(delta_t, buf, n, interleave);
                case SIDDefs.sampling_method.SAMPLE_RESAMPLE_INTERPOLATE:
                    return clock_resample_interpolate(delta_t, buf, n, interleave);
                case SIDDefs.sampling_method.SAMPLE_RESAMPLE_FAST:
                    return clock_resample_fast(delta_t, buf, n, interleave);
            }
        }

        /// <summary>
        /// SID clocking with audio sampling - delta clocking picking nearest sample
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <param name="interleave"></param>
        /// <returns></returns>
        protected int clock_fast(CycleCount delta_t, short[] buf, int n, int interleave)
        {
            int s = 0;

            for (; ; )
            {
                int next_sample_offset = sample_offset + cycles_per_sample + (1 << (FIXP_SHIFT - 1));
                int delta_t_sample = next_sample_offset >> FIXP_SHIFT;
                if (delta_t_sample > delta_t.delta_t)
                {
                    break;
                }
                if (s >= n)
                {
                    return s;
                }
                clock(delta_t_sample);
                delta_t.delta_t -= delta_t_sample;
                sample_offset = (next_sample_offset & FIXP_MASK) - (1 << (FIXP_SHIFT - 1));
                buf[s++ * interleave] = (short)output();
            }

            clock(delta_t.delta_t);
            sample_offset -= delta_t.delta_t << FIXP_SHIFT;
            delta_t.delta_t = 0;
            return s;
        }

        /// <summary>
        /// SID clocking with audio sampling - cycle based with linear sample interpolation.
        /// 
        /// Here the chip is clocked every cycle. This yields higher quality sound
        /// since the samples are linearly interpolated, and since the external
        /// filter attenuates frequencies above 16kHz, thus reducing sampling noise
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <param name="interleave"></param>
        /// <returns></returns>
        protected int clock_interpolate(CycleCount delta_t, short[] buf, int n, int interleave)
        {
            int s = 0;
            int i;

            for (; ; )
            {
                int next_sample_offset = sample_offset + cycles_per_sample;
                int delta_t_sample = next_sample_offset >> FIXP_SHIFT;
                if (delta_t_sample > delta_t.delta_t)
                {
                    break;
                }
                if (s >= n)
                {
                    return s;
                }
                for (i = 0; i < delta_t_sample - 1; i++)
                {
                    clock();
                }
                if (i < delta_t_sample)
                {
                    sample_prev = (short)output();
                    clock();
                }

                delta_t.delta_t -= delta_t_sample;
                sample_offset = next_sample_offset & FIXP_MASK;

                short sample_now = (short)output();
                buf[s++ * interleave] = (short)(sample_prev + (sample_offset * (sample_now - sample_prev) >> FIXP_SHIFT));
                sample_prev = sample_now;
            }

            for (i = 0; i < delta_t.delta_t - 1; i++)
            {
                clock();
            }
            if (i < delta_t.delta_t)
            {
                sample_prev = (short)output();
                clock();
            }
            sample_offset -= delta_t.delta_t << FIXP_SHIFT;
            delta_t.delta_t = 0;
            return s;
        }

        /// <summary>
        /// SID clocking with audio sampling - cycle based with audio resampling.
        /// 
        /// This is the theoretically correct (and computationally intensive) audio
        /// sample generation. The samples are generated by resampling to the
        /// specified sampling frequency. The work rate is inversely proportional to
        /// the percentage of the bandwidth allocated to the filter transition band.
        /// 
        /// This implementation is based on the paper "A Flexible Sampling-Rate
        /// Conversion Method", by J. O. Smith and P. Gosset, or rather on the
        /// expanded tutorial on the "Digital Audio Resampling Home Page":
        /// http://www-ccrma.stanford.edu/~jos/resample/
        /// 
        /// By building shifted FIR tables with samples according to the sampling
        /// frequency, this implementation dramatically reduces the computational
        /// effort in the filter convolutions, without any loss of accuracy. The
        /// filter convolutions are also vectorizable on current hardware.
        /// 
        /// Further possible optimizations are: * An equiripple filter design could
        /// yield a lower filter order, see
        /// http://www.mwrf.com/Articles/ArticleID/7229/7229.html * The Convolution
        /// Theorem could be used to bring the complexity of convolution down from
        /// O(n*n) to O(n*log(n)) using the Fast Fourier Transform, see
        /// http://en.wikipedia.org/wiki/Convolution_theorem * Simply resampling in
        /// two steps can also yield computational savings, since the transition band
        /// will be wider in the first step and the required filter order is thus
        /// lower in this step. Laurent Ganier has found the optimal intermediate
        /// sampling frequency to be (via derivation of sum of two steps): 2 *
        /// pass_freq + sqrt [ 2 * pass_freq * orig_sample_freq * (dest_sample_freq -
        /// 2 * pass_freq) / dest_sample_freq ]
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <param name="interleave"></param>
        /// <returns></returns>
        protected int clock_resample_interpolate(CycleCount delta_t, short[] buf, int n, int interleave)
        {
            int s = 0;

            for (; ; )
            {
                int next_sample_offset = sample_offset + cycles_per_sample;
                int delta_t_sample = next_sample_offset >> FIXP_SHIFT;
                if (delta_t_sample > delta_t.delta_t)
                {
                    break;
                }
                if (s >= n)
                {
                    return s;
                }
                for (int i = 0; i < delta_t_sample; i++)
                {
                    clock();
                    sample[sample_index] = sample[sample_index + RINGSIZE] = (short)output();
                    ++sample_index;
                    sample_index &= 0x3fff;
                }
                delta_t.delta_t -= delta_t_sample;
                sample_offset = next_sample_offset & FIXP_MASK;

                int fir_offset = sample_offset * fir_RES >> FIXP_SHIFT;
                int fir_offset_rmd = sample_offset * fir_RES & FIXP_MASK;
                int fir_start = (fir_offset * fir_N);
                int sample_start = (sample_index - fir_N + RINGSIZE);

                // Convolution with filter impulse response.
                int v1 = 0;
                for (int j = 0; j < fir_N; j++)
                {
                    v1 += sample[sample_start + j] * fir[fir_start + j];
                }

                // Use next FIR table, wrap around to first FIR table using
                // previous sample.
                if (++fir_offset == fir_RES)
                {
                    fir_offset = 0;
                    --sample_start;
                }
                fir_start = (fir_offset * fir_N);

                // Convolution with filter impulse response.
                int v2 = 0;
                for (int j = 0; j < fir_N; j++)
                {
                    v2 += sample[sample_start + j] * fir[fir_start + j];
                }

                // Linear interpolation.
                // fir_offset_rmd is equal for all samples, it can thus be
                // factorized out:
                // sum(v1 + rmd*(v2 - v1)) = sum(v1) + rmd*(sum(v2) - sum(v1))
                int v = v1 + (fir_offset_rmd * (v2 - v1) >> FIXP_SHIFT);

                v >>= FIR_SHIFT;

                // Saturated arithmetics to guard against 16 bit sample overflow.
                int half = 1 << 15;
                if (v >= half)
                {
                    v = half - 1;
                }
                else if (v < -half)
                {
                    v = -half;
                }

                buf[s++ * interleave] = (short)v;
            }

            for (int i = 0; i < delta_t.delta_t; i++)
            {
                clock();
                sample[sample_index] = sample[sample_index + RINGSIZE] = (short)output();
                ++sample_index;
                sample_index &= 0x3fff;
            }
            sample_offset -= delta_t.delta_t << FIXP_SHIFT;
            delta_t.delta_t = 0;
            return s;
        }

        /// <summary>
        /// SID clocking with audio sampling - cycle based with audio resampling
        /// </summary>
        /// <param name="delta_t"></param>
        /// <param name="buf"></param>
        /// <param name="n"></param>
        /// <param name="interleave"></param>
        /// <returns></returns>
        protected int clock_resample_fast(CycleCount delta_t, short[] buf, int n, int interleave)
        {
            int s = 0;

            for (; ; )
            {
                int next_sample_offset = sample_offset + cycles_per_sample;
                int delta_t_sample = next_sample_offset >> FIXP_SHIFT;
                if (delta_t_sample > delta_t.delta_t)
                {
                    break;
                }
                if (s >= n)
                {
                    return s;
                }
                for (int i = 0; i < delta_t_sample; i++)
                {
                    clock();
                    sample[sample_index] = sample[sample_index + RINGSIZE] = (short)output();
                    ++sample_index;
                    sample_index &= 0x3fff;
                }
                delta_t.delta_t -= delta_t_sample;
                sample_offset = next_sample_offset & FIXP_MASK;

                int fir_offset = sample_offset * fir_RES >> FIXP_SHIFT;
                int fir_start = (fir_offset * fir_N);
                int sample_start = (sample_index - fir_N + RINGSIZE);

                // Convolution with filter impulse response.
                int v = 0;
                for (int j = 0; j < fir_N; j++)
                {
                    v += sample[sample_start + j] * fir[fir_start + j];
                }

                v >>= FIR_SHIFT;

                // Saturated arithmetics to guard against 16 bit sample overflow.
                int half = 1 << 15;
                if (v >= half)
                {
                    v = half - 1;
                }
                else if (v < -half)
                {
                    v = -half;
                }

                buf[s++ * interleave] = (short)v;
            }

            for (int i = 0; i < delta_t.delta_t; i++)
            {
                clock();
                sample[sample_index] = sample[sample_index + RINGSIZE] = (short)output();
                ++sample_index;
                sample_index &= 0x3fff;
            }
            sample_offset -= delta_t.delta_t << FIXP_SHIFT;
            delta_t.delta_t = 0;
            return s;
        }

        // serializing
        public void SaveToWriter(BinaryWriter writer)
        {
            voice0.SaveToWriter(writer, voice0, voice1, voice2);
            voice1.SaveToWriter(writer, voice0, voice1, voice2);
            voice2.SaveToWriter(writer, voice0, voice1, voice2);

            filter.SaveToWriter(writer);

            extfilt.SaveToWriter(writer);

            writer.Write(bus_value);
            writer.Write(bus_value_ttl);
            writer.Write(clock_frequency);
            writer.Write(ext_in);
            writer.Write((short)sampling);
            writer.Write(cycles_per_sample);
            writer.Write(sample_offset);
            writer.Write(sample_index);
            writer.Write(sample_prev);
            writer.Write(fir_N);
            writer.Write(fir_RES);

            if (sample == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(sample.Length);
                for (int i = 0; i < sample.Length; i++)
                {
                    writer.Write(sample[i]);
                }
            }

            if (fir == null)
            {
                writer.Write((int)-1);
            }
            else
            {
                writer.Write(fir.Length);
                for (int i = 0; i < fir.Length; i++)
                {
                    writer.Write(fir[i]);
                }
            }
        }
        // deserializing
        protected void LoadFromReader(BinaryReader reader)
        {
            int count;

            voice0 = new Voice(reader);
            voice1 = new Voice(reader);
            voice2 = new Voice(reader);

            voice0.wave.UpdateAfterLoad(voice0, voice1, voice2);
            voice1.wave.UpdateAfterLoad(voice0, voice1, voice2);
            voice2.wave.UpdateAfterLoad(voice0, voice1, voice2);

            filter = new Filter(reader);

            extfilt = new ExternalFilter(reader);

            bus_value = reader.ReadInt32();
            bus_value_ttl = reader.ReadInt32();
            clock_frequency = reader.ReadDouble();
            ext_in = reader.ReadInt32();
            sampling = (SIDDefs.sampling_method)reader.ReadInt16();
            cycles_per_sample = reader.ReadInt32();
            sample_offset = reader.ReadInt32();
            sample_index = reader.ReadInt32();
            sample_prev = reader.ReadInt16();
            fir_N = reader.ReadInt32();
            fir_RES = reader.ReadInt32();

            count = reader.ReadInt32();
            if (count == -1)
            {
                sample = null;
            }
            else
            {
                sample = new short[count];
                for (int i = 0; i < count; i++)
                {
                    sample[i] = reader.ReadInt16();
                }
            }

            count = reader.ReadInt32();
            if (count == -1)
            {
                fir = null;
            }
            else
            {
                fir = new short[count];
                for (int i = 0; i < count; i++)
                {
                    fir[i] = reader.ReadInt16();
                }
            }
        }
    }
}