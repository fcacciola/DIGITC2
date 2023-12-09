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
          FromLargeText.Run(lArgs);

        if ( lArgs.GetBool("FromMultipleTextSizes") )
          FromMultipleTextSizes.Run(lArgs); 

        if ( lArgs.GetBool("FromAudio_ByDuration") )
          FromAudio_ByDuration.Run(lArgs); 
      }
    }
  }
}