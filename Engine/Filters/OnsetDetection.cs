using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CliWrap;
using CliWrap.Buffered;

using MathNet.Numerics.Statistics;

using NWaves.Operations;
using NWaves.Signals;

using OxyPlot.Annotations;


namespace DIGITC2
{
  public class OnsetDetection : WaveFilter
  {
    public class Onset : IWithState
    {
      public Onset( List<double> aTimes, List<int> aPositions )
      {
        Times     = aTimes ;
        Positions = aPositions ;
      }  

      public State GetState() => State.With("Oneset", Times.ToArray());

      public List<double> Times     ;
      public List<int>    Positions ;
    }

    public OnsetDetection() 
    { 
    }

    protected override void Process ( WaveSignal aInput, Branch aInputBranch, List<Branch> rOutput )
    {
      const string AubioOnsetTool_Path = ".\\Tools\\aubioonset.exe";

      List<string> lArgs = new List<string> ();

      //  %1.wav -t 0.6 -o %1.onesets.wav

      double lThreshold = 0.6 ;

      lArgs.Add( aInput.Origin  ) ;
      lArgs.Add( $"-t {lThreshold}") ;

      var lResult = Task.Run(async() => await Cli.Wrap(AubioOnsetTool_Path)
                                                 .WithArguments(lArgs)
                                                 .WithValidation(CommandResultValidation.None)
                                                 .ExecuteBufferedAsync()).Result;

      string lErrors =  lResult.StandardError.ToString();
      if ( lErrors.Length > 0 ) 
        Context.Error(lErrors) ;

      var lSTimes = lResult.StandardOutput.ToString().Replace(Environment.NewLine,"|").Split('|').Where( s => s.Length > 0 ).ToList(); 

      var lTimes = lSTimes.ConvertAll( st => double.Parse( st ) ); 

      if ( lTimes[0] == 0.0 )
        lTimes.RemoveAt(0);

      var lPositions = lTimes.ConvertAll( t => (int)Math.Round(t * (double)aInput.SamplingRate) ) ;

      Onset lOnset = new Onset(lTimes, lPositions) ;

      int lLen = aInput.Rep.Length;

      float[] lOutSignal = new float[lLen];

      for ( int i = 0 ; i < lLen ; i++ )  
        lOutSignal[i] = 0 ;  

      foreach( int lPos in lPositions )
        lOutSignal[lPos] = 1 ;  

      var rR = aInput.CopyWith(new DiscreteSignal(aInput.SamplingRate, lOutSignal));

      if ( Context.Session.Args.GetBool("Plot") )
        rR.SaveTo( Context.Session.LogFile( $"_OnsetSequence.wav") ) ;

      rOutput.Add( new Branch(aInputBranch, rR, "OnsetSequence", null, false, lOnset));
    }


    protected override string Name => "OnsetDetection" ;

  }

}
