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

namespace DIGITC2
{
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

    void Setup_( string aLogFile )
    {
      if ( File.Exists( aLogFile ) )
       File.Delete( aLogFile );  

      var lLogger = new LogStateMonitor();
      lLogger.Open(aLogFile);
      mMonitors.Add( lLogger ) ;

      WriteLine_("DIGITC 2");

      mWindowSizeInSeconds = 0f ;
      mMaxWordLength = 20 ;
    }

    void Shutdown_()
    {
      mMonitors.ForEach( m => m.Close()  );
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

    float mWindowSizeInSeconds ;
    int mMaxWordLength ;

    List<StateMonitor> mMonitors = new List<StateMonitor> ();

    static public float        WindowSizeInSeconds { get { return Instance.mWindowSizeInSeconds ; } set { Instance.mWindowSizeInSeconds = value ; } }
    static public int          MaxWordLength       { get { return  Instance.mMaxWordLength ; } set { Instance.mMaxWordLength = value ; } }

    static public void Setup( string aLogFile )                { Instance.Setup_(aLogFile) ; } 
    static public void Shutdown()                              { Instance.Shutdown_() ; } 
    static public void Throw( Exception e )                    { Instance.Throw_(e);}
    static public void Error( string aText )                   { Instance.Error_(aText);}
    static public void Watch( IWithState aO )                  { Instance.Watch_(aO) ; }
    static public void Write( string aS )                      { Instance.Write_(aS);}
    static public void WriteLine( string aS )                  { Instance.WriteLine_(aS);}

  }
}
