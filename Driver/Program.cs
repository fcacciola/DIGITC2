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

using MathNet.Numerics.Distributions;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace Driver
{
  internal class Program
  {
    static void Plots()
    {
      //var lD = new Zipf(3.0,255);

      //var lY = new Samples( lD.Samples().Take(10000).Select( s => (double)s ) );

      //var lH = new Histogram2(lY, new Histogram2.Params(256,0,255));

      //var lT0 = lH.Table;

      //lT0.CreatePlot().SavePNG("./Zipf.png");

      //lT0.ToLog().CreatePlot().SavePNG("./Zipf_Log.png");
    }

    [STAThread]
    static void Main(string[] args)
    {
      Args lArgs = Args.FromCmdLine(args);

      string lScriptFile = lArgs.Get("File0") ;

      if ( File.Exists(lScriptFile) ) 
      {
        Console.WriteLine($"Running script file: [{lScriptFile}]");

        string lUserScript = File.ReadAllText(lScriptFile); 

        try
        {
          ScriptDriver lScriptDriver = new ScriptDriver();
          lScriptDriver.Run( Path.GetFileNameWithoutExtension(lScriptFile),lUserScript, lArgs);
        }
        catch( Exception e ) 
        {
          Console.WriteLine(e.ToString() ); 
        }
      }
      else
      {
        if ( lArgs.GetBool("FromRandomBits") )
          FromRandomBits.Run(lArgs);

        if ( lArgs.GetBool("FromLargeText") )
          FromLargeText .Run(lArgs);
      }
    }
  }
}