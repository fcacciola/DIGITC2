using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public class Processor
  {
    public Processor( string aName )
    { 
      Name = aName ;
    }

    public Processor Add( Filter aFilter ) 
    {
      mFilters.Add( aFilter ) ;
      return this ;
    }

    public Processor Then ( Processor aNext )
    {
      aNext.mFilters.ForEach( f => Add( f ) ) ;

      Name = $"{Name}--{aNext.Name}";

      return this ;
    }

    public Result Process( Signal aInput )
    {
      Result rR = new Result();

      DContext.Session.SetupProcessor(Name);

      try
      {
        var lStep = rR.AddFirst(aInput) ;

        DContext.Watch(lStep) ; 

        mFilters.ForEach( f => f.Setup() ) ;

        foreach( var lFilter in mFilters )
        { 
          lStep = lFilter.Apply(lStep);
          if ( lStep != null )
          {
            rR.Add( lStep ) ;

            DContext.Watch(lStep) ; 

            if ( lStep.Quit )
            {
              DContext.WriteLine("Quitting processor because of Quit flag");
              break ;
            }
          }
        }

        mFilters.ForEach( f => f.Cleanup() ) ;

        rR.Setup();
      }
      catch( Exception x )
      {
        DContext.Error(x);
      }

      return rR ;  
    }

    public static Processor FromAudioToBits_ByPulseDuration()
    {
      var rProcessor = new Processor("FromAudioToBits_ByPulseDuration");

      rProcessor.Add( new Envelope() )
                .Add( new Discretize() )
                .Add( new ExtractPulseSymbols() )
                .Add( new BinarizeByDuration() ) ;

      return rProcessor ;
    }

    public static Processor FromAudioToBits_ByTapCode()
    {
      var rProcessor = new Processor("FromAudioToBits_ByTapCode");

      rProcessor.Add( new OnsetDetection() )
                .Add( new ExtractTapCode() )  
                .Add( new BinarizeFromTapCode() ) ;

      return rProcessor ;
    }

    public static Processor FromBits()
    {
      var rProcessor = new Processor("FromBits");

      rProcessor.Add( new BinaryToBytes())
                .Add( new ScoreBytesAsLanguageDigits())
                .Add( new Tokenizer())
                .Add( new ScoreTokenLengthDistribution())
                .Add( new TokensToWords()) 
                .Add( new WordsToText()) ;

      return rProcessor ;
    }

    public static Processor FromAudio_ByCode_ToDirectLetters()
    {
      var rProcessor = new Processor("FromAudio_ByCode_ToDirectLetters");

      rProcessor.Add( new OnsetDetection() )
                .Add( new ExtractTapCode() )  
                .Add( new TapCodeToBytes())
                .Add( new ScoreBytesAsLanguageDigits())
                .Add( new Tokenizer())
                .Add( new ScoreTokenLengthDistribution())
                .Add( new TokensToWords()) 
                .Add( new WordsToText()) ;

      return rProcessor ;
    }

    public string Name ;

    List<Filter> mFilters = new List<Filter>();
  }

  public class ProcessorFactory
  {
    public ProcessorFactory()
    {
      mMap.Add("TapCode", Processor.FromAudioToBits_ByTapCode().Then(Processor.FromBits()) ) ;
    }

    public IEnumerable<Processor> EnumProcessors => mMap.Values ;

    Dictionary<string,Processor> mMap = new Dictionary<string,Processor>();
  }

  
}
