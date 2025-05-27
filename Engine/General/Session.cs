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
    Bucket( string aName, string aSubFolder, bool aSetupLogFile )
    {
      Name         = aName;
      mSubFolder   = aSubFolder; 
      SetupLogFile = aSetupLogFile;
    }

    public static Bucket WithLogFile( string aName, string aSubFolderName = null ) => new Bucket( aName, aSubFolderName ?? aName, true );

    public static Bucket WithoutLogFile( string aName, string aSubFolderName = null ) => new Bucket( aName, aSubFolderName ?? aName, false );

    public string FullOutputFolder => mFullOutputFolder;

    public void SetupFullOutputFolder ( string aBaseFolder ) { mFullOutputFolder = $"{aBaseFolder}\\{mSubFolder}" ;}

    public string Name ;
    public bool   SetupLogFile ;

    public override string ToString() => $"{Name} at {mFullOutputFolder ?? mSubFolder}";

    string mSubFolder  ;
    string mFullOutputFolder = null ;
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

      PushBucket( Bucket.WithLogFile(Name) );

      PushBucket( Bucket.WithoutLogFile("Results") );

      RootResultsFolder = CurrentOutputFolder ;

      Utils.SetupFolder($"{RootResultsFolder}\\Undefined");
      Utils.SetupFolder($"{RootResultsFolder}\\Discarded");
      Utils.SetupFolder($"{RootResultsFolder}\\Poor");
      Utils.SetupFolder($"{RootResultsFolder}\\Good");
      Utils.SetupFolder($"{RootResultsFolder}\\Excelent");
      Utils.SetupFolder($"{RootResultsFolder}\\Perfect");

      PopBucket();
    }

    public void Shutdown()
    {
    }

    public Bucket CurrentBucket()
    {
      return mBuckets.Count > 0 ? mBuckets.Peek() : null ;
    }

    public void PushBucket( Bucket aBucket)
    {
      string lBaseFolder = CurrentBucket()?.FullOutputFolder ?? RootOutputFolder ;

      aBucket.SetupFullOutputFolder(lBaseFolder);

      mBuckets.Push(aBucket); 

      ActivateBucket(aBucket); 
    }


    public void PopBucket()
    {
      mBuckets.Pop();

      string lCurrentOutputFolder = CurrentBucket()?.FullOutputFolder ?? RootOutputFolder ;

      SetCurrentOutputFolder(lCurrentOutputFolder);
    }

    public void GotoBucket ( Bucket aBucket )
    {
      if ( aBucket.FullOutputFolder == null )
      {
        PushBucket( aBucket );
      }
      else
      {
        if ( CurrentBucket() != aBucket )
        {
          RebuildBucketStack(aBucket);
          ActivateBucket    (aBucket); 
        }
      }
    }

    void RebuildBucketStack ( Bucket aBucket )
    {
      var lOldStack = mBuckets.ToList ();

      mBuckets.Clear (); 

      foreach( var lBucket in lOldStack )
      { 
        mBuckets.Push (lBucket);

        if ( lBucket == aBucket )
         break ;
      }
    }

    void ActivateBucket ( Bucket aBucket )
    {
      string lCurrentOutputFolder = aBucket.FullOutputFolder ;

      SetCurrentOutputFolder(lCurrentOutputFolder);

      if ( aBucket.SetupLogFile )
        SetupLogFile(aBucket.Name);
    }

    void SetCurrentOutputFolder ( string aOutputFolder  )
    {
      CurrentOutputFolder = aOutputFolder; 

      Utils.SetupFolder(CurrentOutputFolder);
    }

    void SetupLogFile ( string aLogName = null )
    {
      DContext.CloseLogger();
      DContext.OpenLogger( OutputFile($"{aLogName}.txt") ) ;
    }

    public string Name ;
    public Args   Args ;
    public string BaseFolder ;
    public string InputFolder ;
    public string RootOutputFolder ;
    public string GeneralLogsFolder ;
    public string CurrentOutputFolder ;
    public string RootResultsFolder ;


    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string ReportFile( PipelineResult aResult ) => $"{RootResultsFolder}/{aResult.Fitness}/Report.txt";

    Stack<Bucket> mBuckets = new Stack<Bucket>();
  }
}
