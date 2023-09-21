using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public class Context
  {
    public float WindowSizeInSeconds { get ; set ; } = 0f ;

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
    public Result( Context aContext, IEnumerable<Filter> aFilters ) { Context = aContext ; Filters.AddRange(aFilters) ; }

    public Context      Context { get ; private set ; }
    public List<Filter> Filters = new List<Filter>();
    public List<Signal> Steps   = new List<Signal>();
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

    public Result Process( Signal aInput, Context aContext = null )
    {
      aInput.StepIdx = 0 ;

      Result rResult = new Result(aContext, mFilters) ;

      var rSignal  = aInput ;
      var lContext = aContext ?? new Context();

      lContext.Log(aInput.ToString());

      rResult.Steps.Add( rSignal ) ;

      foreach( var lFilter in mFilters )
      { 
        lContext.Log(lFilter.ToString());
        rSignal = lFilter.Apply(rSignal, lContext);

        rSignal.StepIdx = rResult.Steps.Count ;
        rResult.Steps.Add( rSignal ) ;

        lContext.Log(rSignal.ToString());
      }

      return rResult ;  
    }


    List<Filter> mFilters = new List<Filter>();
  }

  
}
