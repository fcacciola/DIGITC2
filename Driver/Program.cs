using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

using DIGITC2;

namespace Driver
{

  public class Sample0
  {
    public void Go(string[] args)
    {
      string lAudioSample0 = @"..\..\Input\AudioSamples\Sample-1_5.wav" ;

      if ( File.Exists( lAudioSample0 ) )
      {
        Context lContext = new Context() { WindowSizeInSeconds = 0 } ;

        Trace.WriteLine("Building Sample Parallel Processor");

        var lProcessor = new Processor();

        lProcessor.Add( new Envelope(.1,.1) )
                  .Add( new AmplitudeGate(0.65) )
                  .Add( new ExtractGatedlSymbols( aMinDuration: 0.05, aMergeGap: 0.1 ) )
                  .Add( new BinarizeByDuration( aThreshold: 0.4 ) )
                  .Add( new BinaryToBytes(aBitsPerByte:5, aLittleEndian: true))
                  .Add( new BytesToText( aCharSet: "us-ascii")) ;

        lContext.Renderer.Render( lProcessor, lContext.RenderOptions, "Sample Parallel Processor");

        var lSource = new WaveSource(lAudioSample0) ;  

        lContext.Renderer.Render(lSource, lContext.RenderOptions, "Source");

        var lResult = lProcessor.Process( lSource, lContext ) ;
      }
    }
  }

  internal class Program
  {
    static void Main(string[] args)
    {
      string lLog = @"..\..\Output.txt" ;

      if ( File.Exists( lLog ) ) { File.Delete( lLog ); } 

      Trace.Listeners.Add( new TextWriterTraceListener(lLog) ) ;
      Trace.IndentSize  = 2 ;
      Trace.AutoFlush = true ;
      Trace.WriteLine("DIGITC 2 Sample Driver");

      var lS0 = new Sample0();

      lS0.Go(args);
    }
  }
}
