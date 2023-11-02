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
  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
      mReferenceHistogram = DTable.FromFile( Context.Session.ReferenceFile("EnglishText_Bytes_Histogram.json") )  ;
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      var lDist = aInput.GetDistribution();

      List<Sample> lSamples = new List<Sample>();

      for( int i = 0; i < 256; i++ )  
        lSamples.Add( new Sample( new FakeSampleSource( new ByteSymbol(-1,(byte)i).Meaning),i));

      lSamples.AddRange(lDist.Samples);

      var lRawHistogram = new Histogram(lSamples) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var lHistogram = lFullRangeHistogram.Normalized();

      if ( mReferenceHistogram.YValues.Count == lHistogram.YValues.Count )
      {
        var a = GoodnessOfFit.RSquared(mReferenceHistogram.YValues, lHistogram.YValues) ; 
        var b = GoodnessOfFit.StandardError(mReferenceHistogram.YValues, lHistogram.YValues, 1) ; 
        var c = GoodnessOfFit.CoefficientOfDetermination(mReferenceHistogram.YValues, lHistogram.YValues) ; 
      }


      var lRankSize = lHistogram.ToRankSize();

      Score lScore = null ;
      
      mStep = aStep.Next( "Byte distribution score for language digits.", this, lScore) ;

      bool lSaveReference = Context.Session.Args.GetBool("SaveReference") ;
      if ( lSaveReference )
      {
        string lDistFile = Context.Session.OutFile( aStep.Label + "_Histogram.json");
        string lRSFile   = Context.Session.OutFile( aStep.Label + "_RankSize.json");

        lHistogram.Save(lDistFile);  
        lRankSize .Save(lRSFile);  
      }

      lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aStep.Label +"_Histogram.png"));
      lRankSize .CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.OutFile(aStep.Label +"_RankSize.png"));


      return mStep ;
    }

    protected override string Name => "ScoreBytesAsLanguageDigits" ;

    DTable mReferenceHistogram = null ;
  }

}

