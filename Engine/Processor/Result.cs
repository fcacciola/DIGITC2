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

namespace DIGITC2_ENGINE
{
  public class ResultPath : IWithState  
  {
    public State GetState() 
    {
      State rState = new State("ResultPath");

      rState.Add( State.With("Fitness", Fitness));

      PathBranches.ForEach( s => rState.Add( s.GetState() ) ) ;  
      
      return rState ; 
    }

    public void Setup()
    {
      int lPoorestFitness = (int)Fitness.Undefined ;

      PathBranches.ForEach( s => lPoorestFitness = Math.Min( (int)(s.Score?.Fitness).GetValueOrDefault(Fitness.Undefined), lPoorestFitness) ) ;

      Fitness = (Fitness)lPoorestFitness ;
    }

    public List<Branch> PathBranches = new List<Branch>();

    public Fitness Fitness = Fitness.Undefined;

    public void Save() 
    {
      Reporter lReporter = new Reporter();

      lReporter.Report(this);

      string lReport = lReporter.GetReport();

      string lOutputFile =  DIGITC_Context.Session.ReportFile(this);

      File.WriteAllText( lOutputFile, lReport );
    }

  }

  public class Result : IWithState  
  {
    public Result() {}

    public Step AddFirst( Signal aInput)
    {
      var lFirstBranch = new List<Branch>(){ new Branch(null, aInput,"Start",null,false,null) };
      return Add( new Step(null, lFirstBranch) ) ;
    }

    public Step Add( Step aStep )
    {
      aStep.Idx = Steps.Count;
      Steps.Add( aStep ) ;
      return aStep ;
    }

    public State GetState() 
    {
      State rState = new State("Result");

      Steps.ForEach( s => rState.Add( s.GetState() ) ) ;  
      
      return rState ; 
    }

    public void Setup()
    {
      Step lLastStep = Steps.Last();
      foreach( Branch lBranch in lLastStep.Branches )
      {
        ResultPath lPath = new ResultPath();

        for( Branch lCurr = lBranch; lCurr != null ; lCurr = lCurr.Prev ) 
          lPath.PathBranches.Add( lCurr );

        Paths.Add( lPath ); 
      }
    }

    public List<Step>       Steps = new List<Step>();
    public List<ResultPath> Paths = new List<ResultPath>();

    public void Save() 
    {
      Paths.ForEach( p => p.Save() );
    }
  }

  
}
