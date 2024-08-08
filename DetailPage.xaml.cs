using DIGITC2.ViewModel;

namespace DIGITC2;

public partial class DetailPage : ContentPage
{
  DetailViewModel ViewModel => (DetailViewModel)BindingContext;

	public DetailPage(DetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    if (ViewModel != null)
    {
      await ViewModel.LoadSession();
    }
  }

}