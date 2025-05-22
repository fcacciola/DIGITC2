using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DIGITC2_ENGINE ;

namespace DIGITC2 {

public class TEST_ONLY_ZipfWordDistribution
{
  public static void Run( Args aArgs )
  {
    DContext.Setup( new Session("TEST_ONLY_ZipfWordDistribution", aArgs, Task.BaseFolder) ) ;

    DContext.WriteLine("Zipf Word Distribution");

    int lBaseSize = 10000 ;
    int lSize     = lBaseSize ;

    int lR = 2 ;

    List<string> lAll = new List<string>() ;
    do
    { 
       string lWord = $"{lSize}";      
       for ( int c = 0 ; c < lSize ; ++ c )  
       {
         lAll.Add( lWord ) ;
       }

       lSize = lBaseSize / lR ;
       lR ++ ;
    }
    while ( lSize > 1 ) ;
    
    string lSourceText = string.Join(" ", lAll.ToArray() );

    DContext.WriteLine("Source text: " + lSourceText );

    var lSource = BitsSource.FromText("ZipfWordDistribution", lSourceText);  

    var lResult = Processor.FromBits().Process( lSource.CreateSignal() ) ;

    DContext.Shutdown(); 
  }
}
}
      
