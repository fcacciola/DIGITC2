using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Vml.Spreadsheet;

namespace DIGITC2
{

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

  }
}
