using Microsoft.Xna.Framework.Audio;
using Playroma.Sound.Core.Audio;
using Playroma.Sound.Core.Rendering;
using Playroma.Sound.Generators.Contracts;
using Playroma.Sound.Generators.Descriptions;
using Playroma.Sound.Generators.Generators;
using Playroma.Sound.Graph.BuiltIns;
using Playroma.Sound.Graph.Compilation;
using PlayromaMusic.Generators.Structured;
using PlayromaMusic.Generators.Rhythms;

namespace Ashen.Game;
public sealed class AudioBank
{
 public Dictionary<string,byte[]> Effects { get; } = [];
 public byte[] Ambient { get; set; } = [];
 public byte[] Combat { get; set; } = [];
 public Dictionary<string,byte[]> Scores { get; } = [];
 public const int Rate=22050;
 public static AudioBank Generate()
 {
  var bank=new AudioBank(); var compiler=new SoundGraphCompiler(BuiltInGraphRegistry.CreateDefault()); var format=new AudioFormat(Rate,1,512);
  MagicArchetype[] types=[MagicArchetype.Fire,MagicArchetype.Lightning,MagicArchetype.Nature,MagicArchetype.Shadow];
  for(int i=0;i<4;i++) for(int special=0;special<2;special++)
  {
   var description=new MagicDescription{Archetype=types[i],CastDuration=special==0?.015f:.06f,Power=special==0?.25f:.65f,SpatialExtent=.2f,ImpactSize=.25f};
   var graph=new MagicGenerator().Generate(description,new SoundGenerationContext{RootSeed=(ulong)(901+i),FamilySeed=77,VariationIndex=special,QualityMode=SoundQualityMode.RealtimeLow});
   var rendered=OfflineRenderer.Render(compiler.Compile(graph,format),format,special==0?.75:1.25);
   bank.Effects[(special==0?"cast":"special")+i]=Pcm(rendered.Samples,.65f);
  }
  foreach(var (name,kind) in new[]{("loot",MagicArchetype.Holy),("heal",MagicArchetype.Healing),("hurt",MagicArchetype.Curse),("hit",MagicArchetype.Frost),("dash",MagicArchetype.Teleportation),("level",MagicArchetype.Holy)})
  {
   var graph=new MagicGenerator().Generate(new MagicDescription{Archetype=kind,CastDuration=.025f,Power=.2f,SpatialExtent=.1f},new SoundGenerationContext{RootSeed=66,QualityMode=SoundQualityMode.RealtimeLow});
   bank.Effects[name]=Pcm(OfflineRenderer.Render(compiler.Compile(graph,format),format,name=="level"?1.5:.65).Samples,.45f);
  }
  // Playroma Music supplies the motif and Euclidean onset structure. Our lightweight
  // instrument renderer turns those notes into bells, bowed harmonics and a low drone.
  var motif=new MotifEngine().Generate(1307,StructuredStyleLibrary.Get("pop-modern"),0);
  var rhythm=EuclideanRhythm.Generate(5,16,1); int[] minor=[0,2,3,5,7,8,10];
  float[] ambient=new float[Rate*32],combat=new float[Rate*32];
  for(int i=0;i<ambient.Length;i++)
  {
   double t=i/(double)Rate; double swell=.6+.4*Math.Sin(Math.PI*t/16);
   ambient[i]=(float)((Math.Sin(Math.Tau*55*t)+.45*Math.Sin(Math.Tau*82.5*t)+.18*Math.Sin(Math.Tau*110*t))*.065*swell);
  }
  for(int step=0;step<64;step++)
  {
   int degree=motif.DegreeOffsets[(step/2)%motif.DegreeOffsets.Count];int octave=(int)Math.Floor(degree/7.0);int pitch=57+minor[((degree%7)+7)%7]+octave*12;
   if(step%2==0)Note(ambient,step*.5,2.7,pitch,.15,false);
   if(rhythm[step%16]){Note(combat,step*.5,.7,pitch-12,.11,true);Note(combat,step*.5,.22,33,.13,true);}
  }
  // Pad is periodic and all decays wrap so the 32-second loops join cleanly.
  bank.Ambient=Pcm(ambient,.85f,false);bank.Combat=Pcm(combat,.8f,false);
  // Short, deterministic interface tones are generated locally with Playroma's
  // existing PCM pipeline; no external recordings or copyrighted UI assets.
  foreach(var (name,hz,duration) in new[]{("ui-hover",480,.045),("ui-confirm",630,.095),("ui-back",390,.11),("ui-error",210,.15),("ui-page",550,.13),("ui-item",710,.085),("ui-spell",830,.16),("ui-turn",590,.23),("ui-portrait",460,.12),("ui-voice-warm",410,.16),("ui-voice-brisk",650,.12),("ui-voice-deep",310,.18)})
   bank.Effects[name]=UiTone(hz,duration);

  return bank;
 }
 static void Note(float[] buffer,double start,double duration,int midi,double gain,bool pluck)
 {
  double hz=440*Math.Pow(2,(midi-69)/12.0);int begin=(int)(start*Rate),count=(int)(duration*Rate);
  for(int j=0;j<count;j++)
  {
   double t=j/(double)Rate,env=Math.Min(1,t/.012)*Math.Exp(-t*(pluck?8:1.7))*Math.Min(1,(duration-t)/.08);
   double wave=Math.Sin(Math.Tau*hz*t)+.3*Math.Sin(Math.Tau*hz*2*t)+.14*Math.Sin(Math.Tau*hz*3*t);
   buffer[(begin+j)%buffer.Length]+=(float)(wave*env*gain);
  }
 }
 static byte[] UiTone(int hz,double duration)
 {
  int count=(int)(Rate*duration);var samples=new float[count];
  for(int i=0;i<count;i++)
  {
   double t=i/(double)Rate,env=Math.Min(1,t/.008)*Math.Pow(Math.Max(0,1-t/duration),2);
   samples[i]=(float)((Math.Sin(Math.Tau*hz*t)+.22*Math.Sin(Math.Tau*hz*2*t))*env*.18);
  }
  return Pcm(samples,.27f,false);
 }
 static byte[] Pcm(float[] samples,float gain,bool fade=true)
 {
  var bytes=new byte[samples.Length*2];float peak=fade?Math.Max(.001f,samples.Max(Math.Abs)):Math.Max(1,samples.Max(Math.Abs));
  for(int i=0;i<samples.Length;i++)
  {
   float envelope=fade?Math.Min(1,Math.Min(i/110f,(samples.Length-1-i)/440f)):1;
   short value=(short)(Math.Clamp(samples[i]/peak*gain*envelope,-1,1)*32760);
   bytes[i*2]=(byte)value;bytes[i*2+1]=(byte)(value>>8);
  }
  return bytes;
 }
 public static void WriteWave(string path,byte[] pcm)
 {
  using var w=new BinaryWriter(File.Create(path));w.Write("RIFF"u8);w.Write(36+pcm.Length);w.Write("WAVEfmt "u8);w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(Rate);w.Write(Rate*2);w.Write((short)2);w.Write((short)16);w.Write("data"u8);w.Write(pcm.Length);w.Write(pcm);
 }
}
