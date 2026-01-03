using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using NWaves.Signals ;

namespace Transgraphier_1_0_App
{
  public class WaveView : UserControl
  {
    private Panel mContainerPanel;
    private TextBox mDataTextBox;
    private WavePanel mWavePanel;
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
    public string InfoText
    {
      get
      {
        return mDataTextBox.Text;
      }
      set
      {
        mDataTextBox.Text = value;
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
      // Main container panel with black border
      mContainerPanel = new Panel();
      mContainerPanel.BackColor = Color.Black;
      mContainerPanel.Dock = DockStyle.Fill;
      mContainerPanel.Padding = new Padding(2);

      // mTitle label
      mTitle = new Label();
      mTitle.Text = "mTitle";
      mTitle.Dock = DockStyle.Top;
      mTitle.Height = 25;
      mTitle.BackColor = Color.LightGray;
      mTitle.ForeColor = Color.Black;
      mTitle.TextAlign = ContentAlignment.MiddleLeft;
      mTitle.Padding = new Padding(5, 0, 0, 0);

      // Inner container (white background for content)
      Panel innerContainer = new Panel();
      innerContainer.BackColor = Color.White;
      innerContainer.Dock = DockStyle.Fill;

      // Left info text box (100 width)
      mDataTextBox = new TextBox();
      mDataTextBox.Dock = DockStyle.Left;
      mDataTextBox.Width = 100;
      mDataTextBox.ReadOnly = true;
      mDataTextBox.Multiline = true;
      mDataTextBox.ScrollBars = ScrollBars.None;
      mDataTextBox.BorderStyle = BorderStyle.Fixed3D;

      // Right waveform panel
      mWavePanel = new WavePanel();
      mWavePanel.Dock = DockStyle.Fill;

      // Add controls
      innerContainer.Controls.Add(mWavePanel);
      innerContainer.Controls.Add(mDataTextBox);

      mContainerPanel.Controls.Add(innerContainer);
      mContainerPanel.Controls.Add(mTitle);

      this.Controls.Add(mContainerPanel);
      this.Height = 100;
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
      e.Graphics.FillRectangle( Brushes.Coral, new Rectangle(0, 0, Width, Height));

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

    DiscreteSignal mSignal = null ;
  }
}
