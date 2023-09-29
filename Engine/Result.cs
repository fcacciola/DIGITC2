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
    public Step( Signal aSignal, string aLabel, Filter aFilter, IWithState aData, bool aNoNewSignal, Score aScore )
    {
      Signal      = aSignal;
      Label       = aLabel ;
      Filter      = aFilter; 
      Data        = aData;
      NoNewSignal = aNoNewSignal ;
      Score       = aScore;
    }

    public T GetData<T>() where T : class => Data as T ;

    public Step Next( Signal aSignal, string aLabel, Filter aFilter, IWithState aData = null, bool aNoNewSignal = false, Score aScore = null )
    {
      aSignal.Name = aLabel ;
      return new Step(aSignal, aLabel, aFilter, aData, aNoNewSignal, aScore);
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

      if ( Score != null ) 
        rS.Add( Score.GetState() );  

      return rS ;
    }

    public Signal     Signal  ;
    public string     Label   ;
    public Filter     Filter  ;
    public IWithState Data    ;
    public bool       NoNewSignal ;
    public Score      Score ;

    public int       StepIdx ;
  }

  public class Result : IWithState  
  {
    public Result() {}

    public Step AddFirst( Signal aInput)
    {
      return Add( new Step(aInput, "Start", null, null, false, null) ) ;
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
