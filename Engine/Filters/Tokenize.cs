using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{ 

  public class TokenSeparators
  {
    public static char[] GetWordSeparators()
    {
      return " ,;.:-!¡¿?()[]{}/$%&#@*=+\\\"'".ToCharArray();
    }

    public TokenSeparators()
    {
      Label = "Standard Token Separators" ;

      mSeparators = BytesSource.GetWordSeparators() ; 
    }

    public string Label ;

    public bool IsSeparator( Symbol aS )
    {
      foreach( var lSeparator in mSeparators )
        if ( lSeparator == aS ) 
          return true ;

      return false ;
    }

    List<Symbol> mSeparators = new List<Symbol>() ;
  }
   
  public class Tokenize : LexicalFilter
  {
    public Tokenize() : base() 
    {
    }

    protected override Packet Process()
    {
      List<Symbol> lCurrToken = new List<Symbol>();

      List<ArraySymbol> lTokens = new List<ArraySymbol>(); 

      TokenSeparators lSeparators = new TokenSeparators();

      foreach( var lByte in LexicalInput.Symbols )
      {
        if ( lSeparators.IsSeparator(lByte) )
        { 
          if ( lCurrToken.Count > 0 ) 
          {
            lTokens.Add( new ArraySymbol(lTokens.Count,lCurrToken) ); 
          }

          lCurrToken = new List<Symbol> ();
        }
        else
        {
          lCurrToken.Add( lByte );  
        }
      }

      if ( lCurrToken.Count > 0 ) 
      {
        lTokens.Add( new ArraySymbol(lTokens.Count,lCurrToken) ); 
      }

      WriteLine($"Tokens:{Environment.NewLine}{string.Join(Environment.NewLine, lTokens.ConvertAll( b => b.Meaning) ) }" ) ;

      return CreateOutput( new LexicalSignal(lTokens), lSeparators.Label ) ;
    }

    public override string Name => this.GetType().Name ;



  }

}
