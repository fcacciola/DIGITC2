using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIGITC2_ENGINE
{

  public class OutputBucket
  {
    OutputBucket( string aName, string aSubFolder, bool aSetupLogFile )
    {
      Name         = aName;
      mSubFolder   = aSubFolder; 
      SetupLogFile = aSetupLogFile;
    }

    public static OutputBucket WithLogFile( string aName, string aSubFolderName = null ) => new OutputBucket( aName, aSubFolderName ?? aName, true );

    public static OutputBucket WithoutLogFile( string aName, string aSubFolderName = null ) => new OutputBucket( aName, aSubFolderName ?? aName, false );

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
    public Session( string aName, Settings aSettings, string aBaseFolder )
    {
      Name             = aName ;
      Settings         = aSettings;
      BaseFolder       = aBaseFolder ;
      InputFolder      = Path.Combine(BaseFolder,"Input") ; 
      RootOutputFolder = Path.Combine(BaseFolder,"Output") ; 
    }  

    public void Setup()
    {
      Utils.SetupFolder(InputFolder);
      Utils.SetupFolder(RootOutputFolder);

      PushBucket( OutputBucket.WithoutLogFile(Name) );
    }

    public void Shutdown()
    {
    }

    public OutputBucket CurrentBucket()
    {
      return mBuckets.Count > 0 ? mBuckets.Peek() : null ;
    }

    public void PushBucket( OutputBucket aBucket)
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

    public void GotoBucket ( OutputBucket aBucket )
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

    void RebuildBucketStack ( OutputBucket aBucket )
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

    void ActivateBucket ( OutputBucket aBucket )
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

    public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

    public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

    public string   Name ;
    public Settings Settings ;
    public string   BaseFolder ;
    public string   InputFolder ;
    public string   RootOutputFolder ;
    public string   CurrentOutputFolder ;
                    
    Stack<OutputBucket> mBuckets = new Stack<OutputBucket>();
  }
}
