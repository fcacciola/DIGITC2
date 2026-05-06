using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

using Series = OxyPlot.Series.Series;

namespace DIGITC2_ENGINE
{
  public class Plotter
  {
    public class Options
    {
      public string Title = "Plot";
      public string Subtitle = "" ;

      public static Options Default = new Options{Type=TypeE.Lines} ;

      public enum TypeE { Lines, Bars }

      public TypeE Type = TypeE.Lines ;

      public static Options Lines => new Options{Type=TypeE.Lines} ;
      public static Options Bars  => new Options{Type=TypeE.Bars} ;
    }

    public Plotter ( Options aOptions = null )  
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

    public Bitmap ToBitmap()
    {
      var lExporter = new PngExporter { Width = 3000, Height = 1500, Resolution = 96 };
      var rBitmap = lExporter.ExportToBitmap(mPlot);
      return rBitmap;
    }

    public void SavePNG( string aFilename )
    {
      DContext.WriteDetailLine($"Saving PNG Image to: [{aFilename}]");
      try
      {
        var lBitmap = ToBitmap();
        lBitmap?.Save(aFilename);
      }
      catch( Exception ex ) 
      {
        DContext.Error(ex);
      }
    }

    public void SaveSVG( string aFilename )
    {
      DContext.WriteDetailLine($"Saving SVG PLOT: [{aFilename}]");
      try
      {
        var lExporter = new OxyPlot.SvgExporter { Width = 1000, Height = 500 };
        lExporter.ExportToFile(mPlot,aFilename);
      }
      catch( Exception ex ) 
      {
        DContext.Error(ex);
      }
    }

    Options   mOptions ;
    PlotModel mPlot ;
  }


}