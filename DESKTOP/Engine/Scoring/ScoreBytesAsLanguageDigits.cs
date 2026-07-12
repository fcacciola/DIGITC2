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

namespace ENGINE
{

  public class ScoreBytesAsLanguageDigits : LexicalFilter
  {
    public ScoreBytesAsLanguageDigits() : base() 
    {
    }

    protected override void OnSetup()
    {
      FillCC();

      mQuitThreshold = Params.GetDouble("QuitThreshold");
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

    void FillCC()
    {
      List<double> lData = new List<double>();

      double lMax = EnglishBytesDistribution.Values.Max();  

      foreach( var lKV in EnglishBytesDistribution )
      {
        lData.Add( lKV.Value / lMax) ;
      }

      mCC = new CorrelationCalculator(lData);
    }

    protected override Packet Process ()
    {
      TokenSeparators lFilterSeparators = new TokenSeparators();

      var lBytesSymbols = LexicalInput.GetSymbols<ByteSymbol>() ;
     
      var lBytes = lBytesSymbols.ConvertAll( bs => bs.Value ) ;

      double lCorrelation = mCC.Calculate(lBytes) ;  

      WriteLine($"Correlation: {lCorrelation}");

      Score lScore = new Score(Name, lCorrelation, Score.TypeE.Correlation) ;

      return CreateOutput( LexicalInput, "Byte distribution score for language digits.", lScore, lCorrelation < mQuitThreshold);

    }

    public override string Name => this.GetType().Name ;

    double                mQuitThreshold;
    CorrelationCalculator mCC ;
  }

}

