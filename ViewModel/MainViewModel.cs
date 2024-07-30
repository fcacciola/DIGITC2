using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DIGITC2_ENGINE;

namespace DIGITC2.ViewModel;

public partial class MainViewModel : ObservableObject
{
  readonly IConnectivity connectivity;
  public MainViewModel(IConnectivity connectivity)
  {
    Items = [];
    this.connectivity = connectivity;

    mAudioService = new AudioService();
    mAudioService.RecStopped += OnRecStopped;
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
  ObservableCollection<string> items;

  [ObservableProperty]
  string text;

  CancellationTokenSource mCancellationTokenSource;

  bool mRecording = false;

  AudioService mAudioService = null;

  Stream mRecordedStream = null ;

  //async Task StartRecording(CancellationToken aCT)
  void StartRecording()
  {
    string lSessionFolder = CreateSessionFolder();
    if ( lSessionFolder != null )
    {
      string lWAVFilePath = Path.Combine(lSessionFolder, "Audio.wav");
    
      mAudioService.StartRecording(lWAVFilePath, 0);

      mRecording = true;
    }

  }

  // async Task 
  void StopRecording()
  {
    mAudioService.StopRecording();
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

  string CreateSessionFolder()
  {
    string lCurrSessionName = Preferences.Get("CurrSession", "Session");

    string localFolder = FileSystem.AppDataDirectory;

    int i = 0 ;

    string rSessionFolder = null ;
    do
    {
      rSessionFolder = Path.Combine(localFolder, $"{lCurrSessionName}_{i}");  
      if ( ! Directory.Exists(rSessionFolder))
      {
        Directory.CreateDirectory(rSessionFolder);
        return rSessionFolder;
      }
      i++ ;
    }
    while ( i < 1000000 ) ;

    return null ;
  }

  public void OnRecStopped()
  {
    mRecording = false;

    OnSetRecButtonToStart();
  }

  [RelayCommand]
  void Delete(string s)
  {
    // If the list of todos contains
    // given string, remove it from list
    if (Items.Contains(s))
    {
      Items.Remove(s);
    }
  }

  [RelayCommand]
  async Task Tap(string s)
  {
    // Trigger a navigation to the detail page
    //  - See [AppShell] for how to add a routing to the app's navigation
    //  - See [DetailViewModel] for how to resolve the 'Text' query parameter
    await Shell.Current.GoToAsync($"{nameof(DetailPage)}?Text={s}");
  }
}
