using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
      return Add( new Step(aInput, "Start", null, null, false, null) ) ;
    }

    public Step Add( Step aStep )
    {
      aStep.StepIdx = Steps.Count;
      Steps.Add( aStep ) ;
      return aStep ;
    }

    public State GetState() 
    {
      State rState = new State("Result");

      rState.Add( State.With("Fitness", Fitness));

      Steps.ForEach( s => rState.Add( s.GetState() ) ) ;  
      
      return rState ; 
    }

    public void Setup()
    {
      int lPoorestFitness = (int)Fitness.Undefined ;

      Steps.ForEach( s => lPoorestFitness = Math.Min( (int)(s.Score?.Fitness).GetValueOrDefault(Fitness.Undefined), lPoorestFitness) ) ;

      Fitness = (Fitness)lPoorestFitness ;
    }

    public List<Step> Steps = new List<Step>();

    public Fitness Fitness = Fitness.Undefined;

    public void Save() 
    {
      Reporter lReporter = new Reporter();

      lReporter.Report(this);

      string lReport = lReporter.GetReport();

      string lOutputFile =  Context.Session.ReportFile(this);

      File.WriteAllText( lOutputFile, lReport );
    }


  }

  
}
