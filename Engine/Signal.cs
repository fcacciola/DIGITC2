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
      StepIdx  = aRHS.StepIdx  ;
      Name     = aRHS.Name ;
    }

    public override string ToString() => $"([{StepIdx}]|{Name})";
    
    public string Name     = "";
    public int    StepIdx  = 0 ;

  }

  }
