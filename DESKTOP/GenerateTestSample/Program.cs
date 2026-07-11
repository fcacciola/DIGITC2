using ENGINE;

Console.WriteLine("Generating MockAudio With TapCode");

var lArgsFile = $"C:\\Users\\User\\AppData\\Local\\Varanormal\\Transgraphier\\Input\\MockAudio_0.txt";

if ( File.Exists(lArgsFile) )
{
  var lSettings = Settings.FromFile(lArgsFile);

  var lGenerator = new Generate_MockAudio_WithTapCode();
  lGenerator.Run(lSettings);
}
else
{
  Console.WriteLine($"Settings file not found: {lArgsFile}");
}
