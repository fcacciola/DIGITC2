using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2
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

    public WaveSignal Transform( Func<float,float> Transformation ) 
    {
      float[] lTransformedSamples = new float[Samples.Length];
      for (int i = 0; i < Samples.Length; i++)  
        lTransformedSamples[i] = Transformation(Samples[i]);
      return CopyWith( new DiscreteSignal(SamplingRate, lTransformedSamples) );
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions )
    {
      aRenderer.Render ( ToString(), aOptions );
    }

    public override string ToString()
    {
      return $"[{base.ToString()} Duration:{Rep.Duration:F2} seconds. SampleRate:{Rep.SamplingRate} Samples:[{Utils.ToStr(Rep.Samples)}]";
    }
  }
}
