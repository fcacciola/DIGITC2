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
    public void SetGUI ( GUI aGUI ) 
    {
      mGUI = aGUI ;
    }

    public void Open( string aLogFile, string aDetailLogFile )
    {
      mStream       = new FileStream(aLogFile, FileMode.OpenOrCreate, FileAccess.Write );
      mWriter       = new StreamWriter( mStream );
      mDetailStream = new FileStream(aDetailLogFile, FileMode.OpenOrCreate, FileAccess.Write );
      mDetailWriter = new StreamWriter( mDetailStream );
    }

    public void Close()
    {
      mWriter?.Close();
      mStream?.Close();  
      mWriter?.Dispose();  
      mStream?.Dispose();
      mWriter = null;
      mStream = null;

      mDetailWriter?.Close();
      mDetailStream?.Close();  
      mDetailWriter?.Dispose();  
      mDetailStream?.Dispose();
      mDetailWriter = null;
      mDetailStream = null;
    }
    
    public void Write( string aS )
    {
      string lS = AddIndentation(aS);
      mWriter?.Write(lS);  
      mWriter?.Flush();
    }

    public void WriteDetailLine( string aS )
    {
      string lS = AddIndentation(aS);
      mDetailWriter?.WriteLine( lS );  
      mDetailWriter?.Flush();
    }

    void DoWriteLine( string aS )
    {
      mDetailWriter?.WriteLine( aS );  
      mDetailWriter?.Flush();
      mWriter?.WriteLine( aS );  
      mWriter?.Flush();
    }

    public void WriteLine( string aS )
    {
      DoWriteLine( AddIndentation(aS) );
    }

    public void WriteErrorLine( string aS )
    {
      DoWriteLine( AddIndentation( $"ERROR: {aS}") ) ;
    }

    public void WriteLine2GUI( string aS )
    {
      string lS = AddIndentation(aS);
      mGUI?.AddMessage(lS);
      DoWriteLine(lS);
    }

    public void WriteError2GUI( string aS )
    {
      string lS = AddIndentation(aS);
      mGUI?.AddErrorMessage(lS);
      DoWriteLine(lS);
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

    GUI        mGUI          = null ; 
    FileStream mStream       = null ;
    TextWriter mWriter       = null ; 
    FileStream mDetailStream = null ;
    TextWriter mDetailWriter = null ; 
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

        mLogger.SetGUI( mSession.GUI );

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

    void OpenLogger_ ( string aLogFile, string aDetailLogFile )
    {
      mLogger.Open( aLogFile, aDetailLogFile );
    }

    void CloseLogger_ ()
    {
      mLogger.Close();  
    }

    void Write_( string aS )
    {
      mLogger.Write( aS );  
    }

    void WriteDetailLine_( string aS )
    {
      mLogger.WriteDetailLine( aS );
    }

    void WriteLine_( string aS )
    {
      mLogger.WriteLine( aS );
    }

    void WriteErrorLine_( string aS )
    {
      mLogger.WriteErrorLine( aS );
    }

    void WriteLine2GUI_( string aS )
    {
      mLogger.WriteLine2GUI( aS );
    }

    void WriteError2GUI_( string aS )
    {
      mLogger.WriteError2GUI( aS );
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
      WriteErrorLine_( aText ); 
      WriteError2GUI_( aText ); 
    }

    void Assert_( bool aCond, string aText )
    {
      if ( ! aCond )
        Throw_( new Exception("ASSERTION FAILED: " + aText) );
    }

    void Throw_ ( Exception e )
    {
      Error_( "EXCEPTION: " + e.ToString() ); 
      throw e ;
    }

    Logger mLogger = new Logger();

    static public Session Session => Instance.mSession ;

    static public void   Setup          ( Session aSession )                       { Instance.Setup_(aSession) ; } 
    static public void   OpenLogger     ( string aLogFile, string aDetailLogFile ) { Instance.OpenLogger_(aLogFile, aDetailLogFile) ; } 
    static public void   CloseLogger    ()                                         { Instance.CloseLogger_() ; } 
    static public void   Shutdown       ()                                         { Instance.Shutdown_() ; } 
    static public void   Throw          ( Exception e )                            { Instance.Throw_(e);}
    static public void   Error          ( Exception e )                            { Instance.Error_(e.ToString());}
    static public void   Error          ( string aText )                           { Instance.Error_(aText);}
    static public void   Assert         ( bool aCond, string aText )               { Instance.Assert_(aCond,aText);}
    static public void   Write          ( string aS )                              { Instance.Write_(aS);}
    static public void   WriteDetailLine( string aS )                              { Instance.WriteDetailLine_(aS);}
    static public void   WriteLine      ( string aS )                              { Instance.WriteLine_(aS);}
    static public void   WriteLine2GUI  ( string aS )                              { Instance.WriteLine2GUI_(aS);}
    static public void   Indent         ()                                         { Instance.Indent_();}
    static public void   Unindent       ()                                         { Instance.Unindent_();}

  }
}
