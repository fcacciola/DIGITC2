using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public class PipelineFactory
  {
    public PipelineFactory()
    {
    }

    public static MainPipeline FromAudioToBits_ByPulseDuration()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new Envelope() )
               .Add( new NoiseFloorGate() )  
               .Add( new Discretize( new GateThresholds(9,8,7,6,5,4,3,2,1) ) )
               .Add( new ExtractPulseSymbols() )
               .Add( new BinarizeFromDuration() ) ;

      return rPipeline ;
    }

    public static MainPipeline FromAudioToBits_ByTapCode()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new SplitBands() )
               .Add( new Envelope() )
               .Add( new NoiseFloorGate() )  
               .Add( new Discretize( new GateThresholds(9,8,7,6,5,4,3,2,1), new GateThresholds(7,5,3), new GateThresholds(7) ) )
               .Add( new ExtractPulseSymbols() )
               .Add( new ExtractTapCode() )  
               .Add( new BinarizeFromTapCode() ) ;

      return rPipeline ;
    }

    public static MainPipeline FromBits()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new BinaryToBytes())
               .Add( new ScoreBytesAsLanguageDigits())
               .Add( new Tokenize())
               .Add( new ScoreTokenLengthDistribution())
               .Add( new TokensToWords()) 
               .Add( new WordsToText()) ;

      return rPipeline ;
    }

    public static MainPipeline FromAudio_ByTapCode_ToDirectLetters()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new Envelope() )
               .Add( new NoiseFloorGate() )  
               .Add( new Discretize( new GateThresholds(9,8,7,6,5,4,3,2,1), new GateThresholds(7,5,3), new GateThresholds(7)) )
               .Add( new ExtractPulseSymbols() )
               .Add( new ExtractTapCode() )  
               .Add( new TapCodeToBytes())
               .Add( new ScoreBytesAsLanguageDigits())
               .Add( new Tokenize())
               .Add( new ScoreTokenLengthDistribution())
               .Add( new TokensToWords()) 
               .Add( new WordsToText()) ;

      return rPipeline ;
    }

  }

  
}
