using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

using DIGITC2_ENGINE;

namespace DIGITC2.ViewModel;

[QueryProperty(nameof(SessionID), "sessionID")]
public partial class DetailViewModel : ObservableObject
{
  public DetailViewModel()
  {
    slices = new ObservableCollection<string>();
    results = new ObservableCollection<string>();
  }

  [ObservableProperty]
  string sessionID;

  [ObservableProperty] ObservableCollection<string> slices;

  [ObservableProperty] ObservableCollection<string> results;

  public event Action SetPlayButtonToStart;
  public event Action SetPlayButtonToStop;

  public void OnSetPlayButtonToStart()
  {
    SetPlayButtonToStart?.Invoke();
  }

  public void OnSetPlayButtonToStop()
  {
    SetPlayButtonToStop?.Invoke();
  }

  void DoSetup()
  {
    mAudioTools = new AudioTools();
    mSession = GetSession();
    if ( mSession != null && mSession.Summary != null)
      PopulateUI(mSession.Summary);
  }

  public async Task Setup()
  {
    await Task.Run(() => DoSetup() );
  }

  //async Task StartRecording(CancellationToken aCT)
  void StartPlayback()
  {
    if ( mIsPlaying )
      StopPlayback();

    if ( mSession != null && !string.IsNullOrEmpty(mSession.WAVFile) && File.Exists(mSession.WAVFile) )
    {
      mAudioTools.AudioService.Load(mSession.WAVFile);
      mAudioTools.AudioService.Play();

      mIsPlaying = true;
    }
  }

  // async Task 
  void StopPlayback()
  {
    if ( mIsPlaying )
    {
      mAudioTools.AudioService.Stop();
      mIsPlaying = false;  
    }
  }

  [RelayCommand]
  async Task Analyze()
  {
    await Task.Run(() => DoAnalyze());
  }

  [RelayCommand]
  async Task Play()
  {
    if (!mIsPlaying)
    {
      OnSetPlayButtonToStop();
      await Task.Run(() => StartPlayback());
    }
    else
    {
      OnSetPlayButtonToStart();
      await Task.Run(() => StopPlayback());
    }
  }

  [RelayCommand]
  async Task GoBack()
  {
    await Shell.Current.GoToAsync("..");
  }

  UserSession GetSession()
  {
    return UserSession.FromID(sessionID);
  }


  void DoAnalyze()
  {
    List<string> lProcessors = new List<string>{"TapCode"};

    UserSession lSession = GetSession();

    if ( lSession != null && ! string.IsNullOrEmpty(lSession.WAVFile) && File.Exists(lSession.WAVFile) )
    {
      lSession.Summary = Analyzer.Analyze(lSession.Folder, lSession.WAVFile, lProcessors).Summary;

      if ( lSession.Summary != null ) 
      {
        lSession.SaveOutcome();

        PopulateUI(lSession.Summary);
      }
    }
  }

  void PopulateUI(OutcomeSummary aOutcome)
  {
    //foreach (var lSlice in aOutcome.Pipelines)
    //{
    //  slices.Add(lSlice.Name);
    //  results.Add(lSlice.Name);
    //}
  }

  AudioTools  mAudioTools = null ;
  bool        mIsPlaying  = false ;
  UserSession mSession    = null ;
}
