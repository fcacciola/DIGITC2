using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class AmplitudeGate : WaveFilter
  {
    public AmplitudeGate( double aThreshold ) 
    { 
      mThreshold = (float)aThreshold;
    }

    protected override Signal Process ( WaveSignal aInput, Context aContext )
    {
      float lMax = aInput.ComputeMax();
      
      float[] lSrc = aInput.Samples ;
      int lLen = lSrc.Length ;

      float[] rOutput = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
      {
        float lOut = lSrc[i] / lMax ;
        lOut = lOut > mThreshold ? 1.0f : 0 ;
        lOut = lOut * lMax ;
        rOutput[i] = lOut ;  
      }

      mResult = aInput.CopyWith(new DiscreteSignal(aInput.SamplingRate, rOutput));

      mResult.Name = "AmplitudeGate";

      return mResult ;
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    { 
      aRenderer.Render($"AmplitudeGate(Threshold:{mThreshold})", aOptions);
    }

    float mThreshold;

  }

}
