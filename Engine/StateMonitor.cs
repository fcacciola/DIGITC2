using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Vml.Spreadsheet;

namespace DIGITC2
{
  public abstract class StateMonitor
  {
    public abstract void Write( string aS ) ;

    public abstract void WriteLine( string aS ) ;

    public abstract void Watch ( State aO ) ;

    public abstract void Close();

    public void Watch ( IWithState aO ) => Watch( aO?.GetState() ) ;

  }

  public abstract class TextOutputStateMonitor : StateMonitor
  {
    public void Open( string aFile )
    {
      mStream = new FileStream(aFile, FileMode.Create, FileAccess.Write);
      mBaseWriter = new StreamWriter(mStream);
      mWriter = new IndentedTextWriter(mBaseWriter,"  ");  
    } 

    public override void Close()
    {
      mWriter.Close();
      mBaseWriter.Close();
      mStream.Close();
    }

    public override void Write( string aS ) 
    {
      mWriter?.Write( aS );
      mWriter?.Flush();
    }

    public override void WriteLine( string aS ) 
    {
      mWriter?.WriteLine( aS );
      mWriter?.Flush();
    }

    protected void Indent()
    {
      if ( mWriter != null )  
        mWriter.Indent++;
    }

    protected void Unindent()
    {
      if ( mWriter != null )  
        mWriter.Indent--;

    }

    FileStream         mStream ;
    TextWriter         mBaseWriter ;
    IndentedTextWriter mWriter ;
  }

  public class LogStateMonitor : TextOutputStateMonitor
  {
    public LogStateMonitor()
    {
    }

    void WatchAtomicArrayElement ( State aO, int aIdx )
    {
      WriteLine( $"[{aIdx}]: {aO.Value.Text}");
    }

    void WatchCompactAtomicArrayElement ( State aO, bool aIsLast )
    {
      Write( aO.Value.Text);
      if ( aIsLast )
        WriteLine("");
    }

    public override void Watch ( State aO )
    {
      if ( aO.Value != null )
            WriteLine( $"{aO.Name}: {aO.Value.Text}");
      else if ( aO.Type != null || aO.Name != null)
            WriteLine( $"{aO.Type}: {aO.Name}");

      if ( aO.Children.Count > 0 )
      {
        Indent();

        if ( aO.IsArray )
        {
          for( int i = 0; i < aO.Children.Count; ++ i )
          {
            var lChild = aO.Children[i];
            if ( lChild.Children.Count == 0 )
            {
              if ( UseCompactFormat(lChild) )
                   WatchCompactAtomicArrayElement( lChild, i == aO.Children.Count - 1 );
              else WatchAtomicArrayElement       ( lChild, i );
            }
            else Watch(lChild);
          }
        }
        else
        {
          aO.Children.ForEach( x => Watch(x) );
        }

        Unindent();

      }
    }

    bool UseCompactFormat(State aO) => aO.Type =="Bit" || aO.Type == "Byte";
  }

  public class ReportStateMonitor : TextOutputStateMonitor
  {
    public ReportStateMonitor()
    {
    }
        
    public override void Watch ( State aO )
    {
      if ( aO.Value != null )
           WriteLine( $"{aO.Name}: {aO.Value.Text}");
      else if ( aO.Type != null || aO.Name != null)
           WriteLine( $"{aO.Type}: {aO.Name}");

      if ( !aO.IsArray )
      {
        Indent();
        aO.Children.ForEach( x => Watch(x) );
        Unindent();
      }
      else
      {
        WriteLine( $"{aO.Children.Count} elements.");
      }
    }

    FileStream         mStream ;
    TextWriter         mBaseWriter ;
    IndentedTextWriter mWriter ;
  }


}
