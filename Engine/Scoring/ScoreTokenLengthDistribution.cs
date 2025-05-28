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
      mReference     = DTable.FromFile( DContext.Session.ReferenceFile("Dracula_Tokens_RankSize.json") )  ;
      mQuitThreshold = DContext.Session.Args.GetOptionalInt("TokenLengthDistribution_QuitThreshold").GetValueOrDefault(1);
      mFitnessMap    = new FitnessMap(DContext.Session.Args.Get("TokenLengthDistribution_FitnessMap"));
    }

    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Scoring Tokens Length Distribution As Language Words");
      DContext.Indent();

      var lDist = aInput.GetDistribution().ExtendedWithBaseline(0,50,1);

      var lRawHistogram = new Histogram(lDist) ;

      var lHistogram = lRawHistogram.Table ;

      var lFullRangeRankSize = lHistogram.ToRankSize();

      var lRankSize = lFullRangeRankSize.Normalized();

      var lGOF = GoodnessOfFit.RSquared(mReference.YValues, lHistogram.YValues);

      DContext.WriteLine($"UNSCALED GoodnessOfFit.RSquared: {lGOF}");

      //
      // Scale the GOF when we have a small number of samples
      //
      if ( aInput.Symbols.Count < 20 )
        lGOF *= 5 ;
      else if ( aInput.Symbols.Count < 50 )
        lGOF *= 3 ;
      else if ( aInput.Symbols.Count < 100 )
        lGOF *= 2 ;

      DContext.WriteLine($"GoodnessOfFit.RSquared: {lGOF}");

      var lLikelihood = (int)Math.Round(lGOF * 100) ; 

      var lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood,lFitness) ;

      if ( DContext.Session.Args.GetBool("SaveReference") )
      {
        lHistogram.Save(DContext.Session.OutputFile( Name + "_Histogram.json"));  
        lRankSize .Save(DContext.Session.OutputFile( Name + "_RankSize.json"));  
      }

      if ( DContext.Session.Args.GetBool("Plot") )
      {
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.OutputFile(Name +"_Histogram.png"));
        lRankSize .CreatePlot(Plot.Options.Bars).SavePNG(DContext.Session.OutputFile(Name +"_RankSize.png"));
      }

      rOutput.Add( new Packet(Name, aInputPacket, aInput, "Token-length distribution score.", lScore, lLikelihood < mQuitThreshold));

      DContext.Unindent();
    }

    public override string Name => this.GetType().Name ;

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
