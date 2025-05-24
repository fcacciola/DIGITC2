using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{

  public class Bucket
  {
    public Bucket( string aName, string aFolderName, bool aSetupLogFile )
    {
      Name         = aName;
      FolderName   = aFolderName; 
      SetupLogFile = aSetupLogFile;
    }

    public static Bucket WithFolder( string aName, string aFolderName = null, bool aSetupLogFile = true ) => new Bucket( aName, aFolderName ?? aName, aSetupLogFile );

    public string Name ;
    public string FolderName ;
    public bool   SetupLogFile ;
  }

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

      PushBucket( Bucket.WithFolder(Name) );

      RootResultsFolder = PushBucket( Bucket.WithFolder("Results", null, false) );

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

    public string PushBucket( Bucket aBucket)
    {
      Buckets.Push(aBucket); 

      return BuildCurrentFolder(aBucket.SetupLogFile);
    }

    public string PopFolder()
    {
      Buckets.Pop();

      return BuildCurrentFolder(false);
    }

    string BuildCurrentFolder( bool aSetupLogFile )
    {
      List<string > lFolders = new List<string> ();

      lFolders.Add(RootOutputFolder);
                              
      foreach ( var lBucket in Buckets.Reverse()) 
        if ( ! string.IsNullOrEmpty(lBucket.FolderName) ) 
          lFolders.Add(lBucket.FolderName);

      CurrentOutputFolder = string.Join("\\", lFolders); 

      Utils.SetupFolder(CurrentOutputFolder);

      if ( aSetupLogFile )
      {
        string lLogName = Buckets.Peek().Name;
        DContext.CloseLogger();
        DContext.OpenLogger( OutputFile($"{lLogName}.txt") ) ;
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

    public Stack<Bucket> Buckets = new Stack<Bucket>();


    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string ReportFile( PipelineResult aResult ) => $"{RootResultsFolder}/{aResult.Fitness}/Report.txt";




  }
}
