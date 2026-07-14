using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENGINE ;

public abstract class DriverApp
{
  public abstract void AddMessage     ( string aMsg ) ;
  public abstract void AddErrorMessage( string aMsg ) ;
}

public class ElapsedTime
{
  public ElapsedTime() 
  { 
    Start();
  }

  public void Start()
  { 
    if ( !mStarted )
    {
      mStarted = true;
      mS = mSS = DateTime.Now; 
    }
  }

  public void PushSection( string section )
  {
    mSections.Push(section);
    BuildCaption();
  }


  public void PopSection() 
  {
    mSections.Pop();
    BuildCaption();
  }

  public string Check( string aCheckpoint )
  {
    var lE = DateTime.Now ;

    var lTT = lE-mS ;
    var lST = lE-mSS ;
    mSS = lE; 

    if ( lST != lTT )
         return $"{mCaption} {aCheckpoint} time: {lST}. TOTAL time: {lTT}.";
    else return $"{mCaption} {aCheckpoint} time: {lST}.";
  }

  public void BuildCaption()
  {
    mCaption = mSections.Count > 0 ? string.Join(" -> ", mSections.Reverse()) : "";
  }

  Stack<string> mSections = new Stack<string>();

  string mCaption = "";

  DateTime mS, mSS ;
  bool mStarted = false ;
}

public class Session
{
  public Session( string aInputFile, string aName, Settings aSettings, DriverApp aDriverApp, Config aConfig)
  {
    InputFile        = aInputFile ;
    Name             = aName;
    Settings         = aSettings;
    DApp             = aDriverApp;  
    Config           = aConfig ;
    InputFolder      = aSettings.GetPath("InputFolder");  
    RootOutputFolder = aSettings.GetPath("OutputFolder");

    SetCurrentOutputFolder( $"{RootOutputFolder}\\{aName}" );

    mLogger.SetDriverApp(DApp);

    Utils.SetupFolder(InputFolder);
    Utils.SetupFolder(RootOutputFolder);

    WriteLine("Transgraphier - " + DateTime.Now.ToString() );

    mElapsedTime.Start();
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

  public void WriteLine2DriverApp( string aS )
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

  public void PushTimeSection(string aCaption) 
  {
    mElapsedTime.PushSection(aCaption);
  }

  public void PopTimeSection() 
  {
    mElapsedTime.PopSection();
  }

  public void MarkTime( string aCheckpoint ) 
  {
    WriteLine2DriverApp(mElapsedTime.Check(aCheckpoint));
  }

  public string ReferenceFile ( string aFilename ) => $"{InputFolder}/References/{aFilename}";

  public string OutputFile    ( string aFilename ) => $"{CurrentOutputFolder}/{aFilename}";

  public string PrevOutputFile( string aFilename ) => $"{ComputePrevOutputFolder()}/{aFilename}";

  string ComputePrevOutputFolder() => CurrentOutputFolder.Remove(CurrentOutputFolder.LastIndexOf('\\'));

  public bool QuitDisabled => Settings.GetBool("CalibrateScores");
  public bool QuitEnabled  => !QuitDisabled;

  public string    InputFile ;
  public string    Name ;
  public Settings  Settings ;
  public DriverApp DApp ;
  public Config    Config ;
  public string    BaseFolder ;
  public string    InputFolder ;
  public string    RootOutputFolder ;
  public string    CurrentPipelineFolder;
  public string    CurrentOutputFolder ;

  ElapsedTime mElapsedTime = new ElapsedTime();
  Logger      mLogger      = new Logger();

}

