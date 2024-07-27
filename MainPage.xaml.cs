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
    RecButton.Text = "START Recording";
    RecButton.BackgroundColor = Colors.Red ;
  }

  private void OnSetRecButtonToStop()
  {
    RecButton.Text = "STOP Recording";
    RecButton.BackgroundColor = Colors.Blue ;
  }


}

