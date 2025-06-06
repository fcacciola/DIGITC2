﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DIGITC2_ENGINE
{
  public class Utils
  {
    static public string SetupFolder ( string aFolder ) 
    {
      if ( ! Directory.Exists( aFolder ) ) 
        Directory.CreateDirectory( aFolder );  
      return aFolder;
    }

    static public void SetupFolderInFullPath ( string aPath ) 
    {
      SetupFolder( Path.GetDirectoryName(aPath) );
    }
  }

}
