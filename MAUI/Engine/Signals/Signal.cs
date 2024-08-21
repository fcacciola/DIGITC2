using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Audio;

namespace DIGITC2_ENGINE
{

  public static class SignalUtils
  {
    public static void Save( this DiscreteSignal aS, string aFilename )
    {
      Save( new WaveFile(aS), aFilename );
    }

    public static void Save( this WaveFile aWF, string aFilename )
    {
      using (var stream = new FileStream(aFilename, FileMode.Create))
      {
        aWF.SaveTo(stream);
      }
    }
  }

  public abstract class Signal : IWithState
  {
    public Source Source { get ; set ; }

    public abstract Signal Copy() ;

    public void Assign( Signal aRHS )
    {
      Name = aRHS.Name ;
    }

    public State GetState()
    {
      State rS = new State("Signal",Name) ;
      UpdateState(rS) ;
      return rS ;
    }

    protected virtual void UpdateState( State rS ) {}

    public override string ToString() => GetState().ToString();
    
    public abstract Distribution GetDistribution() ;

    public string Name = "";

    public string Origin = "";
  }

}
