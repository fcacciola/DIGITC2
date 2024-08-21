using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

using NWaves.Effects.Base;
using NWaves.Filters.Base;
using NWaves.Signals;
using NWaves.Signals.Builders;

namespace DIGITC2_ENGINE
{

public class BurstPulse
{
  public double Duration;
  public int SamplingRate;

  public DiscreteSignal BuildPulse()
  {
    double lSincFreq = 3 / Duration;

    int lLength = MathX.SampleIdx(Duration, SamplingRate) / 2;

    var lSincR = new SincBuilder()
                              .SetParameter("frequency", lSincFreq)

                              .SetParameter("min", 0.5)
                              .SetParameter("max", 0.95)
                              .SampledAt(SamplingRate)
                              .OfLength(lLength)
                              .Build();

    var lSincL = lSincR.Copy();
    lSincL.Reverse();

    var rPulse = lSincL.Concatenate(lSincR);


    if (DIGITC_Context.Session.Args.GetBool("Plot"))
    {
      rPulse.Save(DIGITC_Context.Session.LogFile("_pulse.wav"));
    }


    return rPulse;
  }

}

public class BurstEvent
{
  public double Time;
  public double Duration;

  public double StartTime => Time;
  public double EndTime => Time + Duration;

  public int StartSampleIdx(int aSamplingRate) => (int)Math.Ceiling(StartTime * aSamplingRate);
  public int EndSampleIdx(int aSamplingRate) => (int)Math.Floor(EndTime * aSamplingRate);
}

public class TapCodeEvents
{
  public TapCodeEvents(TapCode aCode, double aBurstDuration,
      double aTapCodeSGap,
          double aTapCodeLGap)
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
      var lEvent = new BurstEvent()
      {
        Time = rTime
                                    ,
        Duration = mBurstDuration
      };

      BurstEvents.Add(lEvent);

      rTime += mBurstDuration + mTapCodeSGap;
    }

    return rTime;
  }


  public double BuildEvents(double aBaseTime)
  {
    double rTime = AddCount(aBaseTime, Code.Row);
    rTime = AddCount(rTime + mTapCodeLGap, Code.Col);
    return rTime;

  }

  double mBurstDuration;
  double mTapCodeSGap;
  double mTapCodeLGap;
}

public class TapCodeMaskSignal
{

  public TapCodeMaskSignal(TapCode aCode
  , double aBurtDuration
  , double aTapCodeSGap
  , double aTapCodeLGap
  , double aTapCodeSeparation)
  {
    mCode = aCode;
    mBurstDuration = aBurtDuration;

    mTapCodeSGap = aTapCodeSGap;
    mTapCodeLGap = aTapCodeLGap;
    mTapCodeSeparation = aTapCodeSeparation;

  }

  public DiscreteSignal BuildSignal(int aSamplingRate, double aDuration)
  {
    BurstPulse lPulse = new BurstPulse() { Duration = mBurstDuration, SamplingRate = aSamplingRate };
    DiscreteSignal lPulseSignal = lPulse.BuildPulse();

    int lLength = MathX.SampleIdx(aDuration, aSamplingRate);
    float[] lSamples = new float[lLength];

    int lTapCount = mCode.Row + mCode.Col;
    double lTapCodeDuration = (lTapCount * mBurstDuration)
                              + ((lTapCount - 2) * mTapCodeSGap) + mTapCodeLGap;

    double lTapCodePeriod = lTapCodeDuration + mTapCodeSeparation;

    int lEventCount = (int)Math.Floor(aDuration / lTapCodePeriod);

    double lTime = 0;

    List<TapCodeEvents> lTapCodeEvents = new List<TapCodeEvents>();
    for (int i = 0; i < lEventCount; ++i)
    {
      var lEvent = new TapCodeEvents(mCode, mBurstDuration, mTapCodeSGap, mTapCodeLGap);

      lEvent.BuildEvents(lTime);

      lTapCodeEvents.Add(lEvent);

      lTime += lTapCodePeriod;
    }

    foreach (TapCodeEvents lTapCodeEvent in lTapCodeEvents)
    {
      foreach (BurstEvent lBurstEvent in lTapCodeEvent.BurstEvents)
      {
        for (int i = lBurstEvent.StartSampleIdx(aSamplingRate), k = 0; i < lBurstEvent.EndSampleIdx(aSamplingRate); i++, k++)
        {
          lSamples[i] = lPulseSignal[k];
        }
      }
    }

    return new DiscreteSignal(aSamplingRate, lSamples);

  }

