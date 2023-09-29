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

    public abstract void Watch ( string aName, StateValue aV, bool aCompact ) ;

    public abstract void Watch ( State aO ) ;

    public abstract void Close();

    public void Watch ( IWithState aO ) => Watch( aO?.GetState() ) ;

  }

  public class LogStateMonitor : StateMonitor
  {
    public LogStateMonitor()
    {
    }

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

    public override void Watch ( string aName, StateValue aV, bool aCompact ) 
    {
      if ( aCompact )
      {
        Write( aV.Text ?? aName);
      }
      else
      {
        if ( aV != null )
             WriteLine( $"{aName}:{aV.Text}");
        else WriteLine(aName);
      }
    }

    public override void Watch ( State aO )
    {
      if ( aO.Name != null )
      {
        Watch(aO.Name,aO.Value,aO.IsCompact) ;
        Indent();
      }

      aO.Children.ForEach( x => Watch(x) );

      if ( !aO.IsCompact && aO.Children.Count > 0 && aO.Children.Last().IsCompact )
        WriteLine("");

      if ( aO.Name != null )
        Unindent();
    }


    void Indent()
    {
      if ( mWriter != null )  
        mWriter.Indent++;
    }

    void Unindent()
    {
      if ( mWriter != null )  
        mWriter.Indent--;

    }

    FileStream         mStream ;
    TextWriter         mBaseWriter ;
    IndentedTextWriter mWriter ;
  }



}
