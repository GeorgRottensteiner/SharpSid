using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace SharpSid
{
  public class InternalPlayer
  {
    #region Memory delegates

    internal delegate short readMemDelegate( int addr );
    internal delegate void writeMemDelegate( int addr, short data );

    #endregion

    #region const

    private const int EOF = -1;

    private const double CLOCK_FREQ_NTSC = 1022727.14;
    private const double CLOCK_FREQ_PAL = 985248.4;

    private const double VIC_FREQ_PAL = 50.0;
    private const double VIC_FREQ_NTSC = 60.0;

    // These texts are used to override the sidtune settings.

    private const string TXT_PAL_VBI = "50 Hz VBI (PAL)";
    private const string TXT_PAL_VBI_FIXED = "60 Hz VBI (PAL FIXED)";
    private const string TXT_PAL_CIA = "CIA (PAL)";
    private const string TXT_NTSC_VBI = "60 Hz VBI (NTSC)";
    private const string TXT_NTSC_VBI_FIXED = "50 Hz VBI (NTSC FIXED)";
    private const string TXT_NTSC_CIA = "CIA (NTSC)";
    private const string TXT_NA = "NA";

    // Error strings

    //private const string ERR_CONF_WHILST_ACTIVE = "SIDPLAYER ERROR: Trying to configure player whilst active.";
    //private const string ERR_UNSUPPORTED_FREQ = "SIDPLAYER ERROR: Unsupported sampling frequency.";
    //private const string ERR_UNSUPPORTED_PRECISION = "SIDPLAYER ERROR: Unsupported sample precision.";
    //private const string ERR_PSIDDRV_NO_SPACE = "ERROR: No space to install psid driver in C64 ram";
    //private const string ERR_PSIDDRV_RELOC = "ERROR: Failed whilst relocating psid driver";

    // 10 credits max
    //private static string[] credit = new string[10];

    private const int PSIDDRV_MAX_PAGE = 0xff;

    public const int SID2_MAX_SIDS = 1;//2;

    public const int SID2_TIME_BASE = 10;

    public const int SID2_MAPPER_SIZE = 32;

    public const int BUF = (9 * 2 + 8); // 16 bit header

    #endregion

    internal EventScheduler m_scheduler;

    private SID6510 sid6510;

    // Sid objects to use.

    private NullSID nullsid;

    internal XSID xsid;

    private C64cia1 cia;

    private C64cia2 cia2;

    private SID6526 sid6526;

    private C64VIC vic;

    private SIDEmu sid;

    /// <summary>
    /// Mapping table in d4xx-d7xx
    /// </summary>
    private int[] m_sidmapper = new int[32];

    private EventMixer mixerEvent;

    private EventRTC rtc;

    /// <summary>
    /// User Configuration Settings
    /// </summary>
    private SidTuneInfo m_tuneInfo = new SidTuneInfo();

    internal SidTune m_tune;

    private short[] m_ram, m_rom;

    internal sid2_info_t m_info = new sid2_info_t();

    private sid2_config_t m_cfg = new sid2_config_t();

    private double m_fastForwardFactor;

    private long m_mileage;

    private SID2Types.sid2_player_t m_playerState;

    private bool m_running;

    private int m_rand;

    private long m_sid2crc;

    private long m_sid2crcCount;

    // Mixer settings

    private long m_sampleClock;

    private long m_samplePeriod;

    private int m_sampleCount;

    private int m_sampleIndex;

    private short[] m_sampleBuffer;

    // C64 environment settings

    private short m_port_pr_out;
    private short m_port_ddr;
    private short m_port_pr_in;

    private short m_playBank;

    // temp stuff -------------

    internal bool isKernal;
    internal bool isBasic;
    internal bool isIO;
    internal bool isChar;


    internal bool inPlay = false;

    private void evalBankSelect( short data )
    {
      // Determine new memory configuration.
      m_port_pr_out = data;
      m_port_pr_in = (short)( ( data & m_port_ddr ) | ( ~m_port_ddr & ( m_port_pr_in | 0x17 ) & 0xdf ) );
      data |= (short)( ~m_port_ddr & 0xff );
      data &= 7;
      isBasic = ( ( data & 3 ) == 3 );
      isIO = ( data > 4 );
      isKernal = ( ( data & 2 ) != 0 );
      isChar = ( ( data ^ 4 ) > 4 );
    }

    /// <summary>
    /// Clock speed changes due to loading a new song
    /// </summary>
    /// <param name="userClock"></param>
    /// <param name="defaultClock"></param>
    /// <param name="forced"></param>
    /// <returns></returns>
    private double clockSpeed( SID2Types.sid2_clock_t userClock, SID2Types.sid2_clock_t defaultClock, bool forced )
    {
      double cpuFreq = CLOCK_FREQ_PAL;

      // Detect the Correct Song Speed
      // Determine song speed when unknown
      if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_UNKNOWN )
      {
        switch ( defaultClock )
        {
          case SID2Types.sid2_clock_t.SID2_CLOCK_PAL:
            m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_PAL;
            break;
          case SID2Types.sid2_clock_t.SID2_CLOCK_NTSC:
            m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_NTSC;
            break;
          case SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT:
            // No default so base it on emulation clock
            m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_ANY;
            break;
        }
      }

      // Since song will run correct at any clock speed
      // set tune speed to the current emulation
      if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_ANY )
      {
        if ( userClock == SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT )
        {
          userClock = defaultClock;
        }

        switch ( userClock )
        {
          case SID2Types.sid2_clock_t.SID2_CLOCK_NTSC:
            m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_NTSC;
            break;
          case SID2Types.sid2_clock_t.SID2_CLOCK_PAL:
          default:
            m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_PAL;
            break;
        }
      }

      if ( userClock == SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT )
      {
        switch ( m_tuneInfo.clockSpeed )
        {
          case SidTune.SIDTUNE_CLOCK_NTSC:
            userClock = SID2Types.sid2_clock_t.SID2_CLOCK_NTSC;
            break;
          case SidTune.SIDTUNE_CLOCK_PAL:
            userClock = SID2Types.sid2_clock_t.SID2_CLOCK_PAL;
            break;
        }
      }

      if ( forced )
      {
        m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_PAL;
        if ( userClock == SID2Types.sid2_clock_t.SID2_CLOCK_NTSC )
        {
          m_tuneInfo.clockSpeed = SidTune.SIDTUNE_CLOCK_NTSC;
        }
      }

      if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_PAL )
      {
        vic.chip( MOS656X.mos656x_model_t.MOS6569 );
      }
      else // if (tuneInfo.clockSpeed == SIDTUNE_CLOCK_NTSC)
      {
        vic.chip( MOS656X.mos656x_model_t.MOS6567R8 );
      }

      if ( userClock == SID2Types.sid2_clock_t.SID2_CLOCK_PAL )
      {
        cpuFreq = CLOCK_FREQ_PAL;
        m_tuneInfo.speedstring = TXT_PAL_VBI;
        if ( m_tuneInfo.songSpeed == SidTune.SIDTUNE_SPEED_CIA_1A )
        {
          m_tuneInfo.speedstring = TXT_PAL_CIA;
        }
        else if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_NTSC )
        {
          m_tuneInfo.speedstring = TXT_PAL_VBI_FIXED;
        }
      }
      else // if (userClock == SID2_CLOCK_NTSC)
      {
        cpuFreq = CLOCK_FREQ_NTSC;
        m_tuneInfo.speedstring = TXT_NTSC_VBI;
        if ( m_tuneInfo.songSpeed == SidTune.SIDTUNE_SPEED_CIA_1A )
        {
          m_tuneInfo.speedstring = TXT_NTSC_CIA;
        }
        else if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_PAL )
        {
          m_tuneInfo.speedstring = TXT_NTSC_VBI_FIXED;
        }
      }
      return cpuFreq;
    }

    private int environment( SID2Types.sid2_env_t env )
    {
      switch ( m_tuneInfo.compatibility )
      {
        case SidTune.SIDTUNE_COMPATIBILITY_R64:
        case SidTune.SIDTUNE_COMPATIBILITY_BASIC:
          env = SID2Types.sid2_env_t.sid2_envR;
          break;
        case SidTune.SIDTUNE_COMPATIBILITY_PSID:
          if ( env == SID2Types.sid2_env_t.sid2_envR )
          {
            env = SID2Types.sid2_env_t.sid2_envBS;
          }
          break;
      }

      // Environment already set?
      if ( !( ( m_ram != null ) && ( m_info.environment == env ) ) )
      {
        // Setup new player environment
        m_info.environment = env;
        if ( m_ram != null )
        {
          if ( m_ram == m_rom )
          {
            m_ram = null;
          }
          else
          {
            m_rom = null;
            m_ram = null;
          }
        }

        m_ram = new short[0x10000];

        // Setup the access functions to the environment and the properties the memory has
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envPS )
        {
          // Playsid has no roms and SID exists in ram space
          m_rom = m_ram;
          //m_mem = new MemPS(this);
          mem_readMemByte = new readMemDelegate( readMemByte_plain );
          mem_writeMemByte = new writeMemDelegate( writeMemByte_playsid );
          mem_readMemDataByte = new readMemDelegate( readMemByte_plain );
        }
        else
        {
          m_rom = new short[0x10000];

          switch ( m_info.environment )
          {
            case SID2Types.sid2_env_t.sid2_envTP:
              mem_readMemByte = new readMemDelegate( readMemByte_plain );
              mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
              mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaytp );
              break;

            // case sid2_envTR:
            case SID2Types.sid2_env_t.sid2_envBS:
              mem_readMemByte = new readMemDelegate( readMemByte_plain );
              mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
              mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaybs );
              break;

            case SID2Types.sid2_env_t.sid2_envR:
            default: // <-- Just to please compiler
              mem_readMemByte = new readMemDelegate( readMemByte_sidplaybs );
              mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
              mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaybs );
              break;
          }
        }
      }

      // Have to reload the song into memory as everything has changed
      int ret;
      SID2Types.sid2_env_t old = m_info.environment;
      m_info.environment = env;
      ret = initialise();
      m_info.environment = old;
      return ret;
    }

    /// <summary>
    /// Makes the next sequence of notes available. For de.quippy.sidplay.sidplay compatibility
    /// this function should be called from interrupt event
    /// </summary>
    private void fakeIRQ()
    {
      // Check to see if the play address has been provided or whether we should pick it up from an IRQ vector
      int playAddr = m_tuneInfo.playAddr;

      // We have to reload the new play address
      if ( playAddr != 0 )
      {
        evalBankSelect( m_playBank );
      }
      else
      {
        if ( isKernal )
        {
          // Setup the entry point from hardware IRQ
          playAddr = SIDEndian.endian_little16( m_ram, 0x0314 );
        }
        else
        {
          // Setup the entry point from software IRQ
          playAddr = SIDEndian.endian_little16( m_ram, 0xFFFE );
        }
      }

      // Setup the entry point and restart the cpu
      sid6510.triggerIRQ();
      sid6510.reset( playAddr, (short)0, (short)0, (short)0 );
    }

    private int initialise()
    {
      // Fix the mileage counter if just finished another song.
      mileageCorrect();
      m_mileage += rtc.getTime();

      reset();

      long page = ((long)m_tuneInfo.loadAddr + m_tuneInfo.c64dataLen - 1) >> 8;
      if ( page > 0xff )
      {
        //m_errorstring = "SIDPLAYER ERROR: Size of music data exceeds C64 memory.";
        return -1;
      }

      if ( psidDrvReloc( m_tuneInfo, m_info ) < 0 )
      {
        return -1;
      }

      // The Basic ROM sets these values on loading a file.
      // Program end address
      int start = m_tuneInfo.loadAddr;
      int end = (int)(start + m_tuneInfo.c64dataLen);
      SIDEndian.endian_little16( m_ram, 0x2d, end ); // Variables start
      SIDEndian.endian_little16( m_ram, 0x2f, end ); // Arrays start
      SIDEndian.endian_little16( m_ram, 0x31, end ); // strings start
      SIDEndian.endian_little16( m_ram, 0xac, start );
      SIDEndian.endian_little16( m_ram, 0xae, end );

      if ( !m_tune.placeSidTuneInC64mem( m_ram ) )
      {
        // Allow loop through errors
        //m_errorstring = m_tuneInfo.statusstring;
        return -1;
      }

      psidDrvInstall( m_info );
      rtc.reset();
      Reset( false );
      return 0;
    }



    // for program injection
    internal void SetCPUPos( int initialAddress )
    {
      sid6510.reset( initialAddress, 0, 0, 0 );
    }



    internal void mixer()
    {
      long cycles;
      short[] buf = m_sampleBuffer;
      int bufOff = m_sampleIndex;
      m_sampleClock += m_samplePeriod;
      cycles = m_sampleClock >> 16;
      m_sampleClock &= 0x0FFFF;
      m_sampleIndex += (int)output( buf, bufOff );

      // Schedule next sample event
      m_scheduler.schedule( mixerEvent, cycles, event_phase_t.EVENT_CLOCK_PHI1 );

      // Filled buffer
      if ( m_sampleIndex >= m_sampleCount )
      {
        m_running = false;
      }
    }

    private void mixerReset()
    {
      m_sampleClock = m_samplePeriod & 0x0FFFF;
      // Schedule next sample event
      m_scheduler.schedule( mixerEvent, m_samplePeriod >> 24, event_phase_t.EVENT_CLOCK_PHI1 );
    }

    private void mileageCorrect()
    {
      // Calculate 1 bit below the timebase so we can round the mileage count
      if ( ( ( ( m_sampleCount * 2 * SID2_TIME_BASE ) / m_cfg.frequency ) & 1 ) != 0 )
      {
        m_mileage++;
      }
      m_sampleCount = 0;
    }

    /// <summary>
    /// Integrate SID emulation from the builder class
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="userModel"></param>
    /// <param name="defaultModel"></param>
    /// <returns></returns>
    private int sidCreate( ReSID resid, SID2Types.sid2_model_t userModel, SID2Types.sid2_model_t defaultModel )
    {
      sid = xsid.emulation();

      // Make xsid forget it's emulation
      xsid.emulation( nullsid );

      // Detect the Correct SID model
      // Determine model when unknown
      if ( m_tuneInfo.sidModel == SidTune.SIDTUNE_SIDMODEL_UNKNOWN )
      {
        switch ( defaultModel )
        {
          case SID2Types.sid2_model_t.SID2_MOS6581:
            m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_6581;
            break;
          case SID2Types.sid2_model_t.SID2_MOS8580:
            m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_8580;
            break;
          case SID2Types.sid2_model_t.SID2_MODEL_CORRECT:
            // No default so base it on emulation clock
            m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_ANY;
            break;
        }
      }

      // Since song will run correct on any sid model
      // set it to the current emulation
      if ( m_tuneInfo.sidModel == SidTune.SIDTUNE_SIDMODEL_ANY )
      {
        if ( userModel == SID2Types.sid2_model_t.SID2_MODEL_CORRECT )
        {
          userModel = defaultModel;
        }

        switch ( userModel )
        {
          case SID2Types.sid2_model_t.SID2_MOS8580:
            m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_8580;
            break;
          case SID2Types.sid2_model_t.SID2_MOS6581:
          default:
            m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_6581;
            break;
        }
      }

      switch ( userModel )
      {
        case SID2Types.sid2_model_t.SID2_MODEL_CORRECT:
          switch ( m_tuneInfo.sidModel )
          {
            case SidTune.SIDTUNE_SIDMODEL_8580:
              userModel = SID2Types.sid2_model_t.SID2_MOS8580;
              break;
            case SidTune.SIDTUNE_SIDMODEL_6581:
              userModel = SID2Types.sid2_model_t.SID2_MOS6581;
              break;
          }
          break;
        // Fixup tune information if model is forced
        case SID2Types.sid2_model_t.SID2_MOS6581:
          m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_6581;
          break;
        case SID2Types.sid2_model_t.SID2_MOS8580:
          m_tuneInfo.sidModel = SidTune.SIDTUNE_SIDMODEL_8580;
          break;
      }

      resid._lock( this );
      resid.model( userModel );
      resid.optimisation( m_cfg.optimisation );

      xsid.emulation( resid );
      sid = xsid;
      return 0;
    }

    private void sidSamples( bool enable )
    {
      sbyte gain = 0;
      xsid.sidSamples( enable );

      // Now balance voices
      if ( !enable )
      {
        gain = -25;
      }

      xsid.gain( (short)( -100 - gain ) );
      sid = xsid.emulation();
      sid.gain( gain );
      sid = xsid;
    }

    private void reset()
    {
      m_playerState = SID2Types.sid2_player_t.sid2_stopped;
      m_running = false;
      m_sid2crc = 0xffffffff;
      m_info.sid2crc = m_sid2crc ^ 0xffffffff;
      m_sid2crcCount = m_info.sid2crcCount = 0;

      // Select Sidplay1 compatible CPU or real thing
      sid6510.environment( m_info.environment );

      m_scheduler.reset();

      sid.reset( (short)0x0f );
      // Synchronize the waveform generators
      // (must occur after reset)
      sid.write( (short)0x04, (short)0x08 );
      sid.write( (short)0x0b, (short)0x08 );
      sid.write( (short)0x12, (short)0x08 );
      sid.write( (short)0x04, (short)0x00 );
      sid.write( (short)0x0b, (short)0x00 );
      sid.write( (short)0x12, (short)0x00 );

      if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
      {
        cia.reset();
        cia2.reset();
        vic.reset();
      }
      else
      {
        sid6526.reset( m_cfg.powerOnDelay <= SID2Types.SID2_MAX_POWER_ON_DELAY );
        sid6526.write( (short)0x0e, (short)1 ); // Start timer
        if ( m_tuneInfo.songSpeed == SidTune.SIDTUNE_SPEED_VBI )
        {
          sid6526._lock();
        }
      }

      // Initialize Memory
      m_port_pr_in = 0;
      for ( int i = 0; i < m_ram.Length; i++ )
      {
        m_ram[i] = 0;
      }
      switch ( m_info.environment )
      {
        case SID2Types.sid2_env_t.sid2_envPS:
          break;
        case SID2Types.sid2_env_t.sid2_envR:
          {
            // Initialize RAM with powerup pattern
            for ( int i = 0x07c0; i < 0x10000; i += 128 )
            {
              for ( int j = 0; j < 64; j++ )
              {
                m_ram[i + j] = 0xff;
              }
            }
            for ( int i = 0; i < m_rom.Length; i++ )
            {
              m_rom[i] = 0;
            }
            break;
          }
        default:
          for ( int i = 0; i < m_rom.Length; i++ )
          {
            m_rom[i] = 0;
          }
          for ( int i = 0; i < 0x2000; i++ )
          {
            m_rom[0xA000 + i] = OpCode.RTSn;
          }
          break;
      }

      if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
      {
        for ( int i = 0; i < memKernal.KERNAL.Length; i++ )
        {
          m_rom[0xe000 + i] = memKernal.KERNAL[i];
        }
        for ( int i = 0; i < memChar.CHAR.Length; i++ )
        {
          m_rom[0xd000 + i] = memChar.CHAR[i];
        }
        m_rom[0xfd69] = 0x9f; // Bypass memory check
        m_rom[0xe55f] = 0x00; // Bypass screen clear
        m_rom[0xfdc4] = 0xea; // Ignore sid volume reset to avoid DC
        m_rom[0xfdc5] = 0xea; // click (potentially incompatibility)!!
        m_rom[0xfdc6] = 0xea;
        if ( m_tuneInfo.compatibility == SidTune.SIDTUNE_COMPATIBILITY_BASIC )
        {
          for ( int i = 0; i < memBasic.BASIC.Length; i++ )
          {
            m_rom[0xa000 + i] = memBasic.BASIC[i];
          }
        }

        // Copy in power on settings. These were created by running
        // the kernel reset routine and storing the useful values
        // from $0000-$03ff. Format is:
        // -offset byte (bit 7 indicates presence rle byte)
        // -rle count byte (bit 7 indicates compression used)
        // data (single byte) or quantity represented by uncompressed count
        // -all counts and offsets are 1 less than they should be
        int addr = 0;
        for ( int i = 0; i < memPowerOn.POWERON.Length; )
        {
          short off = memPowerOn.POWERON[i++];
          short count = 0;
          bool compressed = false;

          // Determine data count/compression
          if ( ( off & 0x80 ) != 0 )
          {
            // fixup offset
            off &= 0x7f;
            count = memPowerOn.POWERON[i++];
            if ( ( count & 0x80 ) != 0 )
            {
              // fixup count
              count &= 0x7f;
              compressed = true;
            }
          }

          // Fix count off by ones (see format details)
          count++;
          addr += off;

          // Extract compressed data
          if ( compressed )
          {
            short data = memPowerOn.POWERON[i++];
            while ( count-- > 0 )
            {
              m_ram[addr++] = data;
            }
          }
          // Extract uncompressed data
          else
          {
            while ( count-- > 0 )
            {
              m_ram[addr++] = memPowerOn.POWERON[i++];
            }
          }
        }
      }
      else // !sid2_envR
      {
        for ( int i = 0; i < 0x2000; i++ )
        {
          m_rom[0xE000 + i] = OpCode.RTSn;
        }
        // fake VBI-interrupts that do $D019, BMI ...
        m_rom[0x0d019] = 0xff;
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envPS )
        {
          m_ram[0xff48] = OpCode.JMPi;
          SIDEndian.endian_little16( m_ram, 0xff49, 0x0314 );
        }

        // Software vectors
        SIDEndian.endian_little16( m_ram, 0x0314, 0xEA31 ); // IRQ
        SIDEndian.endian_little16( m_ram, 0x0316, 0xFE66 ); // BRK
        SIDEndian.endian_little16( m_ram, 0x0318, 0xFE47 ); // NMI
                                                            // Hardware vectors
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envPS )
        {
          SIDEndian.endian_little16( m_rom, 0xfffa, 0xFFFA ); // NMI
        }
        else
        {
          SIDEndian.endian_little16( m_rom, 0xfffa, 0xFE43 ); // NMI
        }
        SIDEndian.endian_little16( m_rom, 0xfffc, 0xFCE2 ); // RESET
        SIDEndian.endian_little16( m_rom, 0xfffe, 0xFF48 ); // IRQ
        for ( int i = 0; i < 6; i++ )
        {
          m_ram[0xfffa + i] = m_rom[0xfffa + i];
        }
      }

      // Will get done later if can't now
      if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_PAL )
      {
        m_ram[0x02a6] = 1;
      }
      else
      {
        // SIDTUNE_CLOCK_NTSC
        m_ram[0x02a6] = 0;
      }
    }

    /// <summary>
    /// Temporary hack till real bank switching code added
    /// </summary>
    /// <param name="addr">A 16-bit effective address</param>
    /// <returns>A default bank-select value for $01</returns>
    private short iomap( int addr )
    {
      if ( m_info.environment != SID2Types.sid2_env_t.sid2_envPS )
      {
        // Force Real C64 Compatibility
        switch ( m_tuneInfo.compatibility )
        {
          case SidTune.SIDTUNE_COMPATIBILITY_R64:
          case SidTune.SIDTUNE_COMPATIBILITY_BASIC:
            return 0; // Special case, converted to 0x37 later
        }

        if ( addr == 0 )
        {
          return 0; // Special case, converted to 0x37 later
        }
        if ( addr < 0xa000 )
        {
          return 0x37; // Basic-ROM, Kernal-ROM, I/O
        }
        if ( addr < 0xd000 )
        {
          return 0x36; // Kernal-ROM, I/O
        }
        if ( addr >= 0xe000 )
        {
          return 0x35; // I/O only
        }
      }
      return 0x34; // RAM only (special I/O in PlaySID mode)
    }

    internal short readMemByte_plain( int addr )
    {
      // Bank Select Register Value DOES NOT get to ram
      if ( addr > 1 )
      {
        return m_ram[addr & 0xffff];
      }
      else if ( addr != 0 )
      {
        return m_port_pr_in;
      }
      return m_port_ddr;
    }

    private short readMemByte_io( int addr )
    {
      int tempAddr = (addr & 0xfc1f);

      // Not SID ?
      if ( ( tempAddr & 0xff00 ) != 0xd400 )
      {
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
        {
          switch ( SIDEndian.endian_16hi8( addr ) )
          {
            case 0:
            case 1:
              return readMemByte_plain( addr );
            case 0xdc:
              return cia.read( (short)( addr & 0x0f ) );
            case 0xdd:
              return cia2.read( (short)( addr & 0x0f ) );
            case 0xd0:
            case 0xd1:
            case 0xd2:
            case 0xd3:
              return vic.read( (short)( addr & 0x3f ) );
            default:
              return m_rom[addr & 0xffff];
          }
        }
        else
        {
          switch ( SIDEndian.endian_16hi8( addr ) )
          {
            case 0:
            case 1:
              return readMemByte_plain( addr );
            // Sidplay1 Random Extension CIA
            case 0xdc:
              return sid6526.read( (short)( addr & 0x0f ) );
            // Sidplay1 Random Extension VIC
            case 0xd0:
              switch ( addr & 0x3f )
              {
                case 0x11:
                case 0x12:
                  return sid6526.read( (short)( ( addr - 13 ) & 0x0f ) );
              }
              // Deliberate run on
              return m_rom[addr & 0xffff];
            default:
              return m_rom[addr & 0xffff];
          }
        }
      }

      // Read real sid for these
      //int i = m_sidmapper[(addr >> 5) & (SID2_MAPPER_SIZE - 1)];
      //return sid[i].read((short)(tempAddr & 0xff));
      return sid.read( (short)( tempAddr & 0xff ) );
    }

    internal short readMemByte_sidplaytp( int addr )
    {
      if ( addr < 0xD000 )
      {
        return readMemByte_plain( addr );
      }
      else
      {
        // Get high-nibble of address.
        switch ( addr >> 12 )
        {
          case 0xd:
            if ( isIO )
              return readMemByte_io( addr );
            else
              return m_ram[addr];
          // break;
          case 0xe:
          case 0xf:
          default: // <-- just to please the compiler
            return m_ram[addr & 0xffff];
        }
      }
    }

    internal short readMemByte_sidplaybs( int addr )
    {
      if ( addr < 0xA000 )
      {
        return readMemByte_plain( addr );
      }
      else
      {
        // Get high-nibble of address.
        switch ( addr >> 12 )
        {
          case 0xa:
          case 0xb:
            if ( isBasic )
            {
              return m_rom[addr];
            }
            else
            {
              return m_ram[addr];
            }
          // break;
          case 0xc:
            return m_ram[addr];
          // break;
          case 0xd:
            if ( isIO )
            {
              return readMemByte_io( addr );
            }
            else if ( isChar )
            {
              return m_rom[addr];
            }
            else
            {
              return m_ram[addr];
            }
          // break;
          case 0xe:
          case 0xf:
          default: // <-- just to please the compiler
            if ( isKernal )
            {
              return m_rom[addr & 0xffff];
            }
            else
            {
              return m_ram[addr & 0xffff];
            }
        }
      }
    }

    private void writeMemByte_plain( int addr, short data )
    {
      if ( addr > 1 )
      {
        m_ram[addr & 0xffff] = data;
      }
      else if ( addr != 0 )
      {
        // Determine new memory configuration.
        evalBankSelect( data );
      }
      else
      {
        m_port_ddr = data;
        evalBankSelect( m_port_pr_out );
      }
    }

    internal void writeMemByte_playsid( int addr, short data )
    {
      int tempAddr = (addr & 0xfc1f);

      // Not SID ?
      if ( ( tempAddr & 0xff00 ) != 0xd400 )
      {
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
        {
          switch ( SIDEndian.endian_16hi8( addr ) )
          {
            case 0:
            case 1:
              writeMemByte_plain( addr, data );
              return;
            case 0xdc:
              cia.write( (short)( addr & 0x0f ), data );
              return;
            case 0xdd:
              cia2.write( (short)( addr & 0x0f ), data );
              return;
            case 0xd0:
            case 0xd1:
            case 0xd2:
            case 0xd3:
              vic.write( (short)( addr & 0x3f ), data );
              return;
            default:
              m_rom[addr & 0xffff] = data;
              return;
          }
        }
        else
        {
          switch ( SIDEndian.endian_16hi8( addr ) )
          {
            case 0:
            case 1:
              writeMemByte_plain( addr, data );
              return;
            case 0xdc: // Sidplay1 CIA
              sid6526.write( (short)( addr & 0x0f ), data );
              return;
            default:
              m_rom[addr & 0xffff] = data;
              return;
          }
        }
      }

      // $D41D/1E/1F, $D43D/3E/3F, ...
      // Map to real address to support PlaySID
      // Extended SID Chip Registers.
      sid2crc( data );
      if ( ( tempAddr & 0x00ff ) >= 0x001d )
      {
        xsid.write16( addr & 0x01ff, data );
      }
      else // Mirrored SID.
      {
        // Convert address to that acceptable by resid
        sid.write( (short)( tempAddr & 0xff ), data );
      }
    }

    internal void writeMemByte_sidplay( int addr, short data )
    {
      if ( addr < 0xA000 )
      {
        writeMemByte_plain( addr, data );
      }
      else
      {
        // Get high-nibble of address.
        switch ( addr >> 12 )
        {
          case 0xa:
          case 0xb:
          case 0xc:
            m_ram[addr] = data;
            break;
          case 0xd:
            if ( isIO )
            {
              writeMemByte_playsid( addr, data );
            }
            else
            {
              m_ram[addr] = data;
            }
            break;
          case 0xe:
          case 0xf:
          default: // <-- just to please the compiler
            m_ram[addr & 0xffff] = data;
            break;
        }
      }
    }


    internal readMemDelegate mem_readMemByte;
    internal writeMemDelegate mem_writeMemByte;
    internal readMemDelegate mem_readMemDataByte;


    /// <summary>
    /// This resets the cpu once the program is loaded to begin running. Also
    /// called when the emulation crashes
    /// </summary>
    /// <param name="safe"></param>
    internal void Reset( bool safe )
    {
      if ( safe )
      {
        // Emulation crashed so run in safe mode
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
        {
          short[] prg = { OpCode.LDAb, 0x7f, OpCode.STAa, 0x0d, 0xdc, OpCode.RTSn };
          sid2_info_t info = new sid2_info_t();
          SidTuneInfo tuneInfo = new SidTuneInfo();
          // Install driver
          tuneInfo.relocStartPage = 0x09;
          tuneInfo.relocPages = 0x20;
          tuneInfo.initAddr = 0x0800;
          tuneInfo.songSpeed = SidTune.SIDTUNE_SPEED_CIA_1A;
          info.environment = m_info.environment;
          psidDrvReloc( tuneInfo, info );
          // Install prg & driver
          for ( int i = 0; i < prg.Length; i++ )
          {
            m_ram[0x0800 + i] = prg[i];
          }
          psidDrvInstall( info );
        }
        else
        {
          // If there is no irqs, song wont continue
          sid6526.reset();
        }

        // Make sid silent
        sid.reset( (short)0 );
      }

      m_port_ddr = 0x2F;

      // defaults: Basic-ROM on, Kernal-ROM on, I/O on
      if ( m_info.environment != SID2Types.sid2_env_t.sid2_envR )
      {
        short song = (short)(m_tuneInfo.currentSong - 1);
        short bank = iomap(m_tuneInfo.initAddr);
        evalBankSelect( bank );
        m_playBank = iomap( m_tuneInfo.playAddr );
        if ( m_info.environment != SID2Types.sid2_env_t.sid2_envPS )
        {
          sid6510.reset( m_tuneInfo.initAddr, song, (short)0, (short)0 );
        }
        else
        {
          sid6510.reset( m_tuneInfo.initAddr, song, song, song );
        }
      }
      else
      {
        evalBankSelect( (short)0x37 );
        sid6510.reset();
      }

      mixerReset();
      xsid.suppress( true );
    }

    private delegate long OutputDelegate( short[] buffer, int off );

    private OutputDelegate output;

    // 8 bit sound output generation routines

    internal long monoOut8MonoIn( short[] buffer, int off )
    {
      buffer[off] = (short)( (byte)( sid.output( (short)8 ) ^ 0x80 ) );
      return 1;
    }

    internal long stereoOut8MonoIn( short[] buffer, int off )
    {
      short sample = (short)((byte)(sid.output((short)8) ^ 0x80));
      buffer[off + 0] = sample;
      buffer[off + 1] = sample;
      return 2;
    }

    // 16 bit sound output generation routines

    internal long monoOut16MonoIn( short[] buffer, int off )
    {
      SIDEndian.endian_16( buffer, off, (int)sid.output( (short)16 ) );
      return 2;
    }

    internal long stereoOut16MonoIn( short[] buffer, int off )
    {
      int sample = (int)sid.output((short)16);
      SIDEndian.endian_16( buffer, off, sample );
      SIDEndian.endian_16( buffer, off + 2, sample );
      return ( 4 );
    }

    public void interruptIRQ( bool state )
    {
      if ( state )
      {
        if ( m_info.environment == SID2Types.sid2_env_t.sid2_envR )
        {
          sid6510.triggerIRQ();
        }
        else
        {
          fakeIRQ();
        }
      }
      else
      {
        sid6510.clearIRQ();
      }
    }

    public void interruptNMI()
    {
      sid6510.triggerNMI();
    }

    public void interruptRST()
    {
      stop();
    }

    public void signalAEC( bool state )
    {
      sid6510.aecSignal( state );
    }

    public short readMemRamByte( int addr )
    {
      return m_ram[addr];
    }

    /// <summary>
    /// Used for sid2crc (tracking sid register writes)
    /// </summary>
    private static long[] crc32Table = {
            0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419,
            0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4,
            0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07,
            0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
            0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856,
            0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
            0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4,
            0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
            0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
            0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A,
            0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599,
            0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
            0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190,
            0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F,
            0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E,
            0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
            0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED,
            0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
            0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3,
            0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
            0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A,
            0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5,
            0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010,
            0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
            0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17,
            0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6,
            0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
            0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
            0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344,
            0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
            0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A,
            0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
            0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1,
            0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C,
            0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF,
            0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
            0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE,
            0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31,
            0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C,
            0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
            0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B,
            0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
            0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1,
            0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
            0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
            0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7,
            0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66,
            0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
            0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605,
            0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8,
            0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B,
            0x2D02EF8D };

    public void sid2crc( short data )
    {
      if ( m_sid2crcCount < m_cfg.sid2crcCount )
      {
        m_info.sid2crcCount = ++m_sid2crcCount;
        m_sid2crc = ( m_sid2crc >> 8 ) ^ crc32Table[(int)( ( m_sid2crc & 0xFF ) ^ data )];
        m_info.sid2crc = m_sid2crc ^ 0xffffffff;
      }
    }

    public void lightpen()
    {
      vic.lightpen();
    }

    // PSID driver

    private int psidDrvReloc( SidTuneInfo tuneInfo, sid2_info_t info )
    {
      int relocAddr;
      int startlp = tuneInfo.loadAddr >> 8;
      int endlp = (int)((tuneInfo.loadAddr + (tuneInfo.c64dataLen - 1)) >> 8);

      if ( info.environment != SID2Types.sid2_env_t.sid2_envR )
      {
        // Sidplay1 modes require no psid driver
        info.driverAddr = 0;
        info.driverLength = 0;
        info.powerOnDelay = 0;
        return 0;
      }

      if ( tuneInfo.compatibility == SidTune.SIDTUNE_COMPATIBILITY_BASIC )
      {
        // The psiddrv is only used for initialization and to autorun basic
        // tunes as running the kernel falls into a manual load/run mode
        tuneInfo.relocStartPage = 0x04;
        tuneInfo.relocPages = 0x03;
      }

      // Check for free space in tune
      if ( tuneInfo.relocStartPage == PSIDDRV_MAX_PAGE )
      {
        tuneInfo.relocPages = 0;
      }
      // Check if we need to find the reloc addr
      else if ( tuneInfo.relocStartPage == 0 )
      {
        // Tune is clean so find some free ram around the load image
        psidRelocAddr( tuneInfo, startlp, endlp );
      }

      if ( tuneInfo.relocPages < 1 )
      {
        //m_errorstring = ERR_PSIDDRV_NO_SPACE;
        return -1;
      }

      relocAddr = tuneInfo.relocStartPage << 8;

      // Place psid driver into ram
      short[] reloc_driver = memPSIDDrv.PSIDDRV;
      int reloc_size = memPSIDDrv.PSIDDRV.Length;

      BufPos bp;
      if ( ( bp = reloc65( reloc_driver, reloc_size, relocAddr - 10 ) ) == null )
      {
        //m_errorstring = ERR_PSIDDRV_RELOC;
        return -1;
      }
      reloc_driver = bp.fBuf;
      int reloc_driverPos = bp.fPos;
      reloc_size = bp.fSize;

      // Adjust size to not included initialization data.
      reloc_size -= 10;
      info.driverAddr = relocAddr;
      info.driverLength = (int)reloc_size;
      // Round length to end of page
      info.driverLength += 0xff;
      info.driverLength &= 0xff00;

      m_rom[0xfffc] = reloc_driver[reloc_driverPos + 0];// RESET
      m_rom[0xfffd] = reloc_driver[reloc_driverPos + 1];// RESET

      // If not a basic tune then the psiddrv must install
      // interrupt hooks and trap programs trying to restart basic
      if ( tuneInfo.compatibility == SidTune.SIDTUNE_COMPATIBILITY_BASIC )
      {
        // Install hook to set subtune number for basic
        short[] prg = { OpCode.LDAb, (short)(tuneInfo.currentSong - 1), OpCode.STAa, 0x0c, 0x03, OpCode.JSRw, 0x2c, 0xa8, OpCode.JMPw, 0xb1, 0xa7 };
        for ( int i = 0; i < prg.Length; i++ )
        {
          m_rom[0xbf53 + i] = prg[i];
        }
        m_rom[0xa7ae] = OpCode.JMPw;
        SIDEndian.endian_little16( m_rom, 0xa7af, 0xbf53 );
      }
      else
      {
        // Only install irq handle for RSID tunes
        if ( tuneInfo.compatibility == SidTune.SIDTUNE_COMPATIBILITY_R64 )
        {
          m_ram[0x0314] = reloc_driver[reloc_driverPos + 2];
          m_ram[0x0315] = reloc_driver[reloc_driverPos + 2 + 1];
        }
        else
        {
          m_ram[0x0314] = reloc_driver[reloc_driverPos + 2];
          m_ram[0x0315] = reloc_driver[reloc_driverPos + 2 + 1];
          m_ram[0x0316] = reloc_driver[reloc_driverPos + 2 + 2];
          m_ram[0x0317] = reloc_driver[reloc_driverPos + 2 + 3];
          m_ram[0x0318] = reloc_driver[reloc_driverPos + 2 + 4];
          m_ram[0x0319] = reloc_driver[reloc_driverPos + 2 + 5];
        }
        // Experimental restart basic trap
        int addr;
        addr = SIDEndian.endian_little16( reloc_driver, reloc_driverPos + 8 );
        m_rom[0xa7ae] = OpCode.JMPw;
        SIDEndian.endian_little16( m_rom, 0xa7af, 0xffe1 );
        SIDEndian.endian_little16( m_ram, 0x0328, addr );
      }
      // Install driver to rom so it can be copied later into
      // ram once the tune is installed.
      for ( int i = 0; i < reloc_size; i++ )
      {
        m_rom[i] = reloc_driver[reloc_driverPos + 10 + i];
      }

      // Setup the Initial entry point

      short[] addr2 = m_rom;
      int pos = 0;

      // Tell C64 about song
      addr2[pos++] = (short)( tuneInfo.currentSong - 1 );
      if ( tuneInfo.songSpeed == SidTune.SIDTUNE_SPEED_VBI )
      {
        addr2[pos] = 0;
      }
      else
      {
        // SIDTUNE_SPEED_CIA_1A
        addr2[pos] = 1;
      }

      pos++;
      SIDEndian.endian_little16( addr2, pos, tuneInfo.compatibility == SidTune.SIDTUNE_COMPATIBILITY_BASIC ? 0xbf55 : tuneInfo.initAddr );
      pos += 2;
      SIDEndian.endian_little16( addr2, pos, tuneInfo.playAddr );
      pos += 2;
      // Initialize random number generator
      info.powerOnDelay = m_cfg.powerOnDelay;
      // Delays above MAX result in random delays
      if ( info.powerOnDelay > SID2Types.SID2_MAX_POWER_ON_DELAY )
      {
        // Limit the delay to something sensible.
        info.powerOnDelay = (short)( m_rand >> 3 ) & SID2Types.SID2_MAX_POWER_ON_DELAY;
      }
      SIDEndian.endian_little16( addr2, pos, info.powerOnDelay );
      pos += 2;
      m_rand = m_rand * 13 + 1;
      addr2[pos++] = iomap( m_tuneInfo.initAddr );
      addr2[pos++] = iomap( m_tuneInfo.playAddr );
      addr2[pos + 1] = ( addr2[pos + 0] = m_ram[0x02a6] ); // PAL/NTSC flag
      pos++;

      // Add the required tune speed
      switch ( m_tune.Info.clockSpeed )
      {
        case SidTune.SIDTUNE_CLOCK_PAL:
          addr2[pos++] = 1;
          break;
        case SidTune.SIDTUNE_CLOCK_NTSC:
          addr2[pos++] = 0;
          break;
        default: // UNKNOWN or ANY
          pos++;
          break;
      }

      // Default processor register flags on calling init
      if ( tuneInfo.compatibility >= SidTune.SIDTUNE_COMPATIBILITY_R64 )
      {
        addr2[pos++] = 0;
      }
      else
      {
        addr2[pos++] = 1 << MOS6510.SR_INTERRUPT;
      }

      return 0;
    }

    /// <summary>
    /// The driver is relocated above and here is actually installed into ram.
    /// The two operations are now split to allow the driver to be installed
    /// inside the load image
    /// </summary>
    /// <param name="info"></param>
    private void psidDrvInstall( sid2_info_t info )
    {
      for ( int i = 0; i < info.driverLength; i++ )
      {
        m_ram[info.driverAddr + i] = m_rom[i];
      }
    }

    private void psidRelocAddr( SidTuneInfo tuneInfo, int startp, int endp )
    {
      // Used memory ranges.
      bool[] pages = new bool[256];
      int[] used = { 0x00, 0x03, 0xa0, 0xbf, 0xd0, 0xff, startp, (startp <= endp) && (endp <= 0xff) ? endp : 0xff };

      // Mark used pages in table.
      for ( int i = 0; i < pages.Length; i++ )
      {
        pages[i] = false;
      }
      for ( int i = 0; i < used.Length; i += 2 )
      {
        for ( int page = used[i]; page <= used[i + 1]; page++ )
        {
          pages[page] = true;
        }
      }

      // Find largest free range.
      int relocPages, lastPage = 0;
      tuneInfo.relocPages = 0;
      for ( int page = 0; page < pages.Length; page++ )
      {
        if ( pages[page] )
        {
          relocPages = page - lastPage;
          if ( relocPages > tuneInfo.relocPages )
          {
            tuneInfo.relocStartPage = (short)lastPage;
            tuneInfo.relocPages = (short)relocPages;
          }
          lastPage = page + 1;
        }
      }

      if ( tuneInfo.relocPages == 0 )
      {
        tuneInfo.relocStartPage = PSIDDRV_MAX_PAGE;
      }
    }

    /// <summary>
    /// Set the ICs environment variable to point to this player
    /// </summary>
    public InternalPlayer()
    {
      // Set default settings for system
      m_scheduler = new EventScheduler( "SIDPlayer" );
      // Environment Function entry Points
      sid6510 = new SID6510( m_scheduler, this );
      nullsid = new NullSID();
      xsid = new XSID( this, nullsid );
      cia = new C64cia1( this );
      cia2 = new C64cia2( this );
      sid6526 = new SID6526( this );
      vic = new C64VIC( this );
      mixerEvent = new EventMixer( this );
      rtc = new EventRTC( m_scheduler );
      m_tune = null;
      m_ram = null;
      m_rom = null;
      //m_errorstring = TXT_NA;
      m_fastForwardFactor = 1.0;
      m_mileage = 0;
      m_playerState = SID2Types.sid2_player_t.sid2_stopped;
      m_running = false;
      m_sid2crc = 0xffffffff;
      m_sid2crcCount = 0;
      m_sampleCount = 0;

#if DEBUG
      m_rand = 1;
#else
            m_rand = (int)DateTime.Now.Millisecond;// System.currentTimeMillis();
#endif

      // SID Initialize
      sid = nullsid;
      xsid.emulation( sid );
      sid = xsid;

      // Setup sid mapping table
      for ( int i = 0; i < SID2_MAPPER_SIZE; i++ )
      {
        m_sidmapper[i] = 0;
      }


      // Setup exported info
      //m_info.credits = credit;
      m_info.channels = 1;
      m_info.driverAddr = 0;
      m_info.driverLength = 0;
      m_info.eventContext = m_scheduler;
      // Number of SIDs support by this library
      m_info.maxsids = SID2_MAX_SIDS;
      m_info.environment = SID2Types.sid2_env_t.sid2_envR;
      m_info.sid2crc = 0;
      m_info.sid2crcCount = 0;

      // Configure default settings
      m_cfg.clockDefault = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      m_cfg.clockForced = false;
      m_cfg.clockSpeed = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      m_cfg.environment = m_info.environment;
      m_cfg.forceDualSids = false;
      m_cfg.frequency = SID2Types.SID2_DEFAULT_SAMPLING_FREQ;
      m_cfg.optimisation = SID2Types.SID2_DEFAULT_OPTIMISATION;
      m_cfg.playback = SID2Types.sid2_playback_t.sid2_mono;
      m_cfg.precision = SID2Types.SID2_DEFAULT_PRECISION;
      m_cfg.sidDefault = SID2Types.sid2_model_t.SID2_MODEL_CORRECT;
      m_cfg.sidModel = SID2Types.sid2_model_t.SID2_MODEL_CORRECT;
      m_cfg.sidSamples = true;
      m_cfg.volume = 255;
      m_cfg.sampleFormat = SID2Types.sid2_sample_t.SID2_LITTLE_SIGNED;
      m_cfg.powerOnDelay = SID2Types.SID2_DEFAULT_POWER_ON_DELAY;
      m_cfg.sid2crcCount = 0;

      config( m_cfg );
    }
    // only used for deserializing
    public InternalPlayer( BinaryReader reader )
    {
      LoadFromReader( reader );

      SetOutput( m_cfg );
    }

    public sid2_config_t config()
    {
      return m_cfg;
    }

    public sid2_info_t info()
    {
      return m_info;
    }

    public int config( sid2_config_t cfg )
    {
      bool monosid = false;

      if ( m_running )
      {
        //m_errorstring = ERR_CONF_WHILST_ACTIVE;
        return -1;
      }

      // Check for base sampling frequency
      if ( cfg.frequency < 4000 )
      {
        //m_errorstring = ERR_UNSUPPORTED_FREQ;
        return -1;
      }

      // Check for legal precision
      switch ( cfg.precision )
      {
        case 8:
        case 16:
        case 24:
          if ( cfg.precision > SID2Types.SID2_MAX_PRECISION )
          {
            //m_errorstring = ERR_UNSUPPORTED_PRECISION;
            return -1;
          }
          break;

        default:
          //m_errorstring = ERR_UNSUPPORTED_PRECISION;
          return -1;
      }

      // Only do these if we have a loaded tune
      if ( m_tune != null && m_tune.StatusOk )
      {
        if ( m_playerState != SID2Types.sid2_player_t.sid2_paused )
        {
          m_tuneInfo = m_tune.Info;
        }

        ReSID rsid = new ReSID();
        rsid.filter( false );
        rsid._lock( this );
        rsid.sampling( m_cfg.frequency );

        // SID emulation setup (must be performed before the environment setup call)
        if ( sidCreate( rsid, cfg.sidModel, cfg.sidDefault ) < 0 )
        {
          //m_errorstring = cfg.sidEmulation.error();
          ///m_cfg.sidEmulation = null;
          // Try restoring old configuration
          if ( m_cfg != cfg )
          {
            config( m_cfg );
          }
          return -1;
        }

        if ( m_playerState != SID2Types.sid2_player_t.sid2_paused )
        {
          double cpuFreq;
          // Must be this order:
          // Determine clock speed
          cpuFreq = clockSpeed( cfg.clockSpeed, cfg.clockDefault, cfg.clockForced );
          m_samplePeriod = (long)( cpuFreq / (double)cfg.frequency * ( 1 << 16 ) * m_fastForwardFactor );
          // Setup fake cia
          sid6526.clock( (int)( cpuFreq / VIC_FREQ_PAL + 0.5 ) );
          if ( m_tuneInfo.songSpeed == SidTune.SIDTUNE_SPEED_CIA_1A || m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_NTSC )
          {
            sid6526.clock( (int)( cpuFreq / VIC_FREQ_NTSC + 0.5 ) );
          }

          // Setup TOD clock
          if ( m_tuneInfo.clockSpeed == SidTune.SIDTUNE_CLOCK_PAL )
          {
            cia.clock( cpuFreq / VIC_FREQ_PAL );
            cia2.clock( cpuFreq / VIC_FREQ_PAL );
          }
          else
          {
            cia.clock( cpuFreq / VIC_FREQ_NTSC );
            cia2.clock( cpuFreq / VIC_FREQ_NTSC );
          }

          // Configure, setup and install C64 environment/events
          if ( environment( cfg.environment ) < 0 )
          {
            // Try restoring old configuration
            if ( m_cfg != cfg )
            {
              config( m_cfg );
            }
            return -1;
          }
          // Start the real time clock event
          rtc.clock( cpuFreq );
        }
      }
      sidSamples( cfg.sidSamples );

      // Setup sid mapping table
      // Note this should be based on m_tuneInfo.sidChipBase1
      // but this is only temporary code anyway

      for ( int i = 0; i < SID2_MAPPER_SIZE; i++ )
      {
        m_sidmapper[i] = 0;
      }
      if ( m_tuneInfo.sidChipBase2 != 0 )
      {
        monosid = false;
        // Assumed to be in d4xx-d7xx range
        m_sidmapper[( m_tuneInfo.sidChipBase2 >> 5 ) & ( SID2_MAPPER_SIZE - 1 )] = 1;
      }

      // All parameters check out, so configure player
      monosid = m_tuneInfo.sidChipBase2 == 0;
      m_info.channels = 1;

      // Only force dual sids if second wasn't detected
      if ( monosid && cfg.forceDualSids )
      {
        monosid = false;
        m_sidmapper[( 0xd500 >> 5 ) & ( SID2_MAPPER_SIZE - 1 )] = 1; // Assumed
      }

      if ( cfg.playback != SID2Types.sid2_playback_t.sid2_mono )
      {
        if ( cfg.playback == SID2Types.sid2_playback_t.sid2_left )
        {
          xsid.mute( true );
        }
      }

      // Setup the audio side, depending on the audio hardware
      // and the information returned by sidtune
      SetOutput( cfg );

      // Update Configuration
      m_cfg = cfg;

      if ( m_cfg.optimisation > SID2Types.SID2_MAX_OPTIMISATION )
      {
        m_cfg.optimisation = (byte)SID2Types.SID2_MAX_OPTIMISATION;
      }
      return 0;

    }

    private void SetOutput( sid2_config_t cfg )
    {
      bool monosid = m_tuneInfo.sidChipBase2 == 0;

      switch ( cfg.precision )
      {
        case 8:
          if ( monosid )
          {
            if ( cfg.playback == SID2Types.sid2_playback_t.sid2_stereo )
            {
              output = new OutputDelegate( stereoOut8MonoIn );
            }
            else
            {
              output = new OutputDelegate( monoOut8MonoIn );
            }
          }
          else
          {
            output = new OutputDelegate( monoOut8MonoIn );
          }
          break;

        case 16:
          if ( monosid )
          {
            if ( cfg.playback == SID2Types.sid2_playback_t.sid2_stereo )
            {
              output = new OutputDelegate( stereoOut16MonoIn );
            }
            else
            {
              output = new OutputDelegate( monoOut16MonoIn );
            }
          }
          else
          {
            output = new OutputDelegate( monoOut16MonoIn );
          }
          break;
      }
    }

    public int fastForward( int percent )
    {
      if ( percent > 3200 )
      {
        //m_errorstring = "SIDPLAYER ERROR: Percentage value out of range";
        return -1;
      }

      double fastForwardFactor;
      fastForwardFactor = (double)percent / 100.0;
      m_samplePeriod = (long)( (double)m_samplePeriod / m_fastForwardFactor * fastForwardFactor );
      m_fastForwardFactor = fastForwardFactor;

      return 0;
    }

    public int load( SidTune tune )
    {
      m_tune = tune;
      if ( tune == null || !tune.StatusOk )
      {
        // Unload tune
        //m_info.tuneInfo = null;
        return 0;
      }

      // Un-mute all voices
      xsid.mute( false );

      short v = 3;
      while ( ( v-- ) != 0 )
      {
        sid.voice( v, (short)0, false );
      }

      // Must re-configure on fly for stereo support!
      int ret = config(m_cfg);
      // Failed configuration with new tune, reject it
      if ( ret < 0 )
      {
        m_tune = null;
        return -1;
      }

      return 0;
    }

    public void pause()
    {
      if ( m_playerState == SID2Types.sid2_player_t.sid2_playing )
      {
        m_playerState = SID2Types.sid2_player_t.sid2_paused;
        m_running = false;
      }
    }

    public void resume()
    {
      if ( m_playerState == SID2Types.sid2_player_t.sid2_paused )
      {
        m_playerState = SID2Types.sid2_player_t.sid2_playing;
      }
    }

    public void start()
    {
      m_playerState = SID2Types.sid2_player_t.sid2_playing;
    }

    public long play( short[] buffer, int length )
    {
      inPlay = true;

      // Make sure a _tune is loaded
      if ( !m_tune.StatusOk )
      {
        return 0;
      }

      // Setup Sample Information
      m_sampleIndex = 0;
      m_sampleCount = length;
      m_sampleBuffer = buffer;

      // Start the player loop
      m_running = m_playerState == SID2Types.sid2_player_t.sid2_playing;

      while ( m_running )
      {
        m_scheduler.clock();
      }

      if ( m_playerState == SID2Types.sid2_player_t.sid2_stopped )
      {
        initialise();
      }

      inPlay = false;

      return m_sampleIndex;
    }

    public SID2Types.sid2_player_t State
    {
      get
      {
        return m_playerState;
      }
    }

    public void stop()
    {
      // Re-start song
      if ( m_tune != null && m_tune.StatusOk && ( m_playerState != SID2Types.sid2_player_t.sid2_stopped ) )
      {
        if ( !m_running )
        {
          initialise();
        }
        else
        {
          m_playerState = SID2Types.sid2_player_t.sid2_stopped;
          m_running = false;
        }
      }
    }

    private int read_options( short[] buf, int pos )
    {
      int c, l = 0;

      c = buf[pos + 0];
      while ( ( c != 0 ) && c != EOF )
      {
        c &= 255;
        l += c;
        c = buf[pos + l];
      }
      return ++l;
    }

    private int read_undef( short[] buf, int pos )
    {
      int n, l = 2;

      n = buf[pos + 0] + 256 * buf[pos + 1];
      while ( n != 0 )
      {
        n--;
        while ( buf[pos + ( l++ )] == 0 )
        {
          /*noop*/
        }
      }
      return l;
    }

    private int reloc_seg( short[] buf, int bufPos, int len, short[] rtab, int rtabPos, file65 fp )
    {
      int adr = -1;
      int type, seg, old, newv;
      while ( rtab[rtabPos] != 0 )
      {
        if ( ( rtab[rtabPos] & 255 ) == 255 )
        {
          adr += 254;
          rtabPos++;
        }
        else
        {
          adr += rtab[rtabPos] & 255;
          rtabPos++;
          type = rtab[rtabPos] & 0xe0;
          seg = rtab[rtabPos] & 0x07;
          rtabPos++;
          switch ( type )
          {
            case 0x80:
              old = buf[bufPos + adr] + 256 * buf[bufPos + adr + 1];
              newv = old + reldiff( seg, fp );
              buf[bufPos + adr] = (short)( newv & 255 );
              buf[bufPos + adr + 1] = (short)( ( newv >> 8 ) & 255 );
              break;
            case 0x40:
              old = buf[bufPos + adr] * 256 + rtab[rtabPos];
              newv = old + reldiff( seg, fp );
              buf[bufPos + adr] = (short)( ( newv >> 8 ) & 255 );
              rtab[rtabPos] = (short)( newv & 255 );
              rtabPos++;
              break;
            case 0x20:
              old = buf[bufPos + adr];
              newv = old + reldiff( seg, fp );
              buf[bufPos + adr] = (short)( newv & 255 );
              break;
          }
          if ( seg == 0 )
          {
            rtabPos += 2;
          }
        }
      }
      return ++rtabPos;
    }

    private int reloc_globals( short[] buf, int bufPos, file65 fp )
    {
      int n, old, newv, seg;

      n = buf[bufPos + 0] + 256 * buf[bufPos + 1];
      bufPos += 2;

      while ( n != 0 )
      {
        while ( ( buf[bufPos++] ) != 0 )
        {
          /*NOOP*/
        }
        seg = buf[bufPos];
        old = buf[bufPos + 1] + 256 * buf[bufPos + 2];
        newv = old + reldiff( seg, fp );
        buf[bufPos + 1] = (short)( newv & 255 );
        buf[bufPos + 2] = (short)( ( newv >> 8 ) & 255 );
        bufPos += 3;
        n--;
      }
      return bufPos;
    }

    private BufPos reloc65( short[] buf, int fsize, int addr )
    {
      file65 file = new file65();
      char[] cmp = { (char)1, (char)0, 'o', '6', '5' };

      int mode, hlen;

      bool tflag = false, dflag = false, bflag = false, zflag = false;
      int tbase = 0, dbase = 0, bbase = 0, zbase = 0;
      int extract = 0;

      file.buf = buf;
      tflag = true;
      tbase = addr;
      extract = 1;

      for ( int i = 0; i < cmp.Length; i++ )//for (int i = 0; i < 5; i++)
      {
        if ( file.buf[i] != cmp[i] )
        {
          return null;
        }
      }

      mode = file.buf[7] * 256 + file.buf[6];
      if ( ( mode & 0x2000 ) != 0 )
      {
        return null;
      }
      else if ( ( mode & 0x4000 ) != 0 )
      {
        return null;
      }

      hlen = BUF + read_options( file.buf, BUF );

      file.tbase = file.buf[9] * 256 + file.buf[8];
      file.tlen = file.buf[11] * 256 + file.buf[10];
      file.tdiff = tflag ? tbase - file.tbase : 0;
      file.dbase = file.buf[13] * 256 + file.buf[12];
      file.dlen = file.buf[15] * 256 + file.buf[14];
      file.ddiff = dflag ? dbase - file.dbase : 0;
      file.bbase = file.buf[17] * 256 + file.buf[16];
      //file.blen = file.buf[19] * 256 + file.buf[18];
      file.bdiff = bflag ? bbase - file.bbase : 0;
      file.zbase = file.buf[21] * 256 + file.buf[20];
      //file.zlen = file.buf[23] * 256 + file.buf[21];
      file.zdiff = zflag ? zbase - file.zbase : 0;

      file.segt = file.buf;
      int segtPos = hlen;
      file.segd = file.segt;
      int sehdPos = segtPos + file.tlen;
      file.utab = file.segd;
      int utabPos = sehdPos + file.dlen;

      file.rttab = file.utab;
      int rttabPos = utabPos + read_undef(file.utab, utabPos);

      file.rdtab = file.rttab;
      file.extab = file.rdtab;
      int rdtabPos = reloc_seg(file.segt, segtPos, file.tlen, file.rttab, rttabPos, file);
      int extabPos = reloc_seg(file.segd, sehdPos, file.dlen, file.rdtab, rdtabPos, file);

      reloc_globals( file.extab, extabPos, file );

      if ( tflag )
      {
        file.buf[9] = (short)( ( tbase >> 8 ) & 255 );
        file.buf[8] = (short)( tbase & 255 );
      }
      if ( dflag )
      {
        file.buf[13] = (short)( ( dbase >> 8 ) & 255 );
        file.buf[12] = (short)( dbase & 255 );
      }
      if ( bflag )
      {
        file.buf[17] = (short)( ( bbase >> 8 ) & 255 );
        file.buf[16] = (short)( bbase & 255 );
      }
      if ( zflag )
      {
        file.buf[21] = (short)( ( zbase >> 8 ) & 255 );
        file.buf[20] = (short)( zbase & 255 );
      }

      switch ( extract )
      {
        case 0: // whole file
          return new BufPos( buf, 0, fsize );
        case 1: // text segment
          return new BufPos( file.segt, segtPos, file.tlen );
        case 2:
          return new BufPos( file.segd, sehdPos, file.dlen );
        default:
          return null;
      }
    }

    private int reldiff( int s, file65 fp )
    {
      return ( ( ( s ) == 2 ) ? fp.tdiff : ( ( ( s ) == 3 ) ? fp.ddiff : ( ( ( s ) == 4 ) ? fp.bdiff : ( ( ( s ) == 5 ) ? fp.zdiff : 0 ) ) ) );
    }

    /// <summary>
    /// Used for Serializing a running player
    /// </summary>
    /// <param name="writer"></param>
    public void SaveToWriter( BinaryWriter writer )
    {
#if DEBUG
      writer.Write( "Start" );
#endif

      m_scheduler.SaveToWriter( writer );

#if DEBUG
      writer.Write( "Events" );
#endif

      // build list of all events
      EventList events = new EventList();
      events.Add( m_scheduler.m_timeWarp );
      events.AddEvent( vic );
      events.AddEvent( cia.event_ta );
      events.AddEvent( cia.event_tb );
      events.AddEvent( cia.event_tod );
      events.AddEvent( cia2.event_ta );
      events.AddEvent( cia2.event_tb );
      events.AddEvent( cia2.event_tod );
      events.AddEvent( sid6526.m_taEvent );
      events.AddEvent( sid6510.cpuEvent );
      events.AddEvent( xsid.xsidEvent );
      events.AddEvent( xsid.ch4.galwayEvent );
      events.AddEvent( xsid.ch4.sampleEvent );
      events.AddEvent( xsid.ch5.galwayEvent );
      events.AddEvent( xsid.ch5.sampleEvent );
      events.AddEvent( mixerEvent );
      events.AddEvent( rtc );

      events.SaveToWriter( writer );

#if DEBUG
      writer.Write( "sid6510" );
#endif
      sid6510.SaveToWriter( writer, sid6510.delayCycle );

#if DEBUG
      writer.Write( "xsid" );
#endif
      xsid.SaveToWriter( writer );

#if DEBUG
      writer.Write( "cia1" );
#endif
      cia.SaveToWriter( writer );

#if DEBUG
      writer.Write( "cia2" );
#endif
      cia2.SaveToWriter( writer );

#if DEBUG
      writer.Write( "sid6526" );
#endif
      sid6526.SaveToWriter( writer );

#if DEBUG
      writer.Write( "SIDs" );
#endif
      writer.Write( (short)sid.GetEmuType() );

      if ( sid is XSID )
      {
        if ( sid == xsid )
        {
          writer.Write( true );
        }
        else
        {
          writer.Write( false );
          sid.SaveToWriter( writer );
        }
      }
      else
      {
        sid.SaveToWriter( writer );
      }

      writer.Write( m_sidmapper.Length );
      for ( int i = 0; i < m_sidmapper.Length; i++ )
      {
        writer.Write( m_sidmapper[i] );
      }

      m_tune.SaveToWriter( writer );

      writer.Write( m_ram.Length );
      for ( int i = 0; i < m_ram.Length; i++ )
      {
        writer.Write( m_ram[i] );
      }

      writer.Write( m_rom.Length );
      for ( int i = 0; i < m_rom.Length; i++ )
      {
        writer.Write( m_rom[i] );
      }

      m_info.SaveToWriter( writer );
      m_cfg.SaveToWriter( writer );

      writer.Write( m_fastForwardFactor );
      writer.Write( m_mileage );

      writer.Write( (short)m_playerState );

      //m_running
      writer.Write( m_rand );
      writer.Write( m_sid2crc );
      writer.Write( m_sid2crcCount );
      writer.Write( m_sampleClock );
      writer.Write( m_samplePeriod );
      //m_sampleCount
      //m_sampleIndex
      //m_sampleBuffer
      writer.Write( m_port_pr_out );
      writer.Write( m_port_ddr );
      writer.Write( m_port_pr_in );
      writer.Write( m_playBank );
      writer.Write( isKernal );
      writer.Write( isBasic );
      writer.Write( isIO );
      writer.Write( isChar );
    }
    /// <summary>
    /// Used for Deserializing a running player
    /// </summary>
    /// <param name="reader"></param>
    public void LoadFromReader( BinaryReader reader )
    {
      int count;

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // Start
#endif
      int id = reader.ReadInt32();
      reader.ReadInt16();
      m_scheduler = new EventScheduler( this, reader, id );

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // Events
#endif
      EventList events = new EventList(this, m_scheduler, reader);
      m_scheduler.m_timeWarp = events.GetEventById( m_scheduler.m_timeWarp_id ) as EventTimeWarp;

      vic = events.GetVIC();

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // sid6510
#endif
      sid6510 = new SID6510( m_scheduler, this, reader, events );


      nullsid = new NullSID();

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // xsid
#endif
      xsid = new XSID( this, reader, events );

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // cia1
#endif
      cia = new C64cia1( this, reader, events );

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // cia2
#endif
      cia2 = new C64cia2( this, reader, events );

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // sid6526
#endif
      sid6526 = new SID6526( this, reader, events );

#if DEBUG
      Debug.WriteLine( reader.ReadString() ); // SIDs
#endif
      SIDEmu.SIDEmuType typ = (SIDEmu.SIDEmuType)reader.ReadInt16();
      switch ( typ )
      {
        case SIDEmu.SIDEmuType.nullsid:
          sid = new NullSID();
          break;
        case SIDEmu.SIDEmuType.xsid:
          if ( reader.ReadBoolean() )
          {
            sid = xsid;
          }
          else
          {
            sid = new XSID( this, reader, events );
          }
          break;
        case SIDEmu.SIDEmuType.resid:
          sid = new ReSID( m_scheduler, reader );
          break;
      }

      count = reader.ReadInt32();
      m_sidmapper = new int[count];
      for ( int i = 0; i < m_sidmapper.Length; i++ )
      {
        m_sidmapper[i] = reader.ReadInt32();
      }

      mixerEvent = events.GetMixer();
      rtc = events.GetRTC();

      m_tune = new SidTune( reader );
      m_tuneInfo = m_tune.info;

      count = reader.ReadInt32();
      m_ram = new short[count];
      for ( int i = 0; i < m_ram.Length; i++ )
      {
        m_ram[i] = reader.ReadInt16();
      }

      count = reader.ReadInt32();
      m_rom = new short[count];
      for ( int i = 0; i < m_rom.Length; i++ )
      {
        m_rom[i] = reader.ReadInt16();
      }

      m_info = new sid2_info_t( reader );
      m_info.eventContext = m_scheduler;
      m_cfg = new sid2_config_t( reader );

      m_fastForwardFactor = reader.ReadDouble();
      m_mileage = reader.ReadInt64();

      m_playerState = (SID2Types.sid2_player_t)reader.ReadInt16();

      m_rand = reader.ReadInt32();
      m_sid2crc = reader.ReadInt64();
      m_sid2crcCount = reader.ReadInt64();
      m_sampleClock = reader.ReadInt64();
      m_samplePeriod = reader.ReadInt64();

      m_port_pr_out = reader.ReadInt16();
      m_port_ddr = reader.ReadInt16();
      m_port_pr_in = reader.ReadInt16();
      m_playBank = reader.ReadInt16();
      isKernal = reader.ReadBoolean();
      isBasic = reader.ReadBoolean();
      isIO = reader.ReadBoolean();
      isChar = reader.ReadBoolean();


      switch ( m_info.environment )
      {
        case SID2Types.sid2_env_t.sid2_envPS:
          m_rom = m_ram;
          mem_readMemByte = new readMemDelegate( readMemByte_plain );
          mem_writeMemByte = new writeMemDelegate( writeMemByte_playsid );
          mem_readMemDataByte = new readMemDelegate( readMemByte_plain );
          break;

        case SID2Types.sid2_env_t.sid2_envTP:
          mem_readMemByte = new readMemDelegate( readMemByte_plain );
          mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
          mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaytp );
          break;

        // case sid2_envTR:
        case SID2Types.sid2_env_t.sid2_envBS:
          mem_readMemByte = new readMemDelegate( readMemByte_plain );
          mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
          mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaybs );
          break;

        case SID2Types.sid2_env_t.sid2_envR:
        default:
          mem_readMemByte = new readMemDelegate( readMemByte_sidplaybs );
          mem_writeMemByte = new writeMemDelegate( writeMemByte_sidplay );
          mem_readMemDataByte = new readMemDelegate( readMemByte_sidplaybs );
          break;
      }
    }

    internal void Sleep()
    {
      if ( m_info.environment != SID2Types.sid2_env_t.sid2_envR )
      {
        // Start the sample sequence
        xsid.suppress( false );
        xsid.suppress( true );
      }
    }

    internal bool CheckBankJump( int addr )
    {
      switch ( m_info.environment )
      {
        case SID2Types.sid2_env_t.sid2_envBS:
          if ( addr >= 0xA000 )
          {
            // Get high-nibble of address.
            switch ( addr >> 12 )
            {
              case 0xa:
              case 0xb:
                if ( isBasic )
                  return false;
                break;

              case 0xc:
                break;

              case 0xd:
                if ( isIO )
                  return false;
                break;

              case 0xe:
              case 0xf:
              default:
                if ( isKernal )
                {
                  return false;
                }
                break;
            }
          }
          break;

        case SID2Types.sid2_env_t.sid2_envTP:
          if ( ( addr >= 0xd000 ) && isKernal )
          {
            return false;
          }
          break;

        default:
          break;
      }

      return true;
    }
  }
}