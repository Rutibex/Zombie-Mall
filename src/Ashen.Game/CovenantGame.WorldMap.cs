using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 string worldMapSelectionId="";

 V WorldMapPoint(Rectangle parchment,TravelDestinationDefinition destination)
 {
  const float nominalW=1280f,nominalH=540f;
  return new(parchment.X+destination.MapX/nominalW*parchment.Width,parchment.Y+destination.MapY/nominalH*parchment.Height);
 }
 void DrawWorldMapIip36(Rectangle r)
 {
  if(string.IsNullOrWhiteSpace(worldMapSelectionId)||!world.DiscoveredTravelDestinations().Any(d=>d.Id==worldMapSelectionId))worldMapSelectionId=world.CurrentTravelDestinationId;
  int x=r.X+36,y=r.Y+84;
  Button(new(x,y,170,34),"< AREA MAP",()=>ui.Screens.Back(),false,.56f);
  Text(world.Campaign==null?"ORLAN VALE":"ZOMBIE MALL",x+205,y+2,Gold,.72f,true);
  Text(world.CampaignTimeText,r.Right-222,y+8,Bright,.49f);
  var parchment=new Rectangle(r.X+44,r.Y+130,r.Width-88,Math.Max(310,r.Height-355));batch.Draw(paper,parchment,Color.White);Border(parchment,Gold*.78f,2);Border(new(parchment.X+5,parchment.Y+5,parchment.Width-10,parchment.Height-10),Ink*.5f);

  foreach(var route in world.TravelRoutes)
  {
   var a=world.ResolveTravelDestination(route.A);var b=world.ResolveTravelDestination(route.B);var sa=world.TravelState(a.Id);var sb=world.TravelState(b.Id);if(!sa.Discovered||!sb.Discovered)continue;
   var pa=WorldMapPoint(parchment,a);var pb=WorldMapPoint(parchment,b);bool unlocked=sa.Unlocked&&sb.Unlocked;Line(pa,pb,unlocked?new Color(105,89,57):new Color(91,84,72),unlocked?5:3);
   var mid=(pa+pb)*.5f;string duration=TravelDuration(route.Minutes);Text(duration,mid.X-Measure(duration,.35f)/2,mid.Y-17,unlocked?Ink:new Color(93,84,72),.35f);
  }

  string hoverId="";
  foreach(var destination in world.DiscoveredTravelDestinations())
  {
   var state=world.TravelState(destination.Id);var availability=world.TravelAvailability(destination.Id);var p=WorldMapPoint(parchment,destination);var node=new Rectangle((int)p.X-17,(int)p.Y-17,34,34);bool selected=destination.Id==worldMapSelectionId,hover=node.Contains(cursor);
   Color c=availability switch{TravelDestinationAvailability.Current=>Teal,TravelDestinationAvailability.Unlocked=>Gold,_=>new Color(112,101,84)};
   batch.Draw(circle,node,c*(availability==TravelDestinationAvailability.Locked?.58f:1f));Border(new(node.X-3,node.Y-3,node.Width+6,node.Height+6),selected?Bright:hover?Gold:c*.55f,selected?3:1);
   if(availability==TravelDestinationAvailability.Current)Center("C",new(node.X,node.Y+6,node.Width,18),Ink,.43f);
   else if(availability==TravelDestinationAvailability.Locked)Center("X",new(node.X,node.Y+6,node.Width,18),Ink,.43f);
   var hit=new Rectangle(node.X-9,node.Y-9,node.Width+18,node.Height+18);buttons.Add((hit,()=>worldMapSelectionId=destination.Id));if(hit.Contains(cursor))hoverId=destination.Id;
   if(selected||hover)
   {
    string label=destination.Name.ToUpperInvariant();int lw=(int)Measure(label,.39f)+18;var lr=new Rectangle((int)p.X-lw/2,node.Bottom+7,lw,24);Box(lr,new Color(38,34,27)*.94f,Gold*.4f);Center(label,new(lr.X,lr.Y+4,lr.Width,18),Bright,.39f);
   }
  }

  var selectedDestination=world.ResolveTravelDestination(worldMapSelectionId);var selectedState=world.TravelState(selectedDestination.Id);var routeSelected=world.ResolveTravelRoute(world.CurrentTravelDestinationId,selectedDestination.Id);var preview=world.PreviewTravel(selectedDestination.Id);
  var info=new Rectangle(r.X+44,parchment.Bottom+17,r.Width-88,169);Box(info,new Color(19,22,21),Gold*.52f);
  Text(selectedDestination.Name.ToUpperInvariant(),info.X+18,info.Y+13,Bright,.61f,true);
  string stateText=world.TravelAvailability(selectedDestination.Id) switch{TravelDestinationAvailability.Current=>"CURRENT LOCATION",TravelDestinationAvailability.Unlocked=>"ROUTE OPEN",_=>"ROUTE LOCKED"};
  Text(stateText,info.Right-184,info.Y+18,world.TravelAvailability(selectedDestination.Id)==TravelDestinationAvailability.Current?Teal:selectedState.Unlocked?Gold:Muted,.46f,true);
  Paragraph(selectedDestination.Description,info.X+18,info.Y+49,info.Width-310,Muted,.46f,18);
  string routeText=routeSelected==null?(selectedDestination.Id==world.CurrentTravelDestinationId?"You are here.":"No direct route from the current region."):$"Travel: {TravelDuration(routeSelected.Minutes)}   ·   Event/encounter chance: {routeSelected.EncounterChancePercent}%";
  Text(routeText,info.X+18,info.Bottom-31,Gold,.43f);
  var travelButton=new Rectangle(info.Right-272,info.Y+55,244,42);
  if(preview.Allowed)Button(travelButton,"TRAVEL",()=>TryWorldTravel(selectedDestination.Id),false,.66f);
  else
  {
   Box(travelButton,new Color(24,27,27),Gold*.22f);Center(selectedDestination.Id==world.CurrentTravelDestinationId?"CURRENT":"TRAVEL",new(travelButton.X,travelButton.Center.Y-10,travelButton.Width,22),Muted*.55f,.64f);buttons.Add((travelButton,()=>Notify(preview.Reason)));
  }
  string selectedExit=world.TravelExitName(selectedDestination.Id);bool exitReady=selectedDestination.Id!=world.CurrentTravelDestinationId&&world.PartyAtTravelExit(selectedDestination.Id);
  string exitStatus=selectedDestination.Id==world.CurrentTravelDestinationId?"Select another discovered destination.":routeSelected==null?"No direct route leaves the current region.":exitReady?$"Travel exit ready: {selectedExit}.":$"Travel requires the party to reach {selectedExit}.";
  Paragraph(exitStatus,info.Right-278,info.Y+108,250,exitReady?Teal:Muted,.41f,16);
  if(hoverId.Length>0)
  {
   var hover=world.ResolveTravelDestination(hoverId);tooltip=$"{hover.Name} — {world.TravelAvailability(hover.Id).ToString().ToUpperInvariant()}. Click to inspect travel details.";
  }
 }
 string TravelDuration(int minutes)=>minutes>=60?minutes%60==0?$"{minutes/60} hr":$"{minutes/60} hr {minutes%60} min":$"{minutes} min";
 void TryWorldTravel(string destinationId)
 {
  var preview=world.PreviewTravel(destinationId);if(!preview.Allowed){Notify(preview.Reason);return;}
  if(!world.TravelTo(destinationId)){Notify("Travel could not begin.");return;}
  worldMapSelectionId=world.CurrentTravelDestinationId;ResetAreaMapState();SelectAll();camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);ui.Screens.ReturnToGame();Notify($"Arrived at {world.CurrentAreaName} — {world.CampaignTimeText}.");
 }
}
