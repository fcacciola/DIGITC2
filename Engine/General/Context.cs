using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{
  public class DContext
  {
    static DContext mInstance = null ;

    public static DContext Instance
    {
      get
      {
        if ( mInstance == null )
          mInstance = new DContext() ; 
        return mInstance ;
      }
    }

    DContext()
    {
    }

    Session mSession = null ; 

    
    void Setup_( Session aSession )
    {
      if ( aSession == null )
        return ;

      try
      {
        mSession = aSession ; 

        mSession.Setup();

        if ( !string.IsNullOrEmpty(mSession.TraceFile) )
        {
          if ( File.Exists( mSession.TraceFile ) )
           File.Delete( mSession.TraceFile );  

          var lLogger = new LogStateMonitor();
          lLogger.Open(mSession.TraceFile);
          mMonitors.Add( lLogger ) ;
        }

        WriteLine_("DIGITC 2 - " + DateTime.Now.ToString() );

      }
      catch( Exception x )
      {

      }
    }

    void Shutdown_()
    {
      mMonitors.ForEach( m => m.Close()  );
      mMonitors.Clear();

      mSession.Shutdown();
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
    static public void   Error( Exception e )      { Instance.Error_(e.ToString());}
    static public void   Error( string aText )     { Instance.Error_(aText);}
    static public void   Watch( IWithState aO )    { Instance.Watch_(aO) ; }
    static public void   Write( string aS )        { Instance.Write_(aS);}
    static public void   WriteLine( string aS )    { Instance.WriteLine_(aS);}
    static public void   Indent()                  { Instance.Indent_();}
    static public void   Unindent()                { Instance.Unindent_();}

  }
}
