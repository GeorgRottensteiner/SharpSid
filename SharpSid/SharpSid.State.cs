using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;



namespace SharpSid
{
  public enum State
  {
    STOPPED,
    PAUSED,
    PLAYING
  }

}
