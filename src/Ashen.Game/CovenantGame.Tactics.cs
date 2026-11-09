using Ashen.Core;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 float tacticsTimer;
 string tacticsActorId="";
 public bool TacticsEnabled=>preferences.TacticsEnabled;

 void ToggleTactics()
 {
  preferences.TacticsEnabled=!preferences.TacticsEnabled;tacticsTimer=0;tacticsActorId="";SavePreferences();
  Notify(preferences.TacticsEnabled?"Party tactics enabled. Active companions may automate their initiative turns.":"Party tactics disabled. Player turns are fully manual.");
  world.LogSystem(preferences.TacticsEnabled?"Party tactics AI enabled.":"Party tactics AI disabled.");
 }
 void SetTacticsProfile(Actor actor,TacticsProfile profile)
 {
  actor.Tactics=profile;Notify($"{actor.Name}: {World.TacticsProfileName(profile)} tactics.");
 }
 void CycleTacticsProfile(Actor actor)
 {
  var values=Enum.GetValues<TacticsProfile>();int index=Array.IndexOf(values,actor.Tactics);SetTacticsProfile(actor,values[(index+1+values.Length)%values.Length]);
 }
 void ResetTacticsPulse(){tacticsTimer=0;tacticsActorId="";}

 void UpdatePlayerTactics(float dt)
 {
  if(!preferences.TacticsEnabled||world.Mode!=Mode.Combat||paused||title||UiBlocksWorld||world.PendingReaction!=null||world.Active is not {Enemy:false,Alive:true} active)
  {ResetTacticsPulse();return;}
  if(tacticsActorId!=active.Id)
  {
   tacticsActorId=active.Id;tacticsTimer=0;
   world.Message(MessageCategory.System,$"TACTICS — {active.Name}: {World.TacticsProfileName(active.Tactics)}.",World.TacticsProfileHint(active.Tactics));
  }
  if(world.IsMoving(active)){tacticsTimer=0;return;}
  tacticsTimer+=dt;if(tacticsTimer<.42f)return;tacticsTimer=0;
  world.ExecuteTacticsStep(active);
 }
}
