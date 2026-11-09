using Microsoft.Xna.Framework.Audio;
namespace Ashen.Game;
public sealed class Audio : IDisposable
{
 readonly Task<AudioBank> generation=Task.Run(AudioBank.Generate);
 readonly Dictionary<string,SoundEffect> effects=[];
 SoundEffect? titleSound,ambientSound,combatSound;SoundEffectInstance? titleMusic,ambient,combat;
 bool loaded,failed,musicReady,musicAssetsChecked;float titleMix=1,combatMix;
 public bool Muted {get;set;}
 public float MasterVolume {get;set;}=.8f;
 public float MusicVolume {get;set;}=.7f;
 public float EffectsVolume {get;set;}=.8f;
 public float UiVolume {get;set;}=.65f;
 // Dialogue is currently text-only, but IIP45 owns the future voice category now so
 // spoken companion/NPC lines can be added without changing the preference schema.
 public float VoiceVolume {get;set;}=.85f;
 public string Status=>failed?"Audio unavailable":Muted?"Muted":musicReady&&!loaded?"Music playing · sound effects loading":!musicReady?"Loading score...":$"Audio {MasterVolume*100:0}% · Music {MusicVolume*100:0}% · FX {EffectsVolume*100:0}%";
 float Master=>Muted?0:Math.Clamp(MasterVolume,0,1);
 public void Update(float dt,bool onTitle,bool battle)
 {
  if(failed)return;
  try
  {
   if(!musicAssetsChecked)
   {
    musicAssetsChecked=true;
    var root=Path.Combine(AppContext.BaseDirectory,"Assets");
    var paths=new[]{"music_title.wav","music_explore.wav","music_combat.wav"}.Select(x=>Path.Combine(root,x)).ToArray();
    if(paths.All(File.Exists))
    {
     try
     {
      using(var stream=File.OpenRead(paths[0]))titleSound=SoundEffect.FromStream(stream);
      using(var stream=File.OpenRead(paths[1]))ambientSound=SoundEffect.FromStream(stream);
      using(var stream=File.OpenRead(paths[2]))combatSound=SoundEffect.FromStream(stream);
      titleMusic=titleSound.CreateInstance();ambient=ambientSound.CreateInstance();combat=combatSound.CreateInstance();
      titleMusic.IsLooped=ambient.IsLooped=combat.IsLooped=true;
      titleMusic.Volume=ambient.Volume=combat.Volume=0;
      titleMusic.Play();ambient.Play();combat.Play();musicReady=true;
     }
     catch(Exception ex)
     {
      System.Diagnostics.Debug.WriteLine(ex);
      titleMusic?.Dispose();ambient?.Dispose();combat?.Dispose();titleSound?.Dispose();ambientSound?.Dispose();combatSound?.Dispose();
      titleMusic=ambient=combat=null;titleSound=ambientSound=combatSound=null;
     }
    }
   }
   if(!loaded&&generation.IsCompleted)
   {
    var bank=generation.GetAwaiter().GetResult();foreach(var pair in bank.Effects)effects[pair.Key]=new SoundEffect(pair.Value,AudioBank.Rate,AudioChannels.Mono);
    if(!musicReady)
    {
     ambientSound=new(bank.Ambient,AudioBank.Rate,AudioChannels.Mono);combatSound=new(bank.Combat,AudioBank.Rate,AudioChannels.Mono);
     ambient=ambientSound.CreateInstance();combat=combatSound.CreateInstance();ambient.IsLooped=combat.IsLooped=true;ambient.Volume=combat.Volume=0;ambient.Play();combat.Play();musicReady=true;
    }
    loaded=true;
   }
   if(!musicReady)return;
   titleMix=Math.Clamp(titleMix+(onTitle?dt:-dt)*.75f,0,1);
   combatMix=Math.Clamp(combatMix+(battle?dt:-dt)*.9f,0,1);
   float music=Master*Math.Clamp(MusicVolume,0,1);
   if(titleMusic!=null)titleMusic.Volume=titleMix*.48f*music;
   ambient!.Volume=(1-titleMix)*(1-combatMix)*.54f*music;
   combat!.Volume=(1-titleMix)*combatMix*.60f*music;
  }
  catch(Exception ex){failed=true;System.Diagnostics.Debug.WriteLine(ex);}
 }
 public void Play(string id)
 {
  if(Muted||!loaded||failed)return;string key=id switch{"spell"=>"special0","combat"=>"level",_=>id};
  try{if(effects.TryGetValue(key,out var fx))fx.Play(.32f*Master*Math.Clamp(EffectsVolume,0,1),0,0);}catch(Exception ex){System.Diagnostics.Debug.WriteLine(ex);}
 }
 readonly Dictionary<string,DateTime> lastCue=[];
 public void PlayUi(string cue)
 {
  if(Muted||!loaded||failed)return;
  string key="ui-"+cue;
  var now=DateTime.UtcNow;
  if(lastCue.TryGetValue(key,out var previous)&&(now-previous).TotalMilliseconds<(cue=="hover"?110:45))return;
  lastCue[key]=now;
  try{if(effects.TryGetValue(key,out var fx))fx.Play(.55f*Master*Math.Clamp(UiVolume,0,1),0,0);}
  catch(Exception ex){System.Diagnostics.Debug.WriteLine(ex);}
 }
 public void PlayAcknowledgment(string? voiceSet)
 {
  string voice=(voiceSet??"").ToLowerInvariant();
  PlayUi(voice switch{"stoic" or "quiet"=>"voice-deep","warm" or "scholarly"=>"voice-warm","wry" or "bold"=>"voice-brisk",_=>"portrait"});
 }
 public void PlayVoice(SoundEffect clip)
 {
  if(Muted||failed)return;try{clip.Play(Master*Math.Clamp(VoiceVolume,0,1),0,0);}catch(Exception ex){System.Diagnostics.Debug.WriteLine(ex);}
 }
 public void Dispose(){titleMusic?.Dispose();ambient?.Dispose();combat?.Dispose();titleSound?.Dispose();ambientSound?.Dispose();combatSound?.Dispose();foreach(var s in effects.Values)s.Dispose();}
}
