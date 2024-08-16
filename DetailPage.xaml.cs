using DIGITC2.ViewModel;

namespace DIGITC2;

public partial class DetailPage : ContentPage
{
  DetailViewModel ViewModel => (DetailViewModel)BindingContext;

	public DetailPage(DetailViewModel vm)
	{
		InitializeComponent();
    vm.SetPlayButtonToStart += OnSetPlayButtonToStart;
    vm.SetPlayButtonToStop  += OnSetPlayButtonToStop;
		BindingContext = vm;
	}

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    if (ViewModel != null)
    {
      await ViewModel.Setup();
    }
  }

  private void OnSetPlayButtonToStart()
  {
    //PlayButton.BackgroundColor = Colors.Red;
    PlayButton.Text = "PLAY";
  }

  private void OnSetPlayButtonToStop()
  {
    //PlayButton.BackgroundColor = Colors.Magenta;
    PlayButton.Text = "STOP";
  }

}