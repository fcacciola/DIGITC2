using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public enum Fitness { Discarded = 0, Poor = 1, Good = 2, Excelent = 3, Perfect= 4, Undefined=100, }

  public class FitnessMap
  {
    public FitnessMap( string aThresholds )
    {
      if ( ! string.IsNullOrEmpty(aThresholds))
      {
        string[] lTS = aThresholds.Split(',');
        if ( lTS.Length == 4 )
        {
          int.TryParse( lTS[0], out PoorFitThreshold);
          int.TryParse( lTS[1], out GoodFitThreshold);
          int.TryParse( lTS[2], out ExcelentFitThreshold);
          int.TryParse( lTS[3], out PerfectFitThreshold);
        }
      }
    }

    public Fitness Map( int aLikelihood )
    {
      return  aLikelihood > PerfectFitThreshold ? Fitness.Perfect
            : aLikelihood > ExcelentFitThreshold? Fitness.Excelent
            : aLikelihood > GoodFitThreshold? Fitness.Good
            : aLikelihood > PoorFitThreshold? Fitness.Poor
            : Fitness.Discarded ;
    }

    int PoorFitThreshold = 35, GoodFitThreshold = 50, ExcelentFitThreshold = 85, PerfectFitThreshold = 99;  
  }

  public class Score 
  {
    public Score( int aLikelihood, Fitness aFitness )
    {
      Likelihood = aLikelihood;
      Fitness    = aFitness;  
    }

    public int     Likelihood = 0 ;
    public Fitness Fitness ;
  }


  public class ZipfDistribtionEstimator
  {
    public ZipfDistribtionEstimator( Distribution aSamples )
    {
      Samples = aSamples ;

      Likelihood = Calculate_LMZ();
    }

    public double Likelihood = 0 ;

    public static double Score ( Distribution aRankSize ) 
    { 
      ZipfDistribtionEstimator  lE = new ZipfDistribtionEstimator ( aRankSize );
      return lE.Likelihood ;
    }

    double Calculate_LMZ()
    {
      double lZ1 = Calculate_Z1();
      double lZ2 = Calculate_Z2();

      double lZ1_Squared = Square(lZ1);
      double lZ2_Squared = Square(lZ2);

      double lT0 = lZ1_Squared ;
      double lT1 = 6.0 * lZ1 * lZ2 ;
      double lT2 = 12.0 * lZ2_Squared ;

      double lT = lT0 + lT1 + lT2 ;

      double n = Samples.Count ;
      double rLMZ = 4 * lT * n;

      return rLMZ ;
    }

    double Calculate_Z1()
    {
      double n  = Samples.Count ;
      double Xn = Samples.Values.Last();

      double lSum = 0 ; Samples.Values.Select( Xi => lSum += Math.Log( Xi  / Xn ) );

      double lSumN = lSum / n ;

      double z1 = 1 - lSumN ;

      return z1 ;
    }

    double Calculate_Z2()
    {
      double n  = Samples.Count ;
      double Xn = Samples.Values.Last();

      double lSum = 0 ; Samples.Values.Select( Xi => lSum += Xn / Xi  );

      double lSumN = lSum / n ;

      double z2 = 0.5 - lSumN ;

      return z2 ;

    }

    static double Square( double n ) => n * n ;

    Distribution Samples ;
  }

  //public class StatisticalScore : Score
  //{
  //  public StatisticalScore( Signal aSignal, Distribution aSamples, Histogram aHistogram, double aRankSizeTailThreshold ) 
  //  {
  //    //aSamples.FillBaseStats();

  //    //Histogram = aHistogram ;

  //    //var lRankSize = Histogram.GetRankSize(aRankSizeTailThreshold);

  //    //lRankSize.Stats.Zipf_Likelihood = ZipfDistribtionEstimator.Score(lRankSize);

  //    //RankSize    = DTable.FromY(lRankSize);
  //    //LogRankSize = RankSize.ToLog();

  //    //Histogram.Table.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aSignal.Name +"_Histogram.png"));
  //    //RankSize       .CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aSignal.Name +"_RanSize.png"));
  //    //LogRankSize    .CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aSignal.Name +"_LogRankSize.png"));
  //  } 

  //  public override State GetState() 
  //  {
  //    State rS = new State("Score") ;

  //    rS.Add( State.With("Likelihood", Likelihood ) ) ;


  //    return rS ;
  //  }

  //  public Histogram Histogram        ;
  //  public DTable    RankSize         ;
  //  public DTable    LogRankSize      ;
  //}


}
