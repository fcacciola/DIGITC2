using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2_ENGINE
{

  public class Step : IWithState
  {
    public Step( Filter aFilter, List<Branch> aBranches )
    {
      Filter = aFilter; 
      Branches.AddRange( aBranches) ; 

    }
    public State GetState()
    {
      State rS = new State("Step",$"{Idx}") ;

      Branches.ForEach( b => rS.Add( b.GetState() ) ) ;

      return rS ;
    }

    public bool ShouldQuit => Branches.All( b => b.ShouldQuit );

    public override string ToString() => Filter.ToString();

    public int          Idx ;
    public Filter       Filter ;
    public List<Branch> Branches = new List<Branch>();
    
  }
 
}
