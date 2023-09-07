using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public class ProcessingNode
  {
    public ProcessingNode( ProcessingTask aTask, string aID ) 
    { 
      Task = aTask ;
      ID   = aID ;  
    }

    public string ID {  get; private set; }

    public ProcessingTask Task { get; private set; } 

    public ProcessingNode Link( ProcessingNode aN )
    {
      this.Next =  aN;  
      aN  .Prev  = this ;

      return aN ;
    }

    public Signal Process( Signal aSignal ) 
    {
      return Task.Process(aSignal, this);
    }

    public ProcessingNode Next { get ; private set ; } = null ;
    public ProcessingNode Prev { get ; private set ; } = null ;
  }

}
