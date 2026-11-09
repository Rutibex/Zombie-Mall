using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;
public sealed partial class CovenantGame
{
 sealed partial class RestPanel
 {
  bool shelterView;
  int residentPage;
  string residentSelection="";
  public void ShowShelter(){Open();if(IsOpen)shelterView=true;}
  void DrawShelter()
  {
   var world=g.world;g.Panel(bounds);g.buttons.Add((bounds,()=>{}));int x=bounds.X+20,y=bounds.Y+18;
   g.Text("SHELTER POPULATION",x,y,Bright,.58f,true);
   g.Text($"Expedition {world.PartyCount}/6 | Residents {world.ShelterResidents.Count}/{world.ShelterCapacity}",x+350,y+6,Gold,.46f);
   if(world.InfrastructureEnabled)g.Button(new(bounds.Right-440,y,160,28),"WORKS / UTILITIES",()=>{worksView=true;shelterView=false;},false,.43f);
   g.Button(new(bounds.Right-270,y,165,28),"SURVIVAL / CARE",()=>shelterView=false,false,.44f);
   g.Button(new(bounds.Right-90,y,65,28),"CLOSE",()=>Close(),false,.42f);
   int leftW=300,midW=330,midX=x+leftW+18,rightX=midX+midW+18,rightW=bounds.Right-rightX-20,top=bounds.Y+76;
   g.Text("EXPEDITION",x,top-23,Gold,.43f,true);g.Text("RESIDENTS - SELECT A PERSON",midX,top-23,Gold,.40f,true);
   int yy=top;
   foreach(var member in world.Party)
   {
    var a=member;var row=new Rectangle(x,yy,leftW,46);g.Box(row,new Color(13,19,19),Gold*.28f);
    g.Text(a.Name.Length>17?a.Name[..17]:a.Name,x+8,yy+5,Bright,.45f,true);g.Text($"HP {a.Hp}/{a.MaxHp}",x+8,yy+27,Muted,.43f);
    string reason=world.HouseSurvivorReason(a.Id);
    g.CommandButton(new(x+leftW-92,yy+8,84,29),"STAY",()=>{if(world.HouseSurvivor(a.Id)){g.SelectAll();residentSelection=a.Id;residentPage=(world.ShelterResidents.Count-1)/6;}},reason=="",reason==""?"Stay in this refuge with current gear, HP and needs. No resources or XP are created.":reason,false,.47f);yy+=51;
   }
   int pages=Math.Max(1,(world.ShelterResidents.Count+5)/6);residentPage=Math.Clamp(residentPage,0,pages-1);
   if(!world.ShelterResidents.Any(r=>r.Actor.Id==residentSelection))residentSelection=world.ShelterResidents.FirstOrDefault()?.Actor.Id??"";
   yy=top;
   foreach(var resident in world.ShelterResidents.Skip(residentPage*6).Take(6))
   {
    var r=resident;var a=r.Actor;var row=new Rectangle(midX,yy,midW,46);g.Box(row,residentSelection==a.Id?new Color(49,47,32):new Color(13,19,19),residentSelection==a.Id?Gold:Gold*.28f);
    g.Text(a.Name+(a.Dead?" - DEAD":!a.Alive?" - UNCONSCIOUS":""),midX+8,yy+5,Bright,.44f,true);
    g.Text($"{r.Duty} | HP {a.Hp}/{a.MaxHp} | Fatigue {a.Needs.Exhaustion}/6",midX+8,yy+27,Muted,.43f);
    g.buttons.Add((row,()=>residentSelection=a.Id));yy+=51;
   }
   if(world.ShelterResidents.Count==0)g.Paragraph("Choose STAY beside an expedition member. The starting leader stays with the expedition.",midX,yy,midW,Muted,.47f,20);
   if(pages>1){g.Button(new(midX,top+309,70,27),"PREV",()=>residentPage=Math.Max(0,residentPage-1),false,.4f);g.Text($"{residentPage+1}/{pages}",midX+90,top+315,Muted,.43f);g.Button(new(midX+midW-70,top+309,70,27),"NEXT",()=>residentPage=Math.Min(pages-1,residentPage+1),false,.4f);}
   string management=world.ShelterManagementReason();
   g.Paragraph(management==""?"Roster changes are available here.":management,x,bounds.Bottom-92,leftW+midW+18,management==""?Teal:Gold,.45f,18);
   g.Text($"Refuge stock: food {world.ShelterSupplyCount("food")} | water {world.ShelterSupplyCount("water")} | bandages {world.ShelterSupplyCount("bandage")} | kits {world.ShelterSupplyCount("first-aid")}",x,bounds.Bottom-35,Muted,.44f);
   var selected=world.ShelterResidents.FirstOrDefault(r=>r.Actor.Id==residentSelection);yy=top-23;
   if(selected==null){g.Paragraph("A resident keeps one real actor identity. Supplies, loaded weapons and injuries stay with them.",rightX,yy,rightW,Muted,.49f,21);return;}
   var patient=selected.Actor;g.Text(patient.Name.ToUpperInvariant()+" - "+selected.Duty,rightX,yy,Gold,.45f,true);yy+=30;
   g.Text(world.SurvivalSummary(patient),rightX,yy,patient.Needs.Exhaustion>0?Red:Muted,.45f);yy+=23;
   g.Text($"Shifts {selected.CompletedShifts} | Supplies used by duty {selected.SuppliesSpent}",rightX,yy,Muted,.44f);yy+=22;
   int minutes=Math.Max(0,selected.NextWorkMinute-world.CampaignMinutes);g.Text($"Next duty review in {minutes/60}h {minutes%60}m",rightX,yy,Muted,.44f);yy+=27;
   foreach(var line in g.Wrap(selected.LastResult,rightW,.46f).Take(3)){g.Text(line,rightX,yy,Teal,.46f);yy+=19;}yy+=10;
   foreach(var duty in Enum.GetValues<ShelterDuty>())
   {
    var d=duty;string reason=world.AssignShelterDutyReason(patient.Id,d);string text=d switch{ShelterDuty.Recuperate=>"RECUPERATE - fed rest recovery",ShelterDuty.Steward=>"STEWARD - automate resident meals",_=>"CARE - treat one injured resident"};
    g.CommandButton(new(rightX,yy,rightW,31),text,()=>world.AssignShelterDuty(patient.Id,d),reason=="",reason==""?"First duty review after 8 hours; then every 24 hours. Changing duties never resets the deadline. Needs and finite supplies apply.":reason,selected.Duty==d,.44f);yy+=37;
   }
   string recall=world.RecallResidentReason(patient.Id);
   g.CommandButton(new(rightX,yy,rightW,31),"REJOIN EXPEDITION",()=>{if(world.RecallResident(patient.Id))g.SelectAll();},recall=="",recall==""?"Rejoin at a legal space with existing equipment, needs and duty history.":recall,false,.46f);yy+=38;
   string care=world.TreatResidentReason(patient.Id);
   g.CommandButton(new(rightX,yy,rightW,31),"TREAT RESIDENT - ONE MEDICAL UNIT",()=>world.TreatResident(patient.Id),care=="",care==""?"The expedition leader uses one carried/refuge bandage, otherwise a kit. The per-patient first-aid limit applies.":care,false,.44f);yy+=38;
   g.CommandButton(new(rightX,yy,rightW,31),"PROVISION ALL RESIDENTS",()=>g.Notify($"Used {world.ProvisionShelter()} resident supplies."),management=="",management==""?"Use each conscious resident's carried food/water, then finite refuge stocks. Already met needs are not charged again.":management,false,.46f);yy+=40;
   g.Paragraph("Stewards feed residents from stock; others use carried supplies at daily rollover. Duty reviews need food, water and a safe refuge. No remote field-party healing or supply production.",rightX,yy,rightW,Muted,.43f,18);
  }
 }
 void StageShelterCapture()
 {
  title=false;if(scene=="shelter-title"){title=true;return;}
  if(scene!="shelter-playthrough")
  {
   foreach(var f in world.Features.Where(f=>f.Kind=="door"))f.Used=true;world.SetDialogueFlag("flag.mall.shelter");int i=0;
   foreach(var a in world.Party)a.Pos=new(13,27+i++);world.HouseSurvivor("actor.mall.sam");world.AssignShelterDuty("actor.mall.sam",ShelterDuty.Steward);world.ProvisionShelter();world.Reveal();
  }
  camera=new(world.Chosen.Position.X,world.Chosen.Position.Y);SelectAll();ui.Rest.ShowShelter();
 }
}
