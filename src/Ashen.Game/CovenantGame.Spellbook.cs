using Ashen.Core;
using Microsoft.Xna.Framework;

namespace Ashen.Game;

public sealed partial class CovenantGame
{
 string spellbookActorId="";
 string spellbookSourceId="";
 int spellbookLevel=1;
 int spellbookPreparedPage;
 int spellbookAvailablePage;

 void ResetSpellbookPaging(){spellbookPreparedPage=0;spellbookAvailablePage=0;}

 SpellcastingSource? ActiveSpellbookSource(Actor actor)
 {
  if(actor.Character.Spellcasting.Count==0)return null;
  if(actor.Id!=spellbookActorId)
  {
   spellbookActorId=actor.Id;spellbookSourceId=actor.Character.Spellcasting[0].SourceId;
   spellbookLevel=actor.Character.Spellcasting[0].Cantrips.Count>0?0:1;ResetSpellbookPaging();
  }
  var source=actor.Character.Spellcasting.FirstOrDefault(s=>s.SourceId.Equals(spellbookSourceId,StringComparison.OrdinalIgnoreCase));
  if(source==null)
  {
   source=actor.Character.Spellcasting[0];spellbookSourceId=source.SourceId;spellbookLevel=source.Cantrips.Count>0?0:1;ResetSpellbookPaging();
  }
  int max=SpellcastingRules.MaximumSpellLevel(source.SlotProgression,SpellcastingRules.ClassLevel(actor.Character,source));
  if(spellbookLevel>max)spellbookLevel=max;
  if(spellbookLevel<0)spellbookLevel=0;
  return source;
 }

 static string SpellLevelLabel(int level)=>level==0?"CANTRIP":$"LEVEL {level}";
 static bool IsPreparedCaster(SpellcastingSource source)=>source.PreparationMode is SpellPreparationMode.Prepared or SpellPreparationMode.Spellbook;

