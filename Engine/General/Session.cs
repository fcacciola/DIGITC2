using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
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

    }  

    public void Setup()
    {
      Utils.SetupFolder(InputFolder);
      Utils.SetupFolder(RootOutputFolder);
    }

    public void Shutdown()
    {
    }

    public void SetCurrentOutputFolder ( string aOutputFolder  )
    {
      CurrentOutputFolder = aOutputFolder; 

      Utils.SetupFolder(CurrentOutputFolder);
    }

    public void SetupLogFile ( string aLogName = null )
    {
      DContext.CloseLogger();
      DContext.OpenLogger( OutputFile($"{aLogName}.txt") ) ;
    }

    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string   InputFile ;
    public string   Name ;
    public Settings Settings ;
    public GUI      GUI ;

    public string   BaseFolder ;
    public string   InputFolder ;
    public string   RootOutputFolder ;
    public string   CurrentPipelineFolder;
    public string   CurrentOutputFolder ;
  }
}
