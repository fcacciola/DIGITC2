using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using ENGINE;

namespace Transgraphier
{
  public class LexicalView : UserControl
  {
    private Form1 mMainWindow ;
    private Label mTitle;
    private RichTextBox mTextPanel;
    private TableLayoutPanel mTableLayout;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
      get => mTitle.Text;
      set => mTitle.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TextContent
    {
      get => mTextPanel.Text;
      set
      {
        mTextPanel.Text = value;
        if (value.Length > 0)
        {
          mTextPanel.SelectionStart = value.Length;
          mTextPanel.SelectionLength = 0;
          mTextPanel.ScrollToCaret();
        }
        mTextPanel.Invalidate();
        mTextPanel.Update();
      }
    }

    public LexicalView(Form1 aMainWindow)
    {
      mMainWindow = aMainWindow; 

      InitializeComponent();
    }

    private void InitializeComponent()
    {
      mTitle = new Label();
      mTextPanel = new RichTextBox();
      mTableLayout = new TableLayoutPanel();

      SuspendLayout();

      Height = 300;
      BackColor = Color.White;

      // TableLayoutPanel setup
      mTableLayout.ColumnCount = 2;
      mTableLayout.RowCount = 2;
      mTableLayout.Dock = DockStyle.Fill;
      mTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Title row
      mTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Main content row
      mTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // TextPanel column
      mTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F)); // Parameters column

      // mTitle
      mTitle.Text = "mTitle";
      mTitle.Dock = DockStyle.Fill;
      mTitle.BackColor = Color.LightGray;
      mTitle.ForeColor = Color.Black;
      mTitle.TextAlign = ContentAlignment.MiddleLeft;
      mTitle.Padding = new Padding(5, 0, 0, 0);
      mTitle.Height = 30;

      // mTextPanel
      mTextPanel.Multiline = true;
      mTextPanel.ReadOnly = true;
      mTextPanel.ScrollBars = RichTextBoxScrollBars.Vertical;
      mTextPanel.BorderStyle = BorderStyle.Fixed3D;
      mTextPanel.WordWrap = false;
      mTextPanel.Dock = DockStyle.Fill;


      // Add controls to TableLayoutPanel
      mTableLayout.Controls.Add(mTitle, 0, 0);
      mTableLayout.SetColumnSpan(mTitle, 2);
      mTableLayout.Controls.Add(mTextPanel, 0, 1);

      Controls.Add(mTableLayout);

      Name = "LexicalView";
      ResumeLayout(false);
      PerformLayout();
    }
  }
}
