using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics;
using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public class ScoreTokenLengthDistribution : LexicalFilter
  {
    public ScoreTokenLengthDistribution() : base() 
    {
    }

    public override void Setup()
    {
      FillReferenceDistribution();

      mQuitThreshold = DContext.Session.Args.GetOptionalInt(Name, "QuitThreshold").GetValueOrDefault(1);
      mFitnessMap    = new FitnessMap(DContext.Session.Args.Get(Name, "FitnessMap"));
    }

    // According to Claude Sonnet 4:
    //
    // Dictionary with word length as key and frequency percentage as value
    public static Dictionary<int, double> WordLengthFrequencyDict = new Dictionary<int, double>
    {
        [1] = 3.68,   // Words like "I", "a"
        [2] = 15.12,  // Words like "of", "to", "in", "it", "is", "be", "as", "at", "so", "we", "he", "by", "or", "on", "do", "if", "me", "my", "up", "an", "go", "no", "us", "am", "her"
        [3] = 18.51,  // Words like "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "way", "who", "boy", "did", "man", "end", "few", "got", "let", "put", "say", "she", "too", "use"
        [4] = 16.44,  // Words like "that", "with", "have", "this", "will", "your", "from", "they", "know", "want", "been", "good", "much", "some", "time", "very", "when", "come", "here", "just", "like", "long", "make", "many", "over", "such", "take", "than", "them", "well", "were", "what"
        [5] = 12.99,  // Words like "which", "their", "would", "there", "could", "other", "after", "first", "never", "these", "think", "where", "being", "every", "great", "might", "shall", "still", "those", "under", "while"
        [6] = 9.42,   // Words like "should", "through", "people", "really", "little", "before", "around", "public", "school", "person", "family", "become", "system", "always", "during", "number", "called", "almost", "coming", "father", "matter", "mother", "others", "rather", "result", "sister", "social", "though", "within"
        [7] = 7.81,   // Words like "because", "without", "another", "between", "nothing", "someone", "already", "against", "feeling", "general", "history", "include", "machine", "morning", "private", "program", "purpose", "service", "several", "special", "student", "teacher", "thought", "through", "working"
        [8] = 5.71,   // Words like "yourself", "possible", "together", "children", "interest", "business", "computer", "decision", "European", "national", "question", "required", "standard", "building", "computer", "everyone", "hospital", "language", "personal", "practice", "question", "research", "security", "services", "somebody", "students", "thinking", "tomorrow", "whatever"
        [9] = 3.96,   // Words like "community", "different", "important", "education", "experience", "following", "political", "situation", "structure", "therefore", "character", "Christmas", "community", "condition", "currently", "developed", "essential, generally", "information", "knowledge", "necessary", "Operation", "president", "professor", "recognize", "scientist", "sometimes", "structure", "technique", "treatment"
        [10] = 2.87,  // Words like "government", "particular", "everything", "management", "themselves", "understand", "developing", "technology", "activities", "background", "conference", "experience", "management", "particular", "population", "production", "understand", "university", "washington", "achievement", "background", "basketball", "california", "commercial", "commission", "comparison", "completion", "conclusion", "conference", "connection"
        [11] = 1.79,  // Words like "development", "information", "environment", "performance", "independent", "application", "competition", "description", "established", "examination", "explanation", "immediately", "independent", "perspective", "preparation", "recognition", "temperature", "traditional", "achievement", "advancement", "appointment", "broadcaster", "calculation", "celebration"
        [12] = 1.08,  // Words like "organization", "relationship", "professional", "responsibility", "construction", "contemporary", "contribution", "conversation", "distribution", "experimental", "international", "neighborhood", "organization", "registration", "relationship", "requirements", "specifically", "successfully", "traditionally"
        [13] = 0.64,  // Words like "communication", "administration", "international", "entertainment", "responsibility", "transportation", "accommodation", "administrator", "investigation", "understanding", "automatically", "characteristic", "communication", "concentration", "consideration", "establishment", "extraordinary", "implementation", "investigation", "manufacturing"
        [14] = 0.37,  // Words like "transformation", "representative", "characteristics", "administration", "implementation", "characteristics", "implementation", "representative", "responsibility", "transformation", "infrastructure", "internationally", "representation", "responsibility", "characteristics", "transformation"
        [15] = 0.21,  // Words like "representatives", "acknowledgement", "characteristics", "responsibilities", "recommendation", "implementation", "characteristics", "infrastructure", "representatives", "responsibilities", "recommendation", "characteristics", "infrastructure", "representatives", "responsibilities"
        [16] = 0.12,  // Words like "internationalization", "characteristics", "responsibilities", "recommendation", "representatives"
        [17] = 0.07,  // Longer technical or compound words
        [18] = 0.04,  // Very long technical terms
        [19] = 0.02,  // Extremely long compound words
        [20] = 0.01   // Exceptionally long words (mostly technical or specialized terms)
    };

    void FillReferenceDistribution()
    {
      List<DPoint> lDPs = new List<DPoint>();

      double lMax = WordLengthFrequencyDict.Values.Max();  

      foreach( var lKV in WordLengthFrequencyDict )
      {
        lDPs.Add( new DPoint( new Sample(null, Convert.ToDouble( (byte)(lKV.Key))), lKV.Value / lMax) );
      }

      mReference = new DTable(lDPs);
    }
    protected override void Process (LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput )
    {
      DContext.WriteLine("Scoring Tokens Length Distribution As Language Words");
      DContext.Indent();

      var lTokenLengths = aInput.Symbols.GetValues();

      double lCorrelation = mReference.ComputeCorrelation(lTokenLengths, (dp,x) => dp.Y ) ;  

      DContext.WriteLine($"Correlation: {lCorrelation}");

      var lLikelihood = (int)Math.Round(lCorrelation * 100) ; 

      var lFitness = mFitnessMap.Map(lLikelihood) ;

      Score lScore = new Score(Name, lLikelihood,lFitness) ;

      rOutput.Add( new Packet(Name, aInputPacket, aInput, "Token-length distribution score.", lScore, lLikelihood < mQuitThreshold));

      DContext.Unindent();
    }

    public override string Name => this.GetType().Name ;

    int        mQuitThreshold;
    FitnessMap mFitnessMap ;
    DTable     mReference = null ;
  }


  //public class ScoreWordLengthDistribution : LexicalFilter
  //{
  //  public ScoreWordLengthDistribution() : base() 
  //  {
  //  }

  //  protected override Step Process ( LexicalSignal aInput, Step aStep )
  //  {
  //    var lDist = aInput.GetDistribution();

  //    var lHistogram = new Histogram(lDist) ;

  //    Score lScore = null ; //new StatisticalScore(aInput, aInput.GetSamples(), lHistogram, 0) ;

  //    mStep = aStep.Next( "Word-length distribution score", this, lScore) ;

  //    return mStep ;
  //  }

  //  protected override string Name => "ScoreWordLengthDistribution" ;

  //}


}
