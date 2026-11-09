using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 int biographyScroll;
 string biographyActorId="";

 void DrawCharacterBiographyScreen(Rectangle r)
 {
  PartySwitch(r);var a=world.Chosen;var n=a.Character.Narrative;
  if(biographyActorId!=a.Id){biographyActorId=a.Id;biographyScroll=0;}
  int x=r.X+34,y=r.Y+78,w=r.Width-68;
  var identity=new Rectangle(x,y,w,51);Box(identity,new Color(19,21,21),Gold*.42f);Border(new(identity.X+4,identity.Y+4,identity.Width-8,identity.Height-8),Gold*.14f);
  Text(a.Name.ToUpperInvariant(),identity.X+18,identity.Y+10,Bright,.61f,true);
  string identityLine=$"{a.Character.Species}  •  {a.Class}{(string.IsNullOrWhiteSpace(a.Character.SubclassName)?"":$" / {a.Character.SubclassName}")}  •  {a.Character.Background}";
  Text(identityLine,identity.X+190,identity.Y+13,Gold,.43f);

  int bottomBand=148;var story=new Rectangle(x,y+63,w,r.Bottom-(y+63)-bottomBand-58);Box(story,new Color(10,12,13)*.96f,Gold*.48f);Border(new(story.X+5,story.Y+5,story.Width-10,story.Height-10),Gold*.16f);
  Text("CHARACTER BIOGRAPHY",story.X+22,story.Y+17,Gold,.58f,true);Fill(new(story.X+20,story.Y+44,story.Width-57,1),Gold*.30f);
  var lines=new List<string>();foreach(var paragraph in (n.Biography??"").Split("\n\n",StringSplitOptions.None)){if(lines.Count>0)lines.Add("");lines.AddRange(Wrap(paragraph,story.Width-72,.52f));}
  int firstY=story.Y+59,lineHeight=21,maxLines=Math.Max(1,(story.Bottom-firstY-20)/lineHeight),maxScroll=Math.Max(0,lines.Count-maxLines);biographyScroll=Math.Clamp(biographyScroll,0,maxScroll);
  for(int i=0;i<maxLines&&i+biographyScroll<lines.Count;i++)Text(lines[i+biographyScroll],story.X+23,firstY+i*lineHeight,Bright,.52f);
  Rectangle up=new Rectangle(story.Right-35,story.Y+56,24,29),down=new Rectangle(story.Right-35,story.Bottom-42,24,29);Button(up,"^",()=>biographyScroll=Math.Max(0,biographyScroll-3),false,.55f);Button(down,"v",()=>biographyScroll=Math.Min(maxScroll,biographyScroll+3),false,.55f);
  int trackY=up.Bottom+5,trackH=Math.Max(18,down.Y-trackY-5);Border(new(story.Right-28,trackY,8,trackH),Gold*.26f);if(maxScroll>0){float t=biographyScroll/(float)maxScroll;int thumbH=Math.Max(18,trackH*maxLines/Math.Max(1,lines.Count));Fill(new(story.Right-27,trackY+(int)((trackH-thumbH)*t),6,thumbH),Gold*.78f);}else Fill(new(story.Right-27,trackY,6,trackH),Gold*.35f);

  int bandY=story.Bottom+12,gap=10,col=(w-gap*2)/3;
  DrawBiographyFact(new(x,bandY,col,bottomBand-15),"PERSONALITY",n.Personality);
  DrawBiographyList(new(x+col+gap,bandY,col,bottomBand-15),"AFFILIATIONS",n.Affiliations);
  DrawBiographyList(new(x+(col+gap)*2,bandY,col,bottomBand-15),"COMPANION NOTES",n.Notes);
  Button(new(r.Center.X-110,r.Bottom-38,220,29),"DONE",()=>ui.Screens.Back(),false,.48f);
 }

 void DrawBiographyFact(Rectangle r,string heading,string value)
 {
  Box(r,new Color(18,20,20),Gold*.30f);Text(heading,r.X+10,r.Y+9,Gold,.42f,true);Fill(new(r.X+9,r.Y+30,r.Width-18,1),Gold*.25f);
  int y=r.Y+39;foreach(var line in Wrap(string.IsNullOrWhiteSpace(value)?"None recorded.":value,r.Width-20,.36f).Take(6)){Text(line,r.X+10,y,Muted,.36f);y+=18;}
 }
 void DrawBiographyList(Rectangle r,string heading,IEnumerable<string> values)
 {
  Box(r,new Color(18,20,20),Gold*.30f);Text(heading,r.X+10,r.Y+9,Gold,.42f,true);Fill(new(r.X+9,r.Y+30,r.Width-18,1),Gold*.25f);int y=r.Y+39;
  var entries=values?.ToArray()??Array.Empty<string>();if(entries.Length==0){Text("None recorded.",r.X+10,y,Muted,.36f);return;}
  foreach(var entry in entries.Take(3)){var wrapped=Wrap("• "+entry,r.Width-20,.35f).Take(3).ToArray();foreach(var line in wrapped){Text(line,r.X+10,y,Muted,.35f);y+=17;}y+=3;if(y>r.Bottom-18)break;}
 }

 void DrawCharacterInformationScreen(Rectangle r)
 {
  PartySwitch(r);var a=world.Chosen;var c=a.Character;var s=a.Statistics;int x=r.X+30,y=r.Y+80,w=r.Width-60,h=r.Height-104;
  Rectangle left=new Rectangle(x,y,250,h);var right=new Rectangle(left.Right+12,y,w-left.Width-12,h);Box(left,new Color(17,19,19),Gold*.35f);Box(right,new Color(12,15,16),Gold*.42f);
  var portrait=new Rectangle(left.X+35,left.Y+22,left.Width-70,198);Portrait(a,portrait);Border(portrait,Gold,2);Center(a.Name.ToUpperInvariant(),new(left.X,left.Y+232,left.Width,24),Bright,.60f,true);Center($"{a.Class} {a.TotalLevel}",new(left.X,left.Y+258,left.Width,20),Gold,.43f);
  int fy=left.Y+295;InfoIdentityLine("SPECIES",c.Species,left.X+16,fy,left.Width-32);fy+=33;InfoIdentityLine("BACKGROUND",c.Background,left.X+16,fy,left.Width-32);fy+=33;InfoIdentityLine("ALIGNMENT",c.Alignment,left.X+16,fy,left.Width-32);fy+=33;InfoIdentityLine("PARTY SLOT",$"{world.PartyIndex(a)+1} / {world.PartyCount}",left.X+16,fy,left.Width-32);fy+=33;InfoIdentityLine("TACTICS",World.TacticsProfileName(a.Tactics),left.X+16,fy,left.Width-32);fy+=42;
  Paragraph("Information records what this character has actually done during the current journey. These statistics persist in the save file and are separate from the mechanical Character Record.",left.X+16,fy,left.Width-32,Muted,.36f,18);

  int gap=12,col=(right.Width-36-gap)/2;Rectangle aCol=new Rectangle(right.X+12,right.Y+14,col,right.Height-28),bCol=new Rectangle(aCol.Right+gap,aCol.Y,col,aCol.Height);
  int ay=aCol.Y;RecordHeading("COMBAT RECORD",aCol.X,ay,aCol.Width);ay+=29;
  InfoStat(aCol.X,ref ay,aCol.Width,"Attacks made",s.AttacksMade.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Hits / misses",$"{s.AttacksHit} / {s.Misses}");
  InfoStat(aCol.X,ref ay,aCol.Width,"Accuracy",$"{s.HitRate:0.0}%");
  InfoStat(aCol.X,ref ay,aCol.Width,"Critical hits",s.CriticalHits.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Enemies defeated",s.EnemiesDefeated.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Damage dealt",s.DamageDealt.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Damage taken",s.DamageTaken.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Times downed",s.TimesDowned.ToString());
  ay+=8;RecordHeading("SUPPORT",aCol.X,ay,aCol.Width);ay+=29;
  InfoStat(aCol.X,ref ay,aCol.Width,"Healing done",s.HealingDone.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Healing received",s.HealingReceived.ToString());
  InfoStat(aCol.X,ref ay,aCol.Width,"Reactions used",s.ReactionsUsed.ToString());

  int by=bCol.Y;RecordHeading("ADVENTURE RECORD",bCol.X,by,bCol.Width);by+=29;
  InfoStat(bCol.X,ref by,bCol.Width,"Encounters won",s.EncountersWon.ToString());
  InfoStat(bCol.X,ref by,bCol.Width,"Distance travelled",$"{s.DistanceTravelledFeet:0} ft");
  InfoStat(bCol.X,ref by,bCol.Width,"Rests taken",s.RestsTaken.ToString());
  InfoStat(bCol.X,ref by,bCol.Width,"Items used",s.ItemsUsed.ToString());
  InfoStat(bCol.X,ref by,bCol.Width,"Ability checks",$"{s.SuccessfulSkillChecks} success / {s.FailedSkillChecks} fail");
  InfoStat(bCol.X,ref by,bCol.Width,"Saving throws",$"{s.SuccessfulSavingThrows} success / {s.FailedSavingThrows} fail");
  by+=8;RecordHeading("CURRENT JOURNEY",bCol.X,by,bCol.Width);by+=29;
  InfoStat(bCol.X,ref by,bCol.Width,"Party gold",$"{world.Gold} gp");
  InfoStat(bCol.X,ref by,bCol.Width,"Carried weight",$"{a.CarriedWeight:0.#}/{a.CarryingCapacity:0.#} lb");
  InfoStat(bCol.X,ref by,bCol.Width,SurvivorProfile(a)!=null?"Party size":"Magic items",SurvivorProfile(a)!=null?$"{world.PartyCount} / 6":$"{a.EquippedMagicItems} / NO CAP");
  InfoStat(bCol.X,ref by,bCol.Width,"Current HP",$"{a.Hp}/{a.MaxHp}");
  InfoStat(bCol.X,ref by,bCol.Width,"Experience",$"{c.Experience:N0} XP");
  if(SurvivorProfile(a)==null)InfoStat(bCol.X,ref by,bCol.Width,"Companion approval",$"{world.ApprovalLabel(a.Id)} ({world.Approval(a.Id):+0;-0;0})");
  if(SurvivorProfile(a)==null)Paragraph("Physical equipment slots limit equipped magic items.",bCol.X,Math.Min(bCol.Bottom-40,by+7),bCol.Width,Teal,.33f,15);
 }

 void InfoIdentityLine(string label,string value,int x,int y,int width)
 {
  Text(label,x,y,Gold,.25f,true);Text(value,x,y+13,Bright,.36f);Fill(new(x,y+28,width,1),Gold*.18f);
 }
 void InfoStat(int x,ref int y,int width,string label,string value)
 {
  Text(label,x,y,Muted,.37f);float vw=Measure(value,.39f);Text(value,x+width-vw,y,Bright,.39f);Fill(new(x,y+21,width,1),Gold*.10f);y+=28;
 }
}
