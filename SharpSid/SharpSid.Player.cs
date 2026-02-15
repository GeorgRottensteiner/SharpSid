using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;



namespace SharpSid
{
  public class Player : IDisposable
  {
    private const int                 MULTIPLIER_SHIFT = 4;
    private const int                 MULTIPLIER_VALUE = 1 << MULTIPLIER_SHIFT;

    private const int                 _Frequency = 44100;
    private const int                 _ByteBufferSize = 2 * _Frequency;
    private const int                 _ShortBufferSize = _ByteBufferSize / 2;

    private bool                      _IsStereo = false;

    private volatile Thread           _Thread = null;
    private volatile bool             _Aborting = false;

    private int                       _PlayBufferSize = 16384;
    private IntPtr                    _PlayBuffer = IntPtr.Zero;

    private IWavePlayer               _WavePlayer;
    private BufferedWaveProvider      _BufferedWaveProvider;

    private short[]                   _ShortBuffer;
    private byte[]                    _ByteBuffer;

    private bool                      _Aborted = true;

    private object                    _LockObj = new object();

    private InternalPlayer            _InternalPlayer;

    private SidTune                   _CurrentTune = null;



    /// <summary>
    /// Create a new Instance of Player
    /// </summary>
    public Player()
    {
      init();
    }



    private void init()
    {
      _ShortBuffer = new short[_ShortBufferSize];
      _ByteBuffer = new byte[_ByteBufferSize];

      _PlayBuffer = Marshal.AllocHGlobal( _PlayBufferSize );
    }



    private void Filler()
    {
      int playedSize = (int)_InternalPlayer.play( _ShortBuffer, _PlayBufferSize );

      int pos = playedSize;
      int idx = 2 * playedSize;

      if ( _IsStereo )
      {
        while ( pos > 0 )
        {

          int sl  = (short)( (short)(_ShortBuffer[--pos] << 8 ) | ( _ShortBuffer[--pos] ) );
          int sr  = (short)( (short)(_ShortBuffer[--pos] << 8 ) | ( _ShortBuffer[--pos] ) );
          sl      = (int)( sl * MULTIPLIER_VALUE ) >> MULTIPLIER_SHIFT;
          sr      = (int)( sr * MULTIPLIER_VALUE ) >> MULTIPLIER_SHIFT;

          _ByteBuffer[--idx] = (byte)( sl >> 8 );
          _ByteBuffer[--idx] = (byte)( sl & 0xff );
          _ByteBuffer[--idx] = (byte)( sr >> 8 );
          _ByteBuffer[--idx] = (byte)( sr & 0xff );
        }
      }
      else
      {
        while ( pos > 0 )
        {
          int s   = (short)( (short)( _ShortBuffer[--pos] << 8 ) | ( _ShortBuffer[--pos] ) );
          s       = (int)( s * MULTIPLIER_VALUE ) >> MULTIPLIER_SHIFT;
          byte sl = (byte)(s >> 8);
          byte sr = (byte)(s & 0xFF);

          _ByteBuffer[--idx] = sl;
          _ByteBuffer[--idx] = sr;
          _ByteBuffer[--idx] = sl;
          _ByteBuffer[--idx] = sr;
        }
      }

      _BufferedWaveProvider.AddSamples( _ByteBuffer, 0, playedSize * 2 );
    }



    public bool LoadSIDInfoFromFile( string Filename, out SidTuneInfo Info )
    {
      Info = null;
      try
      {
        using ( FileStream file = new FileStream( Filename, FileMode.Open, FileAccess.Read ) )
        {
          var tempTune = new SidTune( file );

          if ( !tempTune.StatusOk )
          {
            return false;
          }
          Info = tempTune.info;
          return true;
        }
      }
      catch ( Exception )
      {
        return false;
      }
    }



    public bool LoadSIDInfoFromStream( Stream IOIn, out SidTuneInfo Info )
    {
      Info = null;
      try
      {
        using ( IOIn )
        {
          var tempTune = new SidTune( IOIn );

          if ( !tempTune.StatusOk )
          {
            return false;
          }
          Info = tempTune.info;
          return true;
        }
      }
      catch ( Exception )
      {
        return false;
      }
    }



