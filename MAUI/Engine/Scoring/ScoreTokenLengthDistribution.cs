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

namespace DIGITC2_ENGINE
{
  public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
    }

    public override void Setup()
    {
      mReference = DTable.FromFile( DIGITC_Context.Session.ReferenceFile("Dracula_Tokens_RankSize.json") )  ;
      mQuitThreshold = DIGITC_Context.Session.Args.GetOptionalInt("TokenLengthDistribution_QuitThreshold").GetValueOrDefault(1);
      mFitnessMap    = new FitnessMap(DIGITC_Context.Session.Args.Get("TokenLengthDistribution_FitnessMap"));
    }

    string CreateFakeKey( double i ) => $"{i}";

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      var lDist = aInput.GetDistribution().ExtendedWithBaseline(0,50,1,CreateFakeKey);

      var lRawHistogram = new Histogram(lDist) ;

      var lHistogram = lRawHistogram.Table ;

      var lFullRangeRankSize = lHistogram.ToRankSize();

      var lRankSize = lFullRangeRankSize.Normalized();

      var lLikelihood = (int)Math.Round(GoodnessOfFit.RSquared(mReference.YValues, lRankSize.YValues) * 100) ; 

      var lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(lLikelihood,lFitness) ;

      if ( DIGITC_Context.Session.Args.GetBool("SaveReference") )
      {
        lHistogram.Save(DIGITC_Context.Session.LogFile( Name + "_Histogram.json"));  
        lRankSize .Save(DIGITC_Context.Session.LogFile( Name + "_RankSize.json"));  
      }

      if ( DIGITC_Context.Session.Args.GetBool("Plot") )
      {
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(DIGITC_Context.Session.LogFile(Name +"_Histogram.png"));
        lRankSize.CreatePlot(Plot.Options.Bars).SavePNG(DIGITC_Context.Session.LogFile(Name +"_RankSize.png"));
      }

      rOutput.Add( new Branch(aInputBranch, aInput, "Token-length distribution score.", lScore, lLikelihood < mQuitThreshold));
    }

    protected override string Name => "ScoreTokenLengthDistribution" ;

    int        mQuitThreshold;
    FitnessMap mFitnessMap ;
    DTable     mReference = null ;
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
