using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public class Args
  {
    public class Section
    {
      public Dictionary<string, string> Settings = new Dictionary<string, string>();
    }
    public Dictionary<string, Section> Sections = new Dictionary<string, Section>();

    public Args() {}

    static public Args FromFile      (string   file) => new Args(file);
    static public Args FromCmdLine   (string[] args) => new Args(args);
    static public Args FromDictionary( IDictionary<string, string> aArgs ) => new Args(aArgs);

    Section GetSection( string aSection )
    {
      if ( ! Sections.ContainsKey(aSection) )
        Sections.Add(aSection, new Section());
      return Sections[aSection];
    }

    public string Get(string aSection, string aKey)
    {
      Section lSsection = GetSection( aSection );
      return lSsection.Settings.ContainsKey(aKey) ? lSsection.Settings[aKey] : null; 
    }

    public string Get(string aKey) => Get(MAIN,aKey);

    public void Set(string aSection, string aKey, string aValue )
    {
      Section lSection = GetSection( aSection );
      if ( !lSection.Settings.ContainsKey(aKey) )
           lSection.Settings.Add(aKey, aValue);
      else lSection.Settings[aKey] = aValue; 
    }
    
    public string GetPath( string aSection, string aKey ) => ExpandRelativeFilePath(Get(aSection,aKey));

    public string GetPath( string aKey ) => GetPath(MAIN,aKey);

    public static string MAIN = "Main" ;

    public List<string> GetPaths( string aSection, string aKey )
    {
      string lRaw = Get(aSection,aKey) ;

      List<string> rPaths = new List<string>() ;

      if ( lRaw.Contains(",") )
      {
        foreach( string lPath in lRaw.Split(',') )
          rPaths.Add( ExpandRelativeFilePath(lPath) );
      }
      else
      {
        rPaths.Add( ExpandRelativeFilePath(lRaw) ) ;
      }

      return rPaths ;
    }

    public List<string> GetPaths( string aKey ) => GetPaths(MAIN,aKey) ;  

    public int?    GetOptionalInt   (string aSection, string aKey) { string v = Get(aSection, aKey); if ( v != null ) return Convert.ToInt32  (v) ; else return null ; }
    public float?  GetOptionalFloat (string aSection, string aKey) { string v = Get(aSection, aKey); if ( v != null ) return Convert.ToSingle (v) ; else return null ; }
    public double? GetOptionalDouble(string aSection, string aKey) { string v = Get(aSection, aKey); if ( v != null ) return Convert.ToDouble (v) ; else return null ; }
    public bool?   GetOptionalBool  (string aSection, string aKey) { string v = Get(aSection, aKey); if ( v != null ) return Convert.ToBoolean(v) ; else return null ; }

    public int    GetInt   (string aSection, string aKey) => GetOptionalInt   (aSection, aKey) ?? 0 ;
    public float  GetFloat (string aSection, string aKey) => GetOptionalFloat (aSection, aKey) ?? 0.0f;
    public double GetDouble(string aSection, string aKey) => GetOptionalDouble(aSection, aKey) ?? 0.0;
    public bool   GetBool  (string aSection, string aKey) => GetOptionalBool  (aSection, aKey) ?? false;

    public int    GetInt   (string aKey) => GetInt (MAIN,aKey);
    public bool   GetBool  (string aKey) => GetBool(MAIN,aKey);

    bool IsValidLine(string line)
    {
      return !line.StartsWith("#") && !line.StartsWith("//") && line.Contains("=");
    }

    (string,string) SplitSectionKey(string aL) 
    {
      var lLoc = aL.IndexOf('_');
      string lSection = aL.Substring(0, lLoc);  
      string lKey = aL.Substring(lLoc + 1);
      return (lSection, lKey);  
    }

    Args( IDictionary<string, string> aArgs )  
    {
      foreach( var lKV in aArgs )
      {
        var (lSection,lKey) = SplitSectionKey(lKV.Key);
        Set(lSection,lKey,lKV.Value);
      }
    }

    Args(string file)
    {
      Sections.Clear();
      LoadFromFile(file); 
    }

    Args(string[] aArgs)
    {
      foreach (string lArg in aArgs) 
      {
        if ( lArg.Contains("=") ) 
        {
          var lTokens = lArg.Split('=');
          if ( lTokens.Length == 2 ) 
          {
            var lValue = lTokens[1];  

            if ( lTokens[0] == "@" )
            {
              LoadFromFile(lValue); 
            }
            else
            { 
              var (lSection,lKey)  = SplitSectionKey(lTokens[0]);  
              Set(lSection,lKey, lValue);
            }
          }
        }
      }
    }

    void LoadFromFile( string file)
    {
      if ( File.Exists(file) )
      {
        var lRead = File.ReadLines(file)
                        .Where(IsValidLine)
                        .Select(line => line.Split('='))
                        .ToDictionary(line => line[0], line => line[1]);

        foreach( var lKV in  lRead) 
        {
          var (lSection,lKey) = SplitSectionKey(lKV.Key);
          Set(lSection, lKey, lKV.Value);
        }
      }
    }


    string BaseFolder  => Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DIGITC2") ; 

    string ExpandRelativeFilePath ( string aPath )
    {
      if ( aPath.StartsWith("@") )
           return BaseFolder + aPath.Substring(1);
      else return aPath ;
    }

  }
}
