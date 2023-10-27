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
  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution();

      var lHistogram = new Histogram(lDist) ;

      lHistogram.Table.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aInput.Name +"_Histogram.png"));

      var lRankSize = lHistogram.Table.ToRankSize();

      lRankSize.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aInput.Name +"_RanSize.png"));


      Context.WriteLine("RankSize");

      foreach( var lPoint in lRankSize.Points )
      {
        Context.WriteLine(lPoint.ToString());
      }

      Score lScore = null ;
      
      mStep = aStep.Next( aInput, "Byte distribution score for language digits.", this, null, true, lScore) ;

      return mStep ;
    }

    protected override string Name => "ScoreBytesAsLanguageDigits" ;
  }

}
