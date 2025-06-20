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

public class DynamicFloatArray
{
  public DynamicFloatArray( int aEstimatedCapacity )    
  {
    mBuffer = new float[aEstimatedCapacity]; 
  }

  public float this[int index]
  {
    get { return Get(index); }
    set { Put(index,value); }
  }

  public int PutRange( int index, IEnumerable<float> values )
  {
    foreach(var lValue in values)
    {
      Put(index++, lValue);
    }

    return index;
  }

  public float[] ToArray( int aTotalLength = -1 )
  {
    if (aTotalLength == -1)
      return mBuffer;

    float[] rResult = new float[aTotalLength];

    Array.Copy(mBuffer, rResult, aTotalLength);

    return rResult;
  }

  float Get( int index)
  {
    if ( index >= mBuffer.Length )
      Allocate(index);

    return mBuffer[index];
  }

  void Put( int index, float aV )
  {
    if ( index >= mBuffer.Length )
      Allocate(index);

    mBuffer[index] = aV;
     
  }

  void Allocate(int aIndex)
  {
    int lNewLength = Math.Max(aIndex + 1, mBuffer.Length * 2);

    float[] lNewBuffer = new float[lNewLength];

    Array.Copy(mBuffer, lNewBuffer, mBuffer.Length);

    mBuffer = lNewBuffer;
  }

  float[] mBuffer ;
}



public class BurstPulse
{
  public double Duration;
  public double Temperature ;
  public double BaseLevel ;

  public DiscreteSignal BuildPulse()
  {
    double lSincFreq = 3 / Duration;

    int lLength = MathX.SampleIdx(Duration) / 2;

    double lL  = BaseLevel ;
    double lHH = 0.99 ;

    double lH = MathX.RERP(lL, lHH, Temperature);

    var lSincR = new SincBuilder()
                              .SetParameter("frequency", lSincFreq)
                              .SetParameter("min", lL)
                              .SetParameter("max", lH)
                              .SampledAt(SIG.SamplingRate)
                              .OfLength(lLength)
                              .Build();

    var lSincL = lSincR.Copy();
    lSincL.Reverse();

    var rPulse = lSincL.Concatenate(lSincR);

    return rPulse;
  }
}

public class BurstEvent
{
  public double Time;
  public double Duration;

  public double StartTime => Time;
  public double EndTime => Time + Duration;

  public int StartSampleIdx => (int)Math.Ceiling(StartTime * SIG.SamplingRate);
  public int EndSampleIdx   => (int)Math.Floor  (EndTime   * SIG.SamplingRate);
}

public class TapCodeEvents
{
  public TapCodeEvents( TapCode aCode
                      , double  aBurstDuration
                      , double  aTapCodeSGap
                      , double  aTapCodeLGap
                      )
  {
    Code = aCode;

    mBurstDuration = aBurstDuration;

    mTapCodeSGap = aTapCodeSGap;
    mTapCodeLGap = aTapCodeLGap;
  }

  public TapCode Code;

  public List<BurstEvent> BurstEvents = new List<BurstEvent>();

  double AddCount(double aBaseTime, int aCount)
  {
    double rTime = aBaseTime;

    for (int i = 0; i < aCount; ++i)
    {
      var lEvent = new BurstEvent() { Time = rTime, Duration = mBurstDuration };

      BurstEvents.Add(lEvent);

      rTime += mBurstDuration + mTapCodeSGap;
    }

    return rTime;
  }


  public double BuildEvents(double aBaseTime)
  {
    double rTime = AddCount(aBaseTime, Code.Row);
    if ( Code.Col > 0 )
      rTime = AddCount(rTime + mTapCodeLGap, Code.Col);
    return rTime;

  }

  double mBurstDuration;
  double mTapCodeSGap;
  double mTapCodeLGap;
}

public class TapCodeSignal
{
  public TapCodeSignal( TapCode aCode
                      , double  aBurtDuration
                      , double  aBurstBaseLevel
                      , double  aTapCodeSGap
                      , double  aTapCodeLGap
                      , double  aTemperature
                      )
  {
    mCode           = aCode;
    mBurstDuration  = aBurtDuration;
    mBurstBaseLevel = aBurstBaseLevel ;
    mTapCodeSGap    = aTapCodeSGap;
    mTapCodeLGap    = aTapCodeLGap;
    mTemperature    = aTemperature;
  }

  public DiscreteSignal BuildEnvelope( int aSamplingRate)
  {
    BurstPulse lPulse = new BurstPulse() { Duration = mBurstDuration, Temperature = mTemperature, BaseLevel = mBurstBaseLevel };

    var lEvent = new TapCodeEvents(mCode, mBurstDuration, mTapCodeSGap, mTapCodeLGap);

    double lTotalTime = lEvent.BuildEvents(0);

    DynamicFloatArray lSamples = new DynamicFloatArray( (int)Math.Ceiling(lTotalTime * aSamplingRate) + 1 ) ;

    foreach (BurstEvent lBurstEvent in lEvent.BurstEvents)
    {
      DiscreteSignal lPulseSignal = lPulse.BuildPulse();

      for (int i = lBurstEvent.StartSampleIdx, k = 0; i < lBurstEvent.EndSampleIdx; i++, k++)
      {
        lSamples[i] = lPulseSignal[k] ;
      }
    }

    return new DiscreteSignal(aSamplingRate, lSamples.ToArray() );
  }

