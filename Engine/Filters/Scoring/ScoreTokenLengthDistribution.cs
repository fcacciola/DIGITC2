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
   public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution();

      var lHistogram = new Histogram(lDist) ;

      Score lScore = null ; //new StatisticalScore(aInput, aInput.GetSamples(), lHistogram, 0) ;

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
      var lDist = aInput.GetDistribution();

      var lHistogram = new Histogram(lDist) ;

      Score lScore = null ; //new StatisticalScore(aInput, aInput.GetSamples(), lHistogram, 0) ;

      mStep = aStep.Next( aInput, "Word-length distribution score", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreWordLengthDistribution" ;

  }


}
