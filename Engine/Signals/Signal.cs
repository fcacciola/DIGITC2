using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
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
