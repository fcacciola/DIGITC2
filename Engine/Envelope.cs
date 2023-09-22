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

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      var lES = Operation.Envelope(aInput.Rep, mAttackTime, mReleaseTime);

      mStep = aStep.Next( aInput.CopyWith(lES), "Envelope", this) ;

      return mStep ;
    }

    public override string ToString() => $"Envelope(AttackTime:{mAttackTime},ReleaseTime:{mReleaseTime})";

    float mAttackTime ;
    float mReleaseTime;

  }

}
