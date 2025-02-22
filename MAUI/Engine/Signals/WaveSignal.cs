using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Utils;
using NWaves.Windows;

namespace DIGITC2_ENGINE
{
  public class WaveSignal : Signal
  {
    public WaveSignal( DiscreteSignal aRep ) : base()
    { 
      Rep = aRep ;
    }

    public DiscreteSignal Rep ;
    
    public double  Duration     => Rep.Duration ;
    public int     SamplingRate => Rep.SamplingRate ;
    public float[] Samples      => Rep.Samples ; 

    public WaveSignal CopyWith( DiscreteSignal aDS )
    {
      WaveSignal rCopy = new WaveSignal(aDS);
      rCopy.Assign(this); 
      return rCopy ;
    }

    public override Signal Copy() => CopyWith(Rep.Copy());      

    public float ComputeMax() => Rep.Samples.Max();

    public override Distribution GetDistribution()
    {
      List<Sample> lSamples = new List<Sample>();
      for (int i = 0; i < Samples.Length; ++i)
        lSamples.Add( new Sample( new WaveValueSampleSource(i), Samples[i]) ) ;
      return new Distribution(lSamples);
    }

    public void SaveTo( Stream aStream ) 
    {
      var lWF = new WaveFile(Rep);
      lWF.SaveTo( aStream );  
    }

    public void SaveTo( string aFilename )  
    {
      using (var lStream = new FileStream(aFilename, FileMode.OpenOrCreate, FileAccess.Write))
        SaveTo( lStream );  
    }

    public WaveSignal Transform(Func<float, float> Transformation)
    {
      float[] lTransformedSamples = new float[Samples.Length];
      for (int i = 0; i < Samples.Length; i++)
        lTransformedSamples[i] = Transformation(Samples[i]);
      return CopyWith(new DiscreteSignal(SamplingRate, lTransformedSamples));
    }

    public WaveSignal ZeroPaddedToNextPowerOfTwo()
    {
      int lProperLen = MathUtils.NextPowerOfTwo(Rep.Length);
      if ( Rep.Length < lProperLen)
      {
        float[] lPadded = new float[lProperLen];
        Rep.Samples.FastCopyTo(lPadded, Rep.Length);
        return new WaveSignal( new DiscreteSignal(Rep.SamplingRate, lPadded) ) ;
      }
      else
      {
       return this ;
      }
    }

    public List<WaveSignal> SplitInFrames( int aFrameSize = 2048, int aHopSize = 2048, WindowType aWindowType = WindowType.Rectangular)
    {
      List<WaveSignal> rR = new List<WaveSignal>();

      int lProperFrameSize = MathUtils.NextPowerOfTwo(aFrameSize);

      var lCompleteFramesCount = Rep.Length >= aFrameSize ? (Rep.Length - lProperFrameSize) / aHopSize + 1 : 0;
            
      var lPos = 0;

      float[] lWindowSamples = null;
      
      if (aWindowType != WindowType.Rectangular)
        lWindowSamples = Window.OfType(aWindowType, lProperFrameSize);

      for (var i = 0; i < lCompleteFramesCount; lPos += aHopSize, i++)
      {
        var lCFrameBuffer = new float[lProperFrameSize];

        Rep.Samples.FastCopyTo(lCFrameBuffer, lProperFrameSize, lPos);

        if (aWindowType != WindowType.Rectangular)
          lCFrameBuffer.ApplyWindow(lWindowSamples);

        rR.Add( CopyWith(new DiscreteSignal(SamplingRate, lCFrameBuffer)) ) ;
      }

      var lFrameBuffer = new float[lProperFrameSize];
      Rep.Samples.FastCopyTo(lFrameBuffer, Rep.Length - lPos, lPos);
      if (aWindowType != WindowType.Rectangular)
        lFrameBuffer.ApplyWindow(lWindowSamples);

      rR.Add( CopyWith(new DiscreteSignal(SamplingRate, lFrameBuffer)) );

      return rR ;
    }

    protected override void UpdateState( State rS ) 
    {
      rS.Add( State.With("Duration"    , Duration));
      rS.Add( State.With("SamplingRate", SamplingRate));
      rS.Add( State.With("SamplingRate", Samples));
    }
  }

  public static class DiscreteSignalExtensions2
  {
    public static void NormalizeMax2(this DiscreteSignal signal)
    {
        var norm = .98f/ signal.Samples.Max(s => Math.Abs(s));

        signal.Amplify(norm);
    }

    public static DiscreteSignal WeightedSuperimpose( this DiscreteSignal aS1, DiscreteSignal aS2, float aW1 = 1.0f, float aW2 = 1.0f ) 
    {
      Guard.AgainstInequality(aS1.SamplingRate, aS2.SamplingRate, "Sampling rate of aS1", "sampling rate of aS2");

      DiscreteSignal superimposed;

      if (aS1.Length >= aS2.Length)
      {
          superimposed = aS1.Copy();

          for (var i = 0; i < aS2.Length; i++)
          {
              superimposed[i] = aS1.Samples[i] * aW1 + aS2.Samples[i] * aW2;
          }
      }
      else
      {
          superimposed = aS2.Copy();

          for (var i = 0; i < aS1.Length; i++)
          {
              superimposed[i] = aS2.Samples[i] * aW2 + aS1.Samples[i] * aW1;
          }
      }

      superimposed.NormalizeMax2(); // Normalized BUT in [-.98,.98] because some soft like Audacity has trouble when the calculations endup rounding on ULP above or below 1

      return superimposed ;

    }

    public static DiscreteSignal ModulateWithRandomEnvelope( this DiscreteSignal aSignal )
    {
      var lRandom = new Random();
      float[] lAmplitudeEnvelope = new float[aSignal.Length];
      float[] lModulated         = new float[aSignal.Length];

      // Create a varying amplitude envelope
      for (int i = 0; i < lAmplitudeEnvelope.Length; i++)
      {
        lAmplitudeEnvelope[i] = (float)(0.5 + (lRandom.NextDouble() / 2 ) );
      }

      // Apply the amplitude envelope to the white noise
      for (int i = 0; i < aSignal.Samples.Length; i++)
      {
        lModulated[i] = aSignal.Samples[i] * lAmplitudeEnvelope[i];
      }

      return new DiscreteSignal(aSignal.SamplingRate, lModulated);
    }

    public static DiscreteSignal AddWHiteNoise(this DiscreteSignal aSignal, double aLevel)
    {
      var lLength = aSignal.Length;
      var lNoise = new WhiteNoiseBuilder()
                      .SetParameter("min", -aLevel)
                      .SetParameter("max", aLevel)
                      .OfLength(lLength)
                      .SampledAt(aSignal.SamplingRate)
                      .Build();


      return aSignal.WeightedSuperimpose(lNoise.ModulateWithRandomEnvelope());
    }

  }
}