    public bool LoadSIDFromFile( string Filename )
    {
      Stop();
      try
      {
        using ( FileStream file = new FileStream( Filename, FileMode.Open, FileAccess.Read ) )
        {
          return LoadSIDFromStream( file );
        }
      }
      catch ( Exception )
      {
        return false;
      }
    }



    public bool LoadSIDFromStream( Stream mem )
    {
      Stop();

      if ( mem == null )
      {
        return false;
      }

      _CurrentTune = new SidTune( mem );

      return _CurrentTune.StatusOk;
    }



    /// <summary>
    /// returns the current Status of the Player
    /// </summary>
    public State State
    {
      get
      {
        if ( _InternalPlayer != null )
        {
          switch ( _InternalPlayer.State )
          {
            case SID2Types.sid2_player_t.sid2_paused:
              return State.PAUSED;
            case SID2Types.sid2_player_t.sid2_playing:
              return State.PLAYING;
            case SID2Types.sid2_player_t.sid2_stopped:
            default:
              return State.STOPPED;
          }
        }
        return State.STOPPED;
      }
    }



    public SidTuneInfo TuneInfo
    {
      get
      {
        if ( _CurrentTune != null )
        {
          return _CurrentTune.Info;
        }
        return new SidTuneInfo();
      }
    }



    /// <summary>
    ///  Start playing the tune with the default song
    /// </summary>
    /// <param name="tune">SidTune</param>
    public void Start()
    {
      if ( State == State.PLAYING )
      {
        return;
      }
      Start( 0 );
    }



    /// <summary>
    /// Start playing the tune with the selected song
    /// </summary>
    /// <param name="SongNumber">song id (1..count), 0 = default song</param>
    public void Start( int SongNumber )
    {
      if ( Stopping )
      {
        return;
      }
      if ( _CurrentTune == null )
      {
        return;
      }

      _WavePlayer = new WaveOut();

      WaveFormat    fmt                     = new WaveFormat( _Frequency, 16, 2 );
      _BufferedWaveProvider                 = new BufferedWaveProvider( fmt );
      _BufferedWaveProvider.BufferDuration  = TimeSpan.FromSeconds( 2 ); // allow us to get well ahead of ourselves

      _WavePlayer.Init( _BufferedWaveProvider );

      _InternalPlayer = new InternalPlayer();

      sid2_config_t config = _InternalPlayer.config();

      config.frequency      = _Frequency;
      config.playback       = SID2Types.sid2_playback_t.sid2_mono;
      config.optimisation   = SID2Types.SID2_DEFAULT_OPTIMISATION;
      config.sidModel       = (SID2Types.sid2_model_t)_CurrentTune.Info.sidModel;
      config.clockDefault   = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      config.clockSpeed     = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      config.clockForced    = false;
      config.environment    = SID2Types.sid2_env_t.sid2_envR;
      config.forceDualSids  = false;
      config.volume         = 255;
      config.sampleFormat   = SID2Types.sid2_sample_t.SID2_LITTLE_SIGNED;
      config.sidDefault     = SID2Types.sid2_model_t.SID2_MODEL_CORRECT;
      config.sidSamples     = true;
      config.precision      = SID2Types.SID2_DEFAULT_PRECISION;

      _InternalPlayer.config( config );

      _CurrentTune.selectSong( SongNumber );
      _InternalPlayer.load( _CurrentTune );

      _IsStereo = _CurrentTune.isStereo;

      _InternalPlayer.start();

      _Thread = new Thread( new ThreadStart( ThreadProc ) );
      _Thread.Start();
    }



    private void ThreadProc()
    {
      _WavePlayer.Play();
      while ( !_Aborting )
      {
        Thread.Sleep( 20 );

        if ( ( _BufferedWaveProvider != null )
        &&   ( _BufferedWaveProvider.BufferLength - _BufferedWaveProvider.BufferedBytes >= _BufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4 ) )
        {
          Filler();
        }
      }
      _Thread = null;
    }



