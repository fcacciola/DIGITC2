using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2
{ 

  public class Tokenizer : LexicalFilter
  {
    public Tokenizer() : base() 
    {
      mSeparators = BytesSource.GetWordSeparators() ; 
    }

    protected override Step Process ( LexicalSignal aInput, Step aStep )
    {
      List<Symbol> lCurrToken = new List<Symbol>();

      List<ArraySymbol> lTokens = new List<ArraySymbol>(); 

      foreach( var lByte in aInput.Symbols )
      {
        if ( IsSeparator(lByte) )
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

      mStep = aStep.Next( new LexicalSignal(lTokens), "Tokens", this) ;

      return mStep ;
    }

    bool IsSeparator( Symbol aS )
    {
      foreach( var lSeparator in mSeparators )
        if ( lSeparator == aS ) 
          return true ;

      return false ;
    }

    protected override string Name => "Tokenize" ;

    List<Symbol> mSeparators = new List<Symbol>() ;


  }

}
