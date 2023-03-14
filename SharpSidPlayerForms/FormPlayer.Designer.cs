
namespace SharpSidPlayerForms
{
  partial class FormPlayer
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.btnPlay = new System.Windows.Forms.Button();
      this.btnStop = new System.Windows.Forms.Button();
      this.btnPause = new System.Windows.Forms.Button();
      this.btnPrevious = new System.Windows.Forms.Button();
      this.btnNext = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.labelSongName = new System.Windows.Forms.Label();
      this.btnOpenSong = new System.Windows.Forms.Button();
      this.listSongs = new System.Windows.Forms.ListView();
      this.columnName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.btnTogglePlaylist = new System.Windows.Forms.Button();
      this.btnAddSongToList = new System.Windows.Forms.Button();
      this.btnRemoveSong = new System.Windows.Forms.Button();
      this.btnClearList = new System.Windows.Forms.Button();
      this.btnSaveList = new System.Windows.Forms.Button();
      this.btnLoadPlaylist = new System.Windows.Forms.Button();
      this.btnSettings = new System.Windows.Forms.Button();
      this.labelSongLength = new System.Windows.Forms.Label();
      this.songPlayingTimer = new System.Windows.Forms.Timer(this.components);
      this.labelSongNumber = new System.Windows.Forms.Label();
      this.scrollVolume = new System.Windows.Forms.HScrollBar();
      this.btnMoveUp = new System.Windows.Forms.Button();
      this.btnMoveDown = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // btnPlay
      // 
      this.btnPlay.Location = new System.Drawing.Point(55, 146);
      this.btnPlay.Name = "btnPlay";
      this.btnPlay.Size = new System.Drawing.Size(36, 23);
      this.btnPlay.TabIndex = 0;
      this.btnPlay.Text = "►";
      this.btnPlay.UseVisualStyleBackColor = true;
      this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
      // 
      // btnStop
      // 
      this.btnStop.Location = new System.Drawing.Point(139, 146);
      this.btnStop.Name = "btnStop";
      this.btnStop.Size = new System.Drawing.Size(36, 23);
      this.btnStop.TabIndex = 0;
      this.btnStop.Text = "█";
      this.btnStop.UseVisualStyleBackColor = true;
      this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
      // 
      // btnPause
      // 
      this.btnPause.Location = new System.Drawing.Point(97, 146);
      this.btnPause.Name = "btnPause";
      this.btnPause.Size = new System.Drawing.Size(36, 23);
      this.btnPause.TabIndex = 0;
      this.btnPause.Text = "||";
      this.btnPause.UseVisualStyleBackColor = true;
      this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
      // 
      // btnPrevious
      // 
      this.btnPrevious.Location = new System.Drawing.Point(13, 146);
      this.btnPrevious.Name = "btnPrevious";
      this.btnPrevious.Size = new System.Drawing.Size(36, 23);
      this.btnPrevious.TabIndex = 0;
      this.btnPrevious.Text = "|<";
      this.btnPrevious.UseVisualStyleBackColor = true;
      this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
      // 
      // btnNext
      // 
      this.btnNext.Location = new System.Drawing.Point(181, 146);
      this.btnNext.Name = "btnNext";
      this.btnNext.Size = new System.Drawing.Size(36, 23);
      this.btnNext.TabIndex = 0;
      this.btnNext.Text = ">|";
      this.btnNext.UseVisualStyleBackColor = true;
      this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(11, 9);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(35, 13);
      this.label1.TabIndex = 1;
      this.label1.Text = "Song:";
      // 
      // labelSongName
      // 
      this.labelSongName.Location = new System.Drawing.Point(52, 9);
      this.labelSongName.Name = "labelSongName";
      this.labelSongName.Size = new System.Drawing.Size(341, 13);
      this.labelSongName.TabIndex = 1;
      this.labelSongName.Text = "Song Name";
      // 
      // btnOpenSong
      // 
      this.btnOpenSong.Location = new System.Drawing.Point(312, 146);
      this.btnOpenSong.Name = "btnOpenSong";
      this.btnOpenSong.Size = new System.Drawing.Size(36, 23);
      this.btnOpenSong.TabIndex = 0;
      this.btnOpenSong.Text = "...";
      this.btnOpenSong.UseVisualStyleBackColor = true;
      this.btnOpenSong.Click += new System.EventHandler(this.btnOpenSong_Click);
      // 
      // listSongs
      // 
      this.listSongs.AllowDrop = true;
      this.listSongs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName});
      this.listSongs.FullRowSelect = true;
      this.listSongs.HideSelection = false;
      this.listSongs.Location = new System.Drawing.Point(12, 187);
      this.listSongs.Name = "listSongs";
      this.listSongs.Size = new System.Drawing.Size(378, 173);
      this.listSongs.TabIndex = 2;
      this.listSongs.UseCompatibleStateImageBehavior = false;
      this.listSongs.View = System.Windows.Forms.View.Details;
      this.listSongs.SelectedIndexChanged += new System.EventHandler(this.listSongs_SelectedIndexChanged);
      this.listSongs.DragDrop += new System.Windows.Forms.DragEventHandler(this.listSongs_DragDrop);
      this.listSongs.DragOver += new System.Windows.Forms.DragEventHandler(this.listSongs_DragOver);
      this.listSongs.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listSongs_MouseDoubleClick);
      // 
      // columnName
      // 
      this.columnName.Text = "Name";
      this.columnName.Width = 347;
      // 
      // btnTogglePlaylist
      // 
      this.btnTogglePlaylist.Location = new System.Drawing.Point(354, 146);
      this.btnTogglePlaylist.Name = "btnTogglePlaylist";
      this.btnTogglePlaylist.Size = new System.Drawing.Size(36, 23);
      this.btnTogglePlaylist.TabIndex = 0;
      this.btnTogglePlaylist.Text = "▲▲";
      this.btnTogglePlaylist.UseVisualStyleBackColor = true;
      this.btnTogglePlaylist.Click += new System.EventHandler(this.btnTogglePlaylist_Click);
      // 
      // btnAddSongToList
      // 
      this.btnAddSongToList.Location = new System.Drawing.Point(12, 366);
      this.btnAddSongToList.Name = "btnAddSongToList";
      this.btnAddSongToList.Size = new System.Drawing.Size(37, 23);
      this.btnAddSongToList.TabIndex = 0;
      this.btnAddSongToList.Text = "Add";
      this.btnAddSongToList.UseVisualStyleBackColor = true;
      this.btnAddSongToList.Click += new System.EventHandler(this.btnAddSong_Click);
      // 
      // btnRemoveSong
      // 
      this.btnRemoveSong.Enabled = false;
      this.btnRemoveSong.Location = new System.Drawing.Point(55, 366);
      this.btnRemoveSong.Name = "btnRemoveSong";
      this.btnRemoveSong.Size = new System.Drawing.Size(37, 23);
      this.btnRemoveSong.TabIndex = 0;
      this.btnRemoveSong.Text = "Rem";
      this.btnRemoveSong.UseVisualStyleBackColor = true;
      this.btnRemoveSong.Click += new System.EventHandler(this.btnRemoveSong_Click);
      // 
      // btnClearList
      // 
      this.btnClearList.Enabled = false;
      this.btnClearList.Location = new System.Drawing.Point(98, 366);
      this.btnClearList.Name = "btnClearList";
      this.btnClearList.Size = new System.Drawing.Size(37, 23);
      this.btnClearList.TabIndex = 0;
      this.btnClearList.Text = "Clr";
      this.btnClearList.UseVisualStyleBackColor = true;
      this.btnClearList.Click += new System.EventHandler(this.btnClearList_Click);
      // 
      // btnSaveList
      // 
      this.btnSaveList.Location = new System.Drawing.Point(311, 366);
      this.btnSaveList.Name = "btnSaveList";
      this.btnSaveList.Size = new System.Drawing.Size(37, 23);
      this.btnSaveList.TabIndex = 0;
      this.btnSaveList.Text = "Sav";
      this.btnSaveList.UseVisualStyleBackColor = true;
      this.btnSaveList.Click += new System.EventHandler(this.btnSaveList_Click);
      // 
      // btnLoadPlaylist
      // 
      this.btnLoadPlaylist.Location = new System.Drawing.Point(353, 366);
      this.btnLoadPlaylist.Name = "btnLoadPlaylist";
      this.btnLoadPlaylist.Size = new System.Drawing.Size(37, 23);
      this.btnLoadPlaylist.TabIndex = 0;
      this.btnLoadPlaylist.Text = "Lod";
      this.btnLoadPlaylist.UseVisualStyleBackColor = true;
      this.btnLoadPlaylist.Click += new System.EventHandler(this.btnLoadList_Click);
      // 
      // btnSettings
      // 
      this.btnSettings.Location = new System.Drawing.Point(270, 146);
      this.btnSettings.Name = "btnSettings";
      this.btnSettings.Size = new System.Drawing.Size(36, 23);
      this.btnSettings.TabIndex = 0;
      this.btnSettings.Text = "Set";
      this.btnSettings.UseVisualStyleBackColor = true;
      this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
      // 
      // labelSongLength
      // 
      this.labelSongLength.Location = new System.Drawing.Point(52, 43);
      this.labelSongLength.Name = "labelSongLength";
      this.labelSongLength.Size = new System.Drawing.Size(341, 13);
      this.labelSongLength.TabIndex = 1;
      this.labelSongLength.Text = "Song Length";
      // 
      // songPlayingTimer
      // 
      this.songPlayingTimer.Enabled = true;
      this.songPlayingTimer.Interval = 990;
      this.songPlayingTimer.Tick += new System.EventHandler(this.songPlayingTimer_Tick);
      // 
      // labelSongNumber
      // 
      this.labelSongNumber.Location = new System.Drawing.Point(52, 22);
      this.labelSongNumber.Name = "labelSongNumber";
      this.labelSongNumber.Size = new System.Drawing.Size(341, 13);
      this.labelSongNumber.TabIndex = 1;
      this.labelSongNumber.Text = "0/0";
      // 
      // scrollVolume
      // 
      this.scrollVolume.Location = new System.Drawing.Point(14, 126);
      this.scrollVolume.Name = "scrollVolume";
      this.scrollVolume.Size = new System.Drawing.Size(119, 17);
      this.scrollVolume.TabIndex = 3;
      this.scrollVolume.Value = 100;
      this.scrollVolume.Scroll += new System.Windows.Forms.ScrollEventHandler(this.scrollVolume_Scroll);
      // 
      // btnMoveUp
      // 
      this.btnMoveUp.Location = new System.Drawing.Point(141, 366);
      this.btnMoveUp.Name = "btnMoveUp";
      this.btnMoveUp.Size = new System.Drawing.Size(37, 23);
      this.btnMoveUp.TabIndex = 0;
      this.btnMoveUp.Text = "▲";
      this.btnMoveUp.UseVisualStyleBackColor = true;
      this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
      // 
      // btnMoveDown
      // 
      this.btnMoveDown.Location = new System.Drawing.Point(184, 366);
      this.btnMoveDown.Name = "btnMoveDown";
      this.btnMoveDown.Size = new System.Drawing.Size(37, 23);
      this.btnMoveDown.TabIndex = 0;
      this.btnMoveDown.Text = "▼";
      this.btnMoveDown.UseVisualStyleBackColor = true;
      this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
      // 
      // FormPlayer
      // 
      this.AllowDrop = true;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(404, 401);
      this.Controls.Add(this.scrollVolume);
      this.Controls.Add(this.listSongs);
      this.Controls.Add(this.labelSongLength);
      this.Controls.Add(this.labelSongNumber);
      this.Controls.Add(this.labelSongName);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.btnStop);
      this.Controls.Add(this.btnPause);
      this.Controls.Add(this.btnTogglePlaylist);
      this.Controls.Add(this.btnLoadPlaylist);
      this.Controls.Add(this.btnSaveList);
      this.Controls.Add(this.btnMoveDown);
      this.Controls.Add(this.btnMoveUp);
      this.Controls.Add(this.btnClearList);
      this.Controls.Add(this.btnRemoveSong);
      this.Controls.Add(this.btnAddSongToList);
      this.Controls.Add(this.btnSettings);
      this.Controls.Add(this.btnOpenSong);
      this.Controls.Add(this.btnNext);
      this.Controls.Add(this.btnPrevious);
      this.Controls.Add(this.btnPlay);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
      this.MaximizeBox = false;
      this.Name = "FormPlayer";
      this.Text = "Form1";
      this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormPlayer_FormClosed);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button btnPlay;
    private System.Windows.Forms.Button btnStop;
    private System.Windows.Forms.Button btnPause;
    private System.Windows.Forms.Button btnPrevious;
    private System.Windows.Forms.Button btnNext;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label labelSongName;
    private System.Windows.Forms.Button btnOpenSong;
    private System.Windows.Forms.ListView listSongs;
    private System.Windows.Forms.ColumnHeader columnName;
    private System.Windows.Forms.Button btnTogglePlaylist;
    private System.Windows.Forms.Button btnAddSongToList;
    private System.Windows.Forms.Button btnRemoveSong;
    private System.Windows.Forms.Button btnClearList;
    private System.Windows.Forms.Button btnSaveList;
    private System.Windows.Forms.Button btnLoadPlaylist;
    private System.Windows.Forms.Button btnSettings;
    private System.Windows.Forms.Label labelSongLength;
    private System.Windows.Forms.Timer songPlayingTimer;
    private System.Windows.Forms.Label labelSongNumber;
    private System.Windows.Forms.HScrollBar scrollVolume;
    private System.Windows.Forms.Button btnMoveUp;
    private System.Windows.Forms.Button btnMoveDown;
  }
}

