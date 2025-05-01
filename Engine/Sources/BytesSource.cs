using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using NWaves.Audio;
using NWaves.Signals;
using NWaves.Signals.Builders;

namespace DIGITC2_ENGINE
{
  public class BytesSource : LexicalSource
  {
    public static BytesSource FromText( string aText, string aCharSet)
    {
      Encoding lEncoding = Encoding.GetEncoding(aCharSet);

      BytesSource rSource = new BytesSource();

      rSource.mBytes = lEncoding.GetBytes(aText);
      return rSource; 
    }

    public static List<Symbol> GetWordSeparators()
    {
      List<Symbol> rS = new List<Symbol>();
      var lSeparatorSource = BytesSource.FromText(" ,;.:-!¡¿?()[]{}/$%&#@*=+\\\"'","us-ascii") ;
      var lSeparatorBytes = lSeparatorSource.CreateSignal();
      rS.AddRange((lSeparatorBytes as LexicalSignal).Symbols);
      return rS;
    }

    protected override Signal DoCreateSignal() 
    {
      mSymbols.Clear(); 
      foreach( byte lByte in mBytes )
      {
        ByteSymbol lBS = new ByteSymbol(mSymbols.Count(),lByte); 
        mSymbols.Add(lBS);

      }
      LexicalSignal rSignal = new LexicalSignal(mSymbols);
      return rSignal; 
    }

    public override string Name => "Bytes";  

    byte[] mBytes ;
  }


}
