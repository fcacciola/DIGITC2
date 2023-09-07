using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{
  public abstract class ProcessingNodeBase
  {
    public string ID {  get; protected set; }

    public abstract Signal Process( Signal aSignal, Carrier aCarrier )  ;
  }

  public class ProcessingNode : ProcessingNodeBase
  {
    public ProcessingNode( ProcessingTask aTask, string aID ) 
    { 
      ID   = aID ;  
      Task = aTask ;
    }

    public ProcessingTask Task { get; private set; } 

    public override Signal Process( Signal aSignal, Carrier aCarrier ) 
    {
      return Task.Process(aSignal, this, aCarrier);
    }

    public override string ToString() => $"{ID}" ;
  }

  public class ParallelProcessingNode : ProcessingNodeBase
  {
    public ParallelProcessingNode( List<ProcessingNodeBase> aNodes, string aID  ) 
    { 
      ID     = aID ;  
      mNodes = aNodes ;
    }

    public override Signal Process( Signal aSignal, Carrier aCarrier ) 
    {
      int lC = mNodes.Count ;

      List<Signal> lBranches = aSignal.BranchOut(lC) ;  
      
      List<Signal> lResults = new List<Signal> (lC) ;

      for ( int i = 0 ; i < lC ; ++ i )
      {
        lResults.Add( mNodes[i].Process( lBranches[i], aCarrier ) ) ; 
      }

      var rResultArray = new SignalArray(lResults) ;

      return rResultArray ; 
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder() ;
      mNodes.ForEach( n => sb.Append($"{n.ID}"));
      return sb.ToString() ;
    }

    List<ProcessingNodeBase> mNodes = new List<ProcessingNodeBase>(); 
  }

}
