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

namespace DIGITC2
{
  public abstract class Score : IWithState
  {
    public abstract State GetState() ;

    public bool QuitProcess = false ;

    public double Likelihood = 0 ;
  }

  public class ZipfDistribtionEstimator
  {
    public ZipfDistribtionEstimator( Samples aSamples )
    {
      Samples = aSamples ;

      Likelihood = Calculate_LMZ();
    }

    public double Likelihood = 0 ;

    public static double Score ( Samples aRankSize ) 
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
      double Xn = Samples.Last();

      double lSum = 0 ; Samples.ForEach( Xi => lSum += Math.Log( Xi  / Xn ) );

      double lSumN = lSum / n ;

      double z1 = 1 - lSumN ;

      return z1 ;
    }

    double Calculate_Z2()
    {
      double n  = Samples.Count ;
      double Xn = Samples.Last();

      double lSum = 0 ; Samples.ForEach( Xi => lSum += Xn / Xi  );

      double lSumN = lSum / n ;

      double z2 = 0.5 - lSumN ;

      return z2 ;

    }

    static double Square( double n ) => n * n ;

    Samples Samples ;
  }

  public class StatisticalScore : Score
  {
    public StatisticalScore( Signal aSignal, Samples aSamples, Histogram aHistogram ) 
    {
      Histogram   = aHistogram ;

      var lRankSize = Histogram.GetRankSize();

      Zipf_Likelihood = ZipfDistribtionEstimator.Score(lRankSize);

      RankSize    = DTable.FromY(lRankSize);
      LogRankSize = RankSize.ToLog();

      var lDescriptiveStatistics = new DescriptiveStatistics(aSamples);

      Kurtosis          = lDescriptiveStatistics.Kurtosis;
      Maximum           = lDescriptiveStatistics.Maximum;
      Minimum           = lDescriptiveStatistics.Minimum;
      Mean              = lDescriptiveStatistics.Mean;
      Variance          = lDescriptiveStatistics.Variance;
      StandardDeviation = lDescriptiveStatistics.StandardDeviation;
      Skewness          = lDescriptiveStatistics.Skewness;

      Histogram.Table.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aSignal.Name +"_Histogram.png"));
      RankSize       .CreatePlot(Plot.Options.Lines).SavePNG(Context.Session.OutFile(aSignal.Name +"_RanSize.png"));
      LogRankSize    .CreatePlot(Plot.Options.Lines).SavePNG(Context.Session.OutFile(aSignal.Name +"_LogRankSize.png"));
    } 

    public override State GetState() 
    {
      State rS = new State("Score") ;

      rS.Add( State.With("Zipf_Likelihood"  , Zipf_Likelihood  ) ) ;
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
    public DTable    RankSize         ;
    public DTable    LogRankSize      ;
    public double    Zipf_Likelihood  ;
    public double    Kurtosis         ;
    public double    Maximum          ;
    public double    Minimum          ;
    public double    Mean             ;
    public double    Variance         ;
    public double    StandardDeviation;
    public double    Skewness         ;
  }

  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      for( int i = 0 ; i < 256 ; ++ i )
      {
        ByteSymbol lB = new ByteSymbol(i,(byte)i);

        lEntries.Add( new Histogram.Entry(null, lB.Meaning, lB.Value) ) ;
      }

      foreach( var lSymbol in aInput.Symbols )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, lSymbol.Meaning, lSymbol.Value) ) ;
      }

      var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Byte distribution score for language digits.", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreBytesAsLanguageDigits" ;
  }

  public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      foreach( var lSymbol in aInput.GetSymbols<ArraySymbol>() )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, $"{lSymbol.Symbols.Count}", lSymbol.Symbols.Count) ) ;
      }

      var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreTokenLengthDistribution" ;

  }


  public class ScoreWordLengthDistribution : LexicalFilter
  {
    public ScoreWordLengthDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      foreach( var lSymbol in aInput.GetSymbols<WordSymbol>() )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, $"{lSymbol.Word.Length}", lSymbol.Word.Length) ) ;
      }

       var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreWordLengthDistribution" ;

  }

  public class ScoreWordFrequencyDistribution : LexicalFilter
  {
    public ScoreWordFrequencyDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      foreach( var lSymbol in aInput.GetSymbols<WordSymbol>() )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, $"{lSymbol.Word}", lSymbol.Idx) ) ;
      }

       var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreWordFrequencyDistribution" ;

  }

}
