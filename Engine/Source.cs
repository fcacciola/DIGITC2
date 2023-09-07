using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class Source
  {
    public abstract Signal GetSignal() ;
  }

  public class SourceA : Source
  {
    public override Signal GetSignal() => new TrivialSignal("#");
  }
}
