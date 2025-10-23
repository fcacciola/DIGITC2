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
  public static Result Process ( Session aSession, Settings aSettings, string aName, MainPipeline aMainPipeline, List<Config> aConfigs, Signal aStartSignal )
  {
    var lProcessor = new Processor(aMainPipeline);
    var lResultBuilder = lProcessor.Process( aSession, aSettings, aStartSignal, aConfigs ) ;
    return lResultBuilder.BuildResult(aName);
  }

  public Processor( MainPipeline aMainPipeline )
  {
    mMainPipeline = aMainPipeline ; 
  }

  public ResultBuilder Process( Session aSession, Settings aSettings, Signal aStartSignal, List<Config> aConfigs )
  {
    var lStartBucket = OutputBucket.WithoutLogFile(aStartSignal.Name);
    DContext.Session.PushBucket(lStartBucket);
    ResultBuilder rResult = new ResultBuilder();
    aConfigs.ForEach( lConfig => Process( aSession, aSettings, aStartSignal, lConfig, lStartBucket, rResult) ) ;
    DContext.Session.GotoBucket(lStartBucket) ;
    DContext.CloseLogger();
    return rResult ;
  }

  public void Process( Session aSession, Settings aSettings, Signal aStartSignal, Config aConfig, OutputBucket aStartBucket, ResultBuilder rResult )
  {

    try
    {
      mMainPipeline.Start( aSession, aSettings, aConfig, aStartSignal, aStartBucket ) ;

      mPipelines.Enqueue( mMainPipeline ) ;  

      int lPipelineIdx = 0 ;

      do
      {
        var lPipeline = mPipelines.Peek(); mPipelines.Dequeue();

        aSession.GotoBucket(lPipeline.StartBucket);
        aSession.PushBucket(OutputBucket.WithoutLogFile($"Pipeline_{lPipeline.Level}"));

        var lPipelineResult = lPipeline.Process(this);

        rResult.Add(lPipelineResult) ; 

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

  public void BranchOut ( Pipeline aPipeline, Packet aStartPacket, Config aConfig )
  {
    var lCurrFilterBucket = DContext.Session.CurrentBucket();   

    var lNewPipeline = aPipeline.BranchOut(lCurrFilterBucket, aStartPacket, aConfig ) ;

    EnqueuePipeline( lNewPipeline ) ; 

  }

  readonly MainPipeline    mMainPipeline ;
  readonly Queue<Pipeline> mPipelines = new Queue<Pipeline>();  
}

  
}
