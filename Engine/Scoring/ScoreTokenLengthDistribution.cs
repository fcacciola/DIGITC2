using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics;
using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class TokenLengthDistribution_Score : Score
  {
    public TokenLengthDistribution_Score( double aLikelihood )
    {
      Likelihood  = aLikelihood;
      Passed      = Likelihood > Context.Session.Args.GetOptionalDouble("ScoreTokenLengthDistribution_PassThreshold" ).GetValueOrDefault(0.35);
      QuitProcess = Likelihood < Context.Session.Args.GetOptionalDouble("ScoreTokenLengthDistribution_QuiteThreshold").GetValueOrDefault(0.01);
    }
  }

  public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
      mReference = DTable.FromFile( Context.Session.ReferenceFile("EnglishText_Tokens_RankSize.json") )  ;
    }

    string CreateFakeKey( double i ) => $"{i}";

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution().ExtendedWithBaseline(0,50,1,CreateFakeKey);

      var lRawHistogram = new Histogram(lDist) ;

      var lHistogram = lRawHistogram.Table ;

      var lFullRangeRankSize = lHistogram.ToRankSize();

      var lRankSize = lFullRangeRankSize.Normalized();

      var lLikelihood = GoodnessOfFit.RSquared(mReference.YValues, lHistogram.YValues) ; 

      Score lScore = new TokenLengthDistribution_Score(lLikelihood) ;

      mStep = aStep.Next( "Token-length distribution score", this, lScore) ;

      if ( Context.Session.Args.GetBool("SaveReference") )
      {
        lHistogram.Save(Context.Session.OutFile( aStep.Label + "_Histogram.json"));  
        lRankSize .Save(Context.Session.OutFile( aStep.Label + "_RankSize.json"));  
      }

      if ( Context.Session.Args.GetBool("Plot") )
      {
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aStep.Label +"_Histogram.png"));
        lRankSize.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aStep.Label +"_RankSize.png"));
      }

      return mStep ;
    }

    protected override string Name => "ScoreTokenLengthDistribution" ;

    DTable mReference = null ;
  }


  //public class ScoreWordLengthDistribution : LexicalFilter
  //{
  //  public ScoreWordLengthDistribution() : base() 
  //  {
  //  }

  //  protected override Step Process ( LexicalSignal aInput, Step aStep )
  //  {
  //    var lDist = aInput.GetDistribution();

  //    var lHistogram = new Histogram(lDist) ;

  //    Score lScore = null ; //new StatisticalScore(aInput, aInput.GetSamples(), lHistogram, 0) ;

  //    mStep = aStep.Next( "Word-length distribution score", this, lScore) ;

  //    return mStep ;
  //  }

  //  protected override string Name => "ScoreWordLengthDistribution" ;

  //}


}
