using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE;

using NWaves.Signals;

namespace Transgraphier_1_0_App
{
  public class Ruler
  {
    public double Unit { get; init; }
    public int rulerTop;
    public int rulerHeight;
    public List<RulerTick> Ticks { get; init; } = new List<RulerTick>();

    public Font textFont { get; init; }
    public Brush textBrush = Brushes.Black; 
  }

  public class RulerTick
  {
    public Ruler Ruler { get ; init ; }
    public double Time { get; init; }  // in seconds
    public double X { get; init; }  // in pixels
    public string Label ;

    public void Draw( Graphics g )
    {
      int ix = (int)Math.Round(X);
      // tick height depends on whether it is major tick (multiple of bigger unit)
      const int tickH = 10;
      g.DrawLine(Pens.Black, ix, Ruler.rulerTop + Ruler.rulerHeight - 1, ix, Ruler.rulerTop + Ruler.rulerHeight - 1 - tickH);

      var size = g.MeasureString(Label, Ruler.textFont);
      int tx = ix - (int)(size.Width / 2);
      int ty = Ruler.rulerTop + 2;
      g.DrawString(Label, Ruler.textFont, Ruler.textBrush, tx, ty);

    }


  }


  public static class WaveformRuler
  {
    // Candidate time units in ascending order (seconds)
    private static readonly double[] TimeUnits =
    [
        0.01, 0.05, 0.1, 0.5, 1.0, 10.0, 30.0, 60.0
    ];

    /// <summary>
    /// Formats a time value in seconds into a human-readable label,
    /// adapting precision to the current time unit.
    /// </summary>
    public static string FormatTickLabel(double timeSeconds, double unit)
    {
      TimeSpan ts = TimeSpan.FromSeconds(timeSeconds);

      if (unit >= 60.0)
        return $"{(int)ts.TotalMinutes}m";

      if (unit >= 30.0)
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}m";

      // Sub-second: show decimal places scaled to the unit
      int decimals = unit switch
      {
        >= 10.0 => 1,
        >= 1.0 => 1,
        >= 0.5 => 1,
        >= 0.1 => 2,
        >= 0.05 => 2,
        _ => 2
      };

      string tustr = "s";
      string mstr = "";
      string fracstr = "";
      
      int m = (int)ts.TotalMinutes;
      if ( m > 0 )
      {
        mstr = m > 0 ? $"{m}:" : "" ;
        tustr = "m";
      }
      
      // Extract only the sub-second fractional part
      double frac = ts.TotalSeconds - Math.Floor(ts.TotalSeconds);
      if ( frac > 0 )
      {
        fracstr = decimals switch
                  {
                    1 => $".{(int)(frac * 10)}",
                    2 => $".{(int)(frac * 100):D2}",
                    _ => $".{(int)(frac * 1000):D3}"
                  };
      }

    
      return $"{mstr}{ts.Seconds:D2}{fracstr}{tustr}";
    }
    
    /// <summary>
    /// Picks the smallest time unit (in seconds) such that adjacent
    /// tick marks are at least <paramref name="minSpacingPx"/> pixels apart.
    /// </summary>
    public static double PickTimeUnit(double pixelsPerSecond, double minSpacingPx = 80.0)
    {
      foreach (double unit in TimeUnits)
      {
        if (unit * pixelsPerSecond >= minSpacingPx)
          return unit;
      }

      return TimeUnits[^1]; // fallback: 60s
    }

    /// <summary>
    /// Returns the first tick time that is >= startTime and
    /// falls exactly on a multiple of <paramref name="unit"/>.
    /// </summary>
    public static double FirstTickTime(double startTime, double unit)
    {
      return Math.Ceiling(startTime / unit) * unit;
    }

    /// <summary>
    /// Converts a time value to its pixel X coordinate relative
    /// to the left edge of the viewport.
    /// </summary>
    public static double TimeToPixel(double time, double startTime, double pixelsPerSecond)
    {
      return (time - startTime) * pixelsPerSecond;
    }

