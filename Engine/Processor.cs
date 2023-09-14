using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public void Log( string aText )
    {
      Trace.WriteLine( aText ); 
    }

    public void Error( string aText )
    {
      Trace.WriteLine( "ERROR: " + aText ); 
    }
  }

  public class Result
  {
    public class Slice
    {
      internal Slice() {}

      public List<Signal> Steps = new List<Signal>();
    }

    public Result( Signal aInput ) { Input = aInput ; } 

    public Signal Input  { get ; private  set ; }
    public Signal Output { get ; internal set ; }   

    public List<Slice> Slices = new List<Slice>();
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

    public Result Process( Source aSource, Context aContext = null )
    {
      Signal lInput = aSource.CreateSignal();
      lInput.StepIdx = 0 ;

      aContext?.Renderer.Render( lInput, aContext?.RenderOptions, "Input" ); 

      mResult = new Result(lInput) ;

      var lSlices = lInput.Slice( aContext ) ;

      List<Result.Slice> lPipes = new List<Result.Slice>() ;

      foreach( var lSlice in lSlices )
      {
        Result.Slice lPipe = new Result.Slice();
        lPipes.Add( lPipe ) ;

        Process( lSlice, lPipe, aContext ) ;
      }


      List<Signal> lPipeOutputs = new List<Signal> () ;
      foreach( var lPipe in lPipes )  
        lPipeOutputs.Add(lPipe.Steps.Last());

      mResult.Output = lPipeOutputs.First().MergeWith( lPipeOutputs.Skip(1), aContext ) ;
      mResult.Output.StepIdx = mFilters.Count ;

      aContext?.Renderer.Render( mResult.Output, aContext?.RenderOptions, "Output"  ); 

      return mResult ;  
    }

    void Process( Signal aSignal, Result.Slice aPipe, Context aContext )
    {
      var rSignal = aSignal ;
      var lContext = aContext ?? new Context();

      aPipe.Steps.Add( rSignal ) ;

      foreach( var lFilter in mFilters )
      { 
        rSignal = lFilter.Apply(rSignal, lContext);

        rSignal.StepIdx = aPipe.Steps.Count ;
        aPipe.Steps.Add( rSignal ) ;

        if ( mFilters.IndexOf( lFilter ) < mFilters.Count - 1)
          aContext?.Renderer.Render( rSignal, aContext?.RenderOptions, $"Step[{mFilters.IndexOf(lFilter)}] Result" ); 
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
