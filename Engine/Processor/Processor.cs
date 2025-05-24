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
    var lProcessor = new Processor(aName, aMainPipeline);
    return lProcessor.Process( aStartSignal ) ;
  }

  public Processor( string aName, MainPipeline aMainPipeline )
  {
    mName         = aName ;
    mMainPipeline = aMainPipeline ; 
  }

  public Result Process( Signal aStartSignal )
  {
    Result rResult = new Result();

    DContext.Session.PushBucket(Bucket.WithFolder(aStartSignal.Name));

    try
    {
      mMainPipeline.Start(aStartSignal) ;

      mPipelines.Enqueue( mMainPipeline ) ;  

      int lPipelineIdx = 0 ;

      do
      {
        var lPipeline = mPipelines.Peek(); mPipelines.Dequeue();

        DContext.Session.PushBucket(Bucket.WithFolder($"Pipeline_{lPipelineIdx}"));

        var lPipelineResult = lPipeline.Process(this);

        DContext.Session.PopFolder(); 

        rResult.Add(lPipelineResult) ; 

        lPipelineIdx ++ ;
      }
      while (mPipelines.Count > 0);

      mMainPipeline.End();

      rResult.Setup();
    }
    catch( Exception x )
    {
      DContext.Error(x);
    }

    DContext.Session.PopFolder() ;

    return rResult ;  
  }

  public void EnqueuePipeline ( Pipeline aPipeline ) 
  {
    mPipelines.Enqueue( aPipeline ) ;  
  }

  readonly string          mName ;
  readonly MainPipeline    mMainPipeline ;
  readonly Queue<Pipeline> mPipelines = new Queue<Pipeline>();  
}

  
}
