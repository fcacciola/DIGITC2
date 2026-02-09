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

public class Processor
{
  public static SessionResult Process ( Session aSession, Settings aSettings, string aName, MainPipeline aMainPipeline, Config aConfig, Signal aStartSignal )
  {
    var lProcessor = new Processor(aMainPipeline);
    var lPipelineResults = lProcessor.Process( aSession, aSettings, aStartSignal, aConfig  ) ;

    return new SessionResult(lPipelineResults,aName);
  }

  public Processor( MainPipeline aMainPipeline )
  {
    mMainPipeline = aMainPipeline ; 
  }

  public List<PipelineResult> Process( Session aSession, Settings aSettings, Signal aStartSignal, Config aConfig )
  {
    var lStartBucket = OutputBucket.WithoutLogFile(aStartSignal.Name);
    DContext.Session.PushBucket(lStartBucket);
    var rPipelineResults = new List<PipelineResult>();
    Process( aSession, aSettings, aStartSignal, aConfig, lStartBucket, rPipelineResults);
    DContext.Session.GotoBucket(lStartBucket) ;
    DContext.CloseLogger();
    return rPipelineResults ;
  }

  public void Process( Session aSession, Settings aSettings, Signal aStartSignal, Config aConfig, OutputBucket aStartBucket, List<PipelineResult> rPipelineResults )
  {

    try
    {
      mMainPipeline.Start( aSession, aSettings, aConfig, aStartSignal, aStartBucket ) ;

      mPipelines.Enqueue( mMainPipeline ) ;  

      int lPipelineIdx = 0 ;

      do
      {
        var lPipeline = mPipelines.Peek(); mPipelines.Dequeue();

        lPipeline.SetupFilters();

        aSession.GotoBucket(lPipeline.StartBucket);
        aSession.PushBucket(OutputBucket.WithoutLogFile(lPipeline.Name));
        string lPipelineFolder = aSession.CurrBucket().FullOutputFolder; 
        aSession.CurrentPipelineFolder = lPipelineFolder;
        var lPipelineResult = lPipeline.Process(this);
        if ( lPipelineResult != null )
        {
          lPipelineResult.Folder = lPipelineFolder; 
          rPipelineResults.Add(lPipelineResult) ; 

          if ( lPipelineResult.OverallFitness == Fitness.PERFECT )
          {
            DContext.WriteLine2GUI($"Pipeline {lPipelineIdx} finished with fitness {lPipelineResult.OverallFitness}. Skipping branch-out pipelines.") ;
            break ;
          }
        }
        else
        {
          DContext.WriteLine2GUI($"Pipeline {lPipelineIdx} failed. Aborting.") ;
          break ;
        }

        lPipelineIdx ++ ;
      }
      while (mPipelines.Count > 0);

      mMainPipeline.End();
    }
    catch( Exception x )
    {
      DContext.Error(x);
    }

  }

  public void EnqueuePipeline ( Pipeline aPipeline ) 
  {
    mPipelines.Enqueue( aPipeline ) ;  
  }

  public void BranchOut ( Pipeline aPipeline, Packet aStartPacket, Config aConfig, string aSubName )
  {
    var lPrevFilterBucket = DContext.Session.PrevBucket();   

    if (  lPrevFilterBucket != null )
    { 
      var lNewPipeline = aPipeline.BranchOut(lPrevFilterBucket, aStartPacket.Prev, aConfig, $"{aStartPacket.FilterName}_{aSubName}" ) ;

      EnqueuePipeline( lNewPipeline ) ; 
    }
  }

  readonly MainPipeline    mMainPipeline ;
  readonly Queue<Pipeline> mPipelines = new Queue<Pipeline>();  
}

  
}
