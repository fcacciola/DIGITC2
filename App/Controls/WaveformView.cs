using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

using NWaves.Signals;
using DIGITC2_ENGINE;
using System.Windows.Shapes;

namespace DIGITC2_App.Controls
{

  public class ZoomPanController
  {
    public double    MinSamplesPerPixel { get ; set ; }  
    public double    MaxSamplesPerPixel { get ; set ; }
    public WaveViews WaveViews          { get ; set ; }
    
    public double    SamplesPerPixel    { get ; private set ; }
    public double    StartSample        { get ; private set ; }

    public void UpdateSPP( double aSamplePerPixel )
    {
      SamplesPerPixel = aSamplePerPixel ;
      Invalidate();
    }

    public void UpdateSS( double aStartSample)
    {
      StartSample = aStartSample ;
      Invalidate();
    }

    public void Update( double aSamplePerPixel, double aStartSample)
    {
      SamplesPerPixel = aSamplePerPixel ;
      StartSample     = aStartSample ;
      Invalidate();
    }

    public void Invalidate()
    {
      WaveViews.Invalidate();
    }

  }

  // A lightweight waveform renderer with pan & zoom. Set `Signal` to your DiscreteSignal data.
  // Mouse controls:
  //  - Left-drag to pan horizontally
  //  - Mouse wheel to zoom centered at cursor (use Ctrl to zoom faster)
  public class WaveformView : FrameworkElement
  {
    public ZoomPanController ZoomPanController { get ; set ; }

    DiscreteSignal mSignal = null ;

    public DiscreteSignal Signal { get { return mSignal ; } set { mSignal = value ; InvalidateRender(); } }

    private bool _isPanning;
    private Point _lastPanPoint;

    public WaveformView()
    {
      SnapsToDevicePixels = true;
      Focusable = true;
      MouseDown += WaveformView_MouseDown;
      MouseMove += WaveformView_MouseMove;
      MouseUp += WaveformView_MouseUp;
      MouseWheel += WaveformView_MouseWheel;
//      SizeChanged += (s, e) => CoerceValue(Offset);

    }

    private void WaveformView_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        _isPanning = true;
        _lastPanPoint = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
      }
    }

    private void WaveformView_MouseMove(object sender, MouseEventArgs e)
    {
      if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
      {
        var p = e.GetPosition(this);
        var dx = p.X - _lastPanPoint.X;
        // translate dx pixels into sample offset change
        var sampleDelta = -dx * ZoomPanController.SamplesPerPixel;

        ZoomPanController.UpdateSS( MathX.Clamp(  ZoomPanController.StartSample + sampleDelta, 0, Signal.Length) ) ;

        _lastPanPoint = p;
        e.Handled = true;
      }
    }

    private void WaveformView_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (_isPanning && e.ChangedButton == MouseButton.Left)
      {
        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
      }
    }

    private void WaveformView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var lOldSPP = ZoomPanController.SamplesPerPixel;

      var zoomFactor = Math.Pow(1.0015, e.Delta * (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 2.5 : 1.0));

      double lNewSamplesPerPixel = MathX.Clamp(lOldSPP / zoomFactor, ZoomPanController.MinSamplesPerPixel,  ZoomPanController.MaxSamplesPerPixel ) ;

      // keep sample under mouse fixed when zooming
      var pos = e.GetPosition(this);

      var lInitialSampleUnderCursor = ZoomPanController.StartSample + ( pos.X * lOldSPP ) - ( lOldSPP / 2.0 );

      var lNewSampleUnderCursor = ( pos.X * ZoomPanController.SamplesPerPixel ) - ( ZoomPanController.SamplesPerPixel / 2.0 );

      var lNewStartSample = MathX.Clamp( lInitialSampleUnderCursor - lNewSampleUnderCursor, 0, Signal.Length) ;

      ZoomPanController.Update( lNewSamplesPerPixel, lNewStartSample ); 

      e.Handled = true;
    }

    StreamGeometry Poly = null ;

    public void InvalidateRender()
    {
      Poly = null ;
      InvalidateVisual(); 
    }

    void CacheRender()
    {
      var signal = Signal;
      if (signal == null || signal.Length == 0)
        return;

      var samplesPerPixel = ZoomPanController.SamplesPerPixel;
      var startSample = (int)Math.Floor(ZoomPanController.StartSample);
      var visibleSamples = (int)Math.Ceiling(ActualWidth * samplesPerPixel);
      var endSample = Math.Min(signal.Length - 1, startSample + visibleSamples);

      // For each horizontal pixel compute min and max sample in that pixel column

      var center = ActualHeight / 2.0;
      var halfH  = ActualHeight / 2.0;

      var lPoints = new List<Point>();

      // Aggregate min/max per pixel
      for (int px = 0; px < ActualWidth; px++)
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

        lPoints.Add( new Point( px + 0.5, center - lMin * halfH ) ) ;
        lPoints.Add( new Point( px + 0.5, center - lMax * halfH ) ) ;
      }

      Point lHead = lPoints[0];
      lPoints.RemoveAt(0);

      Poly = new StreamGeometry();

      using (StreamGeometryContext ctx = Poly.Open())
      {
        ctx.BeginFigure(lHead, false, false);
        ctx.PolyLineTo(lPoints, true, false);
      }

      // 3. Freeze the geometry for performance (optional but recommended)
      Poly.Freeze();
    }

    StreamGeometry GetPoly()
    {
      if ( Poly == null )
        CacheRender();
      return Poly ;
    }

    protected override void OnRender(DrawingContext dc)
    {
      base.OnRender(dc);

      var w = (int)ActualWidth;
      var h = (int)ActualHeight;
      if (w <= 0 || h <= 0)
        return;

      // background
      dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

      // center line
      var center = ActualHeight / 2.0;
      dc.DrawLine(new Pen(GridLineBrush, 1.0), new Point(0, center), new Point(ActualWidth, center));

      var lPoly = GetPoly();
      if ( lPoly != null )
        dc.DrawGeometry(WaveformPolyBrush, Pen, lPoly);

    }

    // Customizable brushes
    public static Brush BackgroundBrush { get; set; } = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF5F0E8"));
    public static Brush WaveformPolyBrush { get; set; } = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4169E1"));
    public static Brush GridLineBrush { get; set; } = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B0C4DE"));

    public static Pen   Pen { get ; set ; } = new Pen(WaveformPolyBrush, 1.0); 

    //protected override Size MeasureOverride(Size availableSize)
    //{
    //  // allow stretch
    //  return new Size(Math.Max(100, availableSize.Width), Math.Max(40, availableSize.Height));
    //}
  }
}
