using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class ProcessingTask
  {
    public abstract Signal Process( Signal aSignal, ProcessingNode aNode, Carrier aCarrier ) ;
  }

  public class TrivialProcessingTask : ProcessingTask 
  { 
    public TrivialProcessingTask( string aExtra ) { mExtra = aExtra; }

    public override Signal Process( Signal aSignal, ProcessingNode aNode, Carrier aCarrier )
    {
      TrivialSignal lSrc = aSignal as TrivialSignal ;

      List<string> lNewData = new List<string>();

      lSrc.Data.ForEach( d =>  lNewData.Add( d + mExtra) ); 

      return new TrivialSignal(lNewData) ;
    }

    public override string ToString() => $" + {mExtra}";

    string mExtra ;
  }
}
