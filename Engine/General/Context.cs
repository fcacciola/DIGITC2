using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{

  public class Logger : IDisposable
  {
    public void Open( string aFile )
    {
      mStream = new FileStream(aFile, FileMode.OpenOrCreate, FileAccess.Write );
      mWriter = new StreamWriter( mStream );
    }

    public void Close()
    {
      mWriter?.Close();
      mStream?.Close();  
      mWriter?.Dispose();  
      mStream?.Dispose();
      mWriter = null;
      mStream = null;
    }

    public void Write( string aS )
    {
      mWriter?.Write( AddIndentation(aS) );  
      mWriter?.Flush();
    }

    public void WriteLine( string aS )
    {
      mWriter?.WriteLine( AddIndentation(aS) );  
      mWriter?.Flush();
    }

    public void Indent()
    {
      mIndentation += 2 ;
    }

    public void Unindent()
    {
      mIndentation -= 2 ;
      if ( mIndentation < 0 ) 
       mIndentation = 0 ;
    }

    string AddIndentation( string aS )  => $"{new String(' ', mIndentation)}{aS}";


    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          Close();
        }

        disposedValue = true;
      }
    }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    int mIndentation = 0; 

    FileStream mStream = null ;
    TextWriter mWriter = null ; 
  }

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

        WriteLine_("DIGITC 2 - " + DateTime.Now.ToString() );

      }
      catch( Exception x )
      {
        Error_( x.ToString() );
      }
    }

    void Shutdown_()
    {
      mLogger.Close();
      mSession.Shutdown();
    }

    void OpenLogger_ ( string aLogFile )
    {
      mLogger.Open( aLogFile );
    }

    void CloseLogger_ ()
    {
      mLogger.Close();  
    }

    void Write_( string aS )
    {
      mLogger.Write( aS );  
    }

    void WriteLine_( string aS )
    {
      mLogger.WriteLine( aS );
    }

    void Indent_()
    {
      mLogger.Indent();
    }

    void Unindent_()
    {
      mLogger.Unindent();
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

    Logger mLogger = new Logger();

    static public Session Session => Instance.mSession ;

    static public void   Setup      ( Session aSession ) { Instance.Setup_(aSession) ; } 
    static public void   OpenLogger ( string aLogFile )  { Instance.OpenLogger_(aLogFile) ; } 
    static public void   CloseLogger()                   { Instance.CloseLogger_() ; } 
    static public void   Shutdown   ()                   { Instance.Shutdown_() ; } 
    static public void   Throw      ( Exception e )      { Instance.Throw_(e);}
    static public void   Error      ( Exception e )      { Instance.Error_(e.ToString());}
    static public void   Error      ( string aText )     { Instance.Error_(aText);}
    static public void   Write      ( string aS )        { Instance.Write_(aS);}
    static public void   WriteLine  ( string aS )        { Instance.WriteLine_(aS);}
    static public void   Indent     ()                   { Instance.Indent_();}
    static public void   Unindent  ()                   { Instance.Unindent_();}

  }
}
