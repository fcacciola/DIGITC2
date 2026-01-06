using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE;

using NWaves.Signals;

namespace Transgraphier_1_0_App
{
  public class WaveView : UserControl
  {
    private Label mTitle;
    private WavePanel mWavePanel;
    private ConfigurationTableView mParameters ;

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
    public ZoomPanController ZoomPanController
    {
      get => mWavePanel.ZoomPanController; set => mWavePanel.ZoomPanController = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal
    {
      get => mWavePanel.Signal; set => mWavePanel.Signal = value;
    }

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
      mTitle = new Label();
      mWavePanel = new WavePanel();
      mParameters = new ConfigurationTableView();

      SuspendLayout();

      Height = 250;

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

      mWavePanel.Dock = DockStyle.Fill;
      mWavePanel.Name = "mWavePanel";
      mWavePanel.TabIndex = 0;
      mWavePanel.Height = 200 ;
      Controls.Add(mWavePanel);

      Name = "WaveView";

      ResumeLayout(false);
    }
  }

  public class WavePanel : Control
  {
    public WavePanel()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      SuspendLayout();
      Height = 165 ;
      ResumeLayout(false);
    }

    public ZoomPanController ZoomPanController = null;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal
    {
      get => mSignal; set
      {
        mSignal = value;
        InvalidateRender();
      }
    }


    Point[] Poly = null;

    public void InvalidateRender()
    {
      Poly = null;
      Invalidate();
    }

    Point[] GetPoly()
    {
      if (Poly == null)
        CacheRender();
      return Poly;
    }

    const int LabelH  = 30 ;
    const int MarginS = 2 ;

    int BottomY   => Height - MarginS ;
    int WaveH     => Height - LabelH - ( MarginS * 2 ) ;
    int WaveHalfH => WaveH / 2 ;
    int CenterY   => BottomY - WaveHalfH;

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

      var lPoly = new List<Point>();

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

        float lMin = signal[sampleStart];
        float lMax = signal[sampleStart];

        for (int s = sampleStart + 1; s <= sampleEnd; s++)
        {
          float v = signal[s];
          if (v < lMin)
            lMin = v;

          if (v > lMax)
            lMax = v;
        }

        lPoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMin * WaveHalfH)));
        lPoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMax * WaveHalfH)));
      }

      Poly = lPoly.ToArray();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      // background
      e.Graphics.FillRectangle(Brushes.AntiqueWhite, new Rectangle(0, 0, Width, Height));

      // center line
      e.Graphics.DrawLine(Pens.Black, new Point(0, CenterY), new Point(Width, CenterY));

      var lPoly = GetPoly();
      if (lPoly != null)
        e.Graphics.DrawLines(Pens.Blue, lPoly.ToArray());
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      e.Graphics.Clear(Color.White);
    }

    Point mLastMousePos;
    bool mIsPanning = false;

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if (e.Button == MouseButtons.Left)
      {
        mLastMousePos = e.Location;
        mIsPanning = true;

        this.Capture = true;
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if (e.Button == MouseButtons.Left && mIsPanning)
      {
        mIsPanning = false;

        this.Capture = false;
      }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if (e.Button == MouseButtons.Left && mIsPanning)
      {
        var p = e.Location;

        var dx = p.X - mLastMousePos.X;

        // translate dx pixels into sample offset change
        var sampleDelta = -dx * ZoomPanController.SamplesPerPixel;

        ZoomPanController.UpdateSS(MathX.Clamp(ZoomPanController.StartSample + sampleDelta, 0, Signal.Length));

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

    DiscreteSignal mSignal = null;
  }
}
