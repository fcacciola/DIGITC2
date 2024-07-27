using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Vml.Spreadsheet;

namespace DIGITC2_ENGINE
{
  public class Args
  {
    public Dictionary<string, string> Settings = new Dictionary<string, string>();

    static public Args FromFile   (string   file) => new Args(file);
    static public Args FromCmdLine(string[] args) => new Args(args);

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

  public class Session
  {
    public Session( string aName, Args aArgs, string aInputFolder = "./Input", string aOutputFolder = "./Output" )
    {
      Name         = aName;
      Args         = aArgs;
      InputFolder  = aInputFolder; 
      OutputFolder = aOutputFolder; 

      SetupFolder(OutputFolder);
      SetupFolder($"{OutputFolder}/{LogsSubFolder}");
      SetupFolder($"{OutputFolder}/{ResultsSubFolder}/Discarded");
      SetupFolder($"{OutputFolder}/{ResultsSubFolder}/Poor");
      SetupFolder($"{OutputFolder}/{ResultsSubFolder}/Good");
      SetupFolder($"{OutputFolder}/{ResultsSubFolder}/Excelent");
      SetupFolder($"{OutputFolder}/{ResultsSubFolder}/Perfect");
    }  

    public string SamplesSubFolder    => "Samples";
    public string ReferencesSubFolder => "References";
    public string LogsSubFolder       => "Logs";
    public string ResultsSubFolder    => "Results";

    public string Name ;
    public Args   Args ;
    public string InputFolder ;
    public string OutputFolder ;

    public string SampleFile    ( string aFilename ) => $"{InputFolder}/{SamplesSubFolder}/{aFilename}";
    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/{ReferencesSubFolder}/{aFilename}";
    public string InFile        ( string aFilename ) => $"{InputFolder}/{aFilename}";
    public string LogFile       ( string aTail     ) => $"{OutputFolder}/{LogsSubFolder}/{Name}_{aTail}";

    public string TraceFile    => LogFile("trace.txt");

    public string ReportFile( ResultPath aResult ) => $"{OutputFolder}/{ResultsSubFolder}/{aResult.Fitness}/{Name}_report.txt";

    public void SetupFolder ( string aFolder ) 
    {
      if ( ! Directory.Exists( aFolder ) ) 
        Directory.CreateDirectory( aFolder );  
    }
  }

  public class Context
  {
    static Context mInstance = null ;

    public static Context Instance
    {
      get
      {
        if ( mInstance == null )
          mInstance = new Context() ; 
        return mInstance ;
      }
    }

    Context()
    {
    }

    Session mSession = null ; 

    
    void Setup_( Session aSession )
    {
      mSession = aSession ; 

      if ( File.Exists( mSession.TraceFile ) )
       File.Delete( mSession.TraceFile );  

      var lLogger = new LogStateMonitor();
      lLogger.Open(mSession.TraceFile);
      mMonitors.Add( lLogger ) ;

      WriteLine_("DIGITC 2");
    }

    void Shutdown_()
    {
      mMonitors.ForEach( m => m.Close()  );
      mMonitors.Clear();
    }

    void Write_( string aS )
    {
      mMonitors.ForEach( lMonitor => lMonitor.Write(aS) );
    }

    void WriteLine_( string aS )
    {
      mMonitors.ForEach( lMonitor => lMonitor.WriteLine(aS) );
    }

    void Indent_()
    {
      mMonitors.ForEach( lMonitor => lMonitor.Indent() );
    }

    void Unindent_()
    {
      mMonitors.ForEach( lMonitor => lMonitor.Unindent() );
    }

    void Watch_ ( IWithState aO )
    {
      mMonitors.ForEach( lMonitor => lMonitor.Watch( aO ) );
    }

    void Error_( string aText )
    {
      WriteLine_( "ERROR: " + aText ); 
    }

    void Throw_ ( Exception e )
    {
      WriteLine_( "EXCEPTION: " + e.ToString() ); 
      throw e ;
    }


    List<StateMonitor> mMonitors = new List<StateMonitor> ();

    static public Session Session => Instance.mSession ;

    static public void   Setup( Session aSession ) { Instance.Setup_(aSession) ; } 
    static public void   Shutdown()                { Instance.Shutdown_() ; } 
    static public void   Throw( Exception e )      { Instance.Throw_(e);}
    static public void   Error( string aText )     { Instance.Error_(aText);}
    static public void   Watch( IWithState aO )    { Instance.Watch_(aO) ; }
    static public void   Write( string aS )        { Instance.Write_(aS);}
    static public void   WriteLine( string aS )    { Instance.WriteLine_(aS);}
    static public void   Indent()                  { Instance.Indent_();}
    static public void   Unindent()                { Instance.Unindent_();}

  }
}
