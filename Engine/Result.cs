using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2
{
  public class Step
  {
    public Step( Signal aSignal, string aLabel, Filter aFilter, object aData, Context aContext )
    {
      Signal  = aSignal;
      Label   = aLabel ;
      Context = aContext;
      Filter  = aFilter; 
      Data    = aData;
    }

    public Plot CreatePlot( Plot.Options aOptions ) 
    {
      return Signal.CreatePlot(aOptions);
    }

    public T GetData<T>() where T : class => Data as T ;

    public override string ToString() => $"({StepIdx}|{Label}):{Filter}->{Signal}";

    public Step Next( Signal aSignal, string aLabel, Filter aFilter, object aData = null )
    {
      return new Step(aSignal, aLabel, aFilter, aData, Context);
    }

    public Signal  Signal  ;
    public string  Label   ;
    public Filter  Filter  ;
    public object  Data    ;
    public Context Context ;

    public int     StepIdx ;
  }

  public class Result
  {
    public Result() {}

    public Step AddFirst( Signal aInput, Context aContext)
    {
      return Add( new Step(aInput, "Start", null, null, aContext) ) ;
    }

    public Step Add( Step aStep )
    {
      aStep.StepIdx = Steps.Count;
      Steps.Add( aStep ) ;
      return aStep ;
    }

    public List<Step> Steps = new List<Step>();
  }

  
}
