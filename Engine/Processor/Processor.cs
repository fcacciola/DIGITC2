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

      return rR ;  
    }

    public static Processor FromAudioToBits_ByPulseDuration()
    {
      var rProcessor = new Processor();

      rProcessor.Add( new Envelope() )
                .Add( new AmplitudeGate() )
                .Add( new ExtractGatedlSymbols() )
                .Add( new BinarizeByDuration() ) ;

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
                .Add( new ScoreWordFrequencyDistribution());

      return rProcessor ;
    }

    List<Filter> mFilters = new List<Filter>();
  }

  
}
