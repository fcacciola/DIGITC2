using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE ;

using NWaves.Signals ;

namespace Transgraphier_1_0_App
{
  public class WaveView : UserControl
  {
    private Panel mContainerPanel;
    private ConfigurationTableView mInfoBox;
    private WavePanel mWavePanel;
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
    public ZoomPanController ZoomPanController { get => mWavePanel.ZoomPanController; set => mWavePanel.ZoomPanController = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal { get => mWavePanel.Signal; set => mWavePanel.Signal = value; }

    public WaveView()
    {
      InitializeComponent();
    }


    public void InvalidateRender()
    {
      mWavePanel.InvalidateRender();
    }

        private void InitializeComponent()
        {
            mContainerPanel = new Panel();
            innerContainer = new Panel();
            mWavePanel = new WavePanel();
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
            innerContainer.Controls.Add(mWavePanel);
            innerContainer.Controls.Add(mInfoBox);
            innerContainer.Dock = DockStyle.Fill;
            innerContainer.Location = new Point(2, 27);
            innerContainer.Name = "innerContainer";
            innerContainer.Size = new Size(146, 71);
            innerContainer.TabIndex = 0;
            // 
            // mWavePanel
            // 
            mWavePanel.Dock = DockStyle.Fill;
            mWavePanel.Location = new Point(300, 0);
            mWavePanel.Name = "mWavePanel";
            mWavePanel.Size = new Size(0, 71);
            mWavePanel.TabIndex = 0;
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
            // WaveView
            // 
            Controls.Add(mContainerPanel);
            Name = "WaveView";
            Size = new Size(150, 100);
            mContainerPanel.ResumeLayout(false);
            innerContainer.ResumeLayout(false);
            ResumeLayout(false);
        }
    }

  public class WavePanel : Control
  {
    public ZoomPanController ZoomPanController = null ;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal { get => mSignal; set { mSignal = value; InvalidateRender();  } }


    List<Point> Poly = null ;
     
    public void InvalidateRender()
    {
      Poly = null ;
      Invalidate(); 
    }

    List<Point> GetPoly()
    {
      if ( Poly == null )
        CacheRender();
      return Poly ;
    }

    void CacheRender()
    {
      var signal = mSignal;
      if (signal == null || signal.Length == 0)
        return;

      var samplesPerPixel = ZoomPanController.SamplesPerPixel;
      var startSample = (int)Math.Floor(ZoomPanController.StartSample);
      var visibleSamples = (int)Math.Ceiling(Width * samplesPerPixel);
      var endSample = Math.Min(signal.Length - 1, startSample + visibleSamples);

      // For each horizontal pixel compute min and max sample in that pixel column

      int center = Height / 2;
      int halfH  = Height / 2;

      Poly = new List<Point>();

      // Aggregate min/max per pixel
      for (int px = 0; px < Width; px++)
      {
        var sampleStart = startSample + (int)Math.Floor(px * samplesPerPixel);
        var sampleEnd = startSample + (int)Math.Ceiling((px + 1) * samplesPerPixel) - 1;
        if (sampleStart > endSample)
          break;
        sampleEnd = Math.Min(sampleEnd, endSample);
        if (sampleEnd < sampleStart)
          sampleEnd = sampleStart;

        float lMin = signal[sampleStart] ;
        float lMax = signal[sampleStart] ;

        for (int s = sampleStart + 1 ; s <= sampleEnd; s++)
        {
          float v = signal[s];
          if ( v < lMin)
            lMin = v;
          
          if ( v > lMax)
            lMax = v;
        }

        Poly.Add( new Point( px, center - (int)Math.Ceiling(lMin * halfH) ) ) ;
        Poly.Add( new Point( px, center - (int)Math.Ceiling(lMax * halfH) ) ) ;
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      // background
      e.Graphics.FillRectangle( Brushes.AntiqueWhite, new Rectangle(0, 0, Width, Height));

      // center line
      var center = Height / 2;
      e.Graphics.DrawLine( Pens.Black, new Point(0, center), new Point(Width, center));

      var lPoly = GetPoly();
      if ( lPoly != null )
        e.Graphics.DrawLines(Pens.Blue, lPoly.ToArray());
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      e.Graphics.Clear(Color.White);
    }

    Point mLastMousePos;
    bool  mIsPanning = false ;

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if ( e.Button == MouseButtons.Left )
      {
        mLastMousePos = e.Location; 
        mIsPanning = true ;

        this.Capture = true;
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if ( e.Button == MouseButtons.Left && mIsPanning )
      {
        mIsPanning = false ;

        this.Capture = false;
      }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if ( e.Button == MouseButtons.Left && mIsPanning )
      {
        var p = e.Location;

        var dx = p.X - mLastMousePos.X;

        // translate dx pixels into sample offset change
        var sampleDelta = -dx * ZoomPanController.SamplesPerPixel;

        ZoomPanController.UpdateSS( MathX.Clamp(  ZoomPanController.StartSample + sampleDelta, 0, Signal.Length) ) ;

        mLastMousePos = p;
      }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
      base.OnMouseWheel(e);

      if (ZoomPanController == null || mSignal == null)
        return;

      var lOldSPP = ZoomPanController.SamplesPerPixel;

      var zoomFactor = Math.Pow(1.0015, e.Delta * ((ModifierKeys & Keys.Control) != 0 ? 2.5 : 1.0));

      double lNewSamplesPerPixel = MathX.Clamp(lOldSPP / zoomFactor, ZoomPanController.MinSamplesPerPixel, ZoomPanController.MaxSamplesPerPixel);

      // keep sample under mouse fixed when zooming
      var pos = e.Location;

      var lInitialSampleUnderCursor = ZoomPanController.StartSample + (pos.X * lOldSPP) - (lOldSPP / 2.0);

      var lNewSampleUnderCursor = (pos.X * lNewSamplesPerPixel) - (lNewSamplesPerPixel / 2.0);

      var lNewStartSample = MathX.Clamp(lInitialSampleUnderCursor - lNewSampleUnderCursor, 0, mSignal.Length);

      ZoomPanController.Update(lNewSamplesPerPixel, lNewStartSample);

    }

    DiscreteSignal mSignal = null ;
  }
}
