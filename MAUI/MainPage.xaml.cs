﻿using DIGITC2.ViewModel;

namespace DIGITC2;

public partial class MainPage : ContentPage
{
  MainViewModel ViewModel => (MainViewModel)BindingContext;

  public MainPage(MainViewModel vm)
  {
    InitializeComponent();

    vm.SetRecButtonToStart       += OnSetRecButtonToStart;
    vm.SetRecButtonToStop        += OnSetRecButtonToStop;
    vm.SetNoisePlayButtonToStart += OnSetNoisePlayButtonToStart;
    vm.SetNoisePlayButtonToStop  += OnSetNoisePlayButtonToStop;

    BindingContext = vm;

    Preferences.Set("InputDevice" , ""); // Use Default
    Preferences.Set("OutputDevice", ""); // Use Default
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

  private void OnSetNoisePlayButtonToStart()
  {
    NoiseButton.BackgroundColor = Colors.DarkBlue;
    NoiseButton.Text = "PLAY Noise Background";
  }

  private void OnSetNoisePlayButtonToStop()
  {
    NoiseButton.BackgroundColor = Colors.DarkCyan;
    NoiseButton.Text = "STOP Noise Background";
  }

  bool mPageInitialized = false ;
}
