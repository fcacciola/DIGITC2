using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2
{
  public class Step : IWithState
  {
    public Step( Signal aSignal, string aLabel, Filter aFilter, IWithState aData, bool aNoNewSignal, Context aContext )
    {
      Signal      = aSignal;
      Label       = aLabel ;
      Context     = aContext;
      Filter      = aFilter; 
      Data        = aData;
      NoNewSignal = aNoNewSignal ;
    }

    public Plot CreatePlot( Plot.Options aOptions ) 
    {
      return Signal.CreatePlot(aOptions);
    }

    public T GetData<T>() where T : class => Data as T ;

    public Step Next( Signal aSignal, string aLabel, Filter aFilter, IWithState aData = null, bool aNoNewSignal = false )
    {
      aSignal.Name = aLabel ;
      return new Step(aSignal, aLabel, aFilter, aData, aNoNewSignal, Context);
    }

    public State GetState()
    {
      State rS = new State($"STEP {StepIdx}") ;
      if ( Filter != null ) 
        rS.Add( Filter.GetState() );  

      if ( ! NoNewSignal )
        rS.Add( Signal.GetState() );  

      if ( Data != null ) 
        rS.Add( Data.GetState() );  

      return rS ;
    }

    public Signal     Signal  ;
    public string     Label   ;
    public Filter     Filter  ;
    public IWithState Data    ;
    public bool       NoNewSignal ;
    public Context    Context ;

    public int       StepIdx ;
  }

  public class Result : IWithState  
  {
    public Result() {}

    public Step AddFirst( Signal aInput, Context aContext)
    {
      return Add( new Step(aInput, "Start", null, null, false, aContext) ) ;
    }

    public Step Add( Step aStep )
    {
      aStep.StepIdx = Steps.Count;
      Steps.Add( aStep ) ;
      return aStep ;
    }

    public State GetState() => State.From(null, Steps, false );

    public List<Step> Steps = new List<Step>();
  }

  
}
