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
  public class BranchResult  
  {
    public void Add( ProcessingToken aToken )
    {
      Tokens.Add( aToken ); 
    }

    public void Setup()
    {
      //int lPoorestFitness = (int)Fitness.Undefined ;

      //PathBranches.ForEach( s => lPoorestFitness = Math.Min( (int)(s.Score?.Fitness).GetValueOrDefault(Fitness.Undefined), lPoorestFitness) ) ;

      //Fitness = (Fitness)lPoorestFitness ;
    }


    public Fitness Fitness = Fitness.Undefined;

    public string Report() 
    {
      //Reporter lReporter = new Reporter();

      //lReporter.Report(this);

      //string lReport = lReporter.GetReport();

//      string rOutputFile = DContext.Session.ReportFile(this);

      //File.WriteAllText( rOutputFile, lReport );

      //return rOutputFile;

      return "" ;
    }

    public List<ProcessingToken> Tokens = new List<ProcessingToken>();
  }

  public class Result 
  {
    public Result() {}

    public void Add( BranchResult aBranchResult )
    {
      mBranchResults.Add( aBranchResult );
    }

    public void Setup()
    {
      mBranchResults.ForEach( r => r.Setup() );
    }

    public List<string> Report() 
    {
      return mBranchResults.ConvertAll( p => p.Report() );
    }

    public void Save()
    {

    }

    List<BranchResult> mBranchResults = new List<BranchResult>(); 
  }

  
}