    /// <summary>
    /// stop playing the current tune
    /// </summary>
    public void Stop()
    {
      if ( _CurrentTune == null )
      {
        return;
      }

      if ( Stopping )
      {
        return;
      }

      lock ( _LockObj )
      {
        _Aborting = true;

        if ( _WavePlayer != null )
        {
          _WavePlayer.Stop();
          _WavePlayer.Dispose();
          _WavePlayer = null;
        }

        while ( _Thread != null )
        {
          Thread.Sleep( 10 );
        }
        if ( _InternalPlayer != null )
        {
          _InternalPlayer.stop();
        }
        _Aborting = false;
      }
    }



    /// <summary>
    /// pause playing
    /// </summary>
    public void Pause()
    {
      if ( Stopping )
      {
        return;
      }

      if ( ( _InternalPlayer != null )
      &&   ( _InternalPlayer.State == SID2Types.sid2_player_t.sid2_playing ) )
      {
        _WavePlayer.Pause();
        _InternalPlayer.pause();
        while ( _InternalPlayer.inPlay )
        {
          Thread.Sleep( 1 );
        }
      }
    }



    /// <summary>
    /// resume playing
    /// </summary>
    public void Resume()
    {
      if ( Stopping )
      {
        return;
      }

      _WavePlayer?.Play();
      _InternalPlayer?.resume();
    }



    /// <summary>
    /// is Player currently stopping?
    /// </summary>
    public bool Stopping
    {
      get
      {
        return ( ( _Aborting )
        &&       ( !_Aborted ) );
      }
    }



    public void Dispose()
    {
      Stop();

      if ( _PlayBuffer != IntPtr.Zero )
      {
        Marshal.FreeHGlobal( _PlayBuffer );
        _PlayBuffer = IntPtr.Zero;
      }
      _InternalPlayer = null;
    }



    private static byte[] StringToByteArrayFastest( string hex )
    {
      if ( hex.Length % 2 == 1 )
        throw new Exception( "The binary key cannot have an odd number of digits" );

      byte[] arr = new byte[hex.Length >> 1];

      for ( int i = 0; i < hex.Length >> 1; ++i )
      {
        arr[i] = (byte)( ( GetHexVal( hex[i << 1] ) << 4 ) + ( GetHexVal( hex[( i << 1 ) + 1] ) ) );
      }

      return arr;
    }



    private static int GetHexVal( char hex )
    {
      int val = (int)hex;
      //For uppercase A-F letters:
      //return val - (val < 58 ? 48 : 55);
      //For lowercase a-f letters:
      //return val - (val < 58 ? 48 : 87);
      //Or the two combined, but a bit slower:
      return val - ( val < 58 ? 48 : ( val < 97 ? 55 : 87 ) );
    }



    public bool InjectMemory( string hexData, int dataStartAddress, int cpuPos = -1 )
    {
      if ( _InternalPlayer == null )
      {
        return false;
      }
      //hexData = "0B080A009E32303631000000A90F8D18D4A9018DE20D20E108205409ADE20DF0F5A9008DE20DADE30D205C084C1708ACF109B9DF098DE709A9008D04D4A200E004F009BDE3099DEA099D00D4E8E007D0EEADE7098DEE098D04D460AABDFA0929038DF109A8B9DF098DEE09BDFA094A4A8DF309BD5E0A8DEA09BDC20A8DEB09BD260B8DEC09BD8A0B8DED09BDEE0B8DEF09BD520C8DF009BDB60C8DF509BD1A0D8DF709BD7E0D8DF9094CAD08AEEA09ACEB098EE3098CE409A202BDEA099DE309E8E007D0F5ADF3098DF209ADF9098DF809ADF7098DF609ADF5098DF4094C3008AD12D0C9F8F0F9AD12D0C9F8D0F96086578458A000B1574A4A8DF3098DF209B1572903A88CF109B9DF098DE709A9008D04D4A200A001E004F00CB1579DE3099DEA099D00D4C8E8E007D0EBA004ADE7099DEE098D04D4A007B1578DF4098DF509C8B1578DF6098DF709C8B1578DF8098DF90960ACF209B966098D6409B96A098D65094CFFFF6E9DA3C609090909CEF609F021ADF409100CADE409186DF40990134C8D09ADE409186DF409B0078DE4098D01D460A9008DF4098D04D460CEF609F0F260CEF609D01DADE409186DF8098DE4098D01D4A9008DF409ADF7098DF609A9008DF20960CEF809D011ADF9098DF809ADF40949FF1869018DF4094C6E0911214181000000000000000000000000000000000000000000000002020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525252525254D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4D4DC5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5C5EDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDEDED95959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595959595BDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBDBD030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303030303035E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E5E323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232320000";
      //dataStartAddress = 0x0801;

      // inject code
      var  byteData = StringToByteArrayFastest( hexData );
      for ( int i = 0; i < byteData.Length; ++i )
      {
        _InternalPlayer.mem_writeMemByte( dataStartAddress + i, byteData[i] );
      }

      if ( cpuPos != -1 )
      {
        _InternalPlayer.SetCPUPos( cpuPos );
      }
      _InternalPlayer.Reset( false );
      return true;
    }



