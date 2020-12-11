using System;

namespace SharpSid
{
    /// <summary>
    /// Read/Write State
    /// 
    /// @author Ken Händel
    /// </summary>
    public class State
    {
        public char[] sid_register = new char[0x20];

        public int bus_value;

        public int bus_value_ttl;

        public int accumulator0, accumulator1, accumulator2;

        public int shift_register0, shift_register1, shift_register2;

        public int rate_counter0, rate_counter1, rate_counter2;

        public int rate_counter_period0, rate_counter_period1, rate_counter_period2;

        public int exponential_counter0, exponential_counter1, exponential_counter2;

        public int exponential_counter_period0, exponential_counter_period1, exponential_counter_period2;

        public int envelope_counter0, envelope_counter1, envelope_counter2;

        public EnvelopeGenerator.State envelope_state0, envelope_state1, envelope_state2;

        public bool hold_zero0, hold_zero1, hold_zero2;


        public State()
        {
            int i;

            for (i = 0; i < sid_register.Length; i++)
            {
                sid_register[i] = (char)0;
            }

            bus_value = 0;
            bus_value_ttl = 0;

            accumulator0 = 0;
            shift_register0 = 0x7ffff8;
            rate_counter0 = 0;
            rate_counter_period0 = 9;
            exponential_counter0 = 0;
            exponential_counter_period0 = 1;
            envelope_counter0 = 0;
            envelope_state0 = EnvelopeGenerator.State.RELEASE;
            hold_zero0 = true;

            accumulator1 = 0;
            shift_register1 = 0x7ffff8;
            rate_counter1 = 0;
            rate_counter_period1 = 9;
            exponential_counter1 = 0;
            exponential_counter_period1 = 1;
            envelope_counter1 = 0;
            envelope_state1 = EnvelopeGenerator.State.RELEASE;
            hold_zero1 = true;

            accumulator1 = 0;
            shift_register1 = 0x7ffff8;
            rate_counter1 = 0;
            rate_counter_period1 = 9;
            exponential_counter1 = 0;
            exponential_counter_period1 = 1;
            envelope_counter1 = 0;
            envelope_state1 = EnvelopeGenerator.State.RELEASE;
            hold_zero1 = true;
        }
    }
}