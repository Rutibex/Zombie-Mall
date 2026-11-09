namespace Ashen.Game;

public sealed partial class CovenantGame
{
 void HandleCampaignAreaChanged()
 {
  CancelInventoryDrag();CancelFormationRightPointer();ClearArmedSpell();ClearArmedFeatureAbility();ui.Loot.Close();ui.Dialogue.Close(false);ui.Screens.ReturnToGame();contextOpen=false;following=false;ResetTacticsPulse();
  worldMapSelectionId=world.CurrentTravelDestinationId;ResetAreaMapState();SelectAll();camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);
 }
 void StageCampaignTravelCapture(string target)
 {
  foreach(var id in target=="return"?new[]{"area.travel.service","area.travel.roof","area.travel.service","loc.lower.01"}:target=="roof"?new[]{"area.travel.service","area.travel.roof"}:new[]{"area.travel.service"})
  {
   foreach(var enemy in world.Actors.Where(a=>a.Enemy))enemy.Hp=0;
   var route=world.Campaign!.Connections.First(r=>r.FromAreaId==world.CurrentTravelDestinationId&&r.ToAreaId==id);var exit=world.Features.First(f=>f.Id==route.ExitFeatureId);
   var cells=world.Floors.Where(c=>!world.Blocked(c)&&c.Center.Distance(exit.Pos.Center)<2.5f).OrderBy(c=>c.Distance(exit.Pos)).ToArray();int i=0;foreach(var a in world.Party)a.Pos=cells[i++];world.Reveal();
   if(!world.TravelTo(id))throw new InvalidOperationException("Travel capture setup failed: "+world.Log.LastOrDefault());
  }
  SelectAll();camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);
 }

}
