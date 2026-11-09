using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 void DrawHordeHud()
 {
  if(title||world.Campaign==null||ui.Screens.IsOpen||ui.Loot.IsOpen||ui.Dialogue.IsOpen||ui.Rest.IsOpen||ui.Store.IsOpen||ui.Services.IsOpen||InterfaceViewportExpanded)return;
  if(world.TensionEnabled)
  {
   var s=world.TensionState;var pressure=new Rectangle(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?252:177),245,68);Box(pressure,new Color(22,30,28)*.95f,Gold*.7f);Text($"TENSION {s.Score}/100 - {world.TensionBand}",pressure.X+10,pressure.Y+8,Gold,.35f,true);
   Text($"{s.PendingAttacks.Count} perimeter attack(s) queued",pressure.X+10,pressure.Y+32,Muted,.4f);
   buttons.Add((pressure,()=>ui.Rest.ShowTension()));
  }
  if(world.SurvivorClassesEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?327:252),245,30),$"CLASSES / ABILITIES [{BindingLabel(InputAction.OpenSurvivorClasses,false)}]",()=>ui.Rest.ShowClasses(),false,.39f);
  if(world.EquipmentEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?362:287),245,30),$"EQUIPMENT [{BindingLabel(InputAction.OpenEquipmentCatalogue,false)}]",()=>ui.Rest.ShowEquipment(),false,.4f);
  if(world.WorkshopEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?397:322),245,30),$"WORKSHOP / BARTER [{BindingLabel(InputAction.OpenWorkshop,false)}]",()=>ui.Rest.ShowWorkshop(),false,.4f);
  if(world.StoryEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?432:357),245,30),$"CONTRACTS / ENCOUNTERS [{BindingLabel(InputAction.OpenContracts,false)}]",()=>ui.Rest.ShowContracts(),false,.37f);
  if(world.SocialEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?467:392),245,30),"FACTIONS / RELATIONSHIPS",()=>ui.Rest.ShowSocial(),false,.36f);
  if(world.LowerMallEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?502:427),245,30),"MALL: LOCATION JOURNAL",()=>ui.Rest.ShowLowerMall(),false,.34f);
  if(world.FinaleEnabled)Button(new(MapRect.Right-263,MapRect.Top+(world.Mode==Mode.Combat?537:462),245,30),"MYSTERIES / FINAL ESCAPE",()=>ui.Rest.ShowFinale(),false,.34f);
  int y=MapRect.Top+(world.Mode==Mode.Combat?255:184);
  foreach(var h in world.Campaign.Hordes.Where(h=>h.AreaId==world.CurrentTravelDestinationId&&!world.IsDailyAttack(h.Id)))
  {
   var trigger=world.Features.FirstOrDefault(f=>f.Id==h.TriggerFeatureId);if(trigger==null||!world.IsFeatureDiscovered(trigger))continue;
   string state=world.HordeStatus(h.Id);var r=new Rectangle(MapRect.Left+18,y,245,68);Box(r,new Color(30,24,23)*.95f,state=="Cleared"?Teal:Gold*.7f);Text(h.Name.ToUpperInvariant(),r.X+10,r.Y+8,Gold,.4f,true);
   string text=state=="Dormant"?$"Optional: {h.MemberActorIds.Count} enemies. Noise {world.HordeNoiseCounts.GetValueOrDefault(h.Id)}/{h.NoiseThreshold}.":state+". This wave never respawns.";Paragraph(text,r.X+10,r.Y+30,225,Muted,.39f,15);y+=75;
  }
 }
 // Disposable staging; production games retain authored positions, dormant hordes and normal player gear.
 void StageHordeCapture()
 {
  if(scene=="horde-title"){title=true;return;}
  title=false;var hero=world.PartyLeader;hero.Pos=new(41,16);world.Party.Skip(1).First().Pos=new(39,16);world.SetDialogueFlag("flag.mall.shelter");world.Reveal();camera=new(40,16);SelectCharacter(0);
  var h=world.Campaign!.Hordes.First(h=>h.Id=="horde.mall.east-shutter");
  if(scene=="horde-warning")return;
  if(scene=="horde-pressure"){foreach(var a in world.Party)a.Character.SetCondition("hidden","Hidden",true);world.EmitLocalNoise(hero,"shout",60);world.EmitLocalNoise(hero,"shout",60);return;}
  if(scene=="horde-playthrough")return;
  world.ReleaseHorde(h.Id);world.CheckEncounter();
 }
}
