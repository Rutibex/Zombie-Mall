using Ashen.Game;
using Ashen.Core;
try
{
 if(args.Contains("--campaign-check"))
 {
  int at=Array.IndexOf(args,"--campaign-check");var file=at+1<args.Length?args[at+1]:CampaignLoader.BundledCampaignPath;
  var definition=CampaignLoader.Load(file);var preview=World.New(definition);
  Console.WriteLine($"Campaign {definition.Id}: {definition.Areas.Count} areas, {definition.Items.Count} items, {preview.PartyCount} party actors. Validation and world creation passed.");return;
 }
 if(args.Contains("--audio-check"))
 {
  foreach(var name in new[]{"title","explore","combat"})
  {
   var path=Path.Combine(AppContext.BaseDirectory,"Assets","music_"+name+".wav");
   using var reader=new BinaryReader(File.OpenRead(path));
   if(new string(reader.ReadChars(4))!="RIFF")throw new Exception("Invalid score header: "+name);
   reader.ReadInt32();if(new string(reader.ReadChars(4))!="WAVE"||new string(reader.ReadChars(4))!="fmt ")throw new Exception("Invalid score format: "+name);
   int formatLength=reader.ReadInt32();short encoding=reader.ReadInt16(),channels=reader.ReadInt16();int rate=reader.ReadInt32();reader.ReadBytes(formatLength-8);
   if(encoding!=1||channels!=1||rate!=AudioBank.Rate||new string(reader.ReadChars(4))!="data")throw new Exception("Unsupported score format: "+name);
   int length=reader.ReadInt32();if(length<AudioBank.Rate*2*30||reader.ReadBytes(length).All(b=>b==0))throw new Exception("Missing or silent score: "+name);
   Console.WriteLine($"music_{name}: {length/(AudioBank.Rate*2)} seconds PCM");
  }
  var bank=AudioBank.Generate();
  foreach(var entry in bank.Effects.Append(new("ambient",bank.Ambient)).Append(new("combat",bank.Combat)))
  {if(entry.Value.Length==0||entry.Value.All(b=>b==0))throw new Exception("Silent audio: "+entry.Key);Console.WriteLine(entry.Key+": "+entry.Value.Length+" PCM bytes");}
  Console.WriteLine("Playroma sound and music generation passed.");return;
 }
 using var game=new CovenantGame(args);
 game.Run();
}
catch(Exception ex)
{
 var path=GameIdentity.ProfilePath;Directory.CreateDirectory(path);
 File.WriteAllText(Path.Combine(path,"crash.log"),ex.ToString());Console.Error.WriteLine(ex);Environment.ExitCode=1;
}
