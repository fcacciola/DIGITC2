using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public abstract class Score : IWithState
  {
    public abstract State GetState() ;
  }

  public class StatisticalScore : Score
  {
    public override State GetState() 
    {
      State rS = new State() ;

      rS.Add( State.With("Kurtosis"         , Kurtosis         ) ) ;
      rS.Add( State.With("Maximum"          , Maximum          ) ) ;
      rS.Add( State.With("Minimum"          , Minimum          ) ) ;
      rS.Add( State.With("Mean"             , Mean             ) ) ;
      rS.Add( State.With("Variance"         , Variance         ) ) ;
      rS.Add( State.With("StandardDeviation", StandardDeviation) ) ;
      rS.Add( State.With("Skewness"         , Skewness         ) ) ;

      return rS ;
    }

    public Histogram Histogram        ;
    public Plot      HistogramPlot    ;
    public double    Kurtosis         ;
    public double    Maximum          ;
    public double    Minimum          ;
    public double    Mean             ;
    public double    Variance         ;
    public double    StandardDeviation;
    public double    Skewness         ;
  }

  public class SizeBasedScoring
  {
    public static Score Score( Signal aSignal )
    {
      var lSamples = aSignal.GetSamples();

      var rScore = new StatisticalScore() ;

      var lDescriptiveStatistics = new DescriptiveStatistics(lSamples);

      rScore.Kurtosis          = lDescriptiveStatistics.Kurtosis         ;
      rScore.Maximum           = lDescriptiveStatistics.Maximum          ;
      rScore.Minimum           = lDescriptiveStatistics.Minimum          ;
      rScore.Mean              = lDescriptiveStatistics.Mean             ;
      rScore.Variance          = lDescriptiveStatistics.Variance         ;
      rScore.StandardDeviation = lDescriptiveStatistics.StandardDeviation;
      rScore.Skewness          = lDescriptiveStatistics.Skewness         ;

      rScore.Histogram = new Histogram(lSamples, lSamples.Count * 4 );
      rScore.HistogramPlot = rScore.Histogram.CreatePlot( Plot.Options.Default ) ;

rScore.HistogramPlot.SavePNG($".\\{aSignal.Name}_Score.png");

      return rScore ;

    }
    
  }

  public class ScoreLexicalSignal : LexicalFilter
  {
    public ScoreLexicalSignal() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lScore = SizeBasedScoring.Score(aInput);

      mStep = aStep.Next( aInput, "Lexical Scoring", this, lScore, true) ;

      return mStep ;
    }

  }

}
