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
  public class BitsToText_Sample0
  {
    public void Go( int aBitsPerByteParam )
    {
      string lCharSet = "us-ascii";

      var lSource = BitsSource.FromText("Hello World!", lCharSet);  

      Context lContext = new Context() {} ;

      Trace.WriteLine($"Bits ({aBitsPerByteParam} bits-per-byte) To Text Sample Processor");

      var lProcessor = new Processor();
      lProcessor.Add( new BinaryToBytes( aBitsPerByte: aBitsPerByteParam, aLittleEndian: true))
                .Add( new BytesToText( aCharSet: lCharSet)) ;

      var lResult = lProcessor.Process( lSource, lContext ) ;
    }
  }

  public class BytesToText_Sample0
  {
    public void Go(string[] args)
    {
      string lCharSet = "us-ascii";

      var lSource = BytesSource.FromText("Hello World!", lCharSet);  

      Context lContext = new Context() {} ;

      Trace.WriteLine("Bytes To Text Sample Processor");

      var lProcessor = new Processor();

      lProcessor.Add( new BytesToText( aCharSet: lCharSet)) ;

      var lResult = lProcessor.Process( lSource, lContext ) ;
    }
  }

  public class WaveToTextSample0
  {
    public void Go(string[] args)
    {
      string lAudioSample0 = @"..\..\Input\AudioSamples\Sample-1_5.wav" ;

      if ( File.Exists( lAudioSample0 ) )
      {
        var lSource = new WaveFileSource(lAudioSample0) ;  

        Context lContext = new Context() { WindowSizeInSeconds = 250 } ;

        Trace.WriteLine("Building Sample Parallel Processor");

        var lProcessor = new Processor();

        lProcessor.Add( new Envelope( aAttackTime: .1, aReleaseTime: .1) )
                  .Add( new AmplitudeGate( aThreshold: 0.65) )
                  .Add( new ExtractGatedlSymbols( aMinDuration: 0.05, aMergeGap: 0.1 ) )
                  .Add( new BinarizeByDuration( aThreshold: 0.4 ) )
                  .Add( new BinaryToBytes(aBitsPerByte:5, aLittleEndian: true))
                  .Add( new BytesToText( aCharSet: "us-ascii")) ;

        var lResult = lProcessor.Process( lSource, lContext ) ;
      }
    }
  }

  public class Sample1
  {
    public void Go(string[] args)
    {
      Context lContext = new Context() ;
      
      var lParams = new TextTo_Duration_base_Keying_WaveSource.Params();

      lParams.EnvelopeAttackTime = 0.1 ;
      lParams.EnvelopeReleaseTime = 0.1 ;
      lParams.AmplitudeGateThreshold = 0.65 ;  
      lParams.ExtractGatedlSymbolsMinDuration = 0.05 ;
      lParams.ExtractGatedlSymbolsMergeGap = 0.1 ;
      lParams.BinarizeByDurationThreshold = 0.4 ;

      var lSource = new TextTo_Duration_base_Keying_WaveSource(lParams) ;

      var lProcessor0 = new Processor();
      var lProcessor1 = new Processor();
      var lProcessor2 = new Processor();

      lProcessor0.Add( new Envelope( aAttackTime: .1, aReleaseTime: .1) )
                 .Add( new AmplitudeGate( aThreshold: 0.65) )
                 .Add( new ExtractGatedlSymbols( aMinDuration: 0.05, aMergeGap: 0.1 ) )
                 .Add( new BinarizeByDuration( aThreshold: 0.4 ) );

      lProcessor1.Add( new BinaryToBytes(aBitsPerByte: 5, aLittleEndian: true))
                 .Add( new BytesToText( aCharSet: "us-ascii")) ;

      lProcessor2.Add( new BinaryToBytes(aBitsPerByte: 8, aLittleEndian: true))
                 .Add( new BytesToText( aCharSet: "us-ascii")) ;

      var lResult0 = lProcessor0.Process( lSource, lContext ) ;

      var lResult1 = lProcessor1.Process( lSource, lContext ) ;

      var lResult2 = lProcessor2.Process( lSource, lContext ) ;
     }
  }

  public class DurationBasedKeyingSample
  {
    public void Go(string[] args)
    {
      Context lContext = new Context() ;

      Trace.WriteLine("Building Duration-based Keying Processor");

      var lParams = new  TextTo_Duration_base_Keying_WaveSource.Params(){ Text = args[1]
                                                                        , EnvelopeAttackTime = 0.1
                                                                        , EnvelopeReleaseTime = 0.1
                                                                        , AmplitudeGateThreshold = 0.65
                                                                        , ExtractGatedlSymbolsMinDuration = 0.05
                                                                        , ExtractGatedlSymbolsMergeGap = 0.1
                                                                        , BinarizeByDurationThreshold = 0.1
                                                                        } ;
      
      var lSource = new TextTo_Duration_base_Keying_WaveSource(lParams) ;

      var lProcessor = new Processor();

      lProcessor.Add( new Envelope( aAttackTime: lParams.EnvelopeAttackTime, aReleaseTime: lParams.EnvelopeReleaseTime) )
                .Add( new AmplitudeGate( aThreshold: lParams.AmplitudeGateThreshold) )
                .Add( new ExtractGatedlSymbols( aMinDuration: lParams.ExtractGatedlSymbolsMinDuration, aMergeGap: lParams.ExtractGatedlSymbolsMergeGap ) )
                .Add( new BinarizeByDuration( aThreshold: lParams.BinarizeByDurationThreshold ) )
                .Add( new BinaryToBytes(aBitsPerByte: lParams.BinaryToBytesBitsPerByte, aLittleEndian: lParams.BinaryToBytesLittleEndian))
                .Add( new BytesToText( aCharSet: lParams.BytesToTextCharSet)) ;

      lContext.Renderer.Render( lProcessor, lContext.RenderOptions, "Duration-based Keying Processor");

      lContext.Renderer.Render(lSource, lContext.RenderOptions, "Source");

      lProcessor.Process( lSource, lContext ) ;
     }
  }

  internal class Program
  {
    static void Main(string[] args)
    {
      string lLog = @".\DIGITC2_Output.txt" ;

      if ( File.Exists( lLog ) ) { File.Delete( lLog ); } 

      Trace.Listeners.Add( new TextWriterTraceListener(lLog) ) ;
      Trace.Listeners.Add( new ConsoleTraceListener() ) ;
      Trace.IndentSize  = 2 ;
      Trace.AutoFlush = true ;
      Trace.WriteLine("DIGITC 2");

      if ( args.Length > 0 ) 
      {
        string lUserScript = File.ReadAllText(args[0]); 

        try
        {
          ScriptDriver lScriptDriver = new ScriptDriver();
          lScriptDriver.Run(lUserScript, args);
        }
        catch( Exception e ) 
        {
          Trace.WriteLine(e.ToString() ); 
        }
      }
      else
      { 
        new BitsToText_Sample0().Go(8);
      }

    }
  }
}