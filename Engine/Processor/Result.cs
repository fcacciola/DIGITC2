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
  public class Result : IWithState  
  {
    public Result() {}

    public Step AddFirst( Signal aInput)
    {
      return Add( new Step(aInput, "Start", null, null, null) ) ;
    }

    public Step Add( Step aStep )
    {
      aStep.StepIdx = Steps.Count;
      Steps.Add( aStep ) ;
      return aStep ;
    }

    public State GetState() => State.From("Result",null, Steps );

    public List<Step> Steps = new List<Step>();
  }

  
}
