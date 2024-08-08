using DIGITC2.ViewModel;

namespace DIGITC2;

public partial class MainPage : ContentPage
{
  MainViewModel ViewModel => (MainViewModel)BindingContext;

  public MainPage(MainViewModel vm)
  {
    InitializeComponent();
    vm.SetRecButtonToStart += OnSetRecButtonToStart;
    vm.SetRecButtonToStop  += OnSetRecButtonToStop;
    BindingContext = vm;

    Preferences.Set("InputDevice" , "Microphone Array");
    Preferences.Set("OutputDevice", "Altavoces");
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    if (ViewModel != null)
    {
      if ( ! mPageInitialized )
      {
        mPageInitialized = true; 
        await ViewModel.Setup();
        await ViewModel.LoadSessions();
      }
    }
  }

  private void OnSetRecButtonToStart()
  {
    RecButton.BackgroundColor = Colors.Red;
    RecButton.Text = "START Recording";
  }

  private void OnSetRecButtonToStop()
  {
    RecButton.BackgroundColor = Colors.Magenta;
    RecButton.Text = "STOP Recording";
  }

  bool mPageInitialized = false ;
}

