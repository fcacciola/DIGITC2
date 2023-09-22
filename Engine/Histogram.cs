using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DIGITC2
{

  public class Histogram : IEnumerable< KeyValuePair<Symbol, int> >
  {
    public void Add( Symbol aSymbol )
    {
      if ( mMap.ContainsKey(aSymbol))
           mMap[aSymbol] = mMap[aSymbol] + 1  ;
      else mMap.Add(aSymbol, 1) ;
    }

    public int Count => mMap.Count; 

    public double ShannonEntropy
    {
      get
      {
        if ( mShannonEntropy == null )
          CalculateShannonEntropy();
        return mShannonEntropy.Value;
      }
    }

    public class Bin
    {
      public Bin( Symbol aS, int aF ) { Symbol = aS ; Frequency = aF; }

      public Symbol Symbol ;
      public int    Frequency ;
    }

    public List<Bin> Sorted
    {
      get
      {
        if ( mSorted == null )
          BuildSorted();
        return mSorted; 
      }
    }

    public Plot CreatePlot( Plot.Options aOptions )
    {
      Plot rPlot = new Plot( aOptions );

      rPlot.AddData( Sorted ) ;

      return rPlot; 
    }

    void BuildSorted()
    {
      mSorted = new List<Bin>();
      foreach( var lKV in mMap )
        mSorted.Add( new Bin(lKV.Key, lKV.Value) );

      mSorted.Sort( (x,y) => y.Frequency.CompareTo(x.Frequency) ) ;
    }

    void CalculateShannonEntropy()
    {
      mShannonEntropy = 0;
      foreach (var lItem in mMap)
      {
        var lF = (double)lItem.Value / Count;
        mShannonEntropy -= lF * (Math.Log(lF) / Math.Log(2));
      }
    }

    public IEnumerator<KeyValuePair<Symbol, int>> GetEnumerator()
    {
        return mMap.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        //forces use of the non-generic implementation on the Values collection
        return mMap.GetEnumerator();
    }

    Dictionary<Symbol,int> mMap = new Dictionary<Symbol,int>() ;

    double? mShannonEntropy = null;

    List<Bin> mSorted = null ;
  }




}
