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
               .Add( new Discretize() )
               .Add( new ExtractPulseSymbols() )
               .Add( new BinarizeByDuration() ) ;

      return rPipeline ;
    }

    public static MainPipeline FromAudioToBits_ByTapCode()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new SplitBands() )
               .Add( new Envelope() )
               .Add( new Discretize(3) )
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
               .Add( new Tokenizer())
               .Add( new ScoreTokenLengthDistribution())
               .Add( new TokensToWords()) 
               .Add( new WordsToText()) ;

      return rPipeline ;
    }

    public static MainPipeline FromAudio_ByCode_ToDirectLetters()
    {
      var rPipeline = new MainPipeline();

      rPipeline.Add( new Envelope() )
               .Add( new Discretize() )
               .Add( new ExtractPulseSymbols() )
               .Add( new ExtractTapCode() )  
               .Add( new TapCodeToBytes())
               .Add( new ScoreBytesAsLanguageDigits())
               .Add( new Tokenizer())
               .Add( new ScoreTokenLengthDistribution())
               .Add( new TokensToWords()) 
               .Add( new WordsToText()) ;

      return rPipeline ;
    }

  }

  
}
