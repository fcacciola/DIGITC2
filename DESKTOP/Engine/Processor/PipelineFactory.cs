using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ENGINE
{
  public class PipelineFactory
  {
    public PipelineFactory()
    {
    }

    public static MainPipeline FromAudioToTapCode()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new SelectBand() )
               .Add( new NoiseFloorGate() )  
               .Add( new Envelope() )
               .Add( new UpwardCompress() )
               .Add( new Discretize() )
               .Add( new ExtractPulseSymbols() )
               .Add( new ExtractTapCode() )  ;

      return rPipeline ;
    }

    public static MainPipeline FromTapCode()
    {
      var rPipeline = new MainPipeline();
               
      rPipeline.Add( new BinarizeFromTapCode() ) 
               .Add( new BinaryToBytes())
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

      rPipeline.Add( new SelectBand() )
               .Add( new NoiseFloorGate() )  
               .Add( new Envelope() )
               .Add( new UpwardCompress() )
               .Add( new Discretize() )
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
