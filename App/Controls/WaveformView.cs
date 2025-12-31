using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

using NWaves.Signals;
using DIGITC2_ENGINE;

namespace DIGITC2_App.Controls
{

  public class ZoomPanController
  {
    public double    MinSamplesPerPixel { get ; set ; }  
    public double    MaxSamplesPerPixel { get ; set ; }
    public double    SamplesPerPixel    { get ; set ; }
    public double    Offset             { get ; set ; }
    public Slider    ZoomSlider         { get ; set ; }
    public Slider    OffsetSlider       { get ; set ; }
    public WaveViews WaveViews          { get ; set ;  } 

    public void UpdateSliders()
    {
      ZoomSlider  .Value = SamplesPerPixel / ZoomSlider  .Maximum ;
      OffsetSlider.Value = Offset          / OffsetSlider.Maximum ;
    }

    public void UpdateFromSliders()
    {
      SamplesPerPixel = MaxSamplesPerPixel * (ZoomSlider.Maximum - ZoomSlider.Value) / ZoomSlider.Maximum ;
      Offset          = OffsetSlider.Maximum * (OffsetSlider.Maximum - OffsetSlider.Value) / OffsetSlider.Maximum ;
    }
  }

  // A lightweight waveform renderer with pan & zoom. Set `Signal` to your DiscreteSignal data.
  // Mouse controls:
  //  - Left-drag to pan horizontally
  //  - Mouse wheel to zoom centered at cursor (use Ctrl to zoom faster)
  public class WaveformView : FrameworkElement
  {
    public DiscreteSignal    Signal            { get ; set ; }
    public ZoomPanController ZoomPanController { get ; set ; }

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
        ZoomPanController.Offset = ZoomPanController.Offset + sampleDelta;
        _lastPanPoint = p;
        e.Handled = true;
        ZoomPanController.WaveViews.Invalidate();
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

      ZoomPanController.SamplesPerPixel = MathX.Clamp(lOldSPP / zoomFactor, ZoomPanController.MinSamplesPerPixel,  ZoomPanController.MaxSamplesPerPixel ) ;

      // keep sample under mouse fixed when zooming
      var pos = e.GetPosition(this);
      var sampleUnderMouse = ZoomPanController.Offset + pos.X * ZoomPanController.SamplesPerPixel;

      var newOffset = MathX.Clamp(  sampleUnderMouse - pos.X * ZoomPanController.SamplesPerPixel, 0, ZoomPanController.OffsetSlider.Maximum);
      ZoomPanController.Offset = newOffset;

      ZoomPanController.UpdateSliders();

      e.Handled = true;

      ZoomPanController.WaveViews.Invalidate();
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

      var signal = Signal;
      if (signal == null || signal.Length == 0)
        return;

      var samplesPerPixel = ZoomPanController.SamplesPerPixel;
      var startSample = (int)Math.Floor(ZoomPanController.Offset);
      var visibleSamples = (int)Math.Ceiling(w * samplesPerPixel);
      var endSample = Math.Min(signal.Length - 1, startSample + visibleSamples);

      // For each horizontal pixel compute min and max sample in that pixel column
      var pen = new Pen(WaveformPenBrush, 1.0);
      pen.Freeze();

      var halfH = ActualHeight / 2.0;

      // Aggregate min/max per pixel
      for (int px = 0; px < w; px++)
      {
        var sampleStart = startSample + (int)Math.Floor(px * samplesPerPixel);
        var sampleEnd = startSample + (int)Math.Ceiling((px + 1) * samplesPerPixel) - 1;
        if (sampleStart > endSample)
          break;
        sampleEnd = Math.Min(sampleEnd, endSample);
        if (sampleEnd < sampleStart)
          sampleEnd = sampleStart;

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int s = sampleStart; s <= sampleEnd; s++)
        {
          var v = signal[s];
          if (v < min)
            min = v;
          if (v > max)
            max = v;
        }

        var y1 = center - min * halfH;
        var y2 = center - max * halfH;
        dc.DrawLine(pen, new Point(px + 0.5, y1), new Point(px + 0.5, y2));
      }
    }

    // Customizable brushes
    public Brush BackgroundBrush { get; set; } = Brushes.Black;
    public Brush WaveformPenBrush { get; set; } = Brushes.Lime;
    public Brush GridLineBrush { get; set; } = Brushes.DarkGray;

    //protected override Size MeasureOverride(Size availableSize)
    //{
    //  // allow stretch
    //  return new Size(Math.Max(100, availableSize.Width), Math.Max(40, availableSize.Height));
    //}
  }
}
