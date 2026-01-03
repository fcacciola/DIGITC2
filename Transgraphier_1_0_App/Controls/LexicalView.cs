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
    private Panel mContainerPanel;
    private ConfigurationTableView mInfoBox;
    private TextBox mTextPanel;
    private Panel innerContainer;
    private Label mTitle;

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
    public Dictionary<string, string> InfoBoxData
    {
      get
      {
        return mInfoBox.Data;
      }
      set
      {
        mInfoBox.Data = value;
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
      mContainerPanel = new Panel();
      innerContainer = new Panel();
      mTextPanel = new TextBox();
      mInfoBox = new ConfigurationTableView();
      mTitle = new Label();
      mContainerPanel.SuspendLayout();
      innerContainer.SuspendLayout();
      SuspendLayout();
      // 
      // mContainerPanel
      // 
      mContainerPanel.BackColor = Color.Black;
      mContainerPanel.Controls.Add(innerContainer);
      mContainerPanel.Controls.Add(mTitle);
      mContainerPanel.Dock = DockStyle.Fill;
      mContainerPanel.Location = new Point(0, 0);
      mContainerPanel.Name = "mContainerPanel";
      mContainerPanel.Padding = new Padding(2);
      mContainerPanel.Size = new Size(150, 100);
      mContainerPanel.TabIndex = 0;
      // 
      // innerContainer
      // 
      innerContainer.BackColor = Color.White;
      innerContainer.Controls.Add(mTextPanel);
      innerContainer.Controls.Add(mInfoBox);
      innerContainer.Dock = DockStyle.Fill;
      innerContainer.Location = new Point(2, 27);
      innerContainer.Name = "innerContainer";
      innerContainer.Size = new Size(146, 71);
      innerContainer.TabIndex = 0;
      // 
      // mTextPanel
      // 
      mTextPanel.Dock = DockStyle.Fill;
      mTextPanel.Location = new Point(300, 0);
      mTextPanel.Name = "mTextPanel";
      mTextPanel.ReadOnly = true;
      mTextPanel.Multiline = true;
      mTextPanel.ScrollBars = ScrollBars.Vertical;
      mTextPanel.Size = new Size(0, 71);
      mTextPanel.TabIndex = 0;
      // 
      // mInfoBox
      // 
      mInfoBox.Dock = DockStyle.Left;
      mInfoBox.Location = new Point(0, 0);
      mInfoBox.Name = "mInfoBox";
      mInfoBox.Size = new Size(300, 71);
      mInfoBox.TabIndex = 1;
      // 
      // mTitle
      // 
      mTitle.BackColor = Color.LightGray;
      mTitle.Dock = DockStyle.Top;
      mTitle.ForeColor = Color.Black;
      mTitle.Location = new Point(2, 2);
      mTitle.Name = "mTitle";
      mTitle.Padding = new Padding(5, 0, 0, 0);
      mTitle.Size = new Size(146, 25);
      mTitle.TabIndex = 1;
      mTitle.Text = "mTitle";
      mTitle.TextAlign = ContentAlignment.MiddleLeft;
      // 
      // LexicalView
      // 
      Controls.Add(mContainerPanel);
      Name = "LexicalView";
      Size = new Size(150, 100);
      mContainerPanel.ResumeLayout(false);
      innerContainer.ResumeLayout(false);
      ResumeLayout(false);
    }
  }
}
