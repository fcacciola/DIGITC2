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



  public class TimelineEntryPos
  {
    public TimelineEntryPos(int aPixelX, int aSampleStart, int aSampleEnd)
    {
      PixelX      = aPixelX;
      SampleStart = aSampleStart;
      SampleEnd   = aSampleEnd;
    }

    public int PixelX ;
    public int SampleStart ;
    public int SampleEnd;

    public override string ToString()
    {
      return $"({PixelX}|{SampleStart}->{SampleEnd})";
    }
  }

  public class TimelineEntry
  {
    public TimelineEntry(int aPos, string aLabel)
    {
      Pos   = aPos;
      Label = aLabel;
    }

    public int    Pos ;
    public string Label;

    public bool IsVisible( TimelineEntryPos aPos)
    {
      return Pos >= aPos.SampleStart && Pos <= aPos.SampleEnd;
    }

    public override string ToString()
    {
      return $"({Pos}|{Label})";
    }
  }

  public class TimelineEntriesAtPos
  {
    public TimelineEntriesAtPos( TimelineEntryPos aPos, List<TimelineEntry> aEntries)
    {
      Pos     = aPos;
      Entries = aEntries;
    }
    public TimelineEntryPos Pos;
    public List<TimelineEntry> Entries = new List<TimelineEntry>();

    public override string ToString()
    {
      return $"Pos: {Pos}, Entries: [{string.Join(", ", Entries)}]";
    }
  }

  public class Timeline
  {
    public Timeline( List<TimelineEntry> aEntries)
    {
      Entries = aEntries;
    }

    public TimelineEntriesAtPos GetVisibleEntries( TimelineEntryPos aPos)
    {
      var r = new List<TimelineEntry>();
      foreach (var e in Entries)
      {
        if (e.IsVisible(aPos))
          r.Add(e);
      }
      return r.Count > 0 ? new TimelineEntriesAtPos(aPos, r) : null ;
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

    public List<TimelineEntry> Entries = new List<TimelineEntry>();
  }

    
}
