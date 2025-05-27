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
  public Pipeline BranchOut( Bucket aStartBucket, Packet aNewStartPacket )
  {
    var lRemainingFilters = mFilters.Skip(mFilterIdx+1).ToList() ;

    if ( lRemainingFilters.Count == 0 )
         return null ;
    else return new Pipeline( aStartBucket, aNewStartPacket, mLevel + 1, lRemainingFilters ) ;
  }

  public PipelineResult Process( Processor aProcessor )
  {
    PipelineResult rResult = new PipelineResult() ;  

    mFilterIdx = 0  ;

    var lPacket = mStartPacket ;

    foreach( var lFilter in mFilters )
    {
      try
      {
        // ONLY use the Filter name as a bucket Subfolder at the first filter, then use
        // a really short name such as 'N', from 'Next Filter', becuase with a 
        // large filter pipelline, the directory depth is going to exceed Windows limits.
        
        string lBucketSubFolder = mFilterIdx == 0 ? lFilter.Name : "N" ;

        DContext.Session.PushBucket(Bucket.WithLogFile(lFilter.Name, lBucketSubFolder) );

        var lOutput = lFilter.Apply(lPacket);

        if ( lOutput.Count > 0 )
        {
          lPacket = lOutput.First() ; 

          if ( lPacket != null )
          {
            rResult.Add( lPacket ) ;

            if ( lPacket.ShouldQuit )
            {
              DContext.WriteLine("Filter asked to Quit Processor.");
              break ;
            }
          }

          for ( int i = 1 ; i < lOutput.Count ; i++ ) 
            aProcessor.BranchOut( this, lOutput[i] ) ; 
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

    return rResult ;  
  }

  public string Name { get ; private set ; }

  public int Level => mLevel ;

  public Bucket StartBucket { get ; protected set ; } 

  public override string ToString() => $"{Name} [L={mLevel} FIdx={mFilterIdx} S={mStartPacket.Signal} RFCount={mFilters.Count}]" ;

  protected Pipeline( string aName, Bucket aStartBucket )
  { 
    Name = aName;

    StartBucket = aStartBucket ;
  }

  Pipeline( Bucket aStartBucket, Packet aPacket, int aLevel, List<Filter> aFilters )
  { 
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
  public MainPipeline() : base("Main", null )
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
    
  public void Start( Signal aStartSignal, Bucket aStartBucket ) 
  {
    mStartPacket = new Packet(null, aStartSignal, "") ;

    StartBucket = aStartBucket ;

    mFilters.ForEach( filter => filter.Setup() ) ;
  }

  public void End()
  {
    mFilters.ForEach( filter => filter.Cleanup() ) ;
  }

}

  
}
