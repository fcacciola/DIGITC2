using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{


  public class Processor
  {
    public Processor()
    { 
    }

    public ProcessingNode Add( ProcessingTask aTask, ProcessingNode aPrev = null) 
    {
      return Add( new ProcessingNode(aTask,mNodes.Count.ToString())) ;
    }

    public Signal Process( Signal aSignal )
    {
      var lCurrNode = Start ;
      var rSignal   = aSignal ;

      do
      { 
        rSignal = lCurrNode.Process(rSignal);
        lCurrNode = lCurrNode.Next;
      }
      while ( lCurrNode != null );
     
      return rSignal ;
    }

    ProcessingNode Add( ProcessingNode aNode, ProcessingNode aPrev = null) 
    {

      mNodes.Add( aNode );

      aPrev?.Link( aNode ); 

      return aNode ;
    }   


    ProcessingNode Start => mNodes[0];

    List<ProcessingNode> mNodes = new List<ProcessingNode>();
  }
}
