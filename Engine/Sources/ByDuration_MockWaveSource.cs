using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

namespace DIGITC2
{

  public class ByDuration_MockWaveSource : WaveSource
  {
    public class Params
    {
      public string Text ;
      public double EnvelopeAttackTime ;
      public double EnvelopeReleaseTime ;
      public double AmplitudeGateThreshold ;
      public double ExtractGatedlSymbolsMinDuration ;
      public double ExtractGatedlSymbolsMergeGap ;
      public double BinarizeByDurationThreshold ;

      public int    BitsPerByte  = 8;
      public bool   LittleEndian = true;
      public string CharSet      = "us-ascii";
    }

    public ByDuration_MockWaveSource( Params aParams ) 
    {
      mParams = aParams ;
    }

    public static ByDuration_MockWaveSource FromText( string aText )
    {
      var lParams = new Params() ;
      lParams.Text = aText ;

      return new ByDuration_MockWaveSource(lParams);
    }

    protected override Signal DoCreateSignal()
    {
      if ( mSignal == null ) 
      {
        var lChars = TextToChars(mParams.Text);
        var lBytes = CharsToBytes(lChars); 
        var lBits  = BytesToBits(lBytes);

        var lWave = ModulateBits(lBits);

        lWave.NormalizeMax();

        SaveTo(lWave, Context.Session.LogFile( "Wave.wav"));

        mSignal = new WaveSignal(lWave);
      }

      return mSignal ;
    }

    char[] TextToChars( string aText )
    {
      return aText.ToCharArray() ;  
    }

    List<byte> CharsToBytes( char[] aChars) 
    {
      Encoding lEncoding = Encoding.GetEncoding( mParams.CharSet);
      List<byte> rBytes = new List<byte>();
      char[] lBuffer = new char[1];
      foreach( char lChar in aChars )
      {
        lBuffer[0]=lChar;
        rBytes.AddRange( lEncoding.GetBytes(lBuffer) ) ;
      }
      return rBytes ;
    }

    List<bool> BytesToBits( List<byte> aBytes )
    {
      List<bool> rBits = new List<bool>();

      byte[] lBuffer = new byte[1];

      foreach( byte lByte in aBytes ) 
      {
        lBuffer[0]=lByte; 

        BitArray lBA = new BitArray( lBuffer ); 

        int lC = lBA.Length;

        if ( mParams.LittleEndian )
        {
          for ( int i = lC - 1 ; i >= 0 ; -- i ) 
            rBits.Add(lBA[i]);
   
        }
        else
        {
          for ( int i = 0; i < lC; ++ i ) 
            rBits.Add(lBA[i]);
        }
      }

      return rBits ;

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

      return new DiscreteSignal(mSamplingRate, lAllSamples);
    }

    DiscreteSignal ModulateBits( List<bool> aBits )
    {
      List<Chunk> lChunks = BuildChunks( aBits );

      return ConcatenateChunks( lChunks );  
    }

    DiscreteSignal GetNoise( double aDuration, double aLevel )
    {
      int lLength = (int)Math.Ceiling(aDuration * mSamplingRate) ;

      return new WhiteNoiseBuilder()
                  .SetParameter("min", - aLevel)
                  .SetParameter("max",   aLevel)
                  .OfLength(lLength)
                  .SampledAt(mSamplingRate)
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

      lADSR.NormalizeMax();

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

      DiscreteSignal lR = new DiscreteSignal(mSamplingRate, lNewSamples) ;

      return new Chunk(lR, aSource.Level);
    }

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

    int mSamplingRate => 44100 ;

    readonly Params mParams ;

  }
}
