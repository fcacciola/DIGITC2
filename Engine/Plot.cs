using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using MigraDoc.DocumentObjectModel.Shapes.Charts;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

using Series = OxyPlot.Series.Series;

namespace DIGITC2
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

    }

    public Plot ( Options aOptions)  
    {
      mOptions = aOptions; 
      mPlot = new PlotModel { Title = mOptions.Title, Subtitle = mOptions.Subtitle, PlotAreaBorderThickness = new OxyThickness(0), Background = OxyColor.FromRgb(255,255,255) };
      mPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, AxislineThickness = 2, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });
      mPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left  , AxislineThickness = 2, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });
    }

    public void AddData( List<Histogram.Bin> aData )
    {
      var lLS1 = CreateSeries();

      for ( int i = 0; i < aData.Count ; ++ i )
      {
        lLS1.Points.Add(new DataPoint(i, aData[i].Frequency));
      }

      mPlot.Series.Add(lLS1);
    }

    public BitmapSource ToBitmap()
    {
      var lExporter = new PngExporter { Width = mOptions.BitmapWidth, Height = mOptions.BitmapHeight, Resolution = mOptions.BitmapResolution };
      var rBitmap = lExporter.ExportToBitmap(mPlot);
      return rBitmap;
    }

    public void SavePNG( Stream aStream )
    {
      BitmapSource lBitmap = ToBitmap();
      BitmapEncoder lEncoder = new PngBitmapEncoder();
      lEncoder.Frames.Add(BitmapFrame.Create(lBitmap));
      lEncoder.Save(aStream);
    }

    public void SavePNG( string aFilename )
    {
      using (var lFileStream = new FileStream(aFilename, FileMode.Create))
        SavePNG( lFileStream );  
    }

    LineSeries CreateSeries()
    {
      return new LineSeries();
    }

    Options   mOptions ;
    PlotModel mPlot ;
  }
}