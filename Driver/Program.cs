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
    [STAThread]
    static void Main(string[] args)
    {
      string lLog = @".\DIGITC2_Output.txt" ;

      if ( File.Exists( lLog ) ) { File.Delete( lLog ); } 

      Trace.Listeners.Add( new TextWriterTraceListener(lLog) ) ;
      //Trace.Listeners.Add( new ConsoleTraceListener() ) ;
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
        BitsToText_Sample0.Run( new Context(), args);
      }
    }
  }
}