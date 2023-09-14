Context.Log("BitsToText Sample 0 ");

string lCharSet = "us-ascii";
int lBitsPerByteParam = 8 ;

var lSource = BitsSource.FromText("Hello World!", lCharSet);  

////aContext.Log($"Bits ({aBitsPerByteParam} bits-per-byte) To Text Sample Processor");

var lProcessor = new Processor();

lProcessor.Add( new BinaryToBytes( lBitsPerByteParam, true))
          .Add( new BytesToText( lCharSet )) ;

var lResult = lProcessor.Process( lSource, Context ) ;
      