  public DiscreteSignal BuildSignal( int aSamplingRate, double aNoiseLevel = .95 )
  {
    var lEnvelope = BuildEnvelope(aSamplingRate);

    var rSignal = NoiseLab.GenerateNoise(lEnvelope.Length, aNoiseLevel);

    NoiseLab.ModulateNoise(rSignal, lEnvelope);

    return rSignal; 

  }

  TapCode mCode;
  double  mBurstDuration;
  double  mBurstBaseLevel ;
  double  mTapCodeSGap;
  double  mTapCodeLGap;
  double  mTemperature ;
}


  public class MockWaveSource_WithTapCode : MockWaveSource_FromBits
  {
    public class Params
    {
      public double BurstDuration ;
      public double BurstBaseLevel ;  
      public double TapCodeSGap ;
      public double TapCodeLGap ;
      public double TapCodeSeparation ;
      public double WhiteNoiseLevel ;
      public double Temperature ;  
    }

    public MockWaveSource_WithTapCode( BaseParams aBaseParams, Params aParams ) : base(aBaseParams)
    {
      mParams = aParams ;
    }

    public static MockWaveSource_WithTapCode FromText( Args aArgs, string aText )
    {
      var lBaseParams = new BaseParams() ;
      lBaseParams.Text = aText ;

      var lParams = new Params() ;

      // Randomization parameter. 0 means no randomizarion. 1 means full randomiuzatiion.
      lParams.Temperature = aArgs.GetDouble("MockAudio_WithTapCode_Temperature");

      lParams.BurstBaseLevel = aArgs.GetDouble("MockAudio_WithTapCode_BurstBaseLevel");

      lParams.BurstDuration = aArgs.GetOptionalDouble("MockAudio_WithTapCode_TapBurstDuration").GetValueOrDefault(0.1);

      // This is the SHORT Gap between two taps in a single ROW or COLUMN in a tap code
      lParams.TapCodeSGap = .3 * lParams.BurstDuration ;

      // This is the LONG Gap between the ROW and the COLUMN in a tap code
      lParams.TapCodeLGap =  2 * lParams.BurstDuration ;

      // This is the separation between two tap codes
      lParams.TapCodeSeparation = 5 * lParams.BurstDuration ;

      lParams.WhiteNoiseLevel = aArgs.GetOptionalDouble("MockAudio_WithTapCode_WhiteNoiseLevel").GetValueOrDefault(0.7);

      return new MockWaveSource_WithTapCode(lBaseParams, lParams);
    }

    class TapCodeSignalBuilder
    {
      public TapCodeSignalBuilder( Params aParams, int aEstimatedLength )
      {
        mParams  = aParams ;
        mSamples = new DynamicFloatArray(aEstimatedLength); 
        mTime    = 0.5 ; // Leave a gap at the beginning
      }

      public void AddCode ( TapCode aCode )
      {
        var lTPS = new TapCodeSignal( aCode
                                    , mParams.BurstDuration
                                    , mParams.BurstBaseLevel
                                    , mParams.TapCodeSGap
                                    , mParams.TapCodeLGap
                                    , mParams.Temperature
                                    );

        var lTapEnvelope = lTPS.BuildEnvelope(SIG.SamplingRate);

        var lTap = lTPS.BuildSignal(SIG.SamplingRate,.5);

        int lIndex = (int)Math.Ceiling(mTime * SIG.SamplingRate) ;

        mSamples.PutRange(lIndex, lTap.Samples);

        mTime += lTap.Duration + mParams.TapCodeSeparation ;
      }

      public DiscreteSignal GetSignal()
      {
        int lTotalSampleCount = (int)Math.Ceiling(mTime * SIG.SamplingRate) + 1 ;

        return new DiscreteSignal(SIG.SamplingRate, mSamples.ToArray(lTotalSampleCount));
      }

      readonly Params            mParams ;
      readonly DynamicFloatArray mSamples ;

      double mTime ;
    }

    protected override DiscreteSignal ModulateBytes( List<byte> aBytes )
    {
      var lPS = PolybiusSquare.Binary ;

      int lEstimatedLength = aBytes.Count * 80 * SIG.SamplingRate;

      var lSignalBuilder = new TapCodeSignalBuilder(mParams, lEstimatedLength);

      TapCode lByteSeparatorCode = new TapCode(6,0);

      foreach( byte lByte in aBytes ) 
      {
        var lBits = ByteToBits(lByte) ;

        foreach (var lBit in lBits)
        {
          string lBitStr = lBit?"1":"0";

          var lCode = lPS.Encode(lBitStr);

          lSignalBuilder.AddCode(lCode);
        }
        lSignalBuilder.AddCode(lByteSeparatorCode);
      }

      DiscreteSignal rModulated = lSignalBuilder.GetSignal();

      DiscreteSignal rNoisy = rModulated.AddWHiteNoise(mParams.WhiteNoiseLevel);

      return rNoisy;
    }

    public override string Name => this.GetType().Name ;  

    readonly Params mParams ;

  }
}
