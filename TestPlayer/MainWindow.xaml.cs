using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using System.IO;
using System.Net;

using Microsoft.Win32;

using SharpSid;

namespace TestPlayer
{
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private Player      _Player;



    public MainWindow()
    {
      InitializeComponent();

      _Player = new Player();
    }



    private void Window_Closed( object sender, EventArgs e )
    {
      _Player.Dispose();
    }



    private void btn_play_Click( object sender, RoutedEventArgs e )
    {
      if ( _Player.State != State.PLAYING )
      {
        _Player.Start();
      }
    }



    private void btn_stop_Click( object sender, RoutedEventArgs e )
    {
      _Player.Stop();
    }



    private void btn_pause_Click( object sender, RoutedEventArgs e )
    {
      Mouse.OverrideCursor = Cursors.Wait;
      if ( _Player.State == State.PAUSED )
      {
        _Player.Resume();
      }
      else
      {
        _Player.Pause();
      }
      Mouse.OverrideCursor = Cursors.Arrow;
    }



    private void btn_prev_Click( object sender, RoutedEventArgs e )
    {
      if ( _Player.TuneInfo.currentSong > 1 )
      {
        Mouse.OverrideCursor = Cursors.Wait;

        switch ( _Player.State )
        {
          case State.PLAYING:
          case State.PAUSED:
            _Player.Stop();
            break;
        }

        sp_songInfo.DataContext = null;

        _Player.Start( _Player.TuneInfo.currentSong - 1 );

        sp_songInfo.DataContext = _Player.TuneInfo;

        Mouse.OverrideCursor = Cursors.Arrow;
      }
    }



    private void btn_next_Click( object sender, RoutedEventArgs e )
    {
      if ( _Player.TuneInfo.currentSong < _Player.TuneInfo.songs )
      {
        Mouse.OverrideCursor = Cursors.Wait;

        switch ( _Player.State )
        {
          case State.PLAYING:
          case State.PAUSED:
            _Player.Stop();
            break;
        }

        sp_songInfo.DataContext = null;

        _Player.Start( _Player.TuneInfo.currentSong + 1 );

        sp_songInfo.DataContext = _Player.TuneInfo;

        Mouse.OverrideCursor = Cursors.Arrow;
      }
    }



    private void btn_loadFile_Click( object sender, RoutedEventArgs e )
    {
      Mouse.OverrideCursor = Cursors.Wait;
      try
      {
        if ( File.Exists( tb_filename.Text ) )
        {
          try
          {
            _Player.Stop();

              /*
            using ( FileStream file = new FileStream( tb_filename.Text, FileMode.Open, FileAccess.Read ) )
            {
              _CurrentTune = new SidTune( file );
            }

            if ( _CurrentTune.StatusOk )*/

            if ( _Player.LoadSIDFromFile( tb_filename.Text ) )
            {
              _Player.Start();

              grp_tune.DataContext = _Player.TuneInfo;
              sp_songInfo.DataContext = _Player.TuneInfo;
            }
            else
            {
              MessageBox.Show( "Error loading file" + Environment.NewLine + "Unknown data format", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }
          }
          catch ( Exception ex )
          {
            MessageBox.Show( "Error loading file" + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
          }
        }
        else
        {
          MessageBox.Show( "File not found" + Environment.NewLine + tb_filename.Text, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
        }
      }
      finally
      {
        Mouse.OverrideCursor = Cursors.Arrow;
      }
    }



    private void btn_download_Click( object sender, RoutedEventArgs e )
    {
      try
      {
        Mouse.OverrideCursor = Cursors.Wait;
        this.IsEnabled = false;

        _Player.Stop();

        using ( WebClient client = new WebClient() )
        {
          client.OpenReadCompleted += new OpenReadCompletedEventHandler( client_OpenReadCompleted );
          client.OpenReadAsync( new Uri( tb_url.Text ), Path.GetExtension( tb_url.Text ) );
        }
      }
      catch ( Exception ex )
      {
        MessageBox.Show( "Error downloading file" + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
      }
    }



    private void client_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
    {
      this.IsEnabled = true;
      Mouse.OverrideCursor = Cursors.Arrow;

      if ( e.Error == null )
      {
        if ( !e.Cancelled )
        {
          using ( MemoryStream mem = new MemoryStream() )
          {
            using ( Stream downloadStream = e.Result )
            {
              downloadStream.CopyTo( mem );
            }

            mem.Position = 0;

            if ( _Player.LoadSIDFromStream( mem ) )
            {
              _Player.Start();

              grp_tune.DataContext = _Player.TuneInfo;
              sp_songInfo.DataContext = _Player.TuneInfo;
            }
            else
            {
              MessageBox.Show( "Error downloading file" + Environment.NewLine + "Unknown data format", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
            }
          }
        }
        else
        {
          MessageBox.Show( "Error downloading file" + Environment.NewLine + "Operation cancelled", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
        }
      }
      else
      {
        MessageBox.Show( "Error downloading file" + Environment.NewLine + e.Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
      }
    }



    private void btn_open_Click( object sender, RoutedEventArgs e )
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.FileName = tb_filename.Text;
      dlg.Multiselect = false;

      if ( dlg.ShowDialog( this ) == true )
      {
        tb_filename.Text = dlg.FileName;
      }
    }



  }
}
