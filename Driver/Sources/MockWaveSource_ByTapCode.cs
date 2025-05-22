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
      int lEstimatedLength = aBits.Count * 10 * SIG.SamplingRate;
      DynamicFloatArray lSamples = new DynamicFloatArray(lEstimatedLength);

      // Leave a gap at the beginning
      double lTime = 0.5 ;

      var lPS = PolybiusSquare.Binary ;
      foreach (var lBit in aBits)
      {
        string lBitStr = lBit?"1":"0";

        var lCode = lPS.Encode(lBitStr);

        var lTPS = new TapCodeSignal(lCode, mParams.BurstDuration, mParams.TapCodeSGap, mParams.TapCodeLGap);

        var lTapEnvelope = lTPS.BuildEnvelope(SIG.SamplingRate);

        var lTap = lTPS.BuildSignal(SIG.SamplingRate,.5);

        if (DContext.Session.Args.GetBool("Plot"))
        {
          string lCodeWaveFile = DContext.Session.LogFile($"TapCodeSignal_{lCode.Row}_{lCode.Col}.wav");
          if ( !File.Exists(lCodeWaveFile))
            lTap.Save(lCodeWaveFile);
        }

        int lIndex = (int)Math.Ceiling(lTime * SIG.SamplingRate) ;

        lSamples.PutRange(lIndex, lTap.Samples);

        lTime += lTap.Duration + mParams.TapCodeSeparation ;
      }

      int lTotalSampleCount = (int)Math.Ceiling(lTime * SIG.SamplingRate) + 1 ;

      DiscreteSignal rModulated = new DiscreteSignal(SIG.SamplingRate, lSamples.ToArray(lTotalSampleCount));

      DiscreteSignal rNoisy = rModulated.AddWHiteNoise(.3);

      return rNoisy;
    }

    public override string Name => "MockWaveSource_ByTapCode";  

    readonly Params mParams ;

  }
}
