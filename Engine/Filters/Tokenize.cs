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
   
  public class Tokenizer : LexicalFilter
  {
    public Tokenizer() : base() 
    {
    }

    protected override void Process(LexicalSignal aInput, ProcessingToken aInputBranch, List<ProcessingToken> rOutput)
    {
      Process( new TokenSeparators() , aInput, aInputBranch, rOutput);
    }

    void Process( TokenSeparators aSeparators, LexicalSignal aInput, ProcessingToken aInputBranch, List<ProcessingToken> rOutput)
    {
      List<Symbol> lCurrToken = new List<Symbol>();

      List<ArraySymbol> lTokens = new List<ArraySymbol>(); 

      foreach( var lByte in aInput.Symbols )
      {
        if ( aSeparators.IsSeparator(lByte) )
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

      rOutput.Add( new ProcessingToken(aInputBranch, new LexicalSignal(lTokens), aSeparators.Label ) ) ;
    }


    public override string Name => this.GetType().Name ;



  }

}
