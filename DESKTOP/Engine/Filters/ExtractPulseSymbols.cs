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
      if (aPulses.Count == 0)
        return;

      var lPulseA = aPulses[0];
      lPulseA.Gap = double.MaxValue;  

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
      Options rOptions = new () { MinPulseWidth = Params.GetDouble("MinPulseWidth") };

      return rOptions;
    }

    protected override Packet Process ()
    {
      mOptions = CreateOptions() ;

      CreatePulses();
      FilterShortPulses();

      return CreateOutput( new LexicalSignal(CurrPulses), Name, new Score(Name, CurrPulses.Count, Score.TypeE.Boundless), false, new PartialResultMessage($"{CurrPulses.Count} pulses.") ) ;
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
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build(WaveInput, mPulseStart, mPos ) ;

        CurrPulses.Add( new PulseSymbol(CurrPulses.Count, mPulseStart, mPos, lSteps ) );
      }
    }

    double CalculateMinPulseWidth() 
    {
      if ( mOptions.MinPulseWidth == -1 )
      {
        double lMinPulseWidth = 0 ;

        var lDurations = CurrPulses.Durations();

        FilterHelper.DumpValues(Session, "Pulse_Durations",lDurations);
        var lGMM = GmmFitter.Fit(lDurations) ;

        if ( lGMM != null && lGMM.Components.Count > 0 )
        {
          lGMM.Save(Session, "Raw GMM_For_MinPulseWidth_Calculation");
          lGMM.Plot(Session, "Raw Gaps_Histogram_For_MinPulseWidth_Calculation"); 

          var lFilteredGMM = lGMM.DiscardMeaningless() ;

          if ( lFilteredGMM != null && lFilteredGMM.Components.Count > 0 )
          {
            lFilteredGMM.Save(Session, "GMM_For_MinPulseWidth_Calculation");
            lFilteredGMM.Plot(Session, "Gaps_Histogram_For_MinPulseWidth_Calculation"); 

            var lFirst = lFilteredGMM.Components[0];
            lMinPulseWidth = Math.Max(lFirst.N_Sigma(-4),0);

            if (lFilteredGMM.Components.Count > 1)
            {
              for( int i = 1 ; i < lFilteredGMM.Components.Count ; ++ i )
              {
                var lNext = lFilteredGMM.Components[i];
                //if ( lNext.Weight > lFirst.Weight )
                {
                  var lMinPulseWidth1 = Math.Max(lNext.N_Sigma(-4),0);
                  AddBranch("MinPulseWidth",$"{lMinPulseWidth1:F3}");
                }
              }
            }

            Params.ChangeValue("MinPulseWidth",$"{lMinPulseWidth:F3}");
          }
        }

        return lMinPulseWidth;
      }

      return mOptions.MinPulseWidth;
    }

    void FilterShortPulses() 
    {
      double lMinpulseWidth = CalculateMinPulseWidth();

      var lPulses = CurrPulses;
      lPulses.RemoveAll(p => p.Duration < lMinpulseWidth);
      Plot(CurrPulses,"Pulses");
    }

    class Options
    {
      internal double MinPulseWidth { get; set; }
    }

    Options                   mOptions ;
    List< List<PulseSymbol> > mPulsesBag = new List< List<PulseSymbol> >();
    bool                      mInGap ;
    int                       mPulseStart ;
    int                       mPos ;
  }


}
