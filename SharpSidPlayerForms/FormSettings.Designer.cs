
namespace SharpSidPlayerForms
{
  partial class FormSettings
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
      this.editHVSCLengthFilePath = new System.Windows.Forms.TextBox();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.btnBrowseFile = new System.Windows.Forms.Button();
      this.groupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // editHVSCLengthFilePath
      // 
      this.editHVSCLengthFilePath.Location = new System.Drawing.Point(6, 19);
      this.editHVSCLengthFilePath.Name = "editHVSCLengthFilePath";
      this.editHVSCLengthFilePath.Size = new System.Drawing.Size(327, 20);
      this.editHVSCLengthFilePath.TabIndex = 1;
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.btnBrowseFile);
      this.groupBox1.Controls.Add(this.editHVSCLengthFilePath);
      this.groupBox1.Location = new System.Drawing.Point(12, 12);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(382, 53);
      this.groupBox1.TabIndex = 2;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Path to HVSC length file";
      // 
      // btnBrowseFile
      // 
      this.btnBrowseFile.Location = new System.Drawing.Point(339, 19);
      this.btnBrowseFile.Name = "btnBrowseFile";
      this.btnBrowseFile.Size = new System.Drawing.Size(37, 20);
      this.btnBrowseFile.TabIndex = 2;
      this.btnBrowseFile.Text = "...";
      this.btnBrowseFile.UseVisualStyleBackColor = true;
      this.btnBrowseFile.Click += new System.EventHandler(this.btnBrowseFile_Click);
      // 
      // FormSettings
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(406, 348);
      this.Controls.Add(this.groupBox1);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "FormSettings";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.Text = "Settings";
      this.groupBox1.ResumeLayout(false);
      this.groupBox1.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TextBox editHVSCLengthFilePath;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Button btnBrowseFile;
  }
}