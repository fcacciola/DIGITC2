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

  public class StatisticalScore : Score
  {
    public StatisticalScore( Signal aSignal, Samples aSamples, Histogram aHistogram ) 
    {
      Histogram   = aHistogram ;
      RankSize    = DTable.FromY(Histogram.GetRankSize());
      LogRankSize = RankSize.ToLog();

      var lDescriptiveStatistics = new DescriptiveStatistics(aSamples);

      Kurtosis = lDescriptiveStatistics.Kurtosis;
      Maximum = lDescriptiveStatistics.Maximum;
      Minimum = lDescriptiveStatistics.Minimum;
      Mean = lDescriptiveStatistics.Mean;
      Variance = lDescriptiveStatistics.Variance;
      StandardDeviation = lDescriptiveStatistics.StandardDeviation;
      Skewness = lDescriptiveStatistics.Skewness;

      Histogram.Table.CreatePlot( Plot.Options.Default ).SavePNG($".\\{aSignal.Name}_Histogram.png");
      RankSize.CreatePlot( Plot.Options.Default ).SavePNG($".\\{aSignal.Name}_RankSize.png");
      LogRankSize.CreatePlot( Plot.Options.Default ).SavePNG($".\\{aSignal.Name}_LogRankSize.png");


    } 

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
    public DTable    RankSize         ;
    public DTable    LogRankSize      ;
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
  }

  public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      //for( int i = 0 ; i < Context.MaxWordLength  ; ++ i )
      // lEntries.Add( new Histogram.Entry(null, $"{i}", i) ) ;

      foreach( var lSymbol in aInput.GetSymbols<ArraySymbol>() )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, $"{lSymbol.Symbols.Count}", lSymbol.Symbols.Count) ) ;
      }

      var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

  }


  public class ScoreWordLengthDistribution : LexicalFilter
  {
    public ScoreWordLengthDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Histogram.Entry> lEntries = new List<Histogram.Entry>() ;

      //for( int i = 0 ; i < Context.MaxWordLength ; ++ i )
      // lEntries.Add( new Histogram.Entry(null, $"{i}", i) ) ;

      foreach( var lSymbol in aInput.GetSymbols<WordSymbol>() )
      {
        lEntries.Add( new Histogram.Entry(lSymbol, $"{lSymbol.Word.Length}", lSymbol.Word.Length) ) ;
      }

       var lHistogram = new Histogram(lEntries);

      var lScore = new StatisticalScore(aInput, aInput.GetSamples(), lHistogram) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

  }


}
