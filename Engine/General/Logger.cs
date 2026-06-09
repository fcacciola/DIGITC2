using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENGINE
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
 
}
