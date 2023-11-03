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
    public Step( Signal aSignal, string aLabel, Filter aFilter, Score aScore, IWithState aData )
    {
      Signal = aSignal;
      Label  = aLabel ;
      Filter = aFilter; 
      Score  = aScore;
      Data   = aData;
    }

    public T GetData<T>() where T : class => Data as T ;

    public Step Next( string aLabel, Filter aFilter, Score aScore = null, IWithState aData = null )
    {
      return new Step(Signal, aLabel, aFilter, aScore, aData);
    }

    public Step Next( Signal aSignal, string aLabel, Filter aFilter, IWithState aData = null )
    {
      return new Step(aSignal, aLabel, aFilter, null, aData);
    }

    public State GetState()
    {
      State rS = new State("Step",$"{StepIdx}") ;

      if ( Filter != null ) 
        rS.Add( Filter.GetState() );  

      if ( Signal != null )
        rS.Add( Signal.GetState() );  

      if ( Score != null ) 
        rS.Add( Score.GetState() );  

      if ( Data != null ) 
        rS.Add( Data.GetState() );  

      return rS ;
    }

    public Signal     Signal ;
    public string     Label  ;
    public Filter     Filter ;
    public Score      Score  ;
    public IWithState Data   ;

    public int       StepIdx ;
  }
 
}
