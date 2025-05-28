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
  public static Result Process ( string aName, MainPipeline aMainPipeline, Signal aStartSignal )
  {
    var lProcessor = new Processor(aMainPipeline);
    var lResultBuilder = lProcessor.Process( aStartSignal ) ;
    return lResultBuilder.BuildResult(aName);
  }

  public Processor( MainPipeline aMainPipeline )
  {
    mMainPipeline = aMainPipeline ; 
  }

  public ResultBuilder Process( Signal aStartSignal )
  {
    ResultBuilder rResult = new ResultBuilder();

    var lStartBucket = OutputBucket.WithoutLogFile(aStartSignal.Name);
    DContext.Session.PushBucket(lStartBucket);

    try
    {
      mMainPipeline.Start(aStartSignal, lStartBucket ) ;

      mPipelines.Enqueue( mMainPipeline ) ;  

      int lPipelineIdx = 0 ;

      do
      {
        var lPipeline = mPipelines.Peek(); mPipelines.Dequeue();

        DContext.Session.GotoBucket(lPipeline.StartBucket);
        DContext.Session.PushBucket(OutputBucket.WithoutLogFile($"Pipeline_{lPipeline.Level}"));

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

    DContext.Session.GotoBucket(lStartBucket) ;
    DContext.CloseLogger();

    return rResult ;  
  }

  public void EnqueuePipeline ( Pipeline aPipeline ) 
  {
    mPipelines.Enqueue( aPipeline ) ;  
  }

  public void BranchOut ( Pipeline aPipeline, Packet aNewStartPacket )
  {
    var lCurrFilterBucket = DContext.Session.CurrentBucket();   

    var lNewPipeline = aPipeline.BranchOut(lCurrFilterBucket, aNewStartPacket ) ;

    EnqueuePipeline( lNewPipeline ) ; 

  }

  readonly MainPipeline    mMainPipeline ;
  readonly Queue<Pipeline> mPipelines = new Queue<Pipeline>();  
}

  
}
