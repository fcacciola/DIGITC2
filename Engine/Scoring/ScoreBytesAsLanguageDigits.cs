using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics;
using MathNet.Numerics.Statistics;

using Microsoft.SqlServer.Server;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class BytesAsLanguageDigits_Score : Score
  {
    public BytesAsLanguageDigits_Score( double aLikelihood )
    {
      Likelihood  = aLikelihood;
      Passed      = Likelihood > Context.Session.Args.GetOptionalDouble("BytesAsLanguageDigits_PassThreshold" ).GetValueOrDefault(0.35);
      QuitProcess = Likelihood < Context.Session.Args.GetOptionalDouble("BytesAsLanguageDigits_QuiteThreshold").GetValueOrDefault(0.01);
    }
  }
  
  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
      mReference = DTable.FromFile( Context.Session.ReferenceFile("EnglishText_Bytes_Histogram.json") )  ;
    }

    string CreateFakeKey( double i ) => new ByteSymbol(-1,(byte)i).Meaning;

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution().ExtendedWithBaseline(0, 256, 1, CreateFakeKey);

      var lRawHistogram = new Histogram(lDist) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var lHistogram = lFullRangeHistogram.Normalized();

      var lLikelihood = GoodnessOfFit.RSquared(mReference.YValues, lHistogram.YValues) ; 

      Score lScore = new BytesAsLanguageDigits_Score(lLikelihood) ;
      
      mStep = aStep.Next( "Byte distribution score for language digits.", this, lScore) ;

      if ( Context.Session.Args.GetBool("SaveReference") )
        lHistogram.Save(Context.Session.OutFile( aStep.Label + "_Histogram.json"));  

      if ( Context.Session.Args.GetBool("Plot") )
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aStep.Label +"_Histogram.png"));

      return mStep ;
    }

    protected override string Name => "ScoreBytesAsLanguageDigits" ;

    DTable mReference = null ;
  }

}

