using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;

using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;

using Newtonsoft.Json.Serialization;

using NWaves.Operations;
using NWaves.Signals;

namespace DIGITC2_ENGINE
{
public class PolybiusSquare
{
  static List<string> sLatinAlphabet_Simple = new List<string>{ "A", "B", "C", "D", "E"
                                                              , "F", "G", "H", "I", "K"
                                                              , "L", "M", "N", "O", "P"
                                                              , "Q", "R", "S", "T", "U"
                                                              , "V", "W", "X", "Y", "Z"
                                                              } ; 

  static List<string> sLatinAlphabet_Extended = new List<string>{ "A", "B", "C", "D", "E" , "F"
                                                                , "G", "H", "I", "J", "K" , "L"
                                                                , "M", "N", "O", "P", "Q" , "R"
                                                                , "S", "T", "U", "V", "W" , "X"
                                                                , "Y", "Z", " ", ".", "," , "?"
                                                                , "!", "-", "(", ")", "0" , "1"
                                                                } ;

  static List<string> sBinary = new List<string>{ "00",  "11", "00"
                                                , "11",  "?", "11"
                                                , "00",  "11", "00"
                                                } ;

  static List<string> sBinary_2_1 = new List<string>{ "0",  "0", "1", "1", "0", "0"
                                                    , "0",  "0", "1", "1", "0", "0"
                                                    , "1",  "1", "?", "?", "1", "1"
                                                    , "1",  "1", "?", "?", "1", "1"
                                                    , "0",  "0", "1", "1", "0", "0"
                                                    , "0",  "0", "1", "1", "0", "0"
                                                    } ;

  static List<string> sBinary_2_1_Guarded = new List<string>{ "0",  "0", "?", "1", "1", "?", "0", "0"
                                                            , "0",  "0", "?", "1", "1", "?", "0", "0"
                                                            , "?",  "?", "?", "?", "?", "?", "?", "?"
                                                            , "1",  "1", "?", "?", "?", "?", "1", "1"
                                                            , "1",  "1", "?", "?", "?", "?", "1", "1"
                                                            , "?",  "?", "?", "?", "?", "?", "?", "?"
                                                            , "0",  "0", "?", "1", "1", "?", "0", "0"
                                                            , "0",  "0", "?", "1", "1", "?", "0", "0"
                                                            } ;

  static List<string> sBinary_3_1 = new List<string>{ "0",  "0", "0",  "1", "1", "1", "0",  "0", "0"
                                                    , "0", "00", "0",  "1", "1", "1", "0", "00", "0"
                                                    , "0",  "0", "0",  "1", "1", "1", "0",  "0", "0"
                                                    , "1",  "1", "1",  "?", "?", "?", "1",  "1", "1"
                                                    , "1", "11", "1",  "?", "?", "?", "1", "11", "1"
                                                    , "1",  "1", "1",  "?", "?", "?", "1",  "1", "1"
                                                    , "0",  "0", "0",  "1", "1", "1", "0",  "0", "0"
                                                    , "0", "00", "0",  "1", "1", "1", "0", "00", "0"
                                                    , "0",  "0", "0",  "1", "1", "1", "0",  "0", "0"
                                                    } ;


  static List<string> sBinary_3_1_Guarded = new List<string>{ "0",  "0", "0", "?",  "1",  "1", "1", "?", "0",  "0", "0"
                                                            , "0", "00", "0", "?",  "1", "11", "1", "?", "0", "00", "0"
                                                            , "0",  "0", "0", "?",  "1",  "1", "1", "?", "0",  "0", "0"
                                                            , "?",  "?", "?", "?",  "?",  "?", "?", "?", "?",  "?", "?"
                                                            , "1",  "1", "1", "?",  "?",  "?", "?", "?", "1",  "1", "1"
                                                            , "1", "11", "1", "?",  "?",  "?", "?", "?", "1", "11", "1"
                                                            , "1",  "1", "1", "?",  "?",  "?", "?", "?", "1",  "1", "1"
                                                            , "?",  "?", "?", "?",  "?",  "?", "?", "?", "?",  "?", "?"
                                                            , "0",  "0", "0", "?",  "1",  "1", "1", "?", "0",  "0", "0"
                                                            , "0", "00", "0", "?",  "1", "11", "1", "?", "0", "00", "0"
                                                            , "0",  "0", "0", "?",  "1",  "1", "1", "?", "0",  "0", "0"
                                                            } ;

  public PolybiusSquare( List<string> aAlphabet, string aName ) 
  { 
    Name     = aName ;
    Alphabet = aAlphabet ;
    Size = (int) Math.Ceiling( (Math.Sqrt( aAlphabet.Count ) ));
  }

  static public PolybiusSquare LatinAlphabet_Simple   => new PolybiusSquare(sLatinAlphabet_Simple  , "LatinAlphabet_Simple");
  static public PolybiusSquare LatinAlphabet_Extended => new PolybiusSquare(sLatinAlphabet_Extended, "LatinAlphabet_Extended");
  static public PolybiusSquare Binary                 => new PolybiusSquare(sBinary                , "Binary");
  static public PolybiusSquare Binary_2_1             => new PolybiusSquare(sBinary_2_1            , "Binary_2_1");
  static public PolybiusSquare Binary_2_1_Guarded     => new PolybiusSquare(sBinary_2_1_Guarded    , "Binary_2_1_Guarded");
  static public PolybiusSquare Binary_3_1             => new PolybiusSquare(sBinary_3_1            , "Binary_3_1");
  static public PolybiusSquare Binary_3_1_Guarded     => new PolybiusSquare(sBinary_3_1_Guarded    , "Binary_3_1_Guarded");

  public TapCode Encode( string s )
  {
    int lSize = (int)Math.Sqrt(Alphabet.Count);

    for( int lRow = 0 ; lRow < lSize ; ++ lRow )
      for( int lCol = 0 ; lCol < lSize ; ++ lCol )
        if ( Alphabet[(lRow*Size)+lCol] == s )
          return new TapCode(lRow+1,lCol+1); // Tap counts must start at 1, not 0

    return null ;
  }

  public string Decode( TapCode aCode )
  {
    int lRow = ( aCode.Row % Size ) - 1 ; // Tap counts start at 1, not 0
    int lCol = ( aCode.Col % Size ) - 1 ;
    int lIdx = (lRow*Size)+lCol ;

    return lIdx >= 0 && lIdx <= Alphabet.Count ? Alphabet[(lRow*Size)+lCol] : "?";
  }

  public List<string> Decode( IEnumerable<TapCode> aCodes )
  {
    List<string> rR = new List<string>();
    foreach( var lCode in aCodes )
      rR.Add( Decode( lCode ) );

    return rR ;
  }

  public readonly string       Name ;
  public readonly List<string> Alphabet ;
  public readonly int          Size ;
}



}
