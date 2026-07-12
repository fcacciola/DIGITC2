using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ENGINE ;
  
public class Score
{
  public enum TypeE { Coverage, Correlation, Boundless, Combined }

  public Score( string aFilterName, double aValue, TypeE aType )
  {
    FilterName = aFilterName;
    Value      = aValue;
    Type       = aType;
  }

  public static double ShiftToFraction( double value)
  {
    double absIntPart = Math.Truncate(Math.Abs(value));

    if (absIntPart == 0) 
      return value;

    int digits = (int)Math.Floor(Math.Log10(absIntPart)) + 1;

    return value / Math.Pow(10, digits);
  }

  public static Score Combine( List<Score> aScores) 
  {
    double lCoverageAvg    = 1.0;
    double lCorrelationSum = 0.0;
    double lBoundlessSum   = 0.0;

    foreach (Score lScore in aScores) 
    {
      if (lScore.IsCoverage)
           lCoverageAvg    *= lScore.Value;
      else if (lScore.Type == TypeE.Correlation)
           lCorrelationSum += lScore.Value;
      else if (lScore.Type == TypeE.Boundless)
        lBoundlessSum += lScore.Value;
    }


    // Boundless score is a SECONDARY counting score (like number of pulses or taps)
    // In the combination, the primary score is the INTEGER part and the secondary score is the decimal part.

    double lCombinedValue = ( lCoverageAvg * lCorrelationSum * 100 ) + ShiftToFraction( lBoundlessSum );

    return new Score("COMBINED", lCombinedValue,TypeE.Combined);
  }

  public bool IsCoverage => Type == TypeE.Coverage;

  public override string ToString() => $"{FilterName} SCORE: {Type} {Value:F3}";

  public string FilterName ;
  public double Value  = 0.0;
  public TypeE  Type ;
}

