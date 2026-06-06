using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

using DIGITC2_ENGINE;

using NWaves.Signals;

namespace Transgraphier_1_0_App
{

  public class TimelineView : ControlledView
  {
    private TimelinePanel mTimelinePanel;
    
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Timeline Timeline
    {
      get => mTimelinePanel.Timeline; set => mTimelinePanel.Timeline = value;
    }

    public TimelineView() : base()
    {
      InitializeComponent();
    }

    protected override ControlledPanel Panel => mTimelinePanel;

    protected override void InitializePanelComponent()
    {
      mTimelinePanel = new TimelinePanel();
      mTimelinePanel.Dock = DockStyle.Fill;
      mTimelinePanel.Name = "mWavePanel";
      mTimelinePanel.TabIndex = 0;
      mTimelinePanel.Height = 200;
      Controls.Add(mTimelinePanel);

      Name = "TimelineView";
    }
  }

  public class TimelinePanel : ControlledPanel
  {
    public TimelinePanel() : base()
    {
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Timeline Timeline
    {
      get => mTimeline; set
      {
        mTimeline = value;
        InvalidateRender();
      }
    }

    const int MarginS = 2;
    const int cTitleHeight = 30;
    int ViewH => Height - (MarginS * 2) - cTitleHeight;
    int ViewHalfH => ViewH / 2;

    int CenterY => BottomY - ViewHalfH;

    protected override int BottomY => Height - MarginS;

    protected override void CacheRender()
    {
      List<TimelineEntriesAtPos> lEntriesAtPos = new List<TimelineEntriesAtPos>();

      if ( mTimeline != null && mTimeline.Entries.Count > 0)
      {
        var samplesPerPixel = ViewController.SamplesPerPixel;
        var startSample = (int)Math.Floor(ViewController.PanStartSample);
        var visibleSamples = (int)Math.Ceiling(Width * samplesPerPixel);
        var endSample = Math.Min(ViewController.Length + 1, startSample + visibleSamples);

        List<TimelineEntryPos> lPositions = new List<TimelineEntryPos>();

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

          lPositions.Add(new TimelineEntryPos(px, sampleStart, sampleEnd));
        }

        if ( lPositions.Count < 2 )
          return;

        foreach( var lPos in lPositions )
        {
          var lEAP = mTimeline.GetVisibleEntries(lPos);
          if (lEAP != null)
            lEntriesAtPos.Add(lEAP);
        }
      }

      mRender = new Bitmap(Width, Height);

      using (var g = Graphics.FromImage(mRender))
      {
        // background
        g.FillRectangle(Brushes.AntiqueWhite, new Rectangle(0, 0, Width, Height));

        foreach( var lEAPs in lEntriesAtPos)
        {
          if ( lEAPs.Entries.Count > 0 )
          {
            string lLabel = lEAPs.Entries[0].Label;
            var lSize = g.MeasureString(lLabel, Font);
            g.DrawString(lLabel, Font, Brushes.Black, lEAPs.Pos.PixelX - (lSize.Width / 2), CenterY - (lSize.Height / 2));
          }
       } 
      }
    }


    Timeline mTimeline = null;
  }
}
