using System.IO;
using DIGITC2.ViewModel;

using DIGITC2_ENGINE;

using Newtonsoft.Json ;

namespace DIGITC2;

public class UserSession
{
  public static string RootFolder => Path.Combine(FileSystem.AppDataDirectory,"Sessions");

  public string         ID ;
  public string         Folder ;
  public string         WAVFile ;
  public OutcomeSummary Summary ;

  static public string GetAudioFile  ( string aFolder) => Path.Combine(aFolder, "Audio.wav") ;  
  static public string GetOutcomeFile( string aFolder) => Path.Combine(aFolder, "Outcome.json") ;

  public string AudioFile   => GetAudioFile   (Folder);
  public string OutcomeFile => GetOutcomeFile(Folder);

	public static UserSession FromFolder(string aFolder)
  {
    string lAudioFile = GetAudioFile(aFolder) ;  

    if ( ! File.Exists(lAudioFile) )
      return null ;

    string lOutcomeFile = GetOutcomeFile(aFolder) ;

    var lOutcomeSummary = OutcomeSummary.Load(lOutcomeFile) ; 

    return new UserSession{ ID      = Path.GetFileName(aFolder)
                          , Folder  = aFolder
                          , WAVFile = lAudioFile
                          , Summary = lOutcomeSummary
                          };
  } 

	public static UserSession FromID(string aID)
  {
    if ( string.IsNullOrEmpty( aID ) ) 
      return null ;

    string lFolder = Path.Combine(RootFolder,aID);  
    return FromFolder(lFolder);
  }

  public void SaveOutcome()
  {
    if ( Summary != null )
    {
      Summary.Save(OutcomeFile) ;
    }
  }

  public override string ToString() => ID ;
}

public class AudioDevice
{
  public string Name ;
  public int    Number ;

  public override string ToString() => $"[{Name}|{Number}]";

  public static AudioDevice DEFAULT = new AudioDevice{ Name = "Default", Number = -1 }; 
}


