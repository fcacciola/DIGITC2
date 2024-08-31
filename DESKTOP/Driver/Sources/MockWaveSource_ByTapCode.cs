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

      lParams.BurstDuration = aArgs.GetOptionalDouble("MaskNoise_BurstDuration").GetValueOrDefault(0.3);

      // This is the SHORT Gap between two taps in a single ROW or COLUMN in a tap code
      lParams.TapCodeSGap = .5 * lParams.BurstDuration ;

      // This is the LONG Gap between the ROW and the COLUMN in a tap code
      lParams.TapCodeLGap =  2 * lParams.BurstDuration ;

      // This is the separation between two tap codes
      lParams.TapCodeSeparation = 5 * lParams.BurstDuration ;

      return new MockWaveSource_ByTapCode(lBaseParams, lParams);
    }

    protected override DiscreteSignal ModulateBits( List<bool> aBits )
    {
      List<DiscreteSignal> lTaps = new List<DiscreteSignal>();

      double lTime = 0 ;

      foreach (var lBit in aBits)
      {
        var lCode = GetCode(lBit);

        var lTPS = new TapCodeSignal(lCode, mParams.BurstDuration, mParams.TapCodeSGap, mParams.TapCodeLGap);

        var lTap = lTPS.BuildSignal(lTime, mSamplingRate);

        lTaps.Add(lTap);

        lTime += lTap.Duration + mParams.TapCodeSeparation ;
      }

      DiscreteSignal rModulated = lTaps.Concatenate(); 

      if (DContext.Session.Args.GetBool("Plot"))
      {
        rModulated.Save( DContext.Session.LogFile($"TapCodeMock.wav"));
      }

      return rModulated;
    }

    TapCode GetZeroCode() => mParams.ZeroCodes[mRND.Next(mParams.ZeroCodes.Count)];
    TapCode GetOneCode()  => mParams.OneCodes [mRND.Next(mParams.OneCodes .Count)];

    TapCode GetCode( bool aBit) => aBit ? GetOneCode() : GetZeroCode();

    public override string Name => "MockWaveSource_ByTapCode";  

    Random mRND = new Random();  

    readonly Params mParams ;

  }
}
