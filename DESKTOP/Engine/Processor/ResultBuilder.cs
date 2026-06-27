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
    public string        Folder        { get; set; }
    public string        Name          { get; set; }
    public TextMessage   Text          { get; set; }
    public CombinedScore CombinedScore { get; set; }
    public Config        Config        { get; set; }

    public List<Packet> FilterSequence { get; private set; } = new List<Packet>();

    public List<Score> FilterScores { get; private set; } = new List<Score>();
  }


  public class PipelineResultBuilder  
  {
    public PipelineResultBuilder( ScoreModel aScoreModel, Config aConfig, string aName, string aOutputFolder ) { ScoreModel = aScoreModel; Config = aConfig; Name = aName; OutputFolder = aOutputFolder ; }

    public void Add( Packet aPacket )
    {
      Packets.Add( aPacket ); 
    }

    public PipelineResult BuildResult()
    {
      PipelineResult rResult = null ;

      if ( Packets.Count == 0 )
        return rResult ;

      rResult = new PipelineResult{ Name = Name, Text= Packets.Last().Data as TextMessage, Config = Config };
 
      foreach ( Packet lPacket in Packets )
      {
        Score lFilterScore = lPacket.Score ;
        if ( lFilterScore != null ) 
        {
          rResult.FilterScores.Add( lFilterScore ); 
        }

        rResult.FilterSequence.Add(lPacket) ;
      }

      rResult.CombinedScore = ScoreModel.Combine(rResult.FilterScores);
 
      return rResult ; 
    }

    readonly public ScoreModel   ScoreModel;
    readonly public Config       Config;
    readonly public string       Name ;
    readonly public string       OutputFolder ;
    readonly public List<Packet> Packets = new List<Packet>();
  }

  public class SessionResult
  {
    public SessionResult( List<PipelineResult> aPRs,  string aName)
    {
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

          string lReportName = $"Result.txt" ;

          string lReportPath = Path.Combine( lResultsFolder, lReportName ) ; 

          List<string> lReport      = new List<string>() ;
          List<string> lCollatedLogs = new List<string>() ;

          lReport.Add( "Decoded Text Message:" ) ;
          lReport.Add( lPR.Text != null && ! string.IsNullOrEmpty(lPR.Text.Text) ? lPR.Text.Text : "<<<< SORRY! NO MESSAGE WAS DECODED :( >>>>") ;
          lReport.Add( "" ) ;

          lReport.Add( $"Score: {lPR.CombinedScore.Value} " ) ;
          lReport.Add( "" ) ;

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

          File.WriteAllLines( lReportPath      , lReport ) ;  
          File.WriteAllLines( lCollatedLogsPath, lCollatedLogs ) ;  

          lPR.Config.Save(Path.Combine(lResultsFolder, $"Config.txt"));

          ++ lIdx ;
        }
      }
    }

    void CollateAllLogFiles (string aFolder, List<string> aLogs )
    {
      foreach( var lLogFile in Directory.EnumerateFiles( aFolder, "*.txt") )
      {
        aLogs.AddRange( File.ReadAllLines( lLogFile ) ) ; 
      }
    }

    public List<PipelineResult> PipelineResults { get; private set; }  
    public string               Name            { get; private set; }  
  }

  public class ResultBuilder 
  {
    public ResultBuilder() {}

    public void Add( PipelineResultBuilder aPRBuilder )
    {
      mPRBuilders.Add( aPRBuilder );
    }

    public SessionResult BuildResult( string aName )
    {
      List<PipelineResult> lPResults = new List<PipelineResult>();  

      foreach( var lPR in mPRBuilders )
      {
        PipelineResult lPP = lPR.BuildResult();
        if ( lPP != null )
          lPResults.Add( lPP );
      }

      if ( lPResults.Count > 0 ) 
        return new SessionResult(lPResults,aName);

      return null ;
    }

    List<PipelineResultBuilder> mPRBuilders = new List<PipelineResultBuilder>(); 
  }

  
}