    /// <summary>
    /// Generates all ruler ticks visible within the viewport.
    /// </summary>
    /// <param name="startTime">Time (seconds) at the left edge (x=0).</param>
    /// <param name="viewportWidthPx">Width of the visible ruler in pixels.</param>
    /// <param name="pixelsPerSecond">Zoom factor.</param>
    /// <param name="minSpacingPx">Minimum pixel gap between labels.</param>
    public static Ruler GetRulerTicks(
        Font textFont,
        int rulerTop,
        int rulerHeight,
        double startTime,
        double viewportWidthPx,
        double pixelsPerSecond,
        double minSpacingPx = 80.0
        )
    {

      double unit = PickTimeUnit(pixelsPerSecond, minSpacingPx);
      double endTime = startTime + viewportWidthPx / pixelsPerSecond;
      double t = FirstTickTime(startTime, unit);
      Ruler rR  = new Ruler { Unit = unit, textFont = textFont, rulerTop  = rulerTop, rulerHeight = rulerHeight };

      while (t <= endTime)
      {
        rR.Ticks.Add(new RulerTick
        {
          Ruler = rR,
          Time = t,
          X = TimeToPixel(t, startTime, pixelsPerSecond),
          Label = FormatTickLabel(t,unit)
        });

        // Round to 10 decimal places to prevent floating-point drift
        t = Math.Round(t + unit, 10);
      }

      return rR;
    }

  }


  public class WaveView : UserControl
  {
    private Label mTitle;
    private WavePanel mWavePanel;
    private bool mIncludeRuler;

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
    public WaveViewController WaveViewController
    {
      get => mWavePanel.WaveViewController; set => mWavePanel.WaveViewController = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal
    {
      get => mWavePanel.Signal; set => mWavePanel.Signal = value;
    }

    public WaveView(bool aIncludeRuler)
    {
      mIncludeRuler = aIncludeRuler;
      InitializeComponent();
    }


    public void InvalidateRender()
    {
      mWavePanel.InvalidateRender();
    }

    private void InitializeComponent()
    {
      mTitle = new Label();
      mWavePanel = new WavePanel(mIncludeRuler);

      SuspendLayout();

      Height = 250;

      mTitle.BackColor = Color.LightGray;
      mTitle.Dock = DockStyle.Top;
      mTitle.ForeColor = Color.Black;
      mTitle.Location = new Point(4, 2);
      mTitle.Name = "mTitle";
      mTitle.Padding = new Padding(5, 0, 0, 0);
      mTitle.Height = 30;
      mTitle.TabIndex = 1;
      mTitle.Text = "mTitle";
      mTitle.TextAlign = ContentAlignment.MiddleLeft;

      Controls.Add(mTitle);

      mWavePanel.Dock = DockStyle.Fill;
      mWavePanel.Name = "mWavePanel";
      mWavePanel.TabIndex = 0;
      mWavePanel.Height = 200;
      Controls.Add(mWavePanel);

      Name = "WaveView";

      ResumeLayout(false);
    }
  }

  public class WavePanel : Control
  {
    public WavePanel(bool aIncludeRuler)
    {
      mIncludeRuler = aIncludeRuler;
      RulerH = mIncludeRuler ? 40 : 0;
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      SuspendLayout();
      Height = 165;
      ResumeLayout(false);
    }

    public WaveViewController WaveViewController = null;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal
    {
      get => mSignal; set
      {
        mSignal = value;
        InvalidateRender();
      }
    }

    bool mIncludeRuler;

    Ruler mRuler = null ;

    Bitmap mRender = null ;

    public void InvalidateRender()
    {
      mRender?.Dispose(); 
      mRender = null;
      Invalidate();
    }

    Bitmap GetRender()
    {
      if (mRender == null)
        CacheRender();
      return mRender;
    }

    public int RulerH = 0;
    const int MarginS = 2;
    const int cTitleHeight = 30;

    int BottomY => Height - MarginS - RulerH;
    int WaveH => Height - RulerH - (MarginS * 2) - cTitleHeight;
    int WaveHalfH => WaveH / 2;
    int CenterY => BottomY - WaveHalfH;

    void CacheRender()
    {
      var signal = mSignal;
      if (signal == null || signal.Length == 0)
        return;

      var samplesPerPixel = WaveViewController.SamplesPerPixel;
      var startSample = (int)Math.Floor(WaveViewController.StartSample);
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

      if ( lPoly.Count == 0 )
        return;

      if ( mIncludeRuler )
      {
        // Ruler area
        int rulerTop = Height - RulerH;
        int rulerHeight = RulerH;

        // start time at left pixel
        double startTime = WaveViewController.StartSample / (double)SIG.SamplingRate; // seconds

        // compute pixels per second
        double pixelsPerSecond = SIG.SamplingRate / WaveViewController.SamplesPerPixel;

        mRuler = WaveformRuler.GetRulerTicks(
            textFont       : this.Font,
            rulerTop       : rulerTop,
            rulerHeight    : rulerHeight,
            startTime:       startTime,
            viewportWidthPx: Width,
            pixelsPerSecond: pixelsPerSecond,
            minSpacingPx:    80
        );
      }

      mRender = new Bitmap(Width, Height);

      using (var g = Graphics.FromImage(mRender))
      {
        // background
        g.FillRectangle(Brushes.AntiqueWhite, new Rectangle(0, 0, Width, Height));
  
        // center line
        g.DrawLine(Pens.Black, new Point(0, CenterY), new Point(Width, CenterY));
  
        g.DrawLines(Pens.Blue, lPoly.ToArray());
  
        if ( mIncludeRuler )
          DrawTimeRuler(g);
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      var lRender = GetRender();  
      if ( lRender == null)
        return; 

      Bitmap lCopy = null ;

      // Draw selection overlay if measurement tool is active
      if (WaveViewController.MeasureTimeTool != null && WaveViewController.MeasureTimeTool.IsActive && WaveViewController.MeasureTimeTool.SelectionStartX >= 0)
      {
        lCopy = new Bitmap(lRender); 
        using ( var g = Graphics.FromImage(lCopy))
          DrawSelectionOverlay(g);
        lRender = lCopy;  
      }

      e.Graphics.DrawImageUnscaled(lRender, 0, 0);

      lCopy?.Dispose();
    }

    void DrawTimeRuler(Graphics g)
    {
      if (WaveViewController == null || mRuler == null )
        return;

      // Ruler area
      int rulerTop = Height - RulerH;
      int rulerHeight = RulerH;

      // background
      using (Brush b = new SolidBrush(Color.White))
        g.FillRectangle(b, 0, rulerTop, Width, rulerHeight);

      // top separator line
      g.DrawLine(Pens.Black, 0, rulerTop, Width, rulerTop);

      mRuler.Ticks.ForEach( t => t.Draw(g) ) ;
    }

    void DrawSelectionOverlay(Graphics g)
    {
      if (WaveViewController.MeasureTimeTool.SelectionStartX < 0 || WaveViewController.MeasureTimeTool.SelectionEndX < 0)
        return;

      int startX = Math.Min(WaveViewController.MeasureTimeTool.SelectionStartX, WaveViewController.MeasureTimeTool.SelectionEndX);
      int endX   = Math.Max(WaveViewController.MeasureTimeTool.SelectionStartX, WaveViewController.MeasureTimeTool.SelectionEndX);

      // Draw semi-transparent overlay
      using (Brush brush = new SolidBrush(Color.FromArgb(100, 0, 150, 255)))
      {
        g.FillRectangle(brush, startX, 0, endX - startX, BottomY);
      }

      // Draw selection borders
      using (Pen pen = new Pen(Color.Blue, 2))
      {
        g.DrawLine(pen, startX, 0, startX, BottomY);
        g.DrawLine(pen, endX  , 0, endX  , BottomY);
      }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      //e.Graphics.Clear(Color.White);
    }

    Point mLastMousePos;
    bool mIsPanning = false;
    


    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if (WaveViewController == null || mSignal == null)
        return;

      if (e.Button == MouseButtons.Left)
      {
        if (WaveViewController.MeasureTimeTool != null && WaveViewController.MeasureTimeTool.IsActive)
        {
          // Start selection for measurement tool
          WaveViewController.MeasureTimeTool.SelectionStartX = e.X;
          WaveViewController.MeasureTimeTool.SelectionEndX   = e.X;
        }
        else
        {
          // Start panning
          mLastMousePos = e.Location;
          mIsPanning = true;
        }

        this.Capture = true;
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if (WaveViewController == null || mSignal == null)
        return;

      if (e.Button == MouseButtons.Left)
      {
        if (WaveViewController.MeasureTimeTool != null && WaveViewController.MeasureTimeTool.IsActive)
        {
          // End selection - update the measurement tool with sample positions
          WaveViewController.MeasureTimeTool.SelectionEndX = e.X;
          UpdateMeasurementSelection();
        }
        else if (mIsPanning)
        {
          mIsPanning = false;
        }

        this.Capture = false;
      }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if (WaveViewController == null || mSignal == null)
        return;

      if (e.Button == MouseButtons.Left && WaveViewController.MeasureTimeTool.SelectionStartX >= 0 && WaveViewController.MeasureTimeTool != null && WaveViewController.MeasureTimeTool.IsActive)
      {
        // Update selection end while dragging
        WaveViewController.MeasureTimeTool.SelectionEndX = e.X;
        WaveViewController.Invalidate();
      }
      else if (e.Button == MouseButtons.Left && mIsPanning)
      {
        var p = e.Location;

        var dx = p.X - mLastMousePos.X;

        // translate dx pixels into sample offset change
        var sampleDelta = -dx * WaveViewController.SamplesPerPixel;

        WaveViewController.UpdateSS(MathX.Clamp(WaveViewController.StartSample + sampleDelta, 0, Signal.Length));

        mLastMousePos = p;
      }
    }

    private void UpdateMeasurementSelection()
    {
      if (WaveViewController.MeasureTimeTool == null || mSignal == null)
        return;

      // Convert pixel coordinates to sample coordinates
      int startX = Math.Min(WaveViewController.MeasureTimeTool.SelectionStartX, WaveViewController.MeasureTimeTool.SelectionEndX);
      int endX   = Math.Max(WaveViewController.MeasureTimeTool.SelectionStartX, WaveViewController.MeasureTimeTool.SelectionEndX);

      double startSample = WaveViewController.StartSample + (startX * WaveViewController.SamplesPerPixel);
      double endSample   = WaveViewController.StartSample + (endX   * WaveViewController.SamplesPerPixel);

      WaveViewController.MeasureTimeTool.SelectionStartSample = (int)Math.Round(startSample);
      WaveViewController.MeasureTimeTool.SelectionEndSample   = (int)Math.Round(endSample);

      WaveViewController.Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
      base.OnMouseWheel(e);

      if (WaveViewController == null || mSignal == null)
        return;

      var lOldSPP = WaveViewController.SamplesPerPixel;

      var zoomFactor = Math.Pow(1.0015, e.Delta * ((ModifierKeys & Keys.Control) != 0 ? 2.5 : 1.0));

      double lNewSamplesPerPixel = MathX.Clamp(lOldSPP / zoomFactor, WaveViewController.MinSamplesPerPixel, WaveViewController.MaxSamplesPerPixel);

      // keep sample under mouse fixed when zooming
      var pos = e.Location;

      var lInitialSampleUnderCursor = WaveViewController.StartSample + (pos.X * lOldSPP) - (lOldSPP / 2.0);

      var lNewSampleUnderCursor = (pos.X * lNewSamplesPerPixel) - (lNewSamplesPerPixel / 2.0);

      var lNewStartSample = MathX.Clamp(lInitialSampleUnderCursor - lNewSampleUnderCursor, 0, mSignal.Length);

      WaveViewController.Update(lNewSamplesPerPixel, lNewStartSample);

    }

    DiscreteSignal mSignal = null;
  }
}
