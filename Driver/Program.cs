using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using DIGITC2;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace Driver
{
  internal class Program
  {
    static void TestPlot()
    {
      var rPlot = new PlotModel { Title = "PlotAreaBorderThickness = 0", Subtitle = "AxislineThickness = 1, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid", PlotAreaBorderThickness = new OxyThickness(0) };
      rPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, AxislineThickness = 1, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });
      rPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, AxislineThickness = 1, AxislineColor = OxyColors.Blue, AxislineStyle = LineStyle.Solid });

      var lLS1 = new LineSeries();
      lLS1.Points.Add(new DataPoint(1, 0));
      lLS1.Points.Add(new DataPoint(2, 10));
      lLS1.Points.Add(new DataPoint(3, 10));
      lLS1.Points.Add(new DataPoint(4, 0));
      lLS1.Points.Add(new DataPoint(5, 0));
      lLS1.Points.Add(new DataPoint(7, 7));
      lLS1.Points.Add(new DataPoint(7, 0));
      rPlot.Series.Add(lLS1);

      var pngExporter2 = new PngExporter { Width = 2000, Height = 1500, Resolution = 96 };
      var bitmap = pngExporter2.ExportToBitmap(rPlot);

      using (var fileStream = new FileStream(".\\Plot.png", FileMode.Create))
      {
          BitmapEncoder encoder = new PngBitmapEncoder();
          encoder.Frames.Add(BitmapFrame.Create(bitmap));
          encoder.Save(fileStream);
      }
    }

    [STAThread]
    static void Main(string[] args)
    {
      TestPlot();

      string lLog = @".\DIGITC2_Output.txt" ;

      if ( File.Exists( lLog ) ) { File.Delete( lLog ); } 

      Trace.Listeners.Add( new TextWriterTraceListener(lLog) ) ;
      Trace.Listeners.Add( new ConsoleTraceListener() ) ;
      Trace.IndentSize  = 2 ;
      Trace.AutoFlush = true ;
      Trace.WriteLine("DIGITC 2");

      string lScriptFile = args.Length > 0 ? args[0] : "" ;

      if ( File.Exists(lScriptFile) ) 
      {
        Console.WriteLine($"Running script file: [{lScriptFile}]");

        string lUserScript = File.ReadAllText(lScriptFile); 

        try
        {
          ScriptDriver lScriptDriver = new ScriptDriver();
          lScriptDriver.Run( Path.GetFileNameWithoutExtension(lScriptFile),lUserScript, args);
        }
        catch( Exception e ) 
        {
          Trace.WriteLine(e.ToString() ); 
        }
      }
      else
      {
        BitsToText_Sample1.Run( new Context(), args);
      }
    }
  }
}