using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class ProcessingTask
  {
    public abstract Signal Process( Signal aSignal, ProcessingNode aNode ) ;
  }

  public class TrivialProcessingTask : ProcessingTask 
  { 
    public override Signal Process( Signal aSignal, ProcessingNode aNode )
    {
      TrivialSignal lTS = aSignal as TrivialSignal ;

      return new TrivialSignal( $"{lTS.Data}[{aNode.ID}]" ) ;
    }
  }
}
