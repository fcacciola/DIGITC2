using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public class Context
  {
    public float WindowSizeInSeconds { get ; set ; } = 0f ;

    public RenderOptions RenderOptions { get ; set ; }  = new RenderOptions() ;
    
    public Renderer Renderer { get ; set ; } = new TextRenderer();

    public void Throw ( Exception e )
    {
      throw e ;
    }
  }

  public class Result
  {
    public class Pipe
    {
      internal Pipe() {}

      public List<Signal> Steps = new List<Signal>();
    }

    public Result( Signal aInput ) { Input = aInput ; } 

    public Signal Input  { get ; private  set ; }
    public Signal Output { get ; internal set ; }   

    public List<Pipe> Pipes = new List<Pipe>();
  }

  public class Processor
  {
    public Processor()
    { 
    }

    public Processor Add( Filter aFilter ) 
    {
      string lID = $"[{mFilters.Count}]" ;
      aFilter.ID = lID ;
      mFilters.Add( aFilter ) ;
      return this ;
    }

    public Processor AddParallel( params Filter[] aFilters) 
    {
      string lID = $"[{mFilters.Count}]" ;

      foreach( var lTask in aFilters )
      {
        string lSID = $"[{mFilters.Count}/{aFilters.Length}]" ;
        lTask.ID = lSID ;  
      }

      ParallelFilter lParallelFilter = new ParallelFilter(aFilters);
      lParallelFilter.ID = lID ;  
      mFilters.Add( lParallelFilter ) ;

      return this;
    }

    public Result Process( Source aSource, Context aContext = null )
    {
      Signal lInput = aSource.CreateSignal();
      lInput.StepIdx = 0 ;

      aContext?.Renderer.Render( lInput, aContext?.RenderOptions, "Input Signal" ); 

      mResult = new Result(lInput) ;

      var lSlices = aSource.Slice( lInput, aContext ) ;

      List<Result.Pipe> lPipes = new List<Result.Pipe>() ;

      foreach( var lSlice in lSlices )
      {
        Result.Pipe lPipe = new Result.Pipe();
        lPipes.Add( lPipe ) ;

        Process( lSlice, lPipe, aContext ) ;
      }

      List<Signal> lPipeOutputs = new List<Signal> () ;
      foreach( var lPipe in lPipes )  
        lPipeOutputs.Add(lPipe.Steps.Last());

      mResult.Output = aSource.Merge( lPipeOutputs, aContext ) ;
      mResult.Output.StepIdx = mFilters.Count ;

      aContext?.Renderer.Render( mResult.Output, aContext?.RenderOptions, "Output Signal"  ); 

      return mResult ;  
    }

    void Process( Signal aSignal, Result.Pipe aPipe, Context aContext )
    {
      var rSignal = aSignal ;
      var lContext = aContext ?? new Context();

      aPipe.Steps.Add( rSignal ) ;

      foreach( var lFilter in mFilters )
      { 
        rSignal = lFilter.Apply(rSignal, lContext);

        rSignal.StepIdx = aPipe.Steps.Count ;
        aPipe.Steps.Add( rSignal ) ;

        aContext?.Renderer.Render( rSignal, aContext?.RenderOptions, $"Step[{mFilters.IndexOf(lFilter)}] Signal" ); 
      }
    }

    public void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    {
      mFilters.ForEach( n => n.Render(aRenderer, aOptions) ) ;
    }

    List<Filter> mFilters = new List<Filter>();

    Result mResult ;
  }

  
}
