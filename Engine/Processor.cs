using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
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
      Result rR = new Result();

      var lContext = aContext ?? new Context();

      var lStep = rR.AddFirst(aInput, lContext) ;

      lContext.Log(lStep.ToString());

      foreach( var lFilter in mFilters )
      { 
        lStep = lFilter.Apply(lStep);

        rR.Add( lStep ) ;

        lContext.Log(lStep.ToString());
      }

      return rR ;  
    }


    List<Filter> mFilters = new List<Filter>();
  }

  
}
