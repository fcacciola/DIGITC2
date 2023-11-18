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
  public class ScoreWordFrequencyDistribution : LexicalFilter
  {
    public ScoreWordFrequencyDistribution() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution();

      var lRawHistogram = new Histogram(lDist) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var lHistogram = lFullRangeHistogram.Normalized();

      Score lScore = null ; //new StatisticalScore(aInput, aInput.GetSamples(), lHistogram, -1) ;

      mStep = aStep.Next( "Word-frequency distribution score", this, lScore) ;

      if ( Context.Session.Args.GetBool("SaveReference") )
        lHistogram.Save(Context.Session.LogFile( aStep.Label + "_Histogram.json"));  

      if ( Context.Session.Args.GetBool("Plot") )
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile(aStep.Label +"_Histogram.png"));

      return mStep ;
    }

    protected override string Name => "ScoreWordFrequencyDistribution" ;

  }

}
