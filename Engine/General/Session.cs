using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{

  public class Session
  {
    public Session( string aName, Args aArgs, string aBaseFolder )
    {
      Name             = aName ;
      Args             = aArgs;
      BaseFolder       = aBaseFolder ;
      InputFolder      = Path.Combine(BaseFolder,"Input") ; 
      RootOutputFolder = Path.Combine(BaseFolder,"Output") ; 
    }  

    public void Setup()
    {
      Utils.SetupFolder(InputFolder);
      Utils.SetupFolder(RootOutputFolder);

      PushFolder(Name);

      RootResultsFolder = PushFolder("Results", false);

      Utils.SetupFolder($"{RootResultsFolder}\\Undefined");
      Utils.SetupFolder($"{RootResultsFolder}\\Discarded");
      Utils.SetupFolder($"{RootResultsFolder}\\Poor");
      Utils.SetupFolder($"{RootResultsFolder}\\Good");
      Utils.SetupFolder($"{RootResultsFolder}\\Excelent");
      Utils.SetupFolder($"{RootResultsFolder}\\Perfect");

      PopFolder();
    }

    public void Shutdown()
    {
    }

    public string PushFolder( string aOutputFolder, bool aSetupLogFile = true)
    {
      FoldersStack.Push(aOutputFolder); 

      return BuildCurrentFolder(aSetupLogFile);
    }

    public string PopFolder()
    {
      FoldersStack.Pop();

      return BuildCurrentFolder(true);
    }

    string BuildCurrentFolder( bool aSetupLogFile )
    {
      List<string > lFolders = new List<string> ();

      lFolders.Add(RootOutputFolder);
      foreach (string lFolder in FoldersStack.Reverse()) 
        lFolders.Add(lFolder);

      CurrentOutputFolder = string.Join("\\", lFolders); 

      Utils.SetupFolder(CurrentOutputFolder);

      if ( aSetupLogFile )
      {
        DContext.CloseLogger();
        DContext.OpenLogger( OutputFile("Log.txt") ) ;
      }

      return CurrentOutputFolder;
    }

    public string Name ;
    public Args   Args ;
    public string BaseFolder ;
    public string InputFolder ;
    public string RootOutputFolder ;
    public string GeneralLogsFolder ;
    public string CurrentOutputFolder ;
    public string RootResultsFolder ;

    public Stack<string> FoldersStack = new Stack<string>();


    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string ReportFile( ResultPath aResult ) => $"{RootResultsFolder}/{aResult.Fitness}/Report.txt";




  }
}
