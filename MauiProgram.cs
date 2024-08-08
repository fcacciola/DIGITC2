using System.IO;
using DIGITC2.ViewModel;
using Newtonsoft.Json ;

namespace DIGITC2;

public class Slice
{
  public Slice( string aName ) { Name = aName ; }

  public string Name ;

  public override string ToString() => Name ;
}

public class Result
{
  public Result( string aSummary ) { Summary = aSummary ; }

  public string Summary ;

  public override string ToString() => Summary ;
}

public class AnalyticPart
{
  public Slice  Slice ;
  public Result Result ;

  public override string ToString() => $"{Slice}->{Result}" ;
}

public class Analytics
{
  public Analytics( Result aOverall ) { Result = aOverall ; }

  public List<AnalyticPart> Parts = new List<AnalyticPart>();  

  public Result Result ;

  public override string ToString() => $"{Result}" ;

  public void AddPart( Slice aSlice, Result aResult )
  {
    Parts.Add( new AnalyticPart{ Slice = aSlice, Result = aResult } ) ;
  } 

  public static Analytics Load( string aFile )
  {
    if ( ! File.Exists(aFile) )
      return null;

    return JsonConvert.DeserializeObject<Analytics>(File.ReadAllText(aFile));
  }

  public void Save( string aFile )
  {
    File.WriteAllText(aFile, JsonConvert.SerializeObject(this, Formatting.Indented));
  }
}

public class Session
{
  public static string RootFolder => Path.Combine(FileSystem.AppDataDirectory,"Sessions");

  public string    ID ;
  public string    Folder ;
  public string    WAVFile ;
  public Analytics Analysis ;

  static public string GetAudioFile   ( string aFolder) => Path.Combine(aFolder, "Audio.wav") ;  
  static public string GetAnalysisFile( string aFolder) => Path.Combine(aFolder, "Analysis.json") ;

  public string AudioFile    => GetAudioFile   (Folder);
  public string AnalysisFile => GetAnalysisFile(Folder);

	public static Session FromFolder(string aFolder)
  {
    string lAudioFile = GetAudioFile(aFolder) ;  

    if ( ! File.Exists(lAudioFile) )
      return null ;

    string lAnalysisFile = GetAnalysisFile(aFolder) ;

    var lAnalysis = Analytics.Load(lAnalysisFile) ; 

    return new Session{ ID       = Path.GetFileName(aFolder)
                      , Folder   = aFolder
                      , WAVFile  = lAudioFile
                      , Analysis = lAnalysis
                      };
  } 

	public static Session FromID(string aID)
  {
    if ( string.IsNullOrEmpty( aID ) ) 
      return null ;

    string lFolder = Path.Combine(RootFolder,aID);  
    return FromFolder(lFolder);
  }

  public void SaveAnalysis()
  {
    if ( Analysis != null )
    {
      string lAnalysisFile = Path.Combine(Folder, "Analysis.json") ;  
      Analysis.Save(lAnalysisFile) ;
    }
  }

  public override string ToString() => ID ;
}

public class AudioDevice
{
  public string Name ;
  public int    Number ;

  public override string ToString() => $"[{Name}|{Number}]";
}


public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		
		// See https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes
		// to understand the differences between [AddSingleton] and [AddTransient].
		
		builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainViewModel>();

        builder.Services.AddTransient<DetailPage>();
        builder.Services.AddTransient<DetailViewModel>();

        return builder.Build();
	}
}
