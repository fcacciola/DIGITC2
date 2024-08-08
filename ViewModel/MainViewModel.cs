using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DIGITC2_ENGINE;

namespace DIGITC2.ViewModel;



public partial class MainViewModel : ObservableObject
{
  class Settings
  {
    internal AudioDevice InputDevice  = null ;
    internal AudioDevice OutputDevice = null ;
  }

  Settings mSettings = null ;

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

  public void OnSetRecButtonToStart()
  {
    SetRecButtonToStart?.Invoke();
  }

  public void OnSetRecButtonToStop()
  {
    SetRecButtonToStop?.Invoke();
  }
 
  [ObservableProperty]
  ObservableCollection<string> noiseSources;

  [ObservableProperty]
  ObservableCollection<Session> items;

  [ObservableProperty]
  string selectedNoise;

  [ObservableProperty]
  string text;

  //CancellationTokenSource mCancellationTokenSource;

  bool         mRecording    = false;
  AudioService mAudioService = null;
  Session      mSession      = null ;

  Session CreateNewSession()
  {
    if ( ! Directory.Exists(Session.RootFolder) )
      Directory.CreateDirectory(Session.RootFolder);

    Session rSession = new Session();

    string lCurrSessionName = Preferences.Get("CurrSession", "Session");

    int i = 0 ;

    do
    {
      rSession.ID = $"{lCurrSessionName}_{i}";  
      rSession.Folder = Path.Combine(Session.RootFolder, rSession.ID);  
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

    return null ;
  }

  void DoSetup()
  {
    mAudioService = new AudioService();
    mAudioService.RecStopped += OnRecStopped;

    mSettings = new Settings();

    mSettings.InputDevice  = FindDevice(Preferences.Get("InputDevice",  "Microphone Array"), mAudioService.EnumInputDevices() ) ;
    mSettings.OutputDevice = FindDevice(Preferences.Get("OutputDevice", "Microphone Array"), mAudioService.EnumOutputDevices() ) ;
  }

  public async Task Setup()
  {
    await Task.Run(() => DoSetup() );
  }

  public async Task LoadSessions()
  {
    await Task.Run(() => 
    {
      string lRootFolder = Session.RootFolder ;

      foreach( var lSessionFolder in Directory.GetDirectories(lRootFolder)) 
      {
        Session lSession = Session.FromFolder(lSessionFolder);
        if ( lSession != null )
          Items.Add(lSession);
      }
    }
    );
  }

  //async Task StartRecording(CancellationToken aCT)
  void StartRecording()
  {
    if ( mSettings.InputDevice != null )
    {
      mSession = CreateNewSession();

      if ( mSession != null )
      {
        mSession.WAVFile = Path.Combine(mSession.Folder, "Audio.wav");

        mAudioService.StartRecording(mSession.WAVFile, mSettings.InputDevice.Number);

        mRecording = true;
      }
    }
  }

  // async Task 
  void StopRecording()
  {
    mAudioService.StopRecording();
  }

  [RelayCommand]
  async Task LoadAudio()
  {

  }

  [RelayCommand]
  async Task REC()
  {
    if (!mRecording)
    {
      StartRecording();
      OnSetRecButtonToStop();
      return;
    }
    else
    {
      StopRecording();
    }
  }

  [RelayCommand]
  async Task PlayNoise()
  {

  }


  public void OnRecStopped()
  {
    mRecording = false;

    OnSetRecButtonToStart();

    Items.Add(mSession);
    mSession = null ;
  }

  [RelayCommand]
  void Delete(Session s)
  {
    // If the list of todos contains
    // given string, remove it from list
    if (Items.Contains(s))
    {
      Items.Remove(s);
    }
  }

  [RelayCommand]
  async Task Tap(Session s)
  {
    // Trigger a navigation to the detail page
    //  - See [AppShell] for how to add a routing to the app's navigation
    //  - See [DetailViewModel] for how to resolve the 'Text' query parameter
    await Shell.Current.GoToAsync($"{nameof(DetailPage)}?sessionID={s.ID}");
  }
}
