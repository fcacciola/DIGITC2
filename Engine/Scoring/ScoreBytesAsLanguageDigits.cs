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
      mReference     = DTable.FromFile( Context.Session.ReferenceFile("Dracula_Bytes_Histogram.json") )  ;
      mQuitThreshold = Context.Session.Args.GetOptionalInt("BytesAsLanguageDigits_QuitThreshold").GetValueOrDefault(1);
      mFitnessMap    = new FitnessMap(Context.Session.Args.Get("BytesAsLanguageDigits_FitnessMap"));
    }

    string CreateFakeKey( double i ) => new ByteSymbol(-1,(byte)i).Meaning;

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      var lDist = aInput.GetDistribution().ExtendedWithBaseline(0, 256, 1, CreateFakeKey);

      var lRawHistogram = new Histogram(lDist) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var lHistogram = lFullRangeHistogram.Normalized();

      var lLikelihood = (int)Math.Round(GoodnessOfFit.RSquared(mReference.YValues, lHistogram.YValues) * 100) ; 

      var lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(lLikelihood,lFitness) ;
      
      if ( Context.Session.Args.GetBool("SaveReference") )
        lHistogram.Save(Context.Session.LogFile( Name + "_Histogram.json"));  

      if ( Context.Session.Args.GetBool("Plot") )
        lHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile(Name +"_Histogram.png"));

      rOutput.Add( new Branch(aInputBranch, aInput, "Byte distribution score for language digits.", lScore, lLikelihood < mQuitThreshold));

    }

    protected override string Name => "ScoreBytesAsLanguageDigits" ;

    int        mQuitThreshold;
    FitnessMap mFitnessMap ;
    DTable     mReference = null ;
  }

}

