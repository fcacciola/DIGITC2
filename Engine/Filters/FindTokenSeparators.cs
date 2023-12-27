using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class FindTokenSeparators : LexicalFilter
  {
    public FindTokenSeparators() : base() 
    {
    }

    protected override void Process (LexicalSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      rOutput.Add(aInputBranch);
    }

    protected override string Name => "FindTokenSeparators" ;



  }

}
