using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NWaves.Signals;

namespace ENGINE
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

      WriteLine2GUI($"Applying Filter: {Name}");
      Indent();

      Packet rPacket = null ;

      try
      {
        rPacket = DoApply();
      }
      catch ( Exception x )
      {
        Session.Error(x);
      }

      Unindent();

      Session.MarkTime($"Filter finished");

      return (rPacket,Branches); 
    }

    protected Packet CreateOutput( Signal aSignal, string aName, Score aScore = null, bool aShouldQuit = false, PacketData aData = null)
      => new Packet(Config, Name, InputPacket, aSignal, aName, aScore, aShouldQuit, aData) ;

    protected Packet CreateQuitOutput() => new Packet(Config, Name, InputPacket, null, Name, null, true, null) ;

    protected abstract Packet DoApply() ;

    public abstract string Name { get; } 

    protected Params Params => Config.GetSection(Name);

    protected void AddBranch( params string[] aKVList )
    {
      Config rNew = Config.Copy();
      for( int i = 0 ; i < aKVList.Length ; i += 2 ) 
        rNew.GetSection(Name).ChangeValue(aKVList[i], aKVList[i+1]);
      Branches.Add(rNew) ;
    }

    protected bool DoOutputDetails => Settings.GetBool("OutputDetails");

    protected void Save( DiscreteSignal aDS, string aName )
    {
      aDS.SaveTo( Session.OutputFile( aName ), Session ) ;
    }

    protected void Save( WaveSignal aWS, string aName ) => Save(aWS.Rep, aName);  

    protected void Plot( List<float> aSamples, string aLabel )
    {
      DiscreteSignal lWaveRep = new DiscreteSignal(SIG.SamplingRate, aSamples);
      WaveSignal lWave = new WaveSignal(lWaveRep);
      lWave.SaveTo( Session.OutputFile( aLabel + ".wav"), Session ) ;
    }

    protected void Plot<SYM>( List<SYM> aSymbols, string aLabel ) where SYM : Symbol
    {
      List<float> lSamples = new List<float> ();
      aSymbols.ForEach( s => s.DumpSamples(lSamples ) );
      Plot(lSamples, aLabel);

      Session.MarkTime($"PLOT done.");
    }

    public override string ToString() => Name ;

    protected void WriteDetailLine( string aLine ) => Session.WriteDetailLine    ( aLine ) ;
    protected void WriteLine      ( string aLine ) => Session.WriteLine          ( aLine ) ;
    protected void WriteLine2GUI  ( string aLine ) => Session.WriteLine2DriverApp( aLine ) ;
    protected void Indent                       () => Session.Indent() ;  
    protected void Unindent                     () => Session.Unindent() ;  

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
        throw new ArgumentException("Input Signal must be a Lexical Signal.");

      return Process();
    }
    
    protected abstract Packet Process ();  

    protected LexicalSignal LexicalInput ;
  }

  public abstract class FileLexicalFilter : Filter
  {
    protected FileLexicalFilter() : base() {}

    protected override Packet DoApply()
    {
      FileInput = InputPacket.Signal as FileSignal; 

      if ( FileInput == null )
        throw new ArgumentException("Input Signal must be a File Signal.");

      return Process();
    }
    
    protected abstract Packet Process ();  

    protected FileSignal FileInput ;
  }


}
