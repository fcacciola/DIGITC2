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

    CancellationTokenSource mCancellationTokenSource ;

    bool mRecording = false;  

    AudioService mAudioService = null ;

    async Task StartRecording( CancellationToken aCT )
    {
      mAudioService.StartRecording(0);
    }

    async Task StopRecording()
    {
      mAudioService.StopRecording();
    }

    [RelayCommand]
    async Task REC()
    {
      if ( ! mRecording )
      {
        mCancellationTokenSource = new CancellationTokenSource();
        CancellationToken lToken = mCancellationTokenSource.Token;
        mRecording = true;
        OnSetRecButtonToStart(); 
        Task.Run(async () => await StartRecording(lToken));  
        return ;
      }
      else
      {
        if ( mCancellationTokenSource != null ) 
        {
          mCancellationTokenSource.Cancel();
          mCancellationTokenSource = null;
          mRecording = false;
          OnSetRecButtonToStop();
          Task.Run(async () => await StopRecording());  
        }
      }
        // Assure there's an internet connection
        // else show an alert
        //if(connectivity.NetworkAccess != NetworkAccess.Internet)
        //{
        //    await Shell.Current.DisplayAlert("Uh Oh!", "No Internet", "OK");
        //    return;
        //}

        //string lTT = $"Text. Signal has samples";
        //// Add text to list of todos
        //Items.Add(lTT);
        
        //// Reset Text
        //Text = string.Empty;
    }

    [RelayCommand]
    void Delete(string s)
    {
        // If the list of todos contains
        // given string, remove it from list
        if(Items.Contains(s))
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
