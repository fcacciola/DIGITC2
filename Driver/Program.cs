using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIGITC2_Engine;

namespace Driver
{
  internal class Program
  {
    static Processor MultiProcessorSample0()
    {
      Trace.WriteLine("Building Trivial Parallel Processor");

      var rProcessor = new Processor();

      rProcessor.Add( new TrivialProcessingTask("_M0") );

      rProcessor.Add( new TrivialProcessingTask("_M1") );

      rProcessor.AddParallel( new TrivialProcessingTask("_M2_A"), new TrivialProcessingTask("_M2_B") );

      return rProcessor;  
    }

    static Signal ProcessSample( Source aSource, Processor aProcessor )
    {
      return aProcessor.Process( aSource ) ;
    }

    static void Main(string[] args)
    {
      string lLog = "C:\\Users\\User\\Dropbox\\ECB\\ITC\\Digital ITC\\DIGITC2\\Output.txt" ;

      if ( File.Exists( lLog ) ) { File.Delete( lLog ); } 

      Trace.Listeners.Add( new TextWriterTraceListener(lLog) ) ;
      Trace.IndentSize  = 2 ;
      Trace.AutoFlush = true ;
      Trace.WriteLine("DIGITC 2 Sample Driver");

      var lS = new SourceA();

      var lMPS0 = MultiProcessorSample0();

      Trace.WriteLine("Processing Trivial Parallel Processor");
      var lRS = ProcessSample(lS, lMPS0 );

      TextSignalRenderer lTraceRenderer = new TextSignalRenderer();

      lTraceRenderer.Render( lMPS0, "Trivial Parallel Processor");

      Trace.WriteLine("");

      lTraceRenderer.Render( lRS, "Trivial Parallel Processor results"  ) ;

    }
  }
}
