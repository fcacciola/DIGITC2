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
  public class Params
  {
    public float  WindowSizeInSeconds             = 0;
    public int    MaxWordLength                   = 35;
    public int    BitsPerByte                     = 8 ;
    public bool   LittleEndian                    = true ;
    public double Envelop_AttackTime              = 0.1;
    public double Envelope_ReleaseTime            = 0.1;
    public double AmplitudeGate_Threshold         = 0.65;
    public double ExtractGatedlSymbols_MinDuration= 0.05;
    public double ExtractGatedlSymbols_MergeGap   = 0.1;
    public double BinarizeByDuration_Threshold    = 0.4;
    public string CharSet                         = "us-ascii";
  }

  public class Session
  {
    public Session( string aName, Args aArgs, string aInputFolder = "./Input", string aOutputFolder = "./Output" )
    {
      Name         = aName;
      Args         = aArgs;
      InputFolder  = aInputFolder; 
      OutputFolder = aOutputFolder; 

      if ( ! Directory.Exists( OutputFolder ) ) 
      {
        Directory.CreateDirectory( OutputFolder );  
      }
    }  

    public string Name ;
    public Args   Args ;
    public string InputFolder ;
    public string OutputFolder ;

    public string SampleFile    ( string aFilename ) => $"{InputFolder}/Samples/{aFilename}";
    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";
    public string InFile        ( string aFilename ) => $"{InputFolder}/{aFilename}";
    public string OutFile       ( string aTail     ) => $"{OutputFolder}/{Name}_{aTail}";

    public string LogFile    => OutFile("log.txt");
    public string ReportFile => OutFile("report.txt");

    public Params Params = new Params();
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

      if ( File.Exists( mSession.LogFile ) )
       File.Delete( mSession.LogFile );  

      if ( File.Exists( mSession.ReportFile ) )
       File.Delete( mSession.ReportFile );  

      var lLogger = new LogStateMonitor();
      lLogger.Open(mSession.LogFile);
      mMonitors.Add( lLogger ) ;

      var lReporter = new ReportStateMonitor();
      lReporter.Open(mSession.ReportFile);
      mMonitors.Add( lReporter ) ;

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

    static public void Setup( Session aSession )               { Instance.Setup_(aSession) ; } 
    static public void Shutdown()                              { Instance.Shutdown_() ; } 
    static public void Throw( Exception e )                    { Instance.Throw_(e);}
    static public void Error( string aText )                   { Instance.Error_(aText);}
    static public void Watch( IWithState aO )                  { Instance.Watch_(aO) ; }
    static public void Write( string aS )                      { Instance.Write_(aS);}
    static public void WriteLine( string aS )                  { Instance.WriteLine_(aS);}

  }
}
