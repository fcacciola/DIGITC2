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

    protected override Packet Process ( LexicalSignal aInput, Config aConfig, Packet aInputPacket, List<Config> rBranches )
    {
      Process( new TokenSeparators() , aInput, aInputPacket, rOutput);
    }

    void Process( TokenSeparators aSeparators, LexicalSignal aInput, Packet aInputPacket, List<Packet> rOutput)
    {
      DContext.WriteLine("Tokenzing by Clustering Bytes into Array Separated by a list of Separator bytes ( ,;.:-!¡¿?()[]{}/$%&#@*=+\\\"')");
      DContext.Indent();

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

      DContext.WriteLine($"Tokens:{Environment.NewLine}{string.Join(Environment.NewLine, lTokens.ConvertAll( b => b.Meaning) ) }" ) ;

      rOutput.Add( new Packet(Name, aInputPacket, new LexicalSignal(lTokens), aSeparators.Label ) ) ;

      DContext.Unindent();
    }


    public override string Name => this.GetType().Name ;



  }

}
