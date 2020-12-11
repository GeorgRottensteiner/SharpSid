using System;

namespace SharpSid
{
  public static class SID2Types
  {
    // Maximum values

    internal const short SID2_MAX_PRECISION = 16;

    internal const short SID2_MAX_OPTIMISATION = 2;

    /// <summary>
    /// Delays <= MAX produce constant results.
    /// Delays > MAX produce random results
    /// </summary>
    internal const int SID2_MAX_POWER_ON_DELAY = 0x1FFF;

    // Default settings

    internal const long SID2_DEFAULT_SAMPLING_FREQ = 44100;

    internal const short SID2_DEFAULT_PRECISION = 16;

    internal const byte SID2_DEFAULT_OPTIMISATION = 1;

    internal const int SID2_DEFAULT_POWER_ON_DELAY = SID2_MAX_POWER_ON_DELAY + 1;

    // Types

    public enum sid2_player_t
    {
      sid2_playing, sid2_paused, sid2_stopped
    }

    public enum sid2_playback_t
    {
      sid2_left, sid2_mono, sid2_stereo, sid2_right
    }

    /// <summary>
    /// Environment Modes
    /// - sid2_envPS = Playsid
    /// - sid2_envTP = Sidplay  - Transparent Rom
    /// - sid2_envBS = Sidplay  - Bankswitching
    /// - sid2_envR  = Sidplay2 - Real C64 Environment
    /// </summary>
    public enum sid2_env_t
    {
      sid2_envPS,
      sid2_envTP,
      sid2_envBS,
      sid2_envR,
      sid2_envTR
    }

    public enum sid2_model_t
    {
      SID2_MODEL_CORRECT,
      SID2_MOS6581,
      SID2_MOS8580
    }

    public enum sid2_clock_t
    {
      SID2_CLOCK_CORRECT,
      SID2_CLOCK_PAL,
      SID2_CLOCK_NTSC
    }



    /// <summary>
    /// @author Ken Händel
    /// 
    /// Soundcard sample format
    /// </summary>
    public enum sid2_sample_t
    {
      SID2_LITTLE_SIGNED,
      SID2_LITTLE_UNSIGNED,
      SID2_BIG_SIGNED,
      SID2_BIG_UNSIGNED
    }



    public static string StringNotNull( string s )
    {
      if ( s == null )
      {
        return string.Empty;
      }
      return s;
    }



  }
}