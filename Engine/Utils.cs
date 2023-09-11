using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{
  public class Utils
  {
    public static string ToStr<T>( T n ) {  return $"{n:F2}" ;}  

    public static string ToStr<T>( T[] aArray ) 
    {
      List<string> lStrings = new List<string>();

      const int cMaxSize = 60 ;

      int lLen = aArray.Length;
      if ( lLen > cMaxSize )
      {
        for ( int i = 0; i < cMaxSize/2; i++ )  
          lStrings.Add(ToStr(aArray[i])) ;

       lStrings.Add(".....");
       
       for ( int i = lLen-(cMaxSize/2); i < lLen; i++ )  
          lStrings.Add(ToStr(aArray[i])) ;
      }
      else
      {
        for ( int i = 0; i < lLen; i++ )  
          lStrings.Add(ToStr(aArray[i])+" ") ;
      }
      return string.Join(",",lStrings);
    }
  }
}
