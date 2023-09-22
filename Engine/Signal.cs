using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public abstract class Signal
  {
    public Source Source { get ; set ; }

    public abstract Signal Copy() ;

    public void Assign( Signal aRHS )
    {
      Name = aRHS.Name ;
    }

    public override string ToString() => $"({Name})";
    
    public abstract Plot CreatePlot( Plot.Options aOptions ) ;

    public string Name = "";
  }

  }
