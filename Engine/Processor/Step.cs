using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2
{
  public class Branch : IWithState
  {
    public class Selection
    {
      public Selection( string aActiveBranches )
      {
        if ( ! string.IsNullOrEmpty(aActiveBranches) )
        {
          foreach( string lActiveBranch in aActiveBranches.Split(',') )  
            if ( !lActiveBranch.StartsWith("!") )
              mActiveBranches.Add( lActiveBranch ); 
        }
      }

      public bool IsActive( string aBranch )
      {
        if ( mActiveBranches.Count > 0 )
        {
          return ( mActiveBranches.Find( s => s == aBranch ) != null ) ;
        }
        else return true ;
      }

      List<string> mActiveBranches = new List<string>();
    }

    public Branch( Branch aPrev, Signal aSignal, string aLabel, Score aScore = null, bool aQuit = false, IWithState aData = null )
    {
      Prev   = aPrev ;
      Signal = aSignal;
      Label  = aLabel ;
      Score  = aScore;
      Quit   = aQuit;
      Data   = aData;
    }

    public T GetData<T>() where T : class => Data as T ;

    public State GetState()
    {
      State rS = new State("Branch",$"{Label}") ;

      if ( Signal != null )
        rS.Add( Signal.GetState() );  

      if ( Score != null ) 
        rS.Add( Score.GetState() );  

      if ( Data != null ) 
        rS.Add( Data.GetState() );  

      return rS ;
    }

    public Branch     Prev   ;
    public Signal     Signal ;
    public string     Label  ;
    public Score      Score  ;
    public bool       Quit   ;
    public IWithState Data   ;
    public int        Idx    ;
  }

  public class Step : IWithState
  {
    public Step( Filter aFilter, List<Branch> aBranches )
    {
      Filter = aFilter; 
      Branches.AddRange( aBranches) ; 

    }
    public State GetState()
    {
      State rS = new State("Step",$"{Idx}") ;

      Branches.ForEach( b => rS.Add( b.GetState() ) ) ;

      return rS ;
    }

    public override string ToString() => Filter.ToString();

    public int          Idx ;
    public Filter       Filter ;
    public List<Branch> Branches = new List<Branch>();

  }
 
}
