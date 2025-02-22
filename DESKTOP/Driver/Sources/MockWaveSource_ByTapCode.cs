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

  public class MockWaveSource_ByTapCode : MockWaveSource_FromBits
  {
    public class Params
    {
      public List<TapCode> ZeroCodes = new List<TapCode>{ new TapCode(1,1)
                                                        , new TapCode(1,3)
                                                        , new TapCode(3,1)
                                                        , new TapCode(3,3)}; 

      public List<TapCode> OneCodes  = new List<TapCode>{ new TapCode(1,2)
                                                        , new TapCode(2,1)
                                                        , new TapCode(2,3)
                                                        , new TapCode(3,2)};

      public double BurstDuration ;
      public double TapCodeSGap ;
      public double TapCodeLGap ;
      public double TapCodeSeparation ;
    }

    public MockWaveSource_ByTapCode( BaseParams aBaseParams, Params aParams ) : base(aBaseParams)
    {
      mParams = aParams ;
    }

    public static MockWaveSource_ByTapCode FromText( Args aArgs, string aText )
    {
      var lBaseParams = new BaseParams() ;
      lBaseParams.Text = aText ;

      var lParams = new Params() ;

      lParams.BurstDuration = aArgs.GetOptionalDouble("MaskNoise_BurstDuration").GetValueOrDefault(0.1);

      // This is the SHORT Gap between two taps in a single ROW or COLUMN in a tap code
      lParams.TapCodeSGap = .3 * lParams.BurstDuration ;

      // This is the LONG Gap between the ROW and the COLUMN in a tap code
      lParams.TapCodeLGap =  2 * lParams.BurstDuration ;

      // This is the separation between two tap codes
      lParams.TapCodeSeparation = 5 * lParams.BurstDuration ;

      return new MockWaveSource_ByTapCode(lBaseParams, lParams);
    }

    protected override DiscreteSignal ModulateBits( List<bool> aBits )
    {
      int lEstimatedLength = aBits.Count * 10 * mSamplingRate;
      DynamicFloatArray lSamples = new DynamicFloatArray(lEstimatedLength);

      // Leave a gap at the beginning
      double lTime = 0.5 ;

      foreach (var lBit in aBits)
      {
        var lCode = GetCode(lBit);

        var lTPS = new TapCodeSignal(lCode, mParams.BurstDuration, mParams.TapCodeSGap, mParams.TapCodeLGap);

        var lTapEnvelope = lTPS.BuildEnvelope(mSamplingRate);

        var lTap = lTPS.BuildSignal(mSamplingRate,.5);

        if (DContext.Session.Args.GetBool("Plot"))
        {
          string lCodeWaveFile = DContext.Session.LogFile($"TapCodeSignal_{lCode.Row}_{lCode.Col}.wav");
          if ( !File.Exists(lCodeWaveFile))
            lTap.Save(lCodeWaveFile);
        }

        int lIndex = (int)Math.Ceiling(lTime * mSamplingRate) ;

        lSamples.PutRange(lIndex, lTap.Samples);

        lTime += lTap.Duration + mParams.TapCodeSeparation ;
      }

      int lTotalSampleCount = (int)Math.Ceiling(lTime * mSamplingRate) + 1 ;

      DiscreteSignal rModulated = new DiscreteSignal(mSamplingRate, lSamples.ToArray(lTotalSampleCount));

      DiscreteSignal rNoisy = rModulated.AddWHiteNoise(.3);

      return rNoisy;
    }

    //TapCode GetZeroCode() => mParams.ZeroCodes[mRND.Next(mParams.ZeroCodes.Count)];
    //TapCode GetOneCode()  => mParams.OneCodes [mRND.Next(mParams.OneCodes .Count)];
    TapCode GetZeroCode() => mParams.ZeroCodes[0];
    TapCode GetOneCode()  => mParams.OneCodes [0];

    TapCode GetCode( bool aBit) => aBit ? GetOneCode() : GetZeroCode();

    public override string Name => "MockWaveSource_ByTapCode";  

    Random mRND = new Random();  

    readonly Params mParams ;

  }
}
