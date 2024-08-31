using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public static class DSX
  {
    public static DiscreteSignal Concatenate( this IList<DiscreteSignal> aSignals )
    {
      List<float> lSamples = new List<float>();
      foreach (var lSignal in aSignals)
      {
        lSamples.AddRange(lSignal.Samples);
      }
      return new DiscreteSignal(aSignals[0].SamplingRate, lSamples.ToArray());
    }
  }

  public static class MathX
  {
    public static double LERP( double aL, double aH, double aF )  
    {
      return ( 1.0 - aF ) * aL + aF * aH;
    }

    public static int SampleIdx( double aTime, int aSamplingRate ) => (int)Math.Ceiling(aTime * aSamplingRate) ;
  }

  public class Textualizer
  {
    public static string Textualize( bool aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

    public static string Textualize( int aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

    public static string Textualize( Enum aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

    public static string Textualize( string aN, string aFmt = "F2" ) => aN;  

    public static string Textualize<T>( T aN, string aFmt = "F2" ) => string.Format("{0:" + aFmt + "}", aN );  

    public static string TextualizeArray<T>( T[] aArray, string aFmt = "F2", int aMaxSize = 64 ) 
    {
      List<string> lStrings = new List<string>();
      int lLen = aArray.Length;
      if ( lLen > aMaxSize )
      {
        for ( int i = 0; i < aMaxSize/2; i++ )  
          lStrings.Add( Textualize(aArray[i],aFmt) ) ;

       lStrings.Add("...");
       
       for ( int i = lLen-(aMaxSize/2); i < lLen; i++ )  
          lStrings.Add( Textualize(aArray[i],aFmt)) ;
      }
      else
      {
        for ( int i = 0; i < lLen; i++ )  
          lStrings.Add( Textualize(aArray[i],aFmt)+" ") ;
      }
      return string.Join(",",lStrings);
    } 
  }

  public interface IToJSON
  {
    string ToJSON();
  }

  public static class JsonExtensions
  {
    public static string ToJSON<T> ( this T aModel )
    {
      string rJ = "" ;

      IToJSON lTJ = aModel as IToJSON ;
      if ( lTJ != null )
      {
        rJ = lTJ.ToJSON();
      }
      else
      {
        try
        {
          var lSWriter = new StringWriter();
          var lWriter = new JsonTextWriter(lSWriter);
          lWriter.Formatting = Formatting.Indented; 
          var lSer = JsonSerializer.Create(new JsonSerializerSettings(){ TypeNameHandling = TypeNameHandling.All });
          lSer.Serialize(lWriter, aModel);
          lWriter.Flush();
          rJ = lSWriter.ToString();
        }
        catch
        {
        }
      }

      return rJ ;
    }
  }
    
}
