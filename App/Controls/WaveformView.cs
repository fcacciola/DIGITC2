using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

using NWaves.Signals;

namespace DIGITC2_App.Controls
{
  // A lightweight waveform renderer with pan & zoom. Set `Signal` to your DiscreteSignal data.
  // Mouse controls:
  //  - Left-drag to pan horizontally
  //  - Mouse wheel to zoom centered at cursor (use Ctrl to zoom faster)
  public class WaveformView : FrameworkElement
  {
    public static readonly DependencyProperty SignalProperty = DependencyProperty.Register(
        nameof(Signal), typeof(DiscreteSignal), typeof(WaveformView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public DiscreteSignal Signal
    {
      get => (DiscreteSignal)GetValue(SignalProperty);
      set => SetValue(SignalProperty, value);
    }

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(WaveformView),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceZoom));

    public double Zoom
    {
      get => (double)GetValue(ZoomProperty);
      set => SetValue(ZoomProperty, value);
    }

    private static object CoerceZoom(DependencyObject d, object baseValue)
    {
      var v = (double)baseValue;
      if (double.IsNaN(v) || double.IsInfinity(v))
        return 1.0;
      return Math.Max(0.0001, v);
    }

    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
        nameof(Offset), typeof(double), typeof(WaveformView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceOffset));

    public double Offset
    {
      get => (double)GetValue(OffsetProperty);
      set => SetValue(OffsetProperty, value);
    }

    private static object CoerceOffset(DependencyObject d, object baseValue)
    {
      var w = (WaveformView)d;
      var v = (double)baseValue;
      var signal = w.Signal;
      if (signal == null || signal.Length <= 1)
        return 0.0;

      // offset is in sample units (first sample shown)
      // maxOffset allows the last sample to be shown at the right edge
      var visibleSamples = (int)Math.Ceiling(Math.Max(1.0, w.ActualWidth) / w.Zoom);
      var maxOffset = Math.Max(0, signal.Length - 1 - visibleSamples);
      return Math.Max(0.0, Math.Min(v, maxOffset));
    }

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
      SizeChanged += (s, e) => CoerceValue(OffsetProperty);
    }

    private void WaveformView_MouseDown(object? sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        _isPanning = true;
        _lastPanPoint = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
      }
    }

    private void WaveformView_MouseMove(object? sender, MouseEventArgs e)
    {
      if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
      {
        var p = e.GetPosition(this);
        var dx = p.X - _lastPanPoint.X;
        // translate dx pixels into sample offset change
        var samplesPerPixel = SamplesPerPixel;
        var sampleDelta = -dx * samplesPerPixel;
        Offset = Offset + sampleDelta;
        _lastPanPoint = p;
        e.Handled = true;
      }
    }

    private void WaveformView_MouseUp(object? sender, MouseButtonEventArgs e)
    {
      if (_isPanning && e.ChangedButton == MouseButton.Left)
      {
        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
      }
    }

    private void WaveformView_MouseWheel(object? sender, MouseWheelEventArgs e)
    {
      var oldZoom = Zoom;
      // Use Ctrl for accelerated zoom
      var zoomFactor = Math.Pow(1.0015, e.Delta * (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 2.5 : 1.0));
      var newZoom = Math.Max(0.0001, oldZoom * zoomFactor);

      // keep sample under mouse fixed when zooming
      var pos = e.GetPosition(this);
      var samplesPerPixel = SamplesPerPixel;
      var sampleUnderMouse = Offset + pos.X * samplesPerPixel;

      // Update the bound slider (Zoom property is bound to a Slider)
      // We need to update the binding source
      var binding = BindingOperations.GetBinding(this, ZoomProperty);
      if (binding?.Source is Slider zoomSlider)
      {
        zoomSlider.Value = newZoom;
        var newSamplesPerPixel = SamplesPerPixel;
        var newOffset = sampleUnderMouse - pos.X * newSamplesPerPixel;
        var offsetBinding = BindingOperations.GetBinding(this, OffsetProperty);
        if (offsetBinding?.Source is Slider offsetSlider)
        {
          offsetSlider.Value = newOffset;
        }
      }
      else
      {
        // Fallback if not bound to a slider
        Zoom = newZoom;
        var newSamplesPerPixel = SamplesPerPixel;
        var newOffset = sampleUnderMouse - pos.X * newSamplesPerPixel;
        Offset = newOffset;
      }
      e.Handled = true;
    }

    private double SamplesPerPixel => Math.Max(1.0, ActualWidth) / Math.Max(0.0001, Zoom); // samples shown per pixel

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

      var samplesPerPixel = SamplesPerPixel;
      var startSample = (int)Math.Floor(Offset);
      var visibleSamples = (int)Math.Ceiling(w * samplesPerPixel);
      var endSample = Math.Min(signal.Length - 1, startSample + visibleSamples);

      // For each horizontal pixel compute min and max sample in that pixel column
      var pen = new Pen(WaveformPenBrush, 1.0);
      pen.Freeze();

      var halfH = ActualHeight / 2.0;

      // If zoomed in enough such that samplesPerPixel < 1 (multiple pixels per sample), plot individual samples as a polyline
      if (samplesPerPixel < 1.0)
      {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
          bool started = false;
          for (int s = startSample; s <= endSample; s++)
          {
            var x = (s - Offset) / samplesPerPixel; // pixel coordinate
            var val = signal[s];
            var y = center - val * halfH;
            var pt = new Point(x, y);
            if (!started)
            {
              ctx.BeginFigure(pt, false, false);
              started = true;
            }
            else
            {
              ctx.LineTo(pt, true, false);
            }
          }
        }
        geo.Freeze();
        dc.DrawGeometry(null, pen, geo);
      }
      else
      {
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
    }

    // Customizable brushes
    public Brush BackgroundBrush { get; set; } = Brushes.Black;
    public Brush WaveformPenBrush { get; set; } = Brushes.Lime;
    public Brush GridLineBrush { get; set; } = Brushes.DarkGray;

    protected override Size MeasureOverride(Size availableSize)
    {
      // allow stretch
      return new Size(Math.Max(100, availableSize.Width), Math.Max(40, availableSize.Height));
    }
  }
}
