using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ENGINE
{

public class WordScorer
{
  private readonly SymSpell        mSymSpell;
  private readonly HashSet<string> mPlainDictionary;
  private readonly int             mMaxEditDistance;

  public WordScorer(string aDictionaryPath, int aMaxEditDistance = 2 )
  {
    mMaxEditDistance          = aMaxEditDistance;

    mSymSpell = new SymSpell(initialCapacity: 82765, maxDictionaryEditDistance: aMaxEditDistance);

    mSymSpell.LoadDictionary(aDictionaryPath, termIndex: 0, countIndex: 1);
    
    mPlainDictionary = new HashSet<string>( File.ReadLines(aDictionaryPath)
                                                .Select(lLine => lLine.Split(' ')[0])
                                                .Where(lWord => lWord.Length > 0));
  }

  public double ScoreToken(string aToken)
  {
    if (string.IsNullOrEmpty(aToken))
    {
      return 0.0;
    }

    // Step 1: perfect whole-word fast path.
    if (mPlainDictionary.Contains(aToken))
    {
      return 1.0;
    }

    // Step 3: fuzzy lookup via SymSpell.
    List<SymSpell.SuggestItem> lSuggestions = mSymSpell.Lookup(aToken, SymSpell.Verbosity.Closest, mMaxEditDistance);
    if (lSuggestions.Count == 0)
    {
      return 0.0;
    }

    SymSpell.SuggestItem lBest      = lSuggestions[0];
    int                  lDistance  = lBest.distance;
    int                  lNormLen   = Math.Max(aToken.Length, lBest.term.Length);

    if (lNormLen == 0)
    {
      return 0.0;
    }

    double lScore = 1.0 - (double)lDistance / lNormLen;

    if (!IsPlausibleWord(aToken))
      lScore *= 0.5;

    return Math.Max(0.0, lScore); 
  }

  private bool IsPlausibleWord(string aToken)
  {
    foreach (char lCh in aToken)
    {
      bool lIsLower = lCh >= 'a' && lCh <= 'z';

      if (!lIsLower)
      {
        return false;
      }
    }
    return true;
  }
}


public class ScoreEnglishWords : LexicalFilter
{
  public ScoreEnglishWords() : base()
  {
  }

  protected override void OnSetup()
  {
    string lDictionaryPath = $"{Session.InputFolder}/frequency_dictionary_en_82_765.txt";

    if ( File.Exists(lDictionaryPath) )
    {
      mScorer = new WordScorer(lDictionaryPath);

      mQuitThreshold = Params.GetDouble("QuitThreshold");
    }
  }

  protected override Packet Process()
  {
    if ( mScorer == null )
    {
      WriteLine2GUI("No dictionary loaded, skipping scoring.");

      return CreateOutput(LexicalInput, "English Words score.", new Score(Name, 1.0, 1.0, Score.TypeE.Coverage), false);
    }

    WriteLine2GUI( "Scoring Bytes against dictionary..." );
    Indent();

    var lWordSymbols = LexicalInput.GetSymbols<WordSymbol>();

    double lScoreSum = 0.0;

    foreach ( var lWordSymbol in lWordSymbols )
    {
      string lWord = lWordSymbol.Word.ToLowerInvariant() ;

      double lWordScore = mScorer.ScoreToken(lWord);

      lScoreSum += lWordScore;
    }

    double lCoverage = lWordSymbols.Count > 0 ? lScoreSum / lWordSymbols.Count : 0.0 ;

    Score lScore = new Score( Name, lCoverage, 1.5, Score.TypeE.Coverage ) ;
      
    WriteDetailLine( $"Score: {lScore}" );

    Unindent();

    return CreateOutput( LexicalInput, "English Words score.", lScore, lCoverage < mQuitThreshold ) ;
  }

  public override string Name => this.GetType().Name ;

  WordScorer mScorer ;
  double     mQuitThreshold ;
}
}