 IReadOnlyList<string> SpellbookAvailableForLevel(Actor actor,SpellcastingSource source,int level)
 {
  if(level==0)return source.Cantrips.Where(id=>SpellCatalog.TryGet(id,out var s)&&s.Level==0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id=>SpellCatalog.Get(id).Name).ToArray();
  IEnumerable<string> ids=source.PreparationMode switch
  {
   SpellPreparationMode.Prepared=>SpellCatalog.ForClass(source.SpellListId,level).Where(s=>s.Level==level).Select(s=>s.Id),
   SpellPreparationMode.Spellbook=>source.KnownSpells,
   SpellPreparationMode.Known or SpellPreparationMode.Pact=>source.KnownSpells,
   _=>Array.Empty<string>()
  };
  int max=SpellcastingRules.MaximumSpellLevel(source.SlotProgression,SpellcastingRules.ClassLevel(actor.Character,source));
  return ids.Where(id=>SpellCatalog.TryGet(id,out var spell)&&spell.Level==level&&spell.Level<=max).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id=>SpellCatalog.Get(id).Name).ToArray();
 }

 IReadOnlyList<string> SpellbookPreparedForLevel(SpellcastingSource source,int level)
 {
  IEnumerable<string> ids=level==0?source.Cantrips:IsPreparedCaster(source)?source.PreparedSpells:source.KnownSpells;
  return ids.Where(id=>SpellCatalog.TryGet(id,out var spell)&&spell.Level==level).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id=>SpellCatalog.Get(id).Name).ToArray();
 }

 void SetSpellbookLevel(int level)
 {
  spellbookLevel=Math.Clamp(level,0,9);ResetSpellbookPaging();
 }

 void SelectSpellbookSource(SpellcastingSource source)
 {
  spellbookSourceId=source.SourceId;
  int max=SpellcastingRules.MaximumSpellLevel(source.SlotProgression,SpellcastingRules.ClassLevel(world.Chosen.Character,source));
  spellbookLevel=source.Cantrips.Count>0?0:Math.Min(1,max);ResetSpellbookPaging();
 }

 void TogglePreparedSpell(Actor actor,SpellcastingSource source,string spellId)
 {
  if(!IsPreparedCaster(source))return;
  if(world.Mode==Mode.Combat){Notify("Prepared spells cannot be changed during combat.");return;}
  if(!SpellCatalog.TryGet(spellId,out var spell)||spell.Level==0){Notify("Cantrips are always available and are not prepared.");return;}
  var next=source.PreparedSpells.ToList();
  bool removing=next.RemoveAll(id=>id.Equals(spellId,StringComparison.OrdinalIgnoreCase))>0;
  if(!removing)next.Add(spellId);
  if(!SpellcastingRules.SetPrepared(actor.Character,source,next,out var reason)){Notify(reason);return;}
  Notify(removing?$"{spell.Name} is no longer prepared.":$"{spell.Name} prepared.");
 }

 string SpellRune(SpellDefinition spell)
 {
  if(spell.Name.Length==0)return "?";
  string first=spell.Name[..1].ToUpperInvariant();
  return spell.School switch
  {
   SpellSchool.Abjuration=>$"[{first}]",SpellSchool.Conjuration=>$"<{first}>",SpellSchool.Divination=>$"({first})",SpellSchool.Enchantment=>$"~{first}~",
   SpellSchool.Evocation=>$"*{first}*",SpellSchool.Illusion=>$"/{first}/",SpellSchool.Necromancy=>$"+{first}+",SpellSchool.Transmutation=>$"={first}=",_=>first
  };
 }

 void DrawSpellSlotPips(Rectangle area,SpellcastingSource source)
 {
  int x=area.X;foreach(var slot in source.Slots)
  {
   Text($"L{slot.Level}",x,area.Y+3,Gold,.43f,true);x+=25;
   for(int i=0;i<slot.Maximum;i++)
   {
    var pip=new Rectangle(x,area.Y+5,10,10);Fill(pip,i<slot.Current?Teal:new Color(46,48,45));Border(pip,Gold*.42f,1);x+=14;
   }
   x+=12;
  }
  if(source.Slots.Count==0)Text("NO SPELL SLOTS",x,area.Y+3,Muted,.43f);
 }

 void DrawSpellCard(Actor actor,SpellcastingSource source,string id,Rectangle card,string state,bool clickable,bool prepared)
 {
  if(!SpellCatalog.TryGet(id,out var spell))return;
  Color edge=prepared?Teal:Gold*.48f;Box(card,new Color(28,25,19),edge);if(prepared)Border(new(card.X+2,card.Y+2,card.Width-4,card.Height-4),Teal*.55f,1);
  var icon=new Rectangle(card.X+7,card.Y+8,38,38);Box(icon,new Color(18,20,19),edge);Center(SpellRune(spell),new(icon.X,icon.Y+8,icon.Width,20),prepared?Teal:Gold,.40f,true);
  string shortName=spell.Name.Length>22?spell.Name[..19]+"...":spell.Name;Text(shortName,card.X+52,card.Y+5,Bright,.48f);
  string tags=(spell.Concentration?"C ":"")+(spell.Ritual?"R ":"")+(spell.CastingTime==SpellCastingTime.Reaction?"REACTION":spell.CastingTime==SpellCastingTime.BonusAction?"BONUS":"");
  if(!string.IsNullOrWhiteSpace(tags))Text(tags.Trim(),card.X+52,card.Y+31,spell.Concentration?Gold:Muted,.40f);
  ui.Information.Register(card,SpellInformation(actor,source,id,state),$"{spell.Name} — {state.ToLowerInvariant()}. Right-click for full spell information.");
  if(clickable)
  {
   buttons.Add((card,()=>TogglePreparedSpell(actor,source,id)));
   if(card.Contains(cursor))tooltip=prepared?$"{spell.Name}: click to remove from prepared spells; right-click for details.":$"{spell.Name}: click to prepare; right-click for details.";
  }
 }

 int DrawSpellGrid(Actor actor,SpellcastingSource source,IReadOnlyList<string> ids,Rectangle area,string state,bool clickable,int page,bool markPrepared,out int pageCount)
 {
  const int pageSize=8;pageCount=Math.Max(1,(ids.Count+pageSize-1)/pageSize);page=Math.Clamp(page,0,pageCount-1);
  int gap=8,cols=2,cardW=(area.Width-gap)/cols,cardH=55;var slice=ids.Skip(page*pageSize).Take(pageSize).ToArray();
  for(int i=0;i<slice.Length;i++)
  {
   string spellId=slice[i];int col=i%cols,row=i/cols;var card=new Rectangle(area.X+col*(cardW+gap),area.Y+row*(cardH+gap),cardW,cardH);
   bool prepared=source.PreparedSpells.Contains(spellId,StringComparer.OrdinalIgnoreCase);DrawSpellCard(actor,source,spellId,card,state,clickable,markPrepared&&prepared);
  }
  if(ids.Count==0)Center("NONE",new(area.X,area.Y+55,area.Width,28),Muted,.55f,true);
  return page;
 }

 void DrawSpellbookPaging(Rectangle area,int page,int pageCount,Action<int> setPage)
 {
  if(pageCount<=1)return;page=Math.Clamp(page,0,pageCount-1);
  Rectangle prev=new Rectangle(area.X,area.Y,54,25),next=new Rectangle(area.Right-54,area.Y,54,25);Button(prev,"<",()=>setPage(Math.Max(0,page-1)),page<=0,.55f);Button(next,">",()=>setPage(Math.Min(pageCount-1,page+1)),page>=pageCount-1,.55f);Center($"PAGE {page+1}/{pageCount}",new(area.X+58,area.Y+3,area.Width-116,20),Muted,.38f);
 }

 void DrawSpellPreparationScreen(Rectangle r)
 {
  var actor=world.Chosen;var character=actor.Character;var source=ActiveSpellbookSource(actor);
  int x=r.X+34,y=r.Y+79,w=r.Width-68,h=r.Height-112;
  var book=new Rectangle(x,y,w,h);batch.Draw(paper,book,Color.White);Border(book,Gold,2);Border(new(book.X+8,book.Y+8,book.Width-16,book.Height-16),new Color(93,73,42),1);

  if(source==null)
  {
   Center(actor.Name.ToUpperInvariant(),new(book.X,book.Y+30,book.Width,30),Ink,.66f,true);Center("NO SPELLCASTING SOURCE",new(book.X,book.Y+160,book.Width,36),Ink,.73f,true);
   Paragraph($"{actor.Name} currently has no spellcasting source. Spellcasting acquired through future class advancement will appear here automatically.",book.X+120,book.Y+220,book.Width-240,Ink,.67f,23);return;
  }

  int classLevel=SpellcastingRules.ClassLevel(character,source),maxLevel=SpellcastingRules.MaximumSpellLevel(source.SlotProgression,classLevel),prepLimit=SpellcastingRules.PreparationLimit(character,source);
  Text(actor.Name.ToUpperInvariant(),book.X+24,book.Y+21,Ink,.52f);
  Center(source.PreparationMode==SpellPreparationMode.Prepared?"PRIEST SCROLL":source.PreparationMode==SpellPreparationMode.Spellbook?"SPELLBOOK":"SPELLS",new(book.X,book.Y+13,book.Width,30),Ink,.67f,true);
  Text($"{source.SourceId} · {source.PreparationMode}",book.Right-235,book.Y+22,Ink,.45f);

  // Multiclass/source selectors sit above the level tabs without changing the permanent portrait rail.
  if(character.Spellcasting.Count>1)
  {
   int sx=book.X+24,sy=book.Y+55;foreach(var s in character.Spellcasting)
   {
    int sw=Math.Clamp((int)Measure(s.SourceId,.40f)+28,88,160);var sr=new Rectangle(sx,sy,sw,25);Button(sr,s.SourceId,()=>SelectSpellbookSource(s),s==source,.39f);sx+=sw+7;
   }
  }

  int tabsY=book.Y+88;int tabW=53;int totalTabs=10;int tabsWidth=tabW*totalTabs;int tabsX=book.Center.X-tabsWidth/2;
  for(int level=0;level<=9;level++)
  {
   int tabLevel=level;bool exists=tabLevel==0?source.Cantrips.Count>0:tabLevel<=maxLevel;var tab=new Rectangle(tabsX+tabLevel*tabW,tabsY,tabW-3,29);Box(tab,tabLevel==spellbookLevel?new Color(102,80,48):new Color(61,52,39),exists?Gold:Muted*.35f);Center(tabLevel==0?"0":tabLevel.ToString(),new(tab.X,tab.Y+5,tab.Width,18),exists?Bright:Muted,.48f,true);
   if(exists){buttons.Add((tab,()=>SetSpellbookLevel(tabLevel)));if(tab.Contains(cursor))tooltip=tabLevel==0?"Show cantrips.":$"Show level {tabLevel} spells.";}
  }

  var slotsArea=new Rectangle(book.X+30,book.Y+125,book.Width-60,24);Text("SPELL SLOTS",slotsArea.X,slotsArea.Y+3,Ink,.44f);DrawSpellSlotPips(new(slotsArea.X+104,slotsArea.Y,slotsArea.Width-104,slotsArea.Height),source);
  Text($"SAVE DC {source.SpellSaveDc(character)}   ATTACK +{source.SpellAttackBonus(character)}",book.Right-270,book.Y+152,Ink,.47f);

  int bodyY=book.Y+183,bodyH=book.Height-237;Rectangle left=new Rectangle(book.X+28,bodyY,(book.Width-68)/2,bodyH),right=new Rectangle(left.Right+12,bodyY,book.Right-(left.Right+40),bodyH);
  Box(left,new Color(32,27,19)*.12f,new Color(95,73,43));Box(right,new Color(32,27,19)*.12f,new Color(95,73,43));
  var available=SpellbookAvailableForLevel(actor,source,spellbookLevel);var prepared=SpellbookPreparedForLevel(source,spellbookLevel);
  bool preparer=IsPreparedCaster(source)&&spellbookLevel>0;

  string leftTitle=spellbookLevel==0?"CANTRIPS":preparer?$"PREPARED  {source.PreparedSpells.Count}/{prepLimit}":"KNOWN & CASTABLE";
  string rightTitle=spellbookLevel==0?"ALWAYS AVAILABLE":source.PreparationMode==SpellPreparationMode.Spellbook?"SPELLBOOK":source.PreparationMode==SpellPreparationMode.Prepared?"KNOWN / AVAILABLE":"CASTING DETAILS";
  Text(leftTitle,left.X+13,left.Y+10,Ink,.52f,true);Text(SpellLevelLabel(spellbookLevel),left.Right-104,left.Y+10,Ink,.40f,true);Text(rightTitle,right.X+13,right.Y+10,Ink,.52f,true);

  var leftGrid=new Rectangle(left.X+12,left.Y+42,left.Width-24,left.Height-79);int leftPages;
  spellbookPreparedPage=DrawSpellGrid(actor,source,prepared,leftGrid,spellbookLevel==0?"Cantrip":preparer?"Prepared spell":"Known spell",preparer,spellbookPreparedPage,false,out leftPages);
  DrawSpellbookPaging(new(left.X+14,left.Bottom-31,left.Width-28,25),spellbookPreparedPage,leftPages,v=>spellbookPreparedPage=v);

  if(preparer||spellbookLevel==0)
  {
   var rightGrid=new Rectangle(right.X+12,right.Y+42,right.Width-24,right.Height-79);int rightPages;
   spellbookAvailablePage=DrawSpellGrid(actor,source,available,rightGrid,spellbookLevel==0?"Cantrip":source.PreparationMode==SpellPreparationMode.Spellbook?"Spellbook spell":"Available spell",preparer,spellbookAvailablePage,true,out rightPages);
   DrawSpellbookPaging(new(right.X+14,right.Bottom-31,right.Width-28,25),spellbookAvailablePage,rightPages,v=>spellbookAvailablePage=v);
  }
  else
  {
   int ty=right.Y+51;Text("CASTING MODEL",right.X+18,ty,Ink,.44f,true);ty+=31;
   Text($"{source.PreparationMode} caster",right.X+18,ty,Ink,.50f);ty+=29;
   Text($"Casting ability: {AbilityName(source.CastingAbility)}",right.X+18,ty,Ink,.45f);ty+=25;
   Text($"Highest spell level: {maxLevel}",right.X+18,ty,Ink,.45f);ty+=25;
   Text($"Ritual casting: {(source.CanRitualCast?"Yes":"No")}",right.X+18,ty,Ink,.45f);ty+=25;
   Paragraph("Known casters do not prepare a second list. Every spell shown on the left is castable whenever a legal slot and action are available. IIP32 will expose those spells from the battlefield action bar.",right.X+18,ty+12,right.Width-36,Ink,.52f,19);
  }

  string footer=preparer?(world.Mode==Mode.Combat?"PREPARATION LOCKED DURING COMBAT · Right-click any spell for complete rules.":"Click an available spell to prepare it; click a prepared spell to remove it. Right-click for complete rules."):"Known spells are always castable when their rules and resources permit. Right-click any spell for complete rules.";
  Center(footer,new(book.X+28,book.Bottom-39,book.Width-56,22),Ink,.39f);
 }
}
