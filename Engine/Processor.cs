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
      mFilters.Add( aFilter ) ;
      return this ;
    }

    public Result Process( Signal aInput )
    {
      Result rR = new Result();

      var lStep = rR.AddFirst(aInput) ;

      Context.Watch(lStep) ; 

      foreach( var lFilter in mFilters )
      { 
        lStep = lFilter.Apply(lStep);

        rR.Add( lStep ) ;

        Context.Watch(lStep) ; 
      }

      return rR ;  
    }


    List<Filter> mFilters = new List<Filter>();
  }

  
}
