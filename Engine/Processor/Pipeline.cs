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
  public PipelineSelection( string aActiveBranches )
  {
    if ( ! string.IsNullOrEmpty(aActiveBranches) )
    {
      foreach( string lActiveBranch in aActiveBranches.Split(',') )  
        if ( !lActiveBranch.StartsWith("!") )
          mActive.Add( lActiveBranch ); 
    }
  }

  public bool IsActive( string aBranch )
  {
    if ( mActive.Count > 0 )
    {
      return ( mActive.Find( s => s == aBranch ) != null ) ;
    }
    else return true ;
  }

  List<string> mActive = new List<string>();
}

public class Pipeline
{
    
  public Pipeline BranchOut( Packet aNewStartPacket )
  {
    var lRemainingFilters = mFilters.Skip(mFilterIdx).ToList() ;

    if ( lRemainingFilters.Count == 0 )
          return null ;
    else return new Pipeline( aNewStartPacket, mLevel + 1, lRemainingFilters ) ;
  }

  public PipelineResult Process( Processor aProcessor )
  {
    PipelineResult rResult = new PipelineResult() ;  

    mFilterIdx = 0  ;

    var lPacket = mStartPacket ;

    int lFoldersCount = 0 ;

    foreach( var lFilter in mFilters )
    {
      DContext.Session.PushFolder(lFilter.Name);
      lFoldersCount++;
        
      try
      {
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
            aProcessor.EnqueuePipeline( BranchOut( lOutput[i] ) ) ; 
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

    for( int i = 0 ; i < lFoldersCount ; ++ i )
      DContext.Session.PopFolder();

    return rResult ;  
  }

  public string Name { get ; private set ; }

  public override string ToString() => $"{Name} [L={mLevel} FIdx={mFilterIdx} S={mStartPacket.Signal} RFCount={mFilters.Count}]" ;

  protected Pipeline( string aName )
  { 
    Name = aName;
  }

  Pipeline( Packet aPacket, int aLevel, List<Filter> aFilters )
  { 
    Name = aPacket.Name ;  

    mStartPacket = aPacket ;  
    mLevel       = aLevel ;
    mFilters     = aFilters ;
    mFilterIdx   = 0 ; 
  }

  protected List<Filter> mFilters     = new List<Filter>();
  protected Packet       mStartPacket = null ;

  int          mLevel       = 0 ;
  int          mFilterIdx   = 0 ;  

}

public class MainPipeline : Pipeline
{
  public MainPipeline() : base("Main")
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
    
  public void Start( Signal aStartSignal ) 
  {
    mStartPacket = new Packet(null, aStartSignal, "") ;

    mFilters.ForEach( filter => filter.Setup() ) ;
  }

  public void End()
  {
    mFilters.ForEach( filter => filter.Cleanup() ) ;
  }

}

  
}
