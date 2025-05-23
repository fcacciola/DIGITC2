using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class FindTokenSeparators : LexicalFilter
  {
    public FindTokenSeparators() : base() 
    {
    }

    protected override void Process (LexicalSignal aInput, ProcessingToken aInputBranch, List<ProcessingToken> rOutput )
    {
      rOutput.Add(aInputBranch);
    }

    public override string Name => this.GetType().Name ;



  }

}
