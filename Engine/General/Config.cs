using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE ;

public static class ConfigHelper
{
  public static string BaseFolder => Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DIGITC2") ; 

  public static string ExpandRelativeFilePath ( string aPath )
  {
    if ( aPath.StartsWith("@") )
          return BaseFolder + aPath.Substring(1);
    else return aPath ;
  }

  public static bool IsValidLine(string line)
  {
    return !line.StartsWith("#") && !line.StartsWith("//") && line.Contains("=");
  }

}

public class Params
{
  Dictionary<string, string> mMap = new Dictionary<string, string>();

  public Params() {}

  public static Params FromFile( string file)
  {
    Params rParams = null ;

    if ( File.Exists(file) )
    {
      var lRead = File.ReadLines(file)
                      .Where(ConfigHelper.IsValidLine)
                      .Select(line => line.Split('='))
                      .ToDictionary(line => line[0], line => line[1]);

      foreach( var lKV in  lRead) 
      {
        rParams.Set(lKV.Key, lKV.Value);
      }
    }

    return rParams ;
  }

  public string Get( string aKey)
  {
    return mMap.ContainsKey(aKey) ? mMap[aKey] : null; 
  }

  public void Set( string aKey, string aValue )
  {
    if ( !mMap.ContainsKey(aKey) )
          mMap.Add(aKey, aValue);
    else mMap[aKey] = aValue; 
  }

  public int?    GetOptionalInt   ( string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToInt32  (v) ; else return null ; }
  public float?  GetOptionalFloat ( string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToSingle (v) ; else return null ; }
  public double? GetOptionalDouble( string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToDouble (v) ; else return null ; }
  public bool?   GetOptionalBool  ( string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToBoolean(v) ; else return null ; }

  public int    GetInt   ( string aKey) => GetOptionalInt   ( aKey) ?? 0 ;
  public float  GetFloat ( string aKey) => GetOptionalFloat ( aKey) ?? 0.0f;
  public double GetDouble( string aKey) => GetOptionalDouble( aKey) ?? 0.0;
  public bool   GetBool  ( string aKey) => GetOptionalBool  ( aKey) ?? false;

  public string GetPath( string aKey ) => ConfigHelper.ExpandRelativeFilePath(Get(aKey));


  Params( IDictionary<string, string> aArgs )  
  {
    foreach( var lKV in aArgs )
    {
      Set(lKV.Key,lKV.Value);
    }
  }

}

public class Settings : Params
{
}

public class Config
{
  Dictionary<string, Params> mSections = new Dictionary<string, Params>();

  public Params For ( string aSection ) => mSections[aSection]; 

  public Config() {}

  public static Config FromFile( string file)
  {
    Config rConfig = null ;

    if ( File.Exists(file) )
    {
      var lRead = File.ReadLines(file)
                      .Where(ConfigHelper.IsValidLine)
                      .Select(line => line.Split('='))
                      .ToDictionary(line => line[0], line => line[1]);

      foreach( var lKV in  lRead) 
      {
        var (lSection,lKey) = SplitSectionKey(lKV.Key);
        rConfig.Set(lSection, lKey, lKV.Value);
      }
    }

    return rConfig ;
  }

  public Params GetSection( string aSection )
  {
    return Sections[aSection];
  }
}

