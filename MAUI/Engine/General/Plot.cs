using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using Series = OxyPlot.Series.Series;

namespace DIGITC2_ENGINE
{
  public class Plot
  {
    public class Options
    {
      public string Title = "Plot";
      public string Subtitle = "" ;

      public int    BitmapWidth = 2000 ;
      public int    BitmapHeight = 2000 ;
      public double BitmapResolution = 96 ;

      public static Options Default = new Options(TypeE.Lines) ;

      public enum TypeE { Lines, Bars }

      public TypeE Type = TypeE.Lines ;

      public Options( TypeE aType ) { Type = aType ; }  

      public static Options Lines => new Options(TypeE.Lines) ;
      public static Options Bars  => new Options(TypeE.Bars) ;
    }

    public Plot ( Options aOptions = null )  
    {
      mOptions = aOptions ?? Options.Default; 
      mPlot = new PlotModel { Title = mOptions.Title, Subtitle = mOptions.Subtitle, PlotAreaBorderThickness = new OxyThickness(0), Background = OxyColor.FromRgb(255,255,255) };
      mPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, AxislineThickness = 2, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });
      mPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left  , AxislineThickness = 2, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });
    }

    public void AddSeries( Series aSeries )
    {
      mPlot.Series.Add(aSeries);
    }

    //public BitmapSource ToBitmap()
    //{
    //  var lExporter = new PngExporter { Width = mOptions.BitmapWidth, Height = mOptions.BitmapHeight, Resolution = mOptions.BitmapResolution };
    //  var rBitmap = lExporter.ExportToBitmap(mPlot);
    //  return rBitmap;
    //}

    public void SavePNG( Stream aStream )
    {
      //BitmapSource lBitmap = ToBitmap();
      //BitmapEncoder lEncoder = new PngBitmapEncoder();
      //lEncoder.Frames.Add(BitmapFrame.Create(lBitmap));
      //lEncoder.Save(aStream);
    }

    public void SavePNG( string aFilename )
    {
      using (var lFileStream = new FileStream(aFilename, FileMode.Create))
        SavePNG( lFileStream );  
    }

    Options   mOptions ;
    PlotModel mPlot ;
  }
}