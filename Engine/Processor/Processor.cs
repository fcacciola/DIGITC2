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
    var rPipelineResults = new List<PipelineResult>();

    mMainPipeline.Start( aSession, aSettings, aConfig, aStartSignal ) ;

    string lMainPipelineFolder = mMainPipeline.OutputBucket.CurrFullOutputFolder; 

    const int MAX_PIPELINES = 10 ;

    try
    {

      mPipelines.Enqueue( mMainPipeline ) ;  

      int lPipelineIdx = 0 ;

      do
      {
        var lPipeline = mPipelines.Peek(); mPipelines.Dequeue();

        lPipeline.SetupFilters();

        lPipeline.AddSlot(OutputSlot.WithoutLogFile(lPipeline.Name));
        string lPipelineFolder = lPipeline.OutputBucket.CurrFullOutputFolder; 
        aSession.CurrentPipelineFolder = lPipelineFolder;
        var lPipelineResult = lPipeline.Process(this);
        if ( lPipelineResult != null )
        {
          lPipelineResult.Folder = lPipelineFolder; 
          rPipelineResults.Add(lPipelineResult) ; 

          //if ( lPipelineResult.OverallFitness == Fitness.PERFECT )
          {
            DContext.WriteLine2GUI($"Pipeline {lPipelineIdx} finished with fitness {lPipelineResult.OverallFitness}. Skipping branch-out pipelines.") ;
            break ;
          }
        }
        else
        {
          DContext.Error($"Pipeline {lPipelineIdx} failed. Aborting.") ;
          break ;
        }

        lPipelineIdx ++ ;

        if ( lPipelineIdx > MAX_PIPELINES )
        {
          DContext.Error($"Too many Pipelines. Aborting.") ;
          break ;
        }
      }
      while (mPipelines.Count > 0);

      mMainPipeline.End();
    }
    catch( Exception x )
    {
      DContext.Error(x);
    }

    aSession.SetCurrentOutputFolder(lMainPipelineFolder);
    DContext.CloseLogger();

    return rPipelineResults ;
  }

  public void EnqueuePipeline ( Pipeline aPipeline ) 
  {
    mPipelines.Enqueue( aPipeline ) ;  
  }

  public void BranchOut ( Pipeline aPipeline, Packet aStartPacket, Config aConfig, string aSubName )
  {
    var lPrevFilterSlot = aPipeline.OutputBucket.PrevSlot();   

    if (  lPrevFilterSlot != null )
    { 
      var lNewPipeline = aPipeline.BranchOut(lPrevFilterSlot, aStartPacket.Prev, aConfig, $"{aStartPacket.FilterName}_{aSubName}" ) ;

      EnqueuePipeline( lNewPipeline ) ; 
    }
  }

  readonly MainPipeline    mMainPipeline ;
  readonly Queue<Pipeline> mPipelines = new Queue<Pipeline>();  
}

  
}
