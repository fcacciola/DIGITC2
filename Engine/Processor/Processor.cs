using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  
  public class ProcessingBranch
  {
    public ProcessingBranch( string aName = "Main" )
    { 
      Name = aName;
    }

    public void Start( Signal aStartSignal ) 
    {
      mStartToken = new ProcessingToken(null, aStartSignal, "") ;

      mFilters.ForEach( filter => filter.Setup() ) ;
    }

    public void End()
    {
      mFilters.ForEach( filter => filter.Cleanup() ) ;
    }

    public ProcessingBranch Add( Filter aFilter ) 
    {
      mFilters.Add( aFilter ) ;
      return this ;
    }

    public ProcessingBranch Then ( ProcessingBranch aNext )
    {
      aNext.mFilters.ForEach( f => Add( f ) ) ;

      return this ;
    }
    
    public ProcessingBranch NewBranch( ProcessingToken aToken )
    {
      var lRemainingFilters = mFilters.Skip(mFilterIdx).ToList() ;

      if ( lRemainingFilters.Count == 0 )
           return null ;
      else return new ProcessingBranch( aToken, mLevel + 1, lRemainingFilters ) ;
    }

    public BranchResult Process( Processor aProcessor )
    {
      BranchResult rResult = new BranchResult() ;  

      mFilterIdx = 0  ;

      var lTransientToken = mStartToken ;

      int lFoldersCount = 0 ;

      foreach( var lFilter in mFilters )
      {
        DContext.Session.PushFolder(lFilter.Name);
        lFoldersCount++;
        
        var lOutput = lFilter.Apply(lTransientToken);

        if ( lOutput.Count > 0 )
        {
          lTransientToken = lOutput.First() ; 

          if ( lTransientToken != null )
          {
            rResult.Add( lTransientToken ) ;

            if ( lTransientToken.ShouldQuit )
            {
              DContext.WriteLine("Filter asked to Quit Processor.");
              break ;
            }
          }

          for ( int i = 1 ; i < lOutput.Count ; i++ ) 
            aProcessor.EnqueueBranch( NewBranch( lOutput[i] ) ) ; 
        }
        else
        {
          DContext.WriteLine("Filter returned NO result. Quitting.");
          break ;
        }

        mFilterIdx = mFilterIdx + 1  ;
      }

      for( int i = 0 ; i < lFoldersCount ; ++ i )
        DContext.Session.PopFolder();

      return rResult ;  
    }

    public string Name { get ; private set ; }

    public override string ToString() => $"{Name} [L={mLevel} FIdx={mFilterIdx} S={mStartToken.Signal} RFCount={mFilters.Count}]" ;

    ProcessingBranch( ProcessingToken aToken, int aLevel, List<Filter> aFilters )
    { 
      Name = aToken.Name ;  

      mStartToken = aToken ;  
      mLevel      = aLevel ;
      mFilters    = aFilters ;
      mFilterIdx  = 0 ; 
    }

    List<Filter>    mFilters   = new List<Filter>();
    ProcessingToken mStartToken     = null ;
    int             mLevel     = 0 ;
    int             mFilterIdx = 0 ;  

  }

  public class Processor
  {
    public static Result Process ( string aName, ProcessingBranch aMainBranch, Signal aStartSignal )
    {
      var lProcessor = new Processor(aName, aMainBranch);
      return lProcessor.Process( aStartSignal ) ;
    }

    public Processor( string aName, ProcessingBranch aMainBranch )
    {
      mName = aName ;
      mBranches.Enqueue( aMainBranch ) ;  
    }

    public Result Process( Signal aStartSignal )
    {
      Result rR = new Result();

      DContext.Session.PushFolder(aStartSignal.Name);

      try
      {
        var lBaseBranch = mBranches.Peek() ;

        lBaseBranch.Start(aStartSignal) ;

        do
        {
          var lProcessingBranch = mBranches.Peek(); mBranches.Dequeue();

          DContext.Session.PushFolder(lProcessingBranch.Name);

          var lBranchResult = lProcessingBranch.Process(this);

          DContext.Session.PopFolder(); 

          rR.Add(lBranchResult) ; 
        }
        while (mBranches.Count > 0);

        lBaseBranch.End();

        rR.Setup();
      }
      catch( Exception x )
      {
        DContext.Error(x);
      }

      DContext.Session.PopFolder() ;

      return rR ;  
    }

    public void EnqueueBranch ( ProcessingBranch aBranch ) 
    {
      mBranches.Enqueue( aBranch ) ;  
    }

    string mName ;

    Queue<ProcessingBranch> mBranches = new Queue<ProcessingBranch>();  
  }

  public class ProcessorFactory
  {
    public ProcessorFactory()
    {
      //mMap.Add("TapCode", ProcessorFactory.FromAudioToBits_ByTapCode().Then(ProcessorFactory.FromBits()) ) ;
    }

//    public IEnumerable<Processor> EnumProcessors => mMap.Values ;

    //Dictionary<string,Processor> mMap = new Dictionary<string,Processor>();

    public static ProcessingBranch FromAudioToBits_ByPulseDuration()
    {
      var rProcessor = new ProcessingBranch();

      rProcessor.Add( new Envelope() )
                .Add( new Discretize() )
                .Add( new ExtractPulseSymbols() )
                .Add( new BinarizeByDuration() ) ;

      return rProcessor ;
    }

    public static ProcessingBranch FromAudioToBits_ByTapCode()
    {
      var rProcessor = new ProcessingBranch();

      rProcessor.Add( new SplitBands() )
                .Add( new Envelope() )
                .Add( new Discretize(3) )
                .Add( new ExtractPulseSymbols() )
                .Add( new ExtractTapCode() )  
                .Add( new BinarizeFromTapCode() ) ;

      return rProcessor ;
    }

    public static ProcessingBranch FromBits()
    {
      var rProcessor = new ProcessingBranch();

      rProcessor.Add( new BinaryToBytes())
                .Add( new ScoreBytesAsLanguageDigits())
                .Add( new Tokenizer())
                .Add( new ScoreTokenLengthDistribution())
                .Add( new TokensToWords()) 
                .Add( new WordsToText()) ;

      return rProcessor ;
    }

    public static ProcessingBranch FromAudio_ByCode_ToDirectLetters()
    {
      var rProcessor = new ProcessingBranch();

      rProcessor.Add( new Envelope() )
                .Add( new Discretize() )
                .Add( new ExtractPulseSymbols() )
                .Add( new ExtractTapCode() )  
                .Add( new TapCodeToBytes())
                .Add( new ScoreBytesAsLanguageDigits())
                .Add( new Tokenizer())
                .Add( new ScoreTokenLengthDistribution())
                .Add( new TokensToWords()) 
                .Add( new WordsToText()) ;

      return rProcessor ;
    }

  }

  
}
