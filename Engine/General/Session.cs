using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENGINE
{



  public abstract class GUI
  {
    public abstract void AddMessage     ( string aMsg ) ;
    public abstract void AddErrorMessage( string aMsg ) ;
  }

  public class Session
  {
    public Session( string aInputFile, string aName, Settings aSettings, GUI aGUI )
    {
      InputFile        = aInputFile ;
      Name             = aName;
      Settings         = aSettings;
      GUI              = aGUI;  
      InputFolder      = aSettings.GetPath("InputFolder");  
      RootOutputFolder = aSettings.GetPath("OutputFolder");

      SetCurrentOutputFolder( $"{RootOutputFolder}\\{aName}" );

      mLogger.SetGUI(GUI);

      Utils.SetupFolder(InputFolder);
      Utils.SetupFolder(RootOutputFolder);

      WriteLine("DIGITC 2 - " + DateTime.Now.ToString() );
    }  

    public void Shutdown()
    {
      mLogger.Close();
    }

    public void SetCurrentOutputFolder ( string aOutputFolder  )
    {
      CurrentOutputFolder = aOutputFolder; 

      Utils.SetupFolder(CurrentOutputFolder);
    }

    public void SetupLogFile ( string aLogName = null )
    {
      mLogger.Close();
      mLogger.Open( OutputFile($"{aLogName}.txt"), OutputFile($"{aLogName}_detail.txt") ) ;
    }

    public void OpenLogger ( string aLogFile, string aDetailLogFile )
    {
      mLogger.Open( aLogFile, aDetailLogFile );
    }

    public void CloseLogger ()
    {
      mLogger.Close();  
    }

    public void Write( string aS )
    {
      mLogger.Write( aS );  
    }

    public void WriteDetailLine( string aS )
    {
      mLogger.WriteDetailLine( aS );
    }

    public void WriteLine( string aS )
    {
      mLogger.WriteLine( aS );
    }

    public void WriteErrorLine( string aS )
    {
      mLogger.WriteErrorLine( aS );
    }

    public void WriteLine2GUI( string aS )
    {
      mLogger.WriteLine2GUI( aS );
    }

    public void WriteError2GUI( string aS )
    {
      mLogger.WriteError2GUI( aS );
    }

    public void Indent()
    {
      mLogger.Indent();
    }

    public void Unindent()
    {
      mLogger.Unindent();
    }

    public void Error( string aText )
    {
      WriteErrorLine( aText ); 
      WriteError2GUI( aText ); 
    }

    public void Error( Exception e ) => Error(e.ToString());

    public void Assert( bool aCond, string aText )
    {
      if ( ! aCond )
        Throw( new Exception("ASSERTION FAILED: " + aText) );
    }

    public void Throw ( Exception e )
    {
      Error( "EXCEPTION: " + e.ToString() ); 
      throw e ;
    }

    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string PrevOutputFile( string aFilename ) => $"{ComputePrevOutputFolder()}/{aFilename}";

    string ComputePrevOutputFolder() => CurrentOutputFolder.Remove(CurrentOutputFolder.LastIndexOf('\\'));

    public string   InputFile ;
    public string   Name ;
    public Settings Settings ;
    public GUI      GUI ;

    public string   BaseFolder ;
    public string   InputFolder ;
    public string   RootOutputFolder ;
    public string   CurrentPipelineFolder;
    public string   CurrentOutputFolder ;

    Logger mLogger = new Logger();

  }
}
