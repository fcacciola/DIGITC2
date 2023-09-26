using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
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
      State rS = new State(Name) ;
      UpdateState(rS) ;
      return rS ;
    }

    protected virtual void UpdateState( State rS ) {}

    public override string ToString() => GetState().ToString();
    
    public abstract Plot CreatePlot( Plot.Options aOptions ) ;

    public string Name = "";
  }

  }
