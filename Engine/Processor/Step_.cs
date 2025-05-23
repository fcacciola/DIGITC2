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

  public class Step_ 
  {
    public Step_( Processor aProcessor, Filter aFilter )
    {
      Processor = aProcessor; 
      Filter    = aFilter; 
    }

//    public bool ShouldQuit => Branches.All( b => b.ShouldQuit );

    public override string ToString() => Filter.ToString();

    public Processor    Processor ;
    public int          Idx ;
    public Filter       Filter ;
    public List<ProcessingToken> Branches = new List<ProcessingToken>();
    
  }
 
}
