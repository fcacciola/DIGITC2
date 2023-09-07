using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class Source
  {
    public Signal GetSignal() 
    {
      var rSignal = DoGetSignal();
      rSignal.Source = this;  
      return rSignal;
    }

    public virtual List<Signal> Slice( Signal aSignal ) {  return new List<Signal>(){aSignal} ; }  

    public virtual Signal Merge( List<Signal> aList ) { return aList[0]; }

    public abstract Signal DoGetSignal() ;
  }

  public class SourceA : Source
  {
    public override Signal DoGetSignal() => new TrivialSignal("A","B","C","D","E");
  }
}
