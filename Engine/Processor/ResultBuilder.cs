using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CSCore.XAudio2;

using DocumentFormat.OpenXml.Drawing.Charts;

namespace DIGITC2_ENGINE
{
  public class PipelineResult
  {
    public string      Name           { get; set; }
    public TextMessage Text           { get; set; }
    public Fitness     OverallFitness { get; set; }

    public List<Packet> FilterSequence { get; private set; } = new List<Packet>();

    public List<Score> Scores { get; private set; } = new List<Score>();
  }


  public class PipelineResultBuilder  
  {
    public PipelineResultBuilder( string aName ) { Name = aName; }

    public void Add( Packet aPacket )
    {
      Packets.Add( aPacket ); 
    }

    public PipelineResult BuildResult()
    {
      PipelineResult rProduct = null ;

      if ( Packets.Count == 0 )
        return rProduct ;

      rProduct = new PipelineResult{ Name = Name, Text= Packets.Last().Data as TextMessage };

      int lOverallFitness = (int)Fitness.Undefined ;
  
      foreach ( Packet lPacket in Packets )
      {
        Score lScore = lPacket.Score ;
        if ( lScore != null ) 
        {
          lOverallFitness = Math.Min( (int)lScore.Fitness, lOverallFitness) ;
          rProduct.Scores.Add( lScore ); 
        }

        rProduct.FilterSequence.Add(lPacket) ;
      }

      rProduct.OverallFitness = (Fitness)lOverallFitness ;
 
      return rProduct ; 
    }

    readonly public string       Name ;
    readonly public List<Packet> Packets = new List<Packet>();
  }

  public class Result
  {
    public Result( List<PipelineResult> aPRs,  string aName)
    {
      PipelineResults = aPRs;
      Name            = aName;
    }

    public void Save( string aFolder )
    {
      if (  PipelineResults.Count > 0 ) 
      {
        string lResultsFolder = Path.Combine(aFolder, "Result");
        Utils.SetupFolder( lResultsFolder ); 

        int lIdx = 0 ;

        foreach ( PipelineResult lPR in PipelineResults ) 
        {   
          string lReportName = $"Result {lIdx} ({lPR.OverallFitness}).txt" ;

          string lReportPath = Path.Combine( lResultsFolder, lReportName ) ; 

          List<string> lReport      = new List<string>() ;
          List<string> lCollatedLogs = new List<string>() ;

          lReport.Add( "Decoded Text Message:" ) ;
          lReport.Add( lPR.Text != null && ! string.IsNullOrEmpty(lPR.Text.Text) ? lPR.Text.Text : "<<<< SORRY! NO MESSAGE WAS DECODED :( >>>>") ;
          lReport.Add( "" ) ;

          lReport.Add( $"Overall Fitness: {lPR.OverallFitness} " ) ;
          lReport.Add( "" ) ;

          lReport.Add( "Scores:" ) ;
          lPR.Scores.ForEach( lSC => lReport.Add( lSC.ToString() ) ) ; 
          lReport.Add( "" ) ;

          string lCollatedLogsName = $"Result {lIdx} - COMBINED LOG FILE.txt" ;
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

    public Result BuildResult( string aName )
    {
      List<PipelineResult> lPResults = new List<PipelineResult>();  

      foreach( var lPR in mPRBuilders )
      {
        PipelineResult lPP = lPR.BuildResult();
        if ( lPP != null )
          lPResults.Add( lPP );
      }

      if ( lPResults.Count > 0 ) 
        return new Result(lPResults,aName);

      return null ;
    }

    List<PipelineResultBuilder> mPRBuilders = new List<PipelineResultBuilder>(); 
  }

  
}
