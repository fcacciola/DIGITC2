using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2_ENGINE
{
  public abstract class BranchData
  {

  }

  public class Branch 
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

    public Branch( Branch aPrev, Signal aSignal, string aName, Score aScore = null, bool aShouldQuit = false, BranchData aData = null )
    {
      Prev       = aPrev ;
      Signal     = aSignal;
      Name       = aName ;
      Score      = aScore;
      ShouldQuit = aShouldQuit;
      Data       = aData;
    }

    static public Branch Quit( Branch aPrev, string aLabel ) => new Branch(aPrev, null, aLabel, null, true, null );

    public T GetData<T>() where T : class => Data as T ;

    public Branch     Prev       ;
    public Signal     Signal     ;
    public string     Name       ;
    public Score      Score      ;
    public bool       ShouldQuit ;
    public BranchData Data       ;
    public int        Idx        ;
  }

   
}
