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

    public override List<Signal> Slice( Context aContext = null ) 
    { 
      List<Signal> rList = new List<Signal> ();

      if ( aContext?.WindowSizeInSeconds > 0 )
      {
        int lOriginalLength = Samples.Length;
        int lSegmentLength  = (int)(SamplingRate * aContext.WindowSizeInSeconds);

        int k = 0 ;
        do
        {
          float[] lSegmentSamples = new float[lSegmentLength];  
          for (int i = 0; i < lSegmentLength && k < lOriginalLength ; i++, k++)
            lSegmentSamples[i] = Samples[k];  

          var lSlice = new WaveSignal( new DiscreteSignal(SamplingRate,lSegmentSamples) );
          lSlice.Assign( this );
          lSlice.SliceIdx = rList.Count;

          rList.Add (lSlice);
        }
        while ( k < lOriginalLength );  
      }
      else
      {
        rList.Add(this);
      }

      return rList;    
    }  

    public override Signal MergeWith( IEnumerable<Signal> aSlices, Context aContext = null ) 
    { 
      if ( aSlices.Count() == 0 )
        return this ; 

      List<float> lAllSamples = new List<float> ();
      lAllSamples.AddRange( Rep.Samples );  

      foreach( WaveSignal lSlice in aSlices.Cast<WaveSignal>() ) 
        lAllSamples.AddRange(lSlice.Samples);

      var rS = new WaveSignal ( new DiscreteSignal(SamplingRate, lAllSamples) ); 

      rS.Assign( this );

      return rS;  
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
