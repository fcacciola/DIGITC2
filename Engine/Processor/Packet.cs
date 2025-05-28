using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2_ENGINE
{
  public abstract class PacketData
  {

  }

  public class Packet 
  {
    public Packet( Packet aPrev, Signal aSignal, string aName, Score aScore = null, bool aShouldQuit = false, PacketData aData = null )
    {
      Prev       = aPrev ;
      Signal     = aSignal;
      Name       = aName ;
      Score      = aScore;
      ShouldQuit = aShouldQuit;
      Data       = aData;
    }

    static public Packet Quit( Packet aPrev, string aLabel ) => new Packet(aPrev, null, aLabel, null, true, null );

    public T GetData<T>() where T : class => Data as T ;

    public Packet     Prev         ;
    public Signal     Signal       ;
    public string     Name         ;
    public Score      Score        ;
    public bool       ShouldQuit   ;
    public PacketData Data         ;
    public string     OutputFolder ;
  }

   
}
