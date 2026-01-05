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

      Height = 400;

      // 
      // mTitle
      // 
      mTitle.BackColor = Color.LightGray;
      mTitle.Dock = DockStyle.Top;
      mTitle.ForeColor = Color.Black;
      mTitle.Location = new Point(4, 2);
      mTitle.Name = "mTitle";
      mTitle.Padding = new Padding(5, 0, 0, 0);
      mTitle.Height = 30 ;
      mTitle.TabIndex = 1;
      mTitle.Text = "mTitle";
      mTitle.TextAlign = ContentAlignment.MiddleLeft;

      Controls.Add(mTitle);

      mParameters.Dock = DockStyle.Right;
      mParameters.Name  = "mInfoBox";
      mParameters.Width = 300;
      mParameters.TabIndex = 1;
      Controls.Add(mParameters);

      mTextPanel.Dock = DockStyle.Fill;
      mTextPanel.Name = "mTextPanel";
      mTextPanel.ReadOnly = true;
      mTextPanel.Multiline = true;
      mTextPanel.ScrollBars = ScrollBars.Vertical;
      mTextPanel.TabIndex = 0;
      Controls.Add(mTextPanel);

      Name = "LexicalView";
      ResumeLayout(false);
    }
  }
}
