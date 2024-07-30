using DIGITC2.ViewModel;

namespace DIGITC2;

public partial class MainPage : ContentPage
{

	public MainPage(MainViewModel vm)
	{
		InitializeComponent();
    vm.SetRecButtonToStart += OnSetRecButtonToStart;
    vm.SetRecButtonToStop  += OnSetRecButtonToStop;
		BindingContext = vm;
	}

  private void OnSetRecButtonToStart()
  {
    RecButton.BackgroundColor = Colors.Red ;
    RecButton.Text = "START Recording";
  }

  private void OnSetRecButtonToStop()
  {
    RecButton.BackgroundColor = Colors.Blue ;
    RecButton.Text = "STOP Recording";
  }


}

