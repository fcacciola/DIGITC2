using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_Engine
{

  public class Carrier
  {
  }

  public class Processor
  {
    public Processor()
    { 
    }

    public ProcessingNodeBase Add( ProcessingTask aTask) 
    {
      string lID = $"[{mNodes.Count}]" ;
      var rNode = new ProcessingNode(aTask,lID) ;
      mNodes.Add( rNode ) ;
      return rNode ;
    }

    public ProcessingNodeBase AddParallel( params ProcessingTask[] aTasks) 
    {
      string lID = $"[{mNodes.Count}]" ;

      List<ProcessingNodeBase> lArray = new List<ProcessingNodeBase>();

      foreach( var lTask in aTasks )
      {
        string lSID = $"[{mNodes.Count}/{lArray.Count}]" ;
        var lNode = new ProcessingNode(lTask,lSID) ;
        lArray.Add(lNode ) ;
      }

      ParallelProcessingNode rNode = new ParallelProcessingNode(lArray, lID);
      mNodes.Add( rNode ) ;

      return rNode;
    }

    public Signal Process( Source aSource )
    {
      List<Signal> lSlices = aSource.Slice( aSource.GetSignal() ) ;
      List<Signal> lResults = new List<Signal>();

      foreach( var lSignal in lSlices )
      {
        Signal lResult = Process( lSignal ) ;

        lResults.Add( lResult ) ;
      }

      return aSource.Merge( lResults ) ;
    }

    public Signal Process( Signal aSignal )
    {
      var rSignal = aSignal ;
      var lCarrier = new Carrier();

      foreach( var lNode in mNodes )
      { 
        rSignal = lNode.Process(rSignal, lCarrier);
      }
     
      return rSignal ;
    }

    public void Render ( TextSignalRenderer aRenderer ) 
    {
      mNodes.ForEach( n => aRenderer.Render($"->{n}") ) ;
    }

    List<ProcessingNodeBase> mNodes = new List<ProcessingNodeBase>();
  }
}
