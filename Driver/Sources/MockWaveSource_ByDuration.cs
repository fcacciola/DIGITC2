using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

namespace DIGITC2_ENGINE
{

  public class MockWaveSource_ByDuration : MockWaveSource_FromBits
  {
    public class Params
    {
      public double EnvelopeAttackTime ;
      public double EnvelopeReleaseTime ;
      public double AmplitudeGateThreshold ;
      public double ExtractGatedlSymbolsMinDuration ;
      public double ExtractGatedlSymbolsMergeGap ;
      public double BinarizeByDurationThreshold ;
    }

    public MockWaveSource_ByDuration( BaseParams aBaseParams, Params aParams ) : base(aBaseParams)
    {
      mParams = aParams ;
    }

    public static MockWaveSource_ByDuration FromText( string aText )
    {
      var lBaseParams = new BaseParams() ;
      lBaseParams.Text = aText ;

      var lParams = new Params() ;

      return new MockWaveSource_ByDuration(lBaseParams, lParams);
    }

    internal class Chunk
    {
      internal Chunk( DiscreteSignal aWave, double aLevel ) {  Wave = aWave ; Level = aLevel ; }

      internal DiscreteSignal Wave ;  
      internal double         Level ;
    }

    double GetGapDuration () => GetRandomDuration(mGapMinDuration , mGapMaxDuration);
    double GetZeroDuration() => GetRandomDuration(mZeroMinDuration, mZeroMaxDuration);
    double GetOneDuration () => GetRandomDuration(mOneMinDuration , mOneMaxDuration);
    
    double GetRandomDuration( double aMin, double aMax)
    {
      double lF = mRND.NextDouble() ;

      double rDuration = MathX.LERP(aMin, aMax, lF) ;

      return rDuration ;
    }

    Chunk ModulateZero()
    {
      var lGap = GetNoise( GetZeroDuration(), mZeroLevel ) ;

      return new Chunk(lGap, mZeroLevel);
    }

    Chunk ModulateOne()
    {
      var lGap = GetNoise( GetOneDuration(), mOneLevel ) ;

      return new Chunk(lGap, mOneLevel);
    }

    Chunk ModulateBit( bool aBit )
    {
      var lFlat = aBit ? ModulateOne() : ModulateZero();

      //var rChunk = ApplyEnvelope(lFlat);
      var rChunk = lFlat;

      return rChunk ;
    }

    Chunk ModulateGap()
    {
      var lGap = GetNoise( GetGapDuration(), mGapLevel ) ;

      return new Chunk(lGap, mGapLevel);
    }

    List<Chunk> BuildChunks( List<bool> aBits )
    {
      List<Chunk> rChunks = new List<Chunk>();  

      for ( int i = 0 ; i < aBits.Count ; i++ ) 
      {
        bool lBit = aBits[i] ;  

        rChunks.Add( ModulateBit( lBit ) ) ;  

        rChunks.Add( ModulateGap() ) ;  
      }

      return rChunks ;
    }

    DiscreteSignal ConcatenateChunks( List<Chunk> aChunks )
    {
      List<float> lAllSamples = new List<float>();

      foreach( var lChunk in aChunks )
        lAllSamples.AddRange( lChunk.Wave.Samples );  

      return new DiscreteSignal(X.SamplingRate, lAllSamples);
    }

    protected override DiscreteSignal ModulateBits( List<bool> aBits )
    {
      List<Chunk> lChunks = BuildChunks( aBits );

      return ConcatenateChunks( lChunks );  
    }

    DiscreteSignal GetNoise( double aDuration, double aLevel )
    {
      int lLength = (int)Math.Ceiling(aDuration * X.SamplingRate) ;

      return new WhiteNoiseBuilder()
                  .SetParameter("min", - aLevel)
                  .SetParameter("max",   aLevel)
                  .OfLength(lLength)
                  .SampledAt(X.SamplingRate)
                  .Build();
    }

    DiscreteSignal GetEnvelope( DiscreteSignal aSource, double aLevel )
    { 
      var rCopy = aSource.Copy() ;
      rCopy.FadeInFadeOut(1,1);
      return rCopy ;
    }

    Chunk ApplyEnvelope( Chunk aSource )
    {
      var lADSR = GetEnvelope(aSource.Wave, aSource.Level);

      lADSR.NormalizeMaxWithPeak();

      int lC = aSource.Wave.Length;
      int lC2 = lADSR.Length;

      float[] lNewSamples = new float[lC];

      for ( int i = 0 ; i < lC ; ++ i )
      {
        float lS = aSource.Wave[i];

        float lE = i < lC2 ? lADSR[i] : 1f;

        float lSE = lS * lE ;

        lNewSamples[i] = lSE;
      }

      DiscreteSignal lR = new DiscreteSignal(X.SamplingRate, lNewSamples) ;

      return new Chunk(lR, aSource.Level);
    }

    public override string Name => "MockWave_ByDuration";  

    Random mRND = new Random();  

    double mGapLevel  = 0.02 ;
    double mZeroLevel = 0.6 ;
    double mOneLevel  = 0.6 ;

    double mGapMinDuration = .5 ; 
    double mGapMaxDuration = 1.5 ;  

    double mZeroMinDuration = .1 ; 
    double mZeroMaxDuration = .3 ;  

    double mOneMinDuration = .5 ; 
    double mOneMaxDuration = .8 ;  

    readonly Params mParams ;

  }
}
