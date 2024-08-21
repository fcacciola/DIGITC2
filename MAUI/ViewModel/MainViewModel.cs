using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DIGITC2_ENGINE;

namespace DIGITC2.ViewModel;

public class AudioTools
{
  public AudioTools()
  {
    AudioService = new AudioService();

    InputDevice  = FindDevice(Preferences.Get("InputDevice",  ""), AudioService.EnumInputDevices () ) ;
    OutputDevice = FindDevice(Preferences.Get("OutputDevice", ""), AudioService.EnumOutputDevices() ) ;
  }
  
  public AudioDevice  InputDevice  = null ;
  public AudioDevice  OutputDevice = null ;
  public AudioService AudioService = null ;

  AudioDevice FindDevice( string aPreference, List<AudioDevice> aDevices )  
  {
    if ( aDevices.Count > 0 )
    {
      int lIdx = aDevices.FindIndex( s => s.Name.StartsWith(aPreference));
      if ( lIdx != - 1 )
      {
        return aDevices[lIdx];  
      }
    }


    return AudioDevice.DEFAULT ;
  }
}


public partial class MainViewModel : ObservableObject
{

  readonly IConnectivity connectivity;

  public MainViewModel(IConnectivity connectivity)
  {
    Items = [];
    this.connectivity = connectivity;

    noiseSources = new ObservableCollection<string>
    {
      "White Noise with embeeded TapCode guidance" 
    };

    selectedNoise = noiseSources[0];
  }

  public event Action SetRecButtonToStart;
  public event Action SetRecButtonToStop;
  public event Action SetNoisePlayButtonToStart;
  public event Action SetNoisePlayButtonToStop;

  public void OnSetRecButtonToStart()
  {
    SetRecButtonToStart?.Invoke();
  }

  public void OnSetRecButtonToStop()
  {
    SetRecButtonToStop?.Invoke();
  }

  public void OnSetNoisePlayButtonToStart()
  {
    SetNoisePlayButtonToStart?.Invoke();
  }

  public void OnSetNoisePlayButtonToStop()
  {
    SetNoisePlayButtonToStop?.Invoke();
  } 
  [ObservableProperty]
  ObservableCollection<string> noiseSources;

  [ObservableProperty]
  ObservableCollection<UserSession> items;

  [ObservableProperty]
  string selectedNoise;

  [ObservableProperty]
  string text;


  UserSession CreateNewSession()
  {
    if ( ! Directory.Exists(UserSession.RootFolder) )
      Directory.CreateDirectory(UserSession.RootFolder);

    UserSession rSession = new UserSession();

    string lCurrSessionName = Preferences.Get("CurrSession", "Session");

    int i = 0 ;

    do
    {
      rSession.ID = $"{lCurrSessionName}_{i}";  
      rSession.Folder = Path.Combine(UserSession.RootFolder, rSession.ID);  
      if ( ! Directory.Exists(rSession.Folder))
      {
        Directory.CreateDirectory(rSession.Folder);
        return rSession;
      }
      i++ ;
    }
    while ( i < 1000000 ) ;

    return null ;
  }

  void DoSetup()
  {
    mAudioTools = new AudioTools();
    mAudioTools.AudioService.RecStopped += OnRecStopped;

    mInputFolder  = Path.Combine(FileSystem.AppDataDirectory,"Input") ; 
    mOutputFolder = Path.Combine(FileSystem.AppDataDirectory,"Output") ; 

    Utils.SetupFolder(mInputFolder);
    Utils.SetupFolder(mOutputFolder);
    Utils.SetupFolder(UserSession.RootFolder);
  }

  public async Task Setup()
  {
    await Task.Run(() => DoSetup() );
  }

  public async Task LoadSessions()
  {
    await Task.Run(() => 
    {
      string lRootFolder = UserSession.RootFolder ;

      foreach( var lSessionFolder in Directory.GetDirectories(lRootFolder)) 
      {
        UserSession lSession = UserSession.FromFolder(lSessionFolder);
        if ( lSession != null )
          Items.Add(lSession);
      }
    }
    );
  }

  //async Task StartRecording(CancellationToken aCT)
  void StartRecording()
  {
    mSession = CreateNewSession();

    if ( mSession != null )
    {
      mSession.WAVFile = Path.Combine(mSession.Folder, "Audio.wav");

      mAudioTools.AudioService.StartRecording(mSession.WAVFile, mAudioTools.InputDevice.Number);

      mIsRecording = true;
    }
  }

  // async Task 
  void StopRecording()
  {
    mAudioTools.AudioService.StopRecording();
  }

  [RelayCommand]
  async Task LoadAudio()
  {

  }

  void DoREC()
  {
    if (!mIsRecording)
    {
      StartRecording();
      OnSetRecButtonToStop();
    }
    else
    {
      StopRecording();
    }
  }

  [RelayCommand]
  async Task REC()
  {
    DoREC();
  }

  //async Task StartRecording(CancellationToken aCT)
  void StartNoisePlayback()
  {
    if ( mIsNoisePlaying )
      StopNoisePlayback();

    string lNoiseWAVFile = Path.Combine(mOutputFolder, "Noise.wav"); 
    NoiseGenerator.Generate(lNoiseWAVFile); 

    if ( File.Exists(lNoiseWAVFile) )
    {
      mAudioTools.AudioService.Load(lNoiseWAVFile);
      mAudioTools.AudioService.Play();

      mIsNoisePlaying = true;
    }
  }

  // async Task 
  void StopNoisePlayback()
  {
    if ( mIsNoisePlaying )
    {
      mAudioTools.AudioService.Stop();
      mIsNoisePlaying = false;  
    }
  }


  [RelayCommand]
  async Task PlayNoise()
  {
    if (!mIsNoisePlaying)
    {
      OnSetNoisePlayButtonToStop();
      await Task.Run(() => StartNoisePlayback());
    }
    else
    {
      OnSetNoisePlayButtonToStart();
      await Task.Run(() => StopNoisePlayback());
    }
  }


  public void OnRecStopped()
  {
    mIsRecording = false;

    OnSetRecButtonToStart();

    Items.Add(mSession);
    mSession = null ;
  }

  [RelayCommand]
  void Delete(UserSession s)
  {
    // If the list of todos contains
    // given string, remove it from list
    if (Items.Contains(s))
    {
      Items.Remove(s);
    }
  }

  [RelayCommand]
  async Task Tap(UserSession s)
  {
    // Trigger a navigation to the detail page
    //  - See [AppShell] for how to add a routing to the app's navigation
    //  - See [DetailViewModel] for how to resolve the 'Text' query parameter
    await Shell.Current.GoToAsync($"{nameof(DetailPage)}?sessionID={s.ID}");
  }

  //CancellationTokenSource mCancellationTokenSource;

  bool        mIsRecording    = false;
  bool        mIsNoisePlaying = false;
  UserSession mSession        = null ;
  AudioTools  mAudioTools     = null ;

  string mInputFolder  = null ;
  string mOutputFolder = null ;
}
