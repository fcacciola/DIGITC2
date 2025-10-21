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

namespace DIGITC2_ENGINE
{

  public class XCorrelation
  {
    public XCorrelation( DTable aReference ) { mReference = aReference ; }



    DTable mReference ;
  }

  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
    }

    public override void Setup()
    {
      FillReferenceDistribution();

      mQuitThreshold = DContext.Session.Args.GetOptionalInt(Name, "_QuitThreshold").GetValueOrDefault(1);
      mFitnessMap    = new FitnessMap(DContext.Session.Args.Get(Name, "FitnessMap"));
    }

    //
    // According to Claude Sonnet 4
    //
    public static Dictionary<char, double> EnglishBytesDistribution = new Dictionary<char, double>
    {
        ['A'] = 8.12, ['B'] = 1.49, ['C'] = 2.78, ['D'] = 4.25, ['E'] = 12.02, ['F'] = 2.23, ['G'] = 2.02, ['H'] = 6.09, ['I'] = 6.97, ['J'] = 0.15,
        ['K'] = 0.77, ['L'] = 4.03, ['M'] = 2.41, ['N'] = 6.75, ['O'] = 7.51, ['P'] = 1.93, ['Q'] = 0.10, ['R'] = 5.99, ['S'] = 6.33, ['T'] = 9.06,
        ['U'] = 2.76, ['V'] = 0.98, ['W'] = 2.36, ['X'] = 0.15, ['Y'] = 1.97, ['Z'] = 0.07,
        ['a'] = 8.12, ['b'] = 1.49, ['c'] = 2.78, ['d'] = 4.25, ['e'] = 12.02, ['f'] = 2.23, ['g'] = 2.02, ['h'] = 6.09, ['i'] = 6.97, ['j'] = 0.15,
        ['k'] = 0.77, ['l'] = 4.03, ['m'] = 2.41, ['n'] = 6.75, ['o'] = 7.51, ['p'] = 1.93, ['q'] = 0.10, ['r'] = 5.99, ['s'] = 6.33, ['t'] = 9.06,
        ['u'] = 2.76, ['v'] = 0.98, ['w'] = 2.36, ['x'] = 0.15, ['y'] = 1.97, ['z'] = 0.07,
        ['0'] = 0.96, ['1'] = 1.72, ['2'] = 0.84, ['3'] = 0.54, ['4'] = 0.47, ['5'] = 0.41, ['6'] = 0.39, ['7'] = 0.36, ['8'] = 0.34, ['9'] = 0.32
    };

    void FillReferenceDistribution()
    {
      List<DPoint> lDPs = new List<DPoint>();

      double lMax = EnglishBytesDistribution.Values.Max();  

      foreach( var lKV in EnglishBytesDistribution )
      {
        lDPs.Add( new DPoint( new Sample(null, Convert.ToDouble( (byte)(lKV.Key))), lKV.Value / lMax) );
      }

      mReference = new DTable(lDPs);
    }

    string CreateFakeKey( double i ) => new ByteSymbol(-1,(byte)i).Meaning;

    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Scoring Bytes As Language Digits");
      DContext.Indent();

      TokenSeparators lFilterSeparators = new TokenSeparators();

      var lBytes = aInput.Symbols.Where( s => ! lFilterSeparators.IsSeparator(s) ).GetValues();

      // Validate any byte that is used as a letter (wright 1 for all of these)
      double lCorrelation = mReference.ComputeCorrelation(lBytes, (dp,x) => 1.0 ) ;  

      DContext.WriteLine($"Correlation: {lCorrelation}");

      var lLikelihood = (int)Math.Round(lCorrelation * 100) ; 

      var lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood,lFitness) ;

      rOutput.Add( new Packet(Name, aInputPacket, aInput, "Byte distribution score for language digits.", lScore, lLikelihood < mQuitThreshold));

      DContext.Unindent();

    }

    public override string Name => this.GetType().Name ;

    int        mQuitThreshold;
    FitnessMap mFitnessMap ;
    DTable     mReference = null ;
  }

}

