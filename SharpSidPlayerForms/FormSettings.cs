using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharpSidPlayerForms
{
  public partial class FormSettings : Form
  {
    public Settings     Settings;



    public FormSettings( Settings Settings )
    {
      this.Settings = Settings;
      InitializeComponent();

      editHVSCLengthFilePath.Text = Settings.HVSCLengthFile;
    }



    private void btnBrowseFile_Click( object sender, EventArgs e )
    {
      var openDlg = new OpenFileDialog();
      openDlg.Title = "Open HVSC length file";
      openDlg.Filter = "HVSC length file|*.md5|All Files|*.*";

      if ( openDlg.ShowDialog() != DialogResult.OK )
      {
        return;
      }
      Settings.HVSCLengthFile = openDlg.FileName;
      editHVSCLengthFilePath.Text = Settings.HVSCLengthFile;
    }



  }
}
