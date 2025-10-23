using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace DIGITC2_ENGINE
{
  public abstract class Filter 
  {
    public void Setup( Session aSession, Settings aSettings, Config aConfig ) 
    { 
      Session  = aSession;
      Settings = aSettings; 
      Config   = aConfig ; 
      Branches = new List<Config>(); 

      OnSetup(); 
    }

    public void Cleanup() { OnCleanup(); }

    protected virtual void OnSetup  () {}
    protected virtual void OnCleanup() {}

    public (Packet,List<Config>) Apply( Packet aInput ) 
    {
      InputPacket = aInput;

      WriteLine($"Applying Filter: {Name}");
      Indent();

      Packet rPacket = null ;

      try
      {
        rPacket = DoApply();
      }
      catch ( Exception x )
      {
        DContext.Error(x);
      }

      Unindent();

      return (rPacket,Branches); 
    }

    protected Packet CreateOutput( Signal aSignal, string aName, Score aScore = null, bool aShouldQuit = false, PacketData aData = null)
      => new Packet(Config, Name, InputPacket, aSignal, aName, aScore, aShouldQuit, aData) ;

    protected Packet CreateQuitOutput() => new Packet(Config, Name, InputPacket, null, Name, null, true, null) ;

    protected abstract Packet DoApply() ;

    public abstract string Name { get; } 

    protected Params Params => Config.GetSection(Name);

    protected void AddNewConfig( string aKey, string aValue )
    {
      Config rNew = Config.Copy();
      rNew.GetSection(Name).Set(aKey, aValue);
      Branches.Add(rNew) ;
    }

    protected void AddBranch( string aKey, float aValue ) => AddNewConfig(aKey, $"{aValue}");

    protected bool DoOutputDetails => Settings.GetBool("OutputDetails");

    protected void Save( DiscreteSignal aDS, string aName )
    {
      if ( DoOutputDetails )
        aDS.SaveTo( Session.OutputFile( aName ) ) ;
    }

    protected void Save( WaveSignal aWS, string aName ) => Save(aWS.Rep, aName);  

    public override string ToString() => Name ;

    protected void WriteLine( string aLine ) => DContext.WriteLine( aLine ) ;
    protected void Indent                 () => DContext.Indent() ;  
    protected void Unindent               () => DContext.Unindent() ;  

    protected Session      Session ;
    protected Settings     Settings ;
    protected Config       Config ;
    protected Packet       InputPacket ;
    protected List<Config> Branches ;
  }

  public abstract class WaveFilter : Filter
  {
    protected WaveFilter() : base() {}

    protected override Packet DoApply( )
    {
      WaveInput = InputPacket.Signal as WaveSignal; 
      if ( WaveInput == null )
        throw new ArgumentException("Input Signal must be an Audio Signal.");

      return Process();
    }
    
    protected abstract Packet Process();  

    protected WaveSignal WaveInput ;

  }

  public abstract class LexicalFilter : Filter
  {
    protected LexicalFilter() : base() {}

    protected override Packet DoApply()
    {
      LexicalInput = InputPacket.Signal as LexicalSignal; 
      if ( LexicalInput == null )
        throw new ArgumentException("Input Signal must be a gated Lexical Signal.");

      return Process();
    }
    
    protected abstract Packet Process ();  

    protected LexicalSignal LexicalInput ;
  }


}
