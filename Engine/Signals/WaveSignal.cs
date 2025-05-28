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
  public static class SIG
  {
    public static int SamplingRate = 44100 ;

    static public double ToDigitalFrequency(double aFrequencyInHerz) => aFrequencyInHerz / ( 0.5 * SamplingRate ) ;

    static public double ToNormalizedDigitalFrequency(double aFrequencyInHerz) => aFrequencyInHerz /  SamplingRate ;
  }

  public class WaveSignal : Signal
  {
    public WaveSignal( DiscreteSignal aRep ) : base()
    { 
      Rep = aRep ;
    }

    public DiscreteSignal Rep ;
    
    public double  Duration => Rep.Duration ;
    public float[] Samples  => Rep.Samples ; 

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

    public void SaveTo( Stream aStream   ) => Rep.SaveTo(aStream);
    public void SaveTo( string aFilename ) => Rep.SaveTo(aFilename);

    public WaveSignal Transform(Func<float, float> Transformation)
    {
      float[] lTransformedSamples = new float[Samples.Length];
      for (int i = 0; i < Samples.Length; i++)
        lTransformedSamples[i] = Transformation(Samples[i]);
      return CopyWith(new DiscreteSignal(SIG.SamplingRate, lTransformedSamples));
    }

    public WaveSignal ZeroPaddedToNextPowerOfTwo()
    {
      int lProperLen = MathUtils.NextPowerOfTwo(Rep.Length);
      if ( Rep.Length < lProperLen)
      {
        float[] lPadded = new float[lProperLen];
        Rep.Samples.FastCopyTo(lPadded, Rep.Length);
        return new WaveSignal( new DiscreteSignal(SIG.SamplingRate, lPadded) ) ;
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

        rR.Add( CopyWith(new DiscreteSignal(SIG.SamplingRate, lCFrameBuffer)) ) ;
      }

      var lFrameBuffer = new float[lProperFrameSize];
      Rep.Samples.FastCopyTo(lFrameBuffer, Rep.Length - lPos, lPos);
      if (aWindowType != WindowType.Rectangular)
        lFrameBuffer.ApplyWindow(lWindowSamples);

      rR.Add( CopyWith(new DiscreteSignal(SIG.SamplingRate, lFrameBuffer)) );

      return rR ;
    }
  }

  public static class DiscreteSignalExtensions2
  {
    public static void SaveTo( this DiscreteSignal aDS, Stream aStream ) 
    {
      var lWF = new WaveFile(aDS);
      lWF.SaveTo( aStream );  
    }

    public static void SaveTo( this DiscreteSignal aDS, string aFilename )  
    {
      DContext.WriteLine($"Saving signal to file: [{aFilename}]");

      using (var lStream = new FileStream(aFilename, FileMode.OpenOrCreate, FileAccess.Write))
        aDS.SaveTo( lStream );  
    }

    public static void ClampOutliers(this DiscreteSignal signal, float aFloor = 1e-4f, float aCeiling = .99f)
    {
      for (int i = 0; i < signal.Samples.Length; i++)
      {
        if (signal.Samples[i] < aFloor)
          signal.Samples[i] = aFloor;
        else if (signal.Samples[i] > aCeiling)
          signal.Samples[i] = aCeiling;
      } 
    }

    /// <summary>
    /// Get the average of the N highest peaks, where N is the order (default 3) 
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="aOrder">The number of highest peaks to average</param>
    /// <returns></returns>
    public static float GetPeak(this DiscreteSignal signal, int aOrder = 3)
    {
      var lOrdered = signal.Samples.OrderByDescending( s => s ).ToList() ;

      float[] lPercentiles = new float[aOrder]; 

      for (int i = 0; i < aOrder; i++)
        lPercentiles[i] = lOrdered[i];

      return lPercentiles.Average();
    }

    /// <summary>
    /// Normalize the signal to the max of the N lowest peaks, where N is the order (default 3)
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="aOrder">The number of highest peaks to average</param>
    /// <param name="aRange">The normalization range. Default is .99 so that no values are at 1.0 which can cause some issues.</param>
    public static void NormalizeMaxWithPeak(this DiscreteSignal signal, int aOrder = 3, float aRange = 0.99f)
    {
      float lPeak = signal.GetPeak(aOrder);  

      var norm = aRange/ lPeak;

      signal.Amplify(norm);
    }


    /// <summary>
    /// Clamps a signal to the range [aFloor, aCeiling], then
    /// normalizes it to the max of the N lowest peaks, where N is the order (default 3),
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="aFloor"></param>
    /// <param name="aCeiling"></param>
    /// <param name="aOrder"></param>
    public static void Sanitize(this DiscreteSignal signal, float aFloor = 1e-4f, float aCeiling = .99f, int aOrder = 3)
    {
      signal.ClampOutliers(aFloor, aCeiling); 
      signal.NormalizeMaxWithPeak(aOrder, aCeiling);
    }

    /// <summary>
    /// Rectifies by squaring <paramref name="signal"/> in-place.
    /// </summary>
    /// <param name="signal">Signal</param>
    public static void SquareRectify(this DiscreteSignal signal)
    {
      for (var i = 0; i < signal.Length; i++)
      {
        signal[i] = signal[i] * signal[i];
      }
    }


    public static DiscreteSignal WeightedSuperimpose( this DiscreteSignal aS1, DiscreteSignal aS2, float aW1 = 1.0f, float aW2 = 1.0f ) 
    {
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

      superimposed.NormalizeMaxWithPeak(); // Normalized BUT in [-.98,.98] because some soft like Audacity has trouble when the calculations endup rounding on ULP above or below 1

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

      return new DiscreteSignal(SIG.SamplingRate, lModulated);
    }

    public static DiscreteSignal AddWHiteNoise(this DiscreteSignal aSignal, double aLevel)
    {
      var lLength = aSignal.Length;
      var lNoise = new WhiteNoiseBuilder()
                      .SetParameter("min", -aLevel)
                      .SetParameter("max", aLevel)
                      .OfLength(lLength)
                      .SampledAt(SIG.SamplingRate)
                      .Build();


      return aSignal.WeightedSuperimpose(lNoise.ModulateWithRandomEnvelope());
    }

  }
}
