using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIGITC2_Engine;

namespace Driver
{
  internal class Program
  {
    static Processor LinearSample0()
    {
      var rProcessor = new Processor();

      var lN0 = rProcessor.Add( new TrivialProcessingTask() );

      var lN1 = rProcessor.Add( new TrivialProcessingTask(), lN0 );

      var lN2 = rProcessor.Add( new TrivialProcessingTask(), lN1 );

      return rProcessor;
    }

    static Processor MultiSample0()
    {
      var rProcessor = new Processor();

      var lN0 = rProcessor.Add( new TrivialProcessingTask() );

      var lN1 = rProcessor.Add( new TrivialProcessingTask(), lN0 );

    //  var lN2 = rProcessor.Add( new TrivialProcessingTask(), lN0 );

    //  var lN3 = rProcessor.Add( new TrivialProcessingTask(), lN1, lN2 );

    //  var lN4 = rProcessor.Add( new TrivialProcessingTask(), lN3 );

    //  var lN5 = rProcessor.Add( new TrivialProcessingTask(), lN3 );

      return rProcessor;  
    }

    static Signal ProcessSample( Source aSource, Processor aProcessor )
    {
      return aProcessor.Process( aSource.GetSignal() ) ;
    }

    static void Main(string[] args)
    {
      Console.WriteLine("DIGITC 2 Sample Driver");

      var lS = new SourceA();

      var lP0 = LinearSample0();
      var lP1 = MultiSample0();

      var lS0 = ProcessSample(lS, lP0 );
      var lS1 = ProcessSample(lS, lP1 );

      TextSignalRenderer lConsoleRenderer = new TextSignalRenderer();

      lConsoleRenderer.Render( lS0 ) ;
    }
  }
}
