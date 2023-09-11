using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class Envelope : WaveFilter
  {
    public Envelope( double aAttackTime, double aReleaseTime ) 
    { 
      mAttackTime  = (float)aAttackTime;  
      mReleaseTime = (float)aReleaseTime;  
    }

    protected override Signal Process ( WaveSignal aInput, Context aContext )
    {
      var lES = Operation.Envelope(aInput.Rep, mAttackTime, mReleaseTime);

      mResult = aInput.CopyWith(lES);

      mResult.Name = "Envelope";

      return mResult ;
    }

    public override void Render ( TextRenderer aRenderer, RenderOptions aOptions ) 
    { 
      aRenderer.Render($"Envelope(AttackTime:{mAttackTime},ReleaseTime:{mReleaseTime})", aOptions);
    }

    float mAttackTime ;
    float mReleaseTime;

  }

}
