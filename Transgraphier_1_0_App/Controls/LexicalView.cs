using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE ;

namespace Transgraphier_1_0_App
{
  public class LexicalView : UserControl
  {
    private Label mTitle;
    private TextBox mTextPanel;
    private ConfigurationTableView mParameters;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
      get
      {
        return mTitle.Text;
      }
      set
      {
        mTitle.Text = value;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Dictionary<string, string> Parameters
    {
      get
      {
        return mParameters.Data;
      }
      set
      {
        mParameters.Data = value;
      }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TextContent
    {
      get
      {
        return mTextPanel.Text;
      }
      set
      {
        mTextPanel.Text = value;

        // Move cursor to the end
        if (value.Length > 0)
        {
          mTextPanel.SelectionStart = value.Length;
          mTextPanel.SelectionLength = 0;
          mTextPanel.ScrollToCaret();
        }
        // Force the text box to update and scroll to the end
        mTextPanel.Invalidate();
        mTextPanel.Update();
      }
    }

    public LexicalView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      mTitle = new Label();
      mTextPanel = new TextBox();
      mParameters = new ConfigurationTableView();

      SuspendLayout();

      Height = 300;
      BackColor = Color.White;

      // 
      // mTitle
      // 
      mTitle.BackColor = Color.LightGray;
      mTitle.Dock = DockStyle.Top;
      mTitle.ForeColor = Color.Black;
      mTitle.Name = "mTitle";
      mTitle.Padding = new Padding(5, 0, 0, 0);
      mTitle.Height = 30;
      mTitle.TabIndex = 0;
      mTitle.Text = "mTitle";
      mTitle.TextAlign = ContentAlignment.MiddleLeft;
      Controls.Add(mTitle);

      // 
      // mParameters (right side, fixed width)
      // 
      mParameters.Dock = DockStyle.Right;
      mParameters.Name = "mParameters";
      mParameters.Width = 360;
      mParameters.Height = 300;
      mParameters.TabIndex = 1;
      Controls.Add(mParameters);

      // 
      // mTextPanel (fills remaining space)
      // 
      mTextPanel.Dock = DockStyle.Left;
      mTextPanel.Name = "mTextPanel";
      mTextPanel.ReadOnly = true;
      mTextPanel.Multiline = true;
      mTextPanel.ScrollBars = ScrollBars.Both;
      mTextPanel.BorderStyle = BorderStyle.Fixed3D;
      mTextPanel.TabIndex = 2;
      mTextPanel.WordWrap = false;
      mTextPanel.AcceptsTab = false;
      mTextPanel.HideSelection = false;
      mTextPanel.Width = 1000 ; //this.Width - mParameters.Width - 50 ;
      mTextPanel.Height = 300;
      Controls.Add(mTextPanel);

      Name = "LexicalView";
      ResumeLayout(false);
      PerformLayout();
    }
  }
}
