using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

using MathNet.Numerics.Statistics;

namespace DIGITC2
{
  internal class PulseFilterHelper
  {
    static internal DTable GetHistogram( List<double> aValues )
    {
      var lDist = new Distribution(aValues) ;

      var lRawHistogram = new Histogram(lDist) ;

      var lFullRangeHistogram = lRawHistogram.Table ;

      var rHistogram = lFullRangeHistogram.Normalized();

      return rHistogram;
    }

    static internal void PlotPulses( List<PulseSymbol> aPulses, int aSamplingRate, string aLabel )
    {
      if ( Context.Session.Args.GetBool("Plot") )
      {
        List<float> lSamples = new List<float> ();
        aPulses.ForEach( s => s.DumpSamples(lSamples ) );
        DiscreteSignal lWaveRep = new DiscreteSignal(aSamplingRate, lSamples);
        WaveSignal lWave = new WaveSignal(lWaveRep);
        lWave.SaveTo( Context.Session.LogFile( "Pulses" + aLabel + ".wav") ) ;
      }
    }

    static List<double> GetDurations( List<PulseSymbol> aPulses )  
    {
      List<double> rDurations = new List<double>() ;

      aPulses.ForEach( s => rDurations.Add(s.Duration) );

      rDurations.Sort(); 

      return rDurations ; 

    }
    static internal void PlotPulseDurationHistogram( List<PulseSymbol> aPulses, string aName )
    {
      List<double> lDurations = GetDurations(aPulses);

      var lMergedHistogram = GetHistogram(lDurations) ;  

      if ( Context.Session.Args.GetBool("Plot") )
      { 
        lMergedHistogram.CreatePlot(Plot.Options.Bars).SavePNG(Context.Session.LogFile($"{aName}_Durations_Histogram.png"));
      }

    }
  }

  internal class PulseStepBuilder
  {
    internal static List<PulseStep> Build( PulseSymbol aPulse )
    {
      var lB = new PulseStepBuilder( aPulse );  
      return lB.Create();
    }

    PulseStepBuilder( PulseSymbol aPulse )
    {
      mPulse = aPulse ;
    }

    List<PulseStep> Create()
    {
      mRawSymbols = new List<GatedSymbol>();

      mCurrCount = 0; 
      mPos       = 0 ;

      for ( int i = 0;)
      foreach( float lV in mInput.Samples)
      {
        if( mCurrCount == 0 || mCurrLevel != lV ) 
        {
          AddStep();

          mCurrLevel = lV;
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
        mSteps.Add( new PulseStep(mCurrLevel, mPos - mCurrCount, mCurrCount, (double)mCurrCount / (double)mSamplingRate ) );
    }


    List<PulseStep> mSteps = new List<PulseStep>();
    int             mPos ;
    float           mCurrLevel ;
    int             mCurrCount ;
    int             mSamplingRate ;
  }

  public class ExtractPulseSymbols : WaveFilter
  {
    public ExtractPulseSymbols() 
    { 
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      mInput = aInput ; 

      CreatePulses();

      mStep = aStep.Next( new LexicalSignal(mPulses), "Raw Pulses", this) ;

      return mStep ;
    }
    
    protected override string Name => "ExtractPulses" ;

    void CreatePulses()
    {
      mPulses = new List<PulseSymbol>();

      mInGap = mInput.Samples[0] == 0 ;

      mPulseStart = 0 ;
      mPos        = 0 ;

      for ( mPos = 0 ; mPos < mInput.Samples.Length ; ++ mPos )
      {
        float lV = mInput.Samples[mPos] ;

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

      PulseFilterHelper.PlotPulses(mPulses, mInput.SamplingRate, "Raw");
    }

    void AddPulse()
    {
      if ( ! mInGap )
      {
        var lSteps = PulseStepBuilder.Build()

        mPulses.Add( new PulseSymbol(mPulses.Count, mInput.SamplingRate, mPulseStart, mPos - mPulseStart, lSteps ) );

      }
    }

    PulseSymbol mPulse ;
    bool              mInGap ;
    int               mPulseStart ;
    int               mPos ;
    List<PulseSymbol> mPulses ;
  }

}
