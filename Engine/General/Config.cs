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

  public static (string,string) SplitSectionKey(string aL) 
  {
    var lLoc = aL.IndexOf('_');
    string lSection = aL.Substring(0, lLoc).Trim();  
    string lKey = aL.Substring(lLoc + 1).Trim();
    return (lSection, lKey);  
  }

  public static (string, string) SplitSectionValue(string aL)
  {
    if ( aL.Contains('|') )
    {
      var lLoc = aL.LastIndexOf('|');
      string lValue = aL.Substring(0, lLoc).Trim();
      string lLabel = aL.Substring(lLoc + 1).Trim();
      return (lValue, lLabel);
    }
    else
    {
      return (aL.Trim(), null);
    }
  }
}

public class Param
{
  public Param(string aName, string aValue, string aLabel)
  {
    Name  = aName;
    Value = aValue;
    Label = aLabel;
  }

  public string Name { get; set; }
  public string Value { get; set; } = null;
  public string Label { get; set; } = null;

  public Param Copy() => new Param(Name, Value, Label);

  public override string ToString() => Label != null ? $"{Value}|{Label}" : Value;
}

public class Params
{
  public Dictionary<string, Param> Map = null ;

  public Params() { Map = new Dictionary<string, Param>() ; }

  public Params Copy()
  {
    Dictionary<string, Param> lNewMap = new();

    foreach( var kv in Map )
      lNewMap.Add( kv.Key, kv.Value.Copy() );

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
        (string lValue, string lLabel) = ConfigHelper.SplitSectionValue(lKV.Value);
        Set(lKV.Key.Trim(), lValue, lLabel);
      }
    }

    return this;  
  }

  public Param Get_( string aKey)
  { 
    return Map.ContainsKey(aKey) ? Map[aKey] : null; 
  }

  public string GetValue( string aKey) => Get_(aKey)?.Value ;

  public void Set( string aKey, string aValue, string aLabel )
  {
    if ( !Map.ContainsKey(aKey) )
         Map.Add(aKey, new Param(aKey,aValue,aLabel));
    else Map[aKey] = new Param(aKey,aValue,aLabel); 
  }

  public void ChangeValue( string aKey, string aValue )
  {
    Map[aKey].Value = aValue; 
  }


  static int? ToInt( string aS )
  {
    int rV ;
    if ( int.TryParse(aS, null, out rV) )
      return rV;
    return null;
  }

  static float? ToFloat( string aS )
  {
    float rV ;
    if ( float.TryParse(aS, null, out rV) )
      return rV;
    return null;
  }

  static double? ToDouble( string aS )
  {
    double rV ;
    if ( double.TryParse(aS, null, out rV) )
      return rV;
    return null;
  }

  static bool? ToBool( string aS )
  {
    bool rV ;
    if ( bool.TryParse(aS,  out rV) )
      return rV;
    return null;
  }

  public int?    GetOptionalInt   ( string aKey) { string v = GetValue(aKey); if ( v != null ) return ToInt   (v) ; else return null ; }
  public float?  GetOptionalFloat ( string aKey) { string v = GetValue(aKey); if ( v != null ) return ToFloat (v) ; else return null ; }
  public double? GetOptionalDouble( string aKey) { string v = GetValue(aKey); if ( v != null ) return ToDouble(v) ; else return null ; }
  public bool?   GetOptionalBool  ( string aKey) { string v = GetValue(aKey); if ( v != null ) return ToBool  (v) ; else return null ; }

  public int    GetInt   ( string aKey) => GetOptionalInt   ( aKey ) ?? 0 ;
  public float  GetFloat ( string aKey) => GetOptionalFloat ( aKey ) ?? 0.0f;
  public double GetDouble( string aKey) => GetOptionalDouble( aKey ) ?? 0.0;
  public bool   GetBool  ( string aKey) => GetOptionalBool  ( aKey ) ?? false;

  public string GetPath( string aKey ) => ConfigHelper.ExpandRelativeFilePath(GetValue(aKey));

  public List<T> GetNumberList<T>( string aKey ) where T : INumber<T>
  {
    List<T> rL = new List<T>(); 
    string lLV = GetValue(aKey);
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

  public List<int>   GetIntList  ( string aKey ) => GetNumberList<int>  (aKey);
  public List<float> GetFloatList( string aKey ) => GetNumberList<float>(aKey);

  Params( Dictionary<string, Param> aMap )  
  {
    Map = aMap;
  }
}

public class Settings : Params
{
  public Settings() : base() {} 

  public static Settings FromFile( string aFile)
  {
    Settings rSettings = new Settings() ;
    rSettings.LoadFile( aFile ); 
    return rSettings;
  }

  public void Save( string aFilename )
  {
    var lLines = new List<string>();
    foreach( var lEntryKV in Map )
    {
      lLines.Add( $"{lEntryKV.Key}={lEntryKV.Value}" );
    }

    File.WriteAllLines( aFilename, lLines );
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


  public static Config FromFile( string file)
  {
    if ( File.Exists(file) )
    {
      Config rConfig = new Config() ;
      var lRead = File.ReadLines(file)
                      .Where(ConfigHelper.IsValidLine)
                      .Select(line => line.Split('='))
                      .ToDictionary(line => line[0], line => line[1]);

      foreach( var lKV in  lRead) 
      {
        var (lSection,lKey)  = ConfigHelper.SplitSectionKey(lKV.Key);
        var (lValue, lLabel) = ConfigHelper.SplitSectionValue(lKV.Value);
        rConfig.GetSection(lSection).Set(lKey, lValue, lLabel);
      }

      return rConfig ;
    }
    return null;
  }

  public Params GetSection( string aSection )
  {
    if ( !mSections.ContainsKey( aSection ) )  
      mSections.Add( aSection, new Params() );

    return mSections[aSection];
  }

  public void Save( string aFilename )
  {
    var lLines = new List<string>();
    foreach( var lSectionKV in mSections )
    {
      foreach( var lEntryKV in lSectionKV.Value.Map )
      {
        
        lLines.Add( $"{lSectionKV.Key}_{lEntryKV.Key}={lEntryKV.Value}" );
      }
    }

    File.WriteAllLines( aFilename, lLines );
  }

  public List<Param> GetEditableParams()
  {
    List<Param> rL = new List<Param>();
    foreach (var lSectionKV in mSections)
    {
      foreach (var lEntryKV in lSectionKV.Value.Map)
      {
        if (lEntryKV.Value.Label != null)
          rL.Add(lEntryKV.Value);
      }
    }
    return rL;
  }

  Config( Dictionary<string,Params> aSections ) { mSections= aSections; } 
}

