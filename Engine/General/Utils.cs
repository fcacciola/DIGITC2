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
      return new DiscreteSignal(SIG.SamplingRate, lSamples.ToArray());
    }
  }

  public static class MathX
  {
    private static readonly Random mRND = new Random();

    public static bool IsEven ( int aN ) => aN % 2 == 0 ; 

    /// <summary>
    /// Linear Interpolation in [aL,aH] by convex parameter (0-1) aF
    /// </summary>
    public static double LERP( double aL, double aH, double aF )  
    {
      return ( 1.0 - aF ) * aL + aF * aH;
    }

    /// <summary>
    /// Randomized Linear Interpolation in [aL,aH] by randomly selected
    /// convex parameter (0-1). 
    /// </summary>
    /// <param name="aTemperature">Controls de random selection of the interpolation parameter
    /// 0 - Will always return aL, as the parameters becomes 0
    /// n > 0  - Will vary the parameter randomly from [0-n%]
    /// </param> 
    public static double RERP( double aL, double aH, double aTemperature )  
    {
      double lR = mRND.NextDouble(); // Random number in [0,1]
      return LERP(aL, aH, lR * aTemperature);
    }

    public static double TERP( double aN, double aTemperature, double aDeltaL = 1.0 - 0.15, double aDeltaH = 1.0 + 0.15 )  
    {
      double lL = aN * aDeltaL ;
      double lH = aN * aDeltaH ;
    
      return MathX.RERP(lL, lH, aTemperature);
    }

    public static int SampleIdx( double aTime ) => (int)Math.Ceiling(aTime * SIG.SamplingRate) ;

    public static int Clamp ( int n, int l, int h ) => n < l ? l : n > h ? h : n ;  
  }

  //public class Textualizer
  //{
  //  public static string Textualize( bool aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

  //  public static string Textualize( int aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

  //  public static string Textualize( Enum aN, string aFmt = "F2" ) => string.Format("{0}", aN );  

  //  public static string Textualize( string aN, string aFmt = "F2" ) => aN;  

  //  public static string Textualize<T>( T aN, string aFmt = "F2" ) => string.Format("{0:" + aFmt + "}", aN );  

  //  public static string TextualizeArray<T>( T[] aArray, string aFmt = "F2", int aMaxSize = 64 ) 
  //  {
  //    List<string> lStrings = new List<string>();
  //    int lLen = aArray.Length;
  //    if ( lLen > aMaxSize )
  //    {
  //      for ( int i = 0; i < aMaxSize/2; i++ )  
  //        lStrings.Add( Textualize(aArray[i],aFmt) ) ;

  //     lStrings.Add("...");
       
  //     for ( int i = lLen-(aMaxSize/2); i < lLen; i++ )  
  //        lStrings.Add( Textualize(aArray[i],aFmt)) ;
  //    }
  //    else
  //    {
  //      for ( int i = 0; i < lLen; i++ )  
  //        lStrings.Add( Textualize(aArray[i],aFmt)+" ") ;
  //    }
  //    return string.Join(",",lStrings);
  //  } 
  //}

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

  public static class GeneralExtensions
  {
    public static string Textualize<T>( this List<T> aList ) 
    {
      var lSB = new StringBuilder();
      lSB.Append( "[" );
      for ( int i = 0; i < aList.Count; i++ )
        lSB.Append( $"{aList[i]}{(i<aList.Count-1?",":"")}" );
      lSB.Append( "]" );
      return lSB.ToString();
    }
  }
    
}
