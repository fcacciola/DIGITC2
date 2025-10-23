using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

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
  Dictionary<string, string> mMap = null ;

  public Params() { mMap = new Dictionary<string, string>() ; }

  public Params Copy()
  {
    Dictionary<string, string> lNewMap = new();

    foreach( var kv in mMap )
      lNewMap.Add( kv.Key, kv.Value );

    return new Params( lNewMap );
  }

  public Params LoadFile( string file)
  {
    if ( File.Exists(file) )
    {
      var lRead = File.ReadLines(file)
                      .Where(ConfigHelper.IsValidLine)
                      .Select(line => line.Split('='))
                      .ToDictionary(line => line[0], line => line[1]);

      foreach( var lKV in  lRead) 
      {
        Set(lKV.Key, lKV.Value);
      }
    }

    return this;  
  }

  public static Params FromFile( string aFile)
  {
    Params rParams = new() ;
    return rParams.LoadFile(aFile);
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

  public List<T> GetNumberList<T>( string aKey ) where T : INumber<T>
  {
    List<T> rL = new List<T>(); 
    string lLV = Get(aKey);
    if ( ! IsNullOrEmpty(lLV) )
    {
      foreach ( string lN in lLV.Split(',') )
      {
        T rV = default(T);
        if ( T.TryParse(lN, null, out rV) )
        {
          rL.Add( rV ); 
        }
      }
    }
    return rL;  
  }

  public List<int> GetIntList( string aKey ) => GetNumberList<int>(aKey);

  Params( Dictionary<string, string> aMap )  
  {
    mMap = aMap;
  }

}

public class Settings : Params
{
  public static Settings FromFile( string aFile)
  {
    Settings rSettings = new Settings() ;
    rSettings.LoadFile( aFile ); 
    return rSettings;
  }
}

public class Config
{
  Dictionary<string, Params> mSections = null ;

  public Params For ( string aSection ) => mSections[aSection]; 

  public Config() { mSections = new Dictionary<string, Params>(); }

  public Config Copy()
  {
    Dictionary<string, Params> lNewSections = new();

    foreach( var kv in mSections )
      lNewSections.Add( kv.Key, kv.Value.Copy() );

    return new Config( lNewSections );
  }

  static (string,string) SplitSectionKey(string aL) 
  {
    var lLoc = aL.IndexOf('_');
    string lSection = aL.Substring(0, lLoc);  
    string lKey = aL.Substring(lLoc + 1);
    return (lSection, lKey);  
  }

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
        rConfig.GetSection(lSection).Set(lKey, lKV.Value);
      }
    }

    return rConfig ;
  }

  public Params GetSection( string aSection )
  {
    if ( !mSections.ContainsKey( aSection ) )  
      mSections.Add( aSection, new Params() );

    return mSections[aSection];
  }

  Config( Dictionary<string,Params> aSections ) { mSections= aSections; } 
}

