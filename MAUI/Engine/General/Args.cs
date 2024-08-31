using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public class Args
  {
    public Dictionary<string, string> Settings = new Dictionary<string, string>();

    public Args() {}

    static public Args FromFile      (string   file) => new Args(file);
    static public Args FromCmdLine   (string[] args) => new Args(args);
    static public Args FromDictionary( IDictionary<string, string> aArgs ) => new Args(aArgs);

    public string Get(string aKey) => Settings.ContainsKey(aKey) ? Settings[aKey] : null; 
    
    public int?    GetOptionalInt   (string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToInt32  (v) ; else return null ; }
    public float?  GetOptionalFloat (string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToSingle (v) ; else return null ; }
    public double? GetOptionalDouble(string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToDouble (v) ; else return null ; }
    public bool?   GetOptionalBool  (string aKey) { string v = Get(aKey); if ( v != null ) return Convert.ToBoolean(v) ; else return null ; }

    public int    GetInt   (string aKey) => GetOptionalInt   (aKey) ?? 0 ;
    public float  GetFloat (string aKey) => GetOptionalFloat (aKey) ?? 0.0f;
    public double GetDouble(string aKey) => GetOptionalDouble(aKey) ?? 0.0;
    public bool   GetBool  (string aKey) => GetOptionalBool  (aKey) ?? false;

    bool isValidLine(string line)
    {
      return !line.StartsWith("#") && line.Contains("=");
    }

    Args( IDictionary<string, string> aArgs )  
    {
      foreach( var lKV in aArgs )
        Settings.Add(lKV.Key, lKV.Value );  
    }

    Args(string file)
    {
      Settings.Clear();
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
            var lKey   = lTokens[0];  
            var lValue = lTokens[1];  

            if ( lKey == "@" )
            {
              LoadFromFile(lValue); 
            }
            else
            { 
              Add(lKey, lValue);
            }
          }
        }
        else
        {
          int c = 0 ;
          do
          {
            string lKey = $"File{c}";
            if ( !Settings.ContainsKey(lKey) )
            {
              Add(lKey, lArg);
              break;
            }
            ++ c;
          }
          while ( c < aArgs.Length ) ;
        }
      }
    }

    void LoadFromFile( string file)
    {
      if ( File.Exists(file) )
      {
        var lRead = File.ReadLines(file)
                        .Where(isValidLine)
                        .Select(line => line.Split('='))
                        .ToDictionary(line => line[0], line => line[1]);

        foreach( var lKB in  lRead) 
           Add(lKB.Key, lKB.Value);
      }
    }


    void Add( string aKey, string aValue )  
    {
      if ( !Settings.ContainsKey(aKey) )
        Settings.Add(aKey, aValue);
    }
  }
}
