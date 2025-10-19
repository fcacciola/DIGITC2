using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DIGITC2_ENGINE
{
  public abstract class PacketData
  {

  }

  public class Packet 
  {
    public Packet(string aFilterName, Packet aPrev, Signal aSignal, string aName, Score aScore = null, bool aShouldQuit = false, PacketData aData = null )
    {
      FilterName = aFilterName; 
      Prev       = aPrev ;
      Signal     = aSignal;
      Name       = aName ;
      Score      = aScore;
      ShouldQuit = aShouldQuit;
      Data       = aData;
    }

    static public Packet Quit( string aFilterName, Packet aPrev, string aLabel ) => new Packet(aFilterName, aPrev, null, aLabel, null, true, null );

    public T GetData<T>() where T : class => Data as T ;

    public string     FilterName   ;
    public Packet     Prev         ;
    public Signal     Signal       ;
    public string     Name         ;
    public Score      Score        ;
    public bool       ShouldQuit   ;
    public PacketData Data         ;
    public string     OutputFolder ;
  }

   
}
