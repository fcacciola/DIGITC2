using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public class Processor
  {
    public Processor()
    { 
    }

    public Processor Add( Filter aFilter ) 
    {
      mFilters.Add( aFilter ) ;
      return this ;
    }

    public Processor Then ( Processor aNext )
    {
      aNext.mFilters.ForEach( f => Add( f ) ) ;
      return this ;
    }

    public Result Process( Signal aInput )
    {
      Result rR = new Result();

      var lStep = rR.AddFirst(aInput) ;

      Context.Watch(lStep) ; 

      foreach( var lFilter in mFilters )
      { 
        lStep = lFilter.Apply(lStep);

        rR.Add( lStep ) ;

        Context.Watch(lStep) ; 
      }

      rR.Setup();

      return rR ;  
    }

    public static Processor FromAudioToBits_ByPulseDuration2()
    {
      var rProcessor = new Processor();

      rProcessor.Add( new Envelope() )
                .Add( new Discretize() )
                .Add( new ExtractPulseSymbols() )
                .Add( new BinarizeByDuration() ) ;

      return rProcessor ;
    }

    public static Processor FromAudioToBits_ByPulseDuration()
    {
      var rProcessor = new Processor();

      //rProcessor.Add( new Envelope2() )
      rProcessor.Add( new OnsetDetection() )
                .Add( new DecodeTaps() )  ;

      return rProcessor ;
    }

    public static Processor FromBits()
    {
      var rProcessor = new Processor();

      rProcessor.Add( new BinaryToBytes())
                .Add( new ScoreBytesAsLanguageDigits())
                .Add( new Tokenizer())
                .Add( new ScoreTokenLengthDistribution())
                .Add( new TokensToWords()) 
                .Add( new WordsToText()) ;

      return rProcessor ;
    }

    List<Filter> mFilters = new List<Filter>();
  }

  
}
