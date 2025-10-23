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

public class PipelineSelection
{
  public PipelineSelection( string aActivePipelines )
  {
    if ( ! string.IsNullOrEmpty(aActivePipelines) )
    {
      foreach( string lActivePipeline in aActivePipelines.Split(',') )  
        if ( !lActivePipeline.StartsWith("!") )
          mActive.Add( lActivePipeline ); 
    }
  }

  public bool IsActive( string aPipeline )
  {
    if ( mActive.Count > 0 )
    {
      return ( mActive.Find( s => s == aPipeline ) != null ) ;
    }
    else return true ;
  }

  List<string> mActive = new List<string>();
}

public class Pipeline
{
  public Pipeline BranchOut( OutputBucket aStartBucket, Packet aStartPacket, Config aConfig )
  {
    var lRemainingFilters = mFilters.Skip(mFilterIdx+1).ToList() ;

    if ( lRemainingFilters.Count == 0 )
         return null ;
    else return new Pipeline( Session, Settings, aConfig, aStartBucket, aStartPacket, mLevel + 1, lRemainingFilters ) ;
  }

  public PipelineResultBuilder Process( Processor aProcessor )
  {
    PipelineResultBuilder rRB = new PipelineResultBuilder(Name) ;  

    mFilterIdx = 0  ;

    var lPacket = mStartPacket ;
    lPacket.Config = Config ;

    foreach( var lFilter in mFilters )
    {
      try
      {
        // ONLY use the Filter name as a bucket Subfolder at the first filter, then use
        // a really short name such as 'N', from 'Next Filter', becuase with a 
        // large filter pipelline, the directory depth is going to exceed Windows limits.
        
        string lBucketSubFolder = mFilterIdx == 0 ? lFilter.Name : "N" ;

        DContext.Session.PushBucket(OutputBucket.WithLogFile(lFilter.Name, lBucketSubFolder) );

        List<Config> lBranches ;
        (lPacket,lBranches) = lFilter.Apply(lPacket);

        if ( lPacket is not null )
        {
          lPacket.OutputFolder = DContext.Session.CurrentOutputFolder ;

          rRB.Add( lPacket ) ;

          if ( lPacket.ShouldQuit )
          {
            DContext.WriteLine("Filter asked to Quit Processor.");
            break ;
          }

          lBranches.ForEach( b => aProcessor.BranchOut( this, lPacket, b) ) ; 
        }
        else
        {
          DContext.WriteLine("Filter returned NO result. Quitting.");
          break ;
        }
      }
      catch ( Exception e ) 
      { 
        DContext.Error(e);
      }
        
      mFilterIdx = mFilterIdx + 1  ;
    }

    return rRB ;  
  }

  public Session  Session  { get ; set ; }
  public Settings Settings { get ; set ; }
  public Config   Config   { get ; set ; }

  public string Name { get ; private set ; }

  public int Level => mLevel ;

  public OutputBucket StartBucket { get ; protected set ; } 

  public override string ToString() => $"{Name} [L={mLevel} FIdx={mFilterIdx} S={mStartPacket.Signal} RFCount={mFilters.Count}]" ;

  protected Pipeline( Session aSession, Settings aSettings, Config aConfig, string aName, OutputBucket aStartBucket )
  { 
    Session     = aSession ;
    Settings    = aSettings ;
    Config      = aConfig ;
    Name        = aName;
    StartBucket = aStartBucket ;
  }

  Pipeline( Session aSession, Settings aSettings, Config aConfig, OutputBucket aStartBucket, Packet aPacket, int aLevel, List<Filter> aFilters )
  { 
    Session      = aSession ;
    Settings     = aSettings ;
    Config       = aConfig ;
    Name         = aPacket.Name ;  
    StartBucket  = aStartBucket ;

    mStartPacket = aPacket ;  
    mLevel       = aLevel ;
    mFilters     = aFilters ;
    mFilterIdx   = 0 ; 
  }


  protected List<Filter> mFilters     = new List<Filter>();
  protected Packet       mStartPacket = null ;

  int mLevel    = 0 ;
  int mFilterIdx = 0 ;  

}

public class MainPipeline : Pipeline
{
  public MainPipeline() : base(null, null, null, "Main", null )
  { 
  }

  public MainPipeline Add( Filter aFilter ) 
  {
    mFilters.Add( aFilter ) ;
    return this ;
  }

  public MainPipeline Then ( MainPipeline aNext )
  {
    aNext.mFilters.ForEach( f => Add( f ) ) ;

    return this ;
  } 
    
  public void Start( Session aSession, Settings aSettings, Config aConfig, Signal aStartSignal, OutputBucket aStartBucket ) 
  {
    Session  = aSession ;
    Settings = aSettings ;  
    Config   = aConfig ;

    mStartPacket = new Packet(null, null, null, aStartSignal, "") ;

    StartBucket = aStartBucket ;

    mFilters.ForEach( filter => filter.Setup( Session, Settings, Config) ) ;
  }

  public void End()
  {
    mFilters.ForEach( filter => filter.Cleanup() ) ;
  }

}

  
}
