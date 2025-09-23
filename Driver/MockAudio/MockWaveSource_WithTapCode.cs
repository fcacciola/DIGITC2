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




public class SynthethicBurstPulse
{
  public double BaseDuration;
  public double Temperature ;
  public double BaseLevel ;

  public DiscreteSignal CreateSignal()
  {
    double lDuration = MathX.TERP(BaseDuration, Temperature);

    double lSincFreq = 3 / lDuration;

    int lLength = MathX.SampleIdx(lDuration) / 2;

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
public class TapEvent
{
  public TapEvent( DiscreteSignal aTapSignal, double aTime )
  {
    TapSignal = aTapSignal; 
    Time      = aTime;
  }

  public DiscreteSignal TapSignal ;
  public double         Time;

  public double         Duration => TapSignal.Duration;

  public double StartTime => Time;
  public double EndTime   => Time + Duration;

  public int StartSampleIdx => (int)Math.Ceiling(StartTime * SIG.SamplingRate);
  public int EndSampleIdx   => (int)Math.Floor  (EndTime   * SIG.SamplingRate);

  public void DumpSamples( DynamicFloatArray rSamples )
  {
    for (int i = StartSampleIdx, k = 0; i < EndSampleIdx; i++, k++)
    {
      rSamples[i] = k < TapSignal.Length ? TapSignal[k] : 0 ;
    }
  }
}

public abstract class TapEventBuilder
{
  public abstract TapEvent CreateTapEvent( double aTime ) ;
}

public class TapEventBuilder_Synthetic : TapEventBuilder
{
  public TapEventBuilder_Synthetic( TapCodeSignalBuilderParams aParams )
  {
    mBurstDuration  = aParams.PulseDuration;
    mBurstBaseLevel = aParams.PulseBaseLevel;  
    mTemperature    = aParams.Temperature;
  }

  public override TapEvent CreateTapEvent( double aTime ) 
  {
    SynthethicBurstPulse lPulse = new SynthethicBurstPulse() { BaseDuration = mBurstDuration, Temperature = mTemperature, BaseLevel = mBurstBaseLevel };

    var lTapSignal = lPulse.CreateSignal();

    return new TapEvent( lTapSignal, aTime ); 
  }

  double mBurstDuration ;
  double mBurstBaseLevel ;  
  double mTemperature ; 
}

public class TapEventBuilder_FromSamples : TapEventBuilder
{
  public TapEventBuilder_FromSamples( TapCodeSignalBuilderParams aParams )
  {
    for( int i = 0 ; i < aParams.TapSamplesSequence.Length ; ++  i)
    {
      string lTapSampleFile = $"{aParams.TapSamplesFolder}\\{aParams.TapSamplesSequence[0]}.wav";
      if ( File.Exists(lTapSampleFile) ) 
      {
        var lTapSample = WaveFileSource.Load(lTapSampleFile);
        if ( lTapSample != null ) 
          mTapSamples.Add( lTapSample );
      }
    }
  }

  public override TapEvent CreateTapEvent( double aTime ) 
  {
    DiscreteSignal lTapSignal = mTapSamples[mPicker];

    mPicker = ( mPicker + 1 ) % mTapSamples.Count;  

    return new TapEvent( lTapSignal, aTime ); 
  }

  List<DiscreteSignal> mTapSamples = new List<DiscreteSignal>();

  static int mPicker = 0 ;
}


public class TapCodeEvents
{
  public TapCodeEvents( TapCode         aCode
                      , TapEventBuilder aTapEventBuilder
                      , double          aTapCodeSGap
                      , double          aTapCodeLGap
                      , double          aTemperature
                      )
  {
    Code             = aCode;
    mTapEventBuilder = aTapEventBuilder;
    mTapCodeSGap     = aTapCodeSGap;
    mTapCodeLGap     = aTapCodeLGap;
    mTemperature     = aTemperature;
  }

  public TapCode Code;

  public List<TapEvent> BurstEvents = new List<TapEvent>();

  double AddCount(double aBaseTime, int aCount)
  {
    double rTime = aBaseTime;

    for (int i = 0; i < aCount; ++i)
    {
      var lEvent = mTapEventBuilder.CreateTapEvent(rTime);

      BurstEvents.Add(lEvent);

      rTime += lEvent.Duration + MathX.TERP(mTapCodeSGap,mTemperature);
    }

    return rTime;
  }


  public double BuildEvents()
  {
    double rTime = AddCount(0, Code.Row);
    if ( Code.Col > 0 )
      rTime = AddCount(rTime + MathX.TERP(mTapCodeLGap,mTemperature), Code.Col);
    return rTime;

  }

  TapEventBuilder mTapEventBuilder ;
  double          mTapCodeSGap;
  double          mTapCodeLGap;
  double          mTemperature;
}

public class SingleTapCodeSignalBuilder
{
  public SingleTapCodeSignalBuilder( TapCode         aCode
                                   , TapEventBuilder aTapEventBuilder
                                   , double          aTapCodeSGap
                                   , double          aTapCodeLGap
                                   , double          aTemperature
                                   )
  {
    mCode            = aCode;
    mTapEventBuilder = aTapEventBuilder;
    mTapCodeSGap     = aTapCodeSGap;
    mTapCodeLGap     = aTapCodeLGap;
    mTemperature     = aTemperature;
  }

  public DiscreteSignal BuildEnvelope( int aSamplingRate )
  {
    var lEvents = new TapCodeEvents( mCode, mTapEventBuilder, mTapCodeSGap, mTapCodeLGap, mTemperature);

    double lTotalTime = lEvents.BuildEvents();

    DynamicFloatArray lSamples = new DynamicFloatArray( (int)Math.Ceiling(lTotalTime * aSamplingRate) + 1 ) ;

    lEvents.BurstEvents.ForEach( e => e.DumpSamples(lSamples));

    return new DiscreteSignal(aSamplingRate, lSamples.ToArray() );
  }

  public DiscreteSignal BuildSignal( int aSamplingRate, double aNoiseLevel = .95 )
  {
    var lEnvelope = BuildEnvelope(aSamplingRate);

    var rSignal = NoiseLab.GenerateNoise(lEnvelope.Length, aNoiseLevel);

    NoiseLab.ModulateNoise(rSignal, lEnvelope);

    return rSignal; 

  }

  TapCode         mCode;
  TapEventBuilder mTapEventBuilder;
  double          mTapCodeSGap;
  double          mTapCodeLGap;
  double          mTemperature ;
}


public class TapCodeSignalBuilderParams
{
  public TapCodeSignalBuilderParams( Args aArgs )
  {
    PulseBaseLevel = aArgs.GetDouble("MockAudio_WithTapCode_PulseBaseLevel");

    // This is the duration of a single "Tap Pulse"
    PulseDuration = aArgs.GetOptionalDouble("MockAudio_WithTapCode_TapPulseDuration").GetValueOrDefault(0.1);

    // This is the SHORT Gap between two taps in a single ROW or COLUMN in a tap code
    TapCodeSGap = aArgs.GetDouble("MockAudio_WithTapCode_SGap") ;

    // This is the LONG Gap between the ROW and the COLUMN in a tap code
    TapCodeLGap =  aArgs.GetDouble("MockAudio_WithTapCode_LGap") ;

    // This is the separation between two tap codes
    TapCodeSeparation = aArgs.GetDouble("MockAudio_WithTapCode_Separation") ;

    WhiteNoiseLevel = aArgs.GetOptionalDouble("MockAudio_WithTapCode_WhiteNoiseLevel").GetValueOrDefault(0.3);

    // Randomization parameter. 0 means no randomizarion. 1 means full randomiuzatiion.
    Temperature = aArgs.GetDouble("MockAudio_WithTapCode_Temperature");

    // Folder with .wav files of various Tap samples
    TapSamplesFolder = aArgs.GetPath("MockAudio_WithTapCode_TapSamplesFolder") ;

    TapSamplesSequence = aArgs.Get("MockAudio_WithTapCode_TapSamplesSequence") ;
  }

  public double PulseDuration ;
  public double PulseBaseLevel ;  
  public double TapCodeSGap ;
  public double TapCodeLGap ;
  public double TapCodeSeparation ;
  public double WhiteNoiseLevel ;
  public double Temperature ;  
  public string TapSamplesFolder ;
  public string TapSamplesSequence ;
}


class TapCodeSignalBuilder
{
  public TapCodeSignalBuilder( TapCodeSignalBuilderParams aParams, TapEventBuilder aTapEventBuilder, int aEstimatedLength )
  {
    mTapEventBuilder = aTapEventBuilder;
    mParams  = aParams ;
    mSamples = new DynamicFloatArray(aEstimatedLength); 
    mTime    = 0.5 ; // Leave a gap at the beginning
  }

  public void AddCode ( TapCode aCode )
  {
    var lTPS = new SingleTapCodeSignalBuilder( aCode
                                             , mTapEventBuilder
                                             , mParams.TapCodeSGap
                                             , mParams.TapCodeLGap
                                             , mParams.Temperature
                                             );

    var lTap = lTPS.BuildSignal(SIG.SamplingRate,.5);

    int lIndex = (int)Math.Ceiling(mTime * SIG.SamplingRate) ;

    mSamples.PutRange(lIndex, lTap.Samples);

    mTime += lTap.Duration + MathX.TERP(mParams.TapCodeSeparation, mParams.Temperature) ;
  }

  public DiscreteSignal GetSignal()
  {
    int lTotalSampleCount = (int)Math.Ceiling(mTime * SIG.SamplingRate) + 1 ;

    return new DiscreteSignal(SIG.SamplingRate, mSamples.ToArray(lTotalSampleCount));
  }

  TapCodeSignalBuilderParams mParams ;
  TapEventBuilder            mTapEventBuilder ;
  DynamicFloatArray          mSamples ;

  double mTime ;
}

public abstract class MockWaveSource_WithTapCode_Base : MockWaveSource_FromBits
{
  protected MockWaveSource_WithTapCode_Base( BaseParams                 aBaseParams
                                           , TapCodeSignalBuilderParams aParams
                                           , TapEventBuilder            aTapEventBuilder ) : base(aBaseParams)
  {
    mParams          = aParams;
    mTapEventBuilder = aTapEventBuilder;
  }

  protected override DiscreteSignal ModulateBytes( List<byte> aBytes )
  {
    var lPS = PolybiusSquare.Binary ;

    int lEstimatedLength = aBytes.Count * 80 * SIG.SamplingRate;

   var lSignalBuilder = new TapCodeSignalBuilder(mParams, mTapEventBuilder, lEstimatedLength);

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

  
  TapCodeSignalBuilderParams mParams ;
  TapEventBuilder            mTapEventBuilder ;
}


public class MockWaveSource_WithTapCode_Synthetic : MockWaveSource_WithTapCode_Base
{
  public MockWaveSource_WithTapCode_Synthetic( BaseParams aBaseParams, TapCodeSignalBuilderParams aParams ) : base(aBaseParams, aParams, new TapEventBuilder_Synthetic(aParams) )
  {
  }

  public static MockWaveSource_WithTapCode_Synthetic FromText( Args aArgs, string aText )
  {
    var lBaseParams = new BaseParams(){Text=aText} ;

    var lParams = new TapCodeSignalBuilderParams(aArgs) ;

    return new MockWaveSource_WithTapCode_Synthetic(lBaseParams, lParams);
  }

  public override string Name => this.GetType().Name ;  
}

public class MockWaveSource_WithTapCode_FromSamples : MockWaveSource_WithTapCode_Base
{
  public MockWaveSource_WithTapCode_FromSamples( BaseParams aBaseParams, TapCodeSignalBuilderParams aParams ) : base(aBaseParams, aParams, new TapEventBuilder_FromSamples(aParams) )
  {
  }

  public static MockWaveSource_WithTapCode_FromSamples FromText( Args aArgs, string aText )
  {
    var lBaseParams = new BaseParams(){Text=aText} ;

    var lParams = new TapCodeSignalBuilderParams(aArgs) ;

    return new MockWaveSource_WithTapCode_FromSamples(lBaseParams, lParams);
  }

  public override string Name => this.GetType().Name ;  
}

}
