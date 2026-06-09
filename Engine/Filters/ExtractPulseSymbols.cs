using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

using MathNet.Numerics.Statistics;
using System.Runtime.InteropServices;

namespace ENGINE
{

  public class FilterHelper
  {
    static public void DumpValues<T>( Session aSession, string aName, List<T> aValues )
    {
      try
      {
        string[] lAsStrings = new string[aValues.Count];
        for( int i = 0 ; i <  aValues.Count ; i++ ) 
          lAsStrings[i] = $"{aValues[i]}";

        var lCSV = string.Join(" , ", lAsStrings ) ;

        File.WriteAllText(aSession.OutputFile($"{aName}_CSV.txt"), lCSV);
      }
      catch( Exception e )
      {
        aSession.Error(e);
      }
    }
  }

  public static class PulseSymbolExtensions
  {
    static public void SetupGapDurations( this List<PulseSymbol> aPulses )
    {
      var rGapDurations = new double[aPulses.Count-1];

      var lPulseA = aPulses[0];
      lPulseA.Gap = 0;  

      for ( int i = 1; i < aPulses.Count ; i++ )
      { 
        var lPulseB = aPulses[i]; 

        var lGap = lPulseB.StartTime - lPulseA.EndTime;

        lPulseB.Gap = lGap ;

        lPulseA = lPulseB;
      }
    }

    static public List<double> Gaps( this List<PulseSymbol> aPulses )
    {
      return aPulses.Select( p => p.Gap ).Skip(1).ToList();
    }

    static public List<double> Durations( this List<PulseSymbol> aPulses )
    {
      return aPulses.Select( p => p.Duration ).ToList();
    }

    static public void Plot( this List<PulseSymbol> aPulses, string aLabel, Session aSession )
    {
      List<float> lSamples = new List<float> ();
      aPulses.ForEach( s => s.DumpSamples(lSamples ) );
      DiscreteSignal lWaveRep = new DiscreteSignal(SIG.SamplingRate, lSamples);
      WaveSignal lWave = new WaveSignal(lWaveRep);
      lWave.SaveTo( aSession.OutputFile( aLabel + ".wav"), aSession ) ;
    }
  }

  internal class PulseStepBuilder
  {
    internal static List<PulseStep> Build( WaveSignal aSource, int aPulseStart, int aPulseEnd )
    {
      var lB = new PulseStepBuilder( aSource, aPulseStart, aPulseEnd );  
      return lB.Create();
    }

    PulseStepBuilder( WaveSignal aSource, int aPulseStart, int aPulseEnd )
    {
      mSource     = aSource;  
      mPulseStart = aPulseStart;  
      mPulseEnd   = aPulseEnd;
    }

    List<PulseStep> Create()
    {
      mCurrCount = 0; 
      mPos = mPulseStart ;
      mStepStart = mPos ;
      mAmplitude = -1 ;

      for ( int i = mPulseStart; i < mPulseEnd ; ++ i )
      {
        float lV = mSource.Samples[i];

        if( mCurrCount == 0 || mAmplitude != lV ) 
        {
          AddStep();

          mAmplitude = lV;
          mCurrCount = 1 ;
        }
        else
        { 
          mCurrCount ++ ;
        }

        mPos ++ ;
      }

      AddStep();

      return mSteps ;
    }

    void AddStep()
    {
      if ( mCurrCount > 0 )
      {
        mSteps.Add( new PulseStep(mAmplitude, mStepStart, mPos ) );
        mStepStart = mPos ;
      }
    }


    WaveSignal      mSource ;
    int             mPulseStart ;
    int             mPulseEnd ;
    List<PulseStep> mSteps = new List<PulseStep>();
    int             mStepStart ;
    int             mPos ;
    float           mAmplitude ;
    int             mCurrCount ;
  }

  public class ExtractPulseSymbols : WaveFilter
  {
    public ExtractPulseSymbols() 
    { 
    }

    Options CreateOptions()
    {
      Options rOptions = new ();

      rOptions.MinLevelThreshold           = Params.GetInt("MinLevelThreshold");
      rOptions.MinDurationThreshold        = Params.GetDouble("MinDurationThreshold");
      rOptions.ContiguousPulsesGapDuration = Params.GetDouble("ContiguousPulsesGapDuration");
      rOptions.VeryShortThreshold          = Params.GetDouble("VeryShortThreshold");

      return rOptions;

    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions() ;

      CreatePulses();

      return CreateOutput( new LexicalSignal(CurrPulses), Name, null, false) ;
    }
   
    public override string Name => this.GetType().Name ;

    List<PulseSymbol> CurrPulses => mPulsesBag.Last() ;

    void CreatePulses()
    {
      mPulsesBag.Add( new List<PulseSymbol>() );

      mInGap = WaveInput.Samples[0] == 0 ;

      mPulseStart = 0 ;
      mPos        = 0 ;

      WriteLine2GUI($"Creating pulses for WaveSignal of Length {WaveInput.Samples.Length}");

      for ( mPos = 0 ; mPos < WaveInput.Samples.Length ; ++ mPos )
      {
        float lV = WaveInput.Samples[mPos] ;

        if ( lV > 0 )
        {
          if ( mInGap )
          {
            mPulseStart = mPos ;
          }
          mInGap = false ;  
        }
        else
        {
          AddPulse();
          mInGap = true ;  
        }
      }

      AddPulse();

      CurrPulses.SetupGapDurations(); 

      Plot(CurrPulses,"Pulses");
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build(WaveInput, mPulseStart, mPos ) ;

        CurrPulses.Add( new PulseSymbol(CurrPulses.Count, mPulseStart, mPos, lSteps ) );
      }
    }

    class Options
    {
      internal int    MinLevelThreshold ;
      internal double MinDurationThreshold ;
      internal double ContiguousPulsesGapDuration = -1 ;
      internal double VeryShortThreshold = -1 ;
    }

    Options                   mOptions ;
    List< List<PulseSymbol> > mPulsesBag = new List< List<PulseSymbol> >();
    bool                      mInGap ;
    int                       mPulseStart ;
    int                       mPos ;
  }


}
