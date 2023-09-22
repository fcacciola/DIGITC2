using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2
{
  public class Context
  {
    public float WindowSizeInSeconds { get ; set ; } = 0f ;

    public void Throw ( Exception e )
    {
      throw e ;
    }

    public void Log( string aText )
    {
      Trace.WriteLine( aText ); 
    }

    public void Error( string aText )
    {
      Trace.WriteLine( "ERROR: " + aText ); 
    }
  }

  
}
