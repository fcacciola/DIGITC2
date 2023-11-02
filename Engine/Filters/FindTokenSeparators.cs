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

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      mStep = aStep.Next( "Token Separatos", this) ;

      return mStep ;
    }

    protected override string Name => "FindTokenSeparators" ;



  }

}