    /// <summary>
    /// Inject regular program and set start address
    /// </summary>
    /// <param name="HexData"></param>
    /// <param name="DataStartAddress"></param>
    /// <param name="InitialAddress"></param>
    /// <returns></returns>
    public bool PlayFromBinary( string HexData, int DataStartAddress, int InitialAddress )
    {
      Stop();

      var  byteData = StringToByteArrayFastest( HexData );

      _CurrentTune = new SidTune();

      _CurrentTune.info.loadAddr = DataStartAddress;
      _CurrentTune.info.c64dataLen = byteData.Length;
      _CurrentTune.info.initAddr = InitialAddress;
      _CurrentTune.info.playAddr = InitialAddress;
      _CurrentTune.info.compatibility = SidTune.SIDTUNE_COMPATIBILITY_R64;

      if ( InitialAddress == 0 )
      {
        _CurrentTune.info.compatibility = SidTune.SIDTUNE_COMPATIBILITY_BASIC;
      }

      _CurrentTune.InjectProgramInMemory( byteData, DataStartAddress );
      _CurrentTune.status = true;

      _WavePlayer = new WaveOut();

      WaveFormat    fmt                     = new WaveFormat( _Frequency, 16, 2 );
      _BufferedWaveProvider = new BufferedWaveProvider( fmt );
      _BufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds( 2 ); // allow us to get well ahead of ourselves

      _WavePlayer.Init( _BufferedWaveProvider );

      _InternalPlayer = new InternalPlayer();

      sid2_config_t config = _InternalPlayer.config();

      config.frequency = _Frequency;
      config.playback = SID2Types.sid2_playback_t.sid2_mono;
      config.optimisation = SID2Types.SID2_DEFAULT_OPTIMISATION;
      config.sidModel = (SID2Types.sid2_model_t)_CurrentTune.Info.sidModel;
      config.clockDefault = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      config.clockSpeed = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
      config.clockForced = false;
      config.environment = SID2Types.sid2_env_t.sid2_envR;
      config.forceDualSids = false;
      config.volume = 255;
      config.sampleFormat = SID2Types.sid2_sample_t.SID2_LITTLE_SIGNED;
      config.sidDefault = SID2Types.sid2_model_t.SID2_MODEL_CORRECT;
      config.sidSamples = true;
      config.precision = SID2Types.SID2_DEFAULT_PRECISION;
      config.environment = SID2Types.sid2_env_t.sid2_envR;

      _InternalPlayer.load( _CurrentTune );
      _InternalPlayer.config( config );

      // inject code
      for ( int i = 0; i < byteData.Length; ++i )
      {
        _InternalPlayer.mem_writeMemByte( DataStartAddress + i, byteData[i] );
      }

      _InternalPlayer.SetCPUPos( InitialAddress );
      _IsStereo = _CurrentTune.isStereo;

      _InternalPlayer.start();

      _Thread = new Thread( new ThreadStart( ThreadProc ) );
      _Thread.Start();

      return true;
    }



    public void SetVolume( int Volume )
    {
      if ( Volume < 0 )
      {
        Volume = 0;
      }
      if ( Volume > 100 )
      {
        Volume = 100;
      }
      if ( _WavePlayer != null )
      {
        _WavePlayer.Volume = Volume * 0.01f;
      }
    }


  }
}
