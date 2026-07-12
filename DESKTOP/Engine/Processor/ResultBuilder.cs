using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ENGINE
{
  public class PipelineResult
  {
    public string      Folder { get; set; }
    public string      Name   { get; set; }
    public TextMessage Text   { get; set; }
    public Score       Score  { get; set; }
    public Config      Config { get; set; }

    public List<Packet> FilterSequence { get; private set; } = new List<Packet>();

    public List<Score>                FilterScores          { get; private set; } = new List<Score>();
    public List<PartialResultMessage> PartialResultMessages { get; private set; } = new List<PartialResultMessage>();
  }


  public class PipelineResultBuilder  
  {
    public PipelineResultBuilder( Config aConfig, string aName, string aOutputFolder ) { Config = aConfig; Name = aName; OutputFolder = aOutputFolder ; }

    public void Add( Packet aPacket )
    {
      Packets.Add( aPacket ); 
    }

    public PipelineResult BuildResult()
    {
      PipelineResult rResult = null ;

      if ( Packets.Count == 0 )
        return rResult ;

      rResult = new PipelineResult{ Name = Name, Text = Packets.LastOrDefault()?.Data as TextMessage, Config = Config };
 
      foreach ( Packet lPacket in Packets )
      {
        Score lFilterScore = lPacket.Score ;
        if ( lFilterScore != null ) 
        {
          rResult.FilterScores.Add( lFilterScore ); 
        }

        if (lPacket.Data is PartialResultMessage lPRM) 
        {
          rResult.PartialResultMessages.Add(lPRM);
        }

        rResult.FilterSequence.Add(lPacket) ;
      }

      rResult.Score = Score.Combine(rResult.FilterScores) ;
 
      return rResult ; 
    }

    readonly public Config       Config;
    readonly public string       Name ;
    readonly public string       OutputFolder ;
    readonly public List<Packet> Packets = new List<Packet>();
  }

  public class SessionResult
  {
    public SessionResult( Session aSession, List<PipelineResult> aPRs,  string aName)
    {
      Session         = aSession;
      PipelineResults = aPRs;
      Name            = aName;
    }

    public string LastFilterOutputFolder => PipelineResults.Count > 0 ? PipelineResults.Last().FilterSequence.Last().OutputFolder : null;

    public void Save()
    {
      if (  PipelineResults.Count > 0 ) 
      {
        int lIdx = 0 ;

        foreach ( PipelineResult lPR in PipelineResults ) 
        {   
          string lResultsFolder = lPR.FilterSequence.Last().OutputFolder ;

          string lResultPath = Path.Combine( lResultsFolder, "Result.txt" ) ; 

          List<string> lReport      = new List<string>() ;
          List<string> lCollatedLogs = new List<string>() ;

          if ( lPR.Text != null && ! string.IsNullOrEmpty(lPR.Text.Value) )
          {
            lReport.Add( $"Decoded Text Message: {lPR.Text.Value}" ) ;
            lReport.Add( "" ) ;
          }

          if (lPR.Score == null) 
               lReport.Add("Score: 0");
          else lReport.Add( $"Score: {lPR.Score.Value} " ) ;

          lReport.Add( "" ) ;

          lReport.Add($"Branch: {lPR.Name}");

          lPR.PartialResultMessages.ForEach( lPRM => lPRM.Lines.ForEach( line => lReport.Add($"Partial Result: {line}") ) ) ; 

          lReport.Add( "Scores:" ) ;
          lPR.FilterScores.ForEach( lSC => lReport.Add( lSC.ToString() ) ) ; 
          lReport.Add( "" ) ;

          string lCollatedLogsName = $"COMBINED LOG FILE.txt" ;
          string lCollatedLogsPath = Path.Combine( lResultsFolder, lCollatedLogsName ) ; 

          lReport.Add( "Processing Sequence:" ) ;
          foreach( var lPacket in lPR.FilterSequence )
          {
            lReport.Add(lPacket.FilterName) ; 
          }
          lReport.Add( "" ) ;

          lReport.Add( "Processing Output Folders:" ) ;
          foreach( var lPacket in lPR.FilterSequence )
          {
            lReport.Add(lPacket.OutputFolder) ; 
            
            CollateAllLogFiles(lPacket.OutputFolder, lCollatedLogs) ;  
          }
          lReport.Add( "" ) ;

          File.WriteAllLines( lResultPath      , lReport ) ;  
          File.WriteAllLines( lCollatedLogsPath, lCollatedLogs ) ;  

          lPR.Config.Save(Path.Combine(lResultsFolder, $"Config.txt"));

          ++ lIdx ;
        }
      }
    }

    void CollateAllLogFiles (string aFolder, List<string> aLogs )
    {
      foreach( var lLogFile in Directory.EnumerateFiles( aFolder, "*_detail.txt") )
      {
        aLogs.AddRange( File.ReadAllLines( lLogFile ) ) ; 
      }
    }

    public Session              Session         { get; private set; }
    public List<PipelineResult> PipelineResults { get; private set; }  
    public string               Name            { get; private set; }  
  }

  
}
