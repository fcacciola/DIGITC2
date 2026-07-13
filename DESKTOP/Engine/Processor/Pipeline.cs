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

public class OutputSlot
{
  OutputSlot( string aName, string aSubFolder, bool aSetupLogFile )
  {
    Name         = aName;
    SubFolder    = aSubFolder; 
    SetupLogFile = aSetupLogFile;
  }

  public static OutputSlot WithLogFile( string aName, string aSubFolderName = null ) => new OutputSlot( aName, aSubFolderName ?? aName, true );

  public static OutputSlot WithoutLogFile( string aName, string aSubFolderName = null ) => new OutputSlot( aName, aSubFolderName ?? aName, false );

  public string Name         { get ; private set ; }
  public string SubFolder    { get ; private set ; }
  public bool   SetupLogFile { get ; private set ; }

  public string FullOutputFolder { get ; set ; } = null ;

  public override string ToString() => $"{Name} at {FullOutputFolder ?? SubFolder}";

}


public class OutputBucket
{
  public OutputBucket( Session aSession )
  {
    mSession = aSession ;
  }

  public void Add( OutputSlot aSlot)
  {
    if ( aSlot.FullOutputFolder == null )
    {
      string lBaseFolder = CurrSlot()?.FullOutputFolder ?? mSession.RootOutputFolder ;

      aSlot.FullOutputFolder = $"{lBaseFolder}\\{aSlot.SubFolder}";
    }

    mSlots.Add(aSlot); 

    Activate(aSlot);
  }
  
  public OutputSlot CurrSlot()
  {
    return mSlots.LastOrDefault() ;
  }

  public OutputSlot PrevSlot()
  {
    OutputSlot rO = null ;
    if ( mSlots.Count > 1 )
      rO = mSlots[mSlots.Count-2] ;
    return rO ;
  }
    
  public string CurrFullOutputFolder => CurrSlot().FullOutputFolder ;

  public void Activate( OutputSlot aSlot )
  {
    mSession.SetCurrentOutputFolder(aSlot.FullOutputFolder);

    if ( aSlot.SetupLogFile )
      mSession.SetupLogFile(aSlot.Name);

  }

  Session mSession ;
  List<OutputSlot> mSlots = new List<OutputSlot>();
}


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
  public Pipeline BranchOut( OutputSlot aStartSlot, Packet aStartPacket, Config aConfig, string aSubName )
  {
    var lRemainingFilters = mFilters.Skip(mFilterIdx).ToList() ;

    if ( lRemainingFilters.Count == 0 )
         return null ;
    else return new Pipeline( Session, Settings, aConfig, $"{Name}_{aSubName}", aStartSlot, aStartPacket, lRemainingFilters ) ;
  }

  public PipelineResult Process( Processor aProcessor )
  {
    PipelineResultBuilder rRB = new PipelineResultBuilder( Config, Name, Session.CurrentOutputFolder) ;  

    mFilterIdx = 0  ;

    var lPacket = mStartPacket ;
    lPacket.Config = Config ;

    foreach( var lFilter in mFilters )
    {
      Session.PushTimeSection(lFilter.Name);

      try
      {
        // ONLY use the Filter name as a bucket Subfolder at the first filter, then use
        // a really short name such as 'N', from 'Next Filter', becuase with a 
        // large filter pipelline, the directory depth is going to exceed Windows limits.
        
        string lBucketSubFolder = mFilterIdx == 0 ? lFilter.Name : "N" ;

        AddSlot(OutputSlot.WithLogFile(lFilter.Name, lBucketSubFolder) );

        List<Config> lBranches ;
        (lPacket,lBranches) = lFilter.Apply(lPacket);

        if ( lPacket is not null )
        {
          lPacket.OutputFolder = Session.CurrentOutputFolder ;

          rRB.Add( lPacket ) ;

          for( int i = 0 ; i < lBranches.Count ; i++ )
          {
            var lBranch = lBranches[i] ;  
            string lSN = "_" + (char)('A'+i);
            aProcessor.BranchOut( this, lPacket, lBranch, lSN) ;
          }

          if ( lPacket.ShouldQuit && Session.QuitEnabled)
          {
            Session.WriteLine2DriverApp("Filter asked to Quit Processor.");
            break ;
          }
        }
        else
        {
          Session.WriteLine2DriverApp("Filter returned NO result. Quitting.");
          break ;
        }
      }
      catch ( Exception e ) 
      { 
        Session.Error(e);
      }
        
      mFilterIdx = mFilterIdx + 1  ;

      Session.PopTimeSection();
    }

    Session.MarkTime("Pipeline finished");

    return rRB.BuildResult() ;  
  }

  public Session  Session  { get ; set ; }
  public Settings Settings { get ; set ; }
  public Config   Config   { get ; set ; }

  public string Name { get ; private set ; }

  public override string ToString() => $"{Name} FIdx={mFilterIdx} S={mStartPacket.Signal} RFCount={mFilters.Count}]" ;

  protected Pipeline( string aName )
  { 
    Name = aName;
  }

  Pipeline( Session aSession, Settings aSettings, Config aConfig, string aName, OutputSlot aStartSlot, Packet aPacket, List<Filter> aFilters )
  { 
    Session      = aSession ;
    Settings     = aSettings ;
    Config       = aConfig ;
    Name         = aName ;  

    mOutputBucket = new OutputBucket(Session);

    AddSlot(aStartSlot) ;

    mStartPacket = aPacket ;  
    mFilters     = aFilters ;
    mFilterIdx   = 0 ; 
  }

  public void SetupFilters( ) 
  {
    mFilters.ForEach( filter => filter.Setup( Session, Settings, Config) ) ;

    Session.MarkTime("Filters Setup");
  }

  public void AddSlot( OutputSlot aSlot )
  {
    if ( mOutputBucket != null && aSlot != null )
      mOutputBucket.Add(aSlot);
  }

  public OutputBucket OutputBucket => mOutputBucket ;

  protected List<Filter>  mFilters     = new List<Filter>();
  protected Packet        mStartPacket = null ;
  protected OutputBucket  mOutputBucket ;

  int mFilterIdx = 0 ;  

}

public class MainPipeline : Pipeline
{
  public MainPipeline() : base("Main" )
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
    
  public void Start( Session aSession, Settings aSettings, Config aConfig, Signal aStartSignal ) 
  {
    Session  = aSession ;
    Settings = aSettings ;  
    Config   = aConfig ;

    mStartPacket = new Packet(null, null, null, aStartSignal, "") ;

    mOutputBucket = new OutputBucket(Session);
    AddSlot( OutputSlot.WithoutLogFile(aSession.Name) );
  }

  public void End()
  {
    mFilters.ForEach( filter => filter.Cleanup() ) ;
  }

}

  
}