  TapCode mCode;
  double mBurstDuration;
  double mTapCodeSGap;
  double mTapCodeLGap;
  double mTapCodeSeparation;
}



public sealed class TapCodeMaskNoiseGenerator : NoiseGenerator
{
  public override DiscreteSignal Generate(Args aArgs)
  {
    DIGITC_Context.Setup(new Session("TapCodeMaskNoiseGenerator", aArgs));

    DIGITC_Context.WriteLine("Generate MaskNoise");

    double lDuration = aArgs.GetOptionalDouble("MaskNoise_CarrierDuration").GetValueOrDefault(120);
    double lLevel = aArgs.GetOptionalDouble("MaskNoise_CarrierLevel").GetValueOrDefault(100);
    double lBurstDuration = aArgs.GetOptionalDouble("MaskNoise_BurstDuration").GetValueOrDefault(0.5);
    double lTapCodeSGap = aArgs.GetOptionalDouble("MaskNoise_TapCodeSGap").GetValueOrDefault(0.3);
    double lTapCodeLGap = aArgs.GetOptionalDouble("MaskNoise_TapCodeLGap").GetValueOrDefault(1);
    double lTapCodeSeparation = aArgs.GetOptionalDouble("MaskNoise_TapCodeSeparation").GetValueOrDefault(5);

    var rResult = BuildNoiseCarrier(lDuration, lLevel);

    if (DIGITC_Context.Session.Args.GetBool("Plot"))
    {
      rResult.Save(DIGITC_Context.Session.LogFile($"_carrier.wav"));
    }

    List<TapCode> lCodes = new List<TapCode>(){ new TapCode(2,2)
                                              , new TapCode(10,2)
                                              , new TapCode(2,10)
                                              , new TapCode(10,10)

                                              , new TapCode(6,2)
                                              , new TapCode(2,6)
                                              , new TapCode(10,6)
                                            , new TapCode(6,10) };

    List<DiscreteSignal> lMasks = new List<DiscreteSignal>();

    foreach (var lCode in lCodes)
    {
      var lMask = new TapCodeMaskSignal(lCode
      , lBurstDuration, lTapCodeSGap, lTapCodeLGap, lTapCodeSeparation);

      var lMaskSignal = lMask.BuildSignal(rResult.SamplingRate, lDuration);

      if (DIGITC_Context.Session.Args.GetBool("Plot"))
      {
        lMaskSignal.Save( DIGITC_Context.Session.LogFile($"mask_{lCode.Row}_{lCode.Col}.wav"));
      }

      lMasks.Add(lMaskSignal);
    }

    ModulateCarrier(rResult, lMasks);

    return rResult ;
  }

  DiscreteSignal BuildNoiseCarrier(double aDuration, double aLevel)
  {
    DIGITC_Context.WriteLine("Building Noise Carrier");

    var rNoise = GenerateNoise(aDuration, aLevel);

    return rNoise;

  }

  void ModulateCarrier(DiscreteSignal rCarrier, List<DiscreteSignal> aMasks)
  {
    float lCarrierWeight = .65f;
    float lAllMasksWeights = 1.0f - lCarrierWeight;
    float lSingleMaskWeight = lAllMasksWeights / aMasks.Count;

    for (int i = 0; i < rCarrier.Length; i++)
    {
      float lSample = lCarrierWeight * rCarrier[i];
      float lSign = lSample > 0 ? 1.0f : -1.0f;
      foreach (var lMask in aMasks)
      {
        float lMaskSample = lSign * lSingleMaskWeight * lMask[i];

        lSample += lMaskSample;
      }

      rCarrier[i] = lSample;
    }
  }



}

}
