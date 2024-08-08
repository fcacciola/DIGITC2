using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace DIGITC2.ViewModel;

[QueryProperty(nameof(SessionID), "sessionID")]
public partial class DetailViewModel : ObservableObject
{
    public DetailViewModel()
    {
      slices  = new ObservableCollection<Slice>();
      results = new ObservableCollection<Result>();
    }

    [ObservableProperty]
    string sessionID;

    [ObservableProperty] ObservableCollection<Slice> slices;

    [ObservableProperty] ObservableCollection<Result> results;

    [RelayCommand]
    async Task Analyze()
    {
      await Task.Run( () => DoAnalyze() );
    }

    [RelayCommand]
    async Task Play()
    {
      await Task.Run( () => DoAnalyze() );
    }

    [RelayCommand]
    async Task GoBack()
    {
      await Shell.Current.GoToAsync("..");
    }

    Session GetSession()
    {
      return Session.FromID(sessionID);
    }

    public async Task LoadSession()
    {
      await Task.Run(() => 
      {
        Session lSession = GetSession();
        if ( lSession != null && lSession.Analysis != null)
          PopulateParts( lSession.Analysis );
      }
      );
    }

    void DoAnalyze()
    {
      Session lSession = GetSession();
      if ( lSession != null) 
      {  
        lSession.Analysis = new Analytics(new Result("Overall result"));

        lSession.Analysis.AddPart( new Slice("Slice 0"), new Result("Result 0" ) ) ;
        lSession.Analysis.AddPart( new Slice("Slice 1"), new Result("Result 1" ) ) ;
        lSession.Analysis.AddPart( new Slice("Slice 2"), new Result("Result 2" ) ) ;

        lSession.SaveAnalysis();

        PopulateParts( lSession.Analysis );
      }
    }

    void PopulateParts( Analytics aAnalysis )
    {
      foreach( var lPart in aAnalysis.Parts ) 
      {   
        slices .Add( lPart.Slice  );
        results.Add( lPart.Result );
      }
    }
}
