using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ENGINE ;
  
public class Score
{
  public enum TypeE { Coverage, Correlation, Combined }

  public Score( string aFilterName, double aValue, double aWeight, TypeE aType )
  {
    FilterName = aFilterName;
    Value      = aValue;
    Weight     = aWeight;
    Type       = aType;
  }

  public static Score Combine( List<Score> aScores) 
  {
    
    double lCoverageAvg    = 1.0;
    double lCorrelationSum = 0.0;

    foreach (Score lScore in aScores) 
    {
      if (lScore.IsCoverage)
           lCoverageAvg    *= lScore.WeightedValue;
      else lCorrelationSum += lScore.WeightedValue;
    }

    return new Score("COMBINED", lCoverageAvg * lCorrelationSum, 1.0, TypeE.Combined);
  }

  public double WeightedValue => Value * Weight;

  public bool IsCoverage => Type == TypeE.Coverage;

  public override string ToString() => $"{FilterName} SCORE: {Type} {Value:F3} W:{Weight:F2}";

  public string FilterName ;
  public double Value = 0.0;
  public double Weight = 0.0;
  public TypeE  Type ;
}

