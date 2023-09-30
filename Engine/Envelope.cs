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
    public Envelope() 
    { 
      mAttackTime  = (float)Context.Session.Params.Envelop_AttackTime;  
      mReleaseTime = (float)Context.Session.Params.Envelope_ReleaseTime;  
    }

    protected override Step Process ( WaveSignal aInput, Step aStep )
    {
      var lES = Operation.Envelope(aInput.Rep, mAttackTime, mReleaseTime);

      mStep = aStep.Next( aInput.CopyWith(lES), "Envelope", this) ;

      return mStep ;
    }

    protected override string Name => "Envelope" ;

    float mAttackTime ;
    float mReleaseTime;

  }

}
