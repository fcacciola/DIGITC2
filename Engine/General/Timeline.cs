using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{



  public class TimelinePixelSpan
  {
    public TimelinePixelSpan(int aPixelX, int aSampleStart, int aSampleEnd)
    {
      Pixel      = aPixelX;
      SampleStart = aSampleStart;
      SampleEnd   = aSampleEnd;
    }

    public int Pixel ;
    public int SampleStart ;
    public int SampleEnd;

    public override string ToString()
    {
      return $"({Pixel}|{SampleStart}->{SampleEnd})";
    }
  }

  public class TimelineLabel
  {
    public TimelineLabel(int aPixel, string aLabel)
    {
      Pixel = aPixel;
      Label = aLabel;
    }

    public int    Pixel ;
    public string Label;

    public bool IsVisible( TimelinePixelSpan aPos)
    {
      return Pixel >= aPos.SampleStart && Pixel <= aPos.SampleEnd;
    }

    public override string ToString()
    {
      return $"({Pixel}|{Label})";
    }
  }

  public class TimelineLabelsAtPixel
  {
    public TimelineLabelsAtPixel( TimelinePixelSpan aSpan, List<TimelineLabel> alabels)
    {
      Span   = aSpan;
      Labels = alabels;
    }
    public TimelinePixelSpan Span;

    public List<TimelineLabel> Labels = new List<TimelineLabel>();

    public override string ToString()
    {
      return $"Span: {Span}, Labels: [{string.Join(", ", Labels)}]";
    }
  }

  public class Timeline
  {
    public Timeline( List<TimelineLabel> aEntries)
    {
      Entries = aEntries;
    }

    public TimelineLabelsAtPixel GetLabelsAtPixel( TimelinePixelSpan aPos)
    {
      var r = new List<TimelineLabel>();
      foreach (var e in Entries)
      {
        if (e.IsVisible(aPos))
          r.Add(e);
      }
      return r.Count > 0 ? new TimelineLabelsAtPixel(aPos, r) : null ;
    }

    public void Save( string aFilename )
    {
      JsonSerializer serializer = new JsonSerializer();
      using (StreamWriter sw = new StreamWriter(aFilename))
      using (JsonWriter writer = new JsonTextWriter(sw))
      {
        serializer.Serialize(writer, this);
      }
    }

    public static Timeline Load(string aFilename)
    {
      JsonSerializer serializer = new JsonSerializer();
      using (StreamReader sr = new StreamReader(aFilename))
      using (JsonReader reader = new JsonTextReader(sr))
      {
        return serializer.Deserialize<Timeline>(reader);
      }
    }

    public List<TimelineLabel> Entries = new List<TimelineLabel>();
  }

    
}
