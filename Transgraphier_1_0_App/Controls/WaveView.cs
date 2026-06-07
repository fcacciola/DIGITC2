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

  public abstract class ControlledView : UserControl
  {
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
    public ViewController ViewController
    {
      get => Panel.ViewController; set => Panel.ViewController = value;
    }

    protected ControlledView()
    {
    }


    public void InvalidateRender()
    {
      Panel.InvalidateRender();
    }

    protected abstract ControlledPanel Panel { get ; }

    protected abstract void InitializePanelComponent();

    protected void InitializeComponent()
    {
      mTitle = new Label();

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

      InitializePanelComponent();

      ResumeLayout(false);
    }
  }

  public abstract class ControlledPanel : Control
  {
    protected ControlledPanel()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      SuspendLayout();
      Height = 165;
      ResumeLayout(false);
    }

    public ViewController ViewController = null;

    protected Bitmap mRender = null ;

    public void InvalidateRender()
    {
      mRender?.Dispose(); 
      mRender = null;
      Invalidate();
    }

    Bitmap GetRender()
    {
      if (mRender == null)
      {
        CacheRender();
      }
      return mRender;
    }


    protected abstract void CacheRender() ;

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      var lRender = GetRender();  
      if ( lRender == null)
        return; 

      Bitmap lCopy = null ;

      // Draw selection overlay if measurement tool is active
      if (ViewController.MeasureTimeTool != null && ViewController.MeasureTimeTool.IsActive && ViewController.MeasureTimeTool.SelectionStartX >= 0)
      {
        lCopy = new Bitmap(lRender); 
        using ( var g = Graphics.FromImage(lCopy))
          DrawSelectionOverlay(g);
        lRender = lCopy;  
      }

      e.Graphics.DrawImageUnscaled(lRender, 0, 0);

      lCopy?.Dispose();
    }

    protected abstract int BottomY { get; }

    void DrawSelectionOverlay(Graphics g)
    {
      if (ViewController.MeasureTimeTool.SelectionStartX < 0 || ViewController.MeasureTimeTool.SelectionEndX < 0)
        return;

      int startX = Math.Min(ViewController.MeasureTimeTool.SelectionStartX, ViewController.MeasureTimeTool.SelectionEndX);
      int endX   = Math.Max(ViewController.MeasureTimeTool.SelectionStartX, ViewController.MeasureTimeTool.SelectionEndX);

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

      if (ViewController == null )
        return;

      if (e.Button == MouseButtons.Left)
      {
        if (ViewController.MeasureTimeTool != null && ViewController.MeasureTimeTool.IsActive)
        {
          // Start selection for measurement tool
          ViewController.MeasureTimeTool.SelectionStartX = e.X;
          ViewController.MeasureTimeTool.SelectionEndX   = e.X;
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

      if (ViewController == null )
        return;

      if (e.Button == MouseButtons.Left)
      {
        if (ViewController.MeasureTimeTool != null && ViewController.MeasureTimeTool.IsActive)
        {
          // End selection - update the measurement tool with sample positions
          ViewController.MeasureTimeTool.SelectionEndX = e.X;
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

      if (ViewController == null )
        return;

      if (e.Button == MouseButtons.Left && ViewController.MeasureTimeTool.SelectionStartX >= 0 && ViewController.MeasureTimeTool != null && ViewController.MeasureTimeTool.IsActive)
      {
        // Update selection end while dragging
        ViewController.MeasureTimeTool.SelectionEndX = e.X;
        ViewController.Invalidate();
      }
      else if (e.Button == MouseButtons.Left && mIsPanning)
      {
        var p = e.Location;

        var dx = p.X - mLastMousePos.X;

        // translate dx pixels into sample offset change
        var sampleDelta = -dx * ViewController.SamplesPerPixel;

        ViewController.UpdateSS(MathX.Clamp(ViewController.PanStartSample + sampleDelta, 0, ViewController.Length));

        mLastMousePos = p;
      }
    }

    private void UpdateMeasurementSelection()
    {
      if (ViewController.MeasureTimeTool == null )
        return;

      // Convert pixel coordinates to sample coordinates
      int startX = Math.Min(ViewController.MeasureTimeTool.SelectionStartX, ViewController.MeasureTimeTool.SelectionEndX);
      int endX   = Math.Max(ViewController.MeasureTimeTool.SelectionStartX, ViewController.MeasureTimeTool.SelectionEndX);

      double startSample = ViewController.PanStartSample + (startX * ViewController.SamplesPerPixel);
      double endSample   = ViewController.PanStartSample + (endX   * ViewController.SamplesPerPixel);

      ViewController.MeasureTimeTool.SelectionStartSample = (int)Math.Round(startSample);
      ViewController.MeasureTimeTool.SelectionEndSample   = (int)Math.Round(endSample);

      ViewController.Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
      base.OnMouseWheel(e);

      if (ViewController == null )
        return;

      var lOldSPP = ViewController.SamplesPerPixel;

      var zoomFactor = Math.Pow(1.0015, e.Delta * ((ModifierKeys & Keys.Control) != 0 ? 2.5 : 1.0));

      double lNewSamplesPerPixel = MathX.Clamp(lOldSPP / zoomFactor, ViewController.MinSamplesPerPixel, ViewController.MaxSamplesPerPixel);

      // keep sample under mouse fixed when zooming
      var pos = e.Location;

      var lInitialSampleUnderCursor = ViewController.PanStartSample + (pos.X * lOldSPP) - (lOldSPP / 2.0);

      var lNewSampleUnderCursor = (pos.X * lNewSamplesPerPixel) - (lNewSamplesPerPixel / 2.0);

      var lNewStartSample = MathX.Clamp(lInitialSampleUnderCursor - lNewSampleUnderCursor, 0, ViewController.Length);

      ViewController.Update(lNewSamplesPerPixel, lNewStartSample);

    }
  }


  public class WaveView : ControlledView
  {
    private WavePanel mWavePanel;
    private bool mIncludeRuler;
    private bool mColorCoded;

    
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiscreteSignal Signal
    {
      get => mWavePanel.Signal; set => mWavePanel.Signal = value;
    }

    public WaveView(bool aIncludeRuler, bool aColorCoded) : base()
    {
      mIncludeRuler = aIncludeRuler;
      mColorCoded = aColorCoded;

      InitializeComponent();

    }

    protected override ControlledPanel Panel => mWavePanel;

    protected override void InitializePanelComponent()
    {
      mWavePanel = new WavePanel(mIncludeRuler, mColorCoded);

      mWavePanel.Dock = DockStyle.Fill;
      mWavePanel.Name = "mWavePanel";
      mWavePanel.TabIndex = 0;
      mWavePanel.Height = 200;
      Controls.Add(mWavePanel);

      Name = "WaveView";
    }
  }

  public class WavePanel : ControlledPanel
  {
    public WavePanel(bool aIncludeRuler, bool aColorCoded) : base() 
    {
      mIncludeRuler = aIncludeRuler;
      mColorCoded = aColorCoded;
      RulerH = mIncludeRuler ? 40 : 0;
    }

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
    bool mColorCoded;
    Ruler mRuler = null ;

    public int RulerH = 0;
    const int MarginS = 2;
    const int cTitleHeight = 30;
    int WaveH => Height - RulerH - (MarginS * 2) - cTitleHeight;
    int WaveHalfH => WaveH / 2;
    int CenterY => BottomY - WaveHalfH;

    protected override int BottomY => Height - MarginS - RulerH;
 
    protected override void CacheRender()
    {
      var signal = mSignal;
      if (signal == null || signal.Length == 0)
        return;

      var samplesPerPixel = ViewController.SamplesPerPixel;
      var startSample = (int)Math.Floor(ViewController.PanStartSample);
      var visibleSamples = (int)Math.Ceiling(Width * samplesPerPixel);
      var endSample = Math.Min( Math.Min( signal.Length, ViewController.Length ) - 1, startSample + visibleSamples);

      // For each horizontal pixel compute min and max sample in that pixel column

      var lRedPoly   = new List<Point>();
      var lBluePoly  = new List<Point>();
      var lBlackPoly = new List<Point>();
      var lGrayPoly  = new List<Point>();

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

        if ( mColorCoded )
        {
          var lPoly = mColorCoded ? ( lMin != lMax ? lGrayPoly : ( lMax > .8 ? lBlackPoly : lMax > .5 ? lBluePoly : lRedPoly ) ) : lBluePoly;  

          lPoly.Add(new Point(px, CenterY));
          lPoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMin * WaveHalfH)));
          lPoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMax * WaveHalfH)));
          lPoly.Add(new Point(px, CenterY));
        }
        else
        {
          lBluePoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMin * WaveHalfH)));
          lBluePoly.Add(new Point(px, CenterY - (int)Math.Ceiling(lMax * WaveHalfH)));
        }
      }

      if ( lRedPoly.Count == 0 && lBluePoly.Count == 0 && lBlackPoly.Count == 0 )
        return;

      if ( mIncludeRuler )
      {
        // Ruler area
        int rulerTop = Height - RulerH;
        int rulerHeight = RulerH;

        // start time at left pixel
        double startTime = ViewController.PanStartSample / (double)SIG.SamplingRate; // seconds

        // compute pixels per second
        double pixelsPerSecond = SIG.SamplingRate / ViewController.SamplesPerPixel;

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
  
        if ( lRedPoly  .Count > 0 ) g.DrawLines(Pens.Red     , lRedPoly  .ToArray());
        if ( lBluePoly .Count > 0 ) g.DrawLines(Pens.Blue    , lBluePoly .ToArray());
        if ( lBlackPoly.Count > 0 ) g.DrawLines(Pens.Black   , lBlackPoly.ToArray());
        if ( lGrayPoly .Count > 0 ) g.DrawLines(Pens.DarkGray, lGrayPoly .ToArray());
  
        if ( mIncludeRuler )
          DrawTimeRuler(g);
      }
    }
    void DrawTimeRuler(Graphics g)
    {
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

    DiscreteSignal mSignal = null;
  }
}
