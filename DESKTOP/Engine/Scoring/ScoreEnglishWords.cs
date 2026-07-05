using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ENGINE
{
  public class ScoreEnglishWords : LexicalFilter
  {
    public ScoreEnglishWords() : base()
    {
    }

    protected override void OnSetup()
    {
      LoadDictionary( $"{Session.InputFolder}/EnglishWords.txt" );

      mMinWordLen = Params.GetInt( "MinWordLen" );

      mQuitThreshold = Params.GetDouble("QuitThreshold");
    }

    void LoadDictionary( string aPath )
    {
      if ( !File.Exists(aPath))
      {
        WriteLine2GUI($"Dictionary file not found: {aPath}");
        return ;
      }

      mWords = new HashSet<string>( StringComparer.Ordinal );

      foreach( var lLine in File.ReadLines( aPath ) )
      {
        string lWord = lLine.Trim().ToLowerInvariant();
        if ( lWord.Length > 0 )
          mWords.Add( lWord );
      }

      WriteLine( $"Dictionary loaded: {mWords.Count} words" );
    }

    protected override Packet Process()
    {
      if (mWords == null || mWords.Count == 0)
      {
        WriteLine2GUI("No dictionary loaded, skipping scoring.");

        return CreateOutput(LexicalInput, "English Words score.", new Score(Name, 1.0, 1.0, Score.TypeE.Coverage), false);
      }

      WriteLine2GUI( "Scoring Bytes against dictionary..." );
      Indent();

      var lWordSymbols = LexicalInput.GetSymbols<WordSymbol>();

      int lCountedWords = 0 ;
      int lMatchedWords = 0 ;

      foreach( var lWordSymbol in lWordSymbols )
      {
        string lWord = lWordSymbol.Word.ToLowerInvariant() ;

        if ( lWord.Length >= mMinWordLen )
        {
          lCountedWords += 1;

          if ( mWords.Contains( lWord ) )
          {
            lMatchedWords += 1 ;
          }
        }
      }

      double lCoverage = lCountedWords > 0 ? (double)lMatchedWords / (double)lCountedWords : 0.0 ;

      Score lScore = new Score( Name, lCoverage, 0.5, Score.TypeE.Coverage ) ;
      
      WriteDetailLine( $"Words matched: {lMatchedWords}/{lCountedWords}" );    
      WriteDetailLine( $"Score: {lScore}" );

      Unindent();

      return CreateOutput( LexicalInput, "English Words score.", lScore, lCoverage < mQuitThreshold ) ;
    }

    public override string Name => this.GetType().Name ;

    HashSet<string>  mWords ;
    int              mMinWordLen ;
    double           mQuitThreshold ;
  }
}