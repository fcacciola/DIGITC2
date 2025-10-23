using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;

using LowPassFilter = NWaves.Filters.Elliptic.LowPassFilter ;


namespace DIGITC2_ENGINE
{
  public class NoiseFloorGate : WaveFilter
  {
    public class NoiseFloorEstimationParams 
    {
      public float TrimRatio  = 0.05f ;
      public int   Percentile = 10;
    }

    public NoiseFloorGate() 
    { 
    }

    protected override Packet Process ()
    {
      WaveInput.Rep.Sanitize() ;

      var lEnvelopeParams = new Envelope.Args{AttackTime=Params.GetFloat("EnvelopeAttack"), ReleaseTime= Params.GetFloat("EnvelopeRelease") };
      var lEnvelope = Envelope.Apply(WaveInput.Rep, lEnvelopeParams);

      string lEnvelopeLabel = $"Envelope_{lEnvelopeParams}";
      
      Save( lEnvelope, $"{lEnvelopeLabel}.wav" ) ;

      float lFloor = GetNoiseFloor(lEnvelope);

      WriteLine($"Applying Noise Gate at: {lFloor}");

      var lNewSamples = RawApplyGate(lEnvelope.Samples, lFloor);

      var lGated = new DiscreteSignal(SIG.SamplingRate, lNewSamples);

      lGated.Sanitize();

      string lLabel = $"NoiseGate_{lEnvelopeLabel}_{(int)(lFloor*100)}]";

      Save(lGated, $"{lLabel}.wav") ;

      var lES = WaveInput.CopyWith(lGated);
      lES.Name = lLabel;

      return CreateOutput(lES, lLabel);
    }

    float GetNoiseFloor( DiscreteSignal aEnvelope )
    {
      //
      // Along the very first pipeline, a noise floor value is automatically estimated.
      // Then branches are open with varations of that estimation
      float rNF ;

      float? rNF_ = Params.GetOptionalFloat("NoiseFloor");
      if ( rNF_ is null )
      {
        rNF = EstimateBaseline(aEnvelope.Samples, new NoiseFloorEstimationParams() );

        AddBranch("NoiseFloor",rNF * 0.5f) ;
        AddBranch("NoiseFloor",rNF * 1.5f) ;
      }
      else rNF = rNF_.Value ;

      return rNF ;
    }

    static float[] Trim( float[] aSamples, float aTrimRatio )
    {
      int lMargin = (int)(aSamples.Length * aTrimRatio);

      int lNewLen = aSamples.Length - lMargin - lMargin ;

      float[] rR = new float[lNewLen];

      Array.ConstrainedCopy(aSamples, lMargin, rR, 0, lNewLen);

      return rR ; 
    }


    static float EstimateBaseline(float[] aSamples, NoiseFloorEstimationParams aParams )
    {
      Array.Sort(aSamples); 

      var lTrimmed = Trim(aSamples, aParams.TrimRatio);

      float rR = lTrimmed.Percentile(aParams.Percentile);

      return rR ;
    }

    static float[] RawApplyGate(float[] envelope, float aBaseline)
    {
      float[] filtered = new float[envelope.Length];

      for (int i = 0; i < envelope.Length; i++)
      {
        float lN = envelope[i] - aBaseline ;

        filtered[i] = lN < 0 ? 0 : lN ;
      }

      return filtered;
    }

    NoiseFloorEstimationParams mParams = new NoiseFloorEstimationParams();

    public override string Name => this.GetType().Name ;

  }



}

