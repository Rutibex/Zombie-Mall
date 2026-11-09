using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class PortraitRail
 {
  readonly CovenantGame g;
  const int SlotCount=World.MaxPartySize;
  const float DragThreshold=7f;
  enum Glyph { Guard, Concentration, Unconscious, Dead, LevelUp, Condition }
  readonly record struct Badge(Glyph Glyph,string Label,string Tip,Color Color);

  bool pointerCaptured,dragging,pressExtend;
  int pressSlot=-1,dropSlot=-1;
  string pressActorId="";
  Point pressPoint;

  public PortraitRail(CovenantGame game)=>g=game;
  public bool IsDragging=>dragging;
  public int DragSourceSlot=>pressSlot;
  public int DragDropSlot=>dropSlot;

  (int Top,int Bottom,int Gap,int CardHeight,int Pad) Metrics(Rectangle rail)
  {
   float u=g.ui.Layout.Scale;int pad=Math.Max(6,(int)Math.Round(8*u));
   int top=rail.Y+39,bottom=rail.Bottom-89,gap=Math.Max(4,(int)Math.Round(5*u));
   int cardH=Math.Max(92,(bottom-top-gap*(SlotCount-1))/SlotCount);
   return(top,bottom,gap,cardH,pad);
  }

  public Rectangle SlotRectangle(int slot)
  {
   var rail=g.ui.Layout.RightRail;var m=Metrics(rail);
   return new(rail.X+m.Pad,m.Top+slot*(m.CardHeight+m.Gap),rail.Width-m.Pad*2,m.CardHeight);
  }

  int SlotAt(Point p)
  {
   for(int slot=0;slot<SlotCount;slot++)if(SlotRectangle(slot).Contains(p))return slot;
   return -1;
  }

  int DropSlotAt(Point p)
  {
   int count=g.world.PartyCount;if(count<=0||!g.ui.Layout.RightRail.Contains(p))return -1;
   int slot=SlotAt(p);if(slot<0)return -1;
   return Math.Clamp(slot,0,count-1);
  }

  Actor? PressActor()=>g.world.Party.FirstOrDefault(a=>a.Id==pressActorId);

  public void CancelInteraction()
  {
   pointerCaptured=dragging=false;pressExtend=false;pressSlot=dropSlot=-1;pressActorId="";
  }

  /// <summary>
  /// Dedicated portrait input path. Portrait cells deliberately do not use the generic
  /// UI button list because they need left-click, modifier-click, double-click and drag
  /// semantics without a mouse-down click firing before a reorder gesture is known.
  /// </summary>
  public bool HandleLeftPointer(bool down,bool held,bool up,bool extend)
  {
   if(g.title){CancelInteraction();return false;}
   if(g.ui.Dialogue.IsOpen||g.ui.Modals.ReactionOpen||g.ui.Modals.MapNoteOpen||g.ui.Modals.JournalNoteOpen){CancelInteraction();return false;}
   if(down)
   {
    int slot=SlotAt(g.cursor);var actor=slot>=0?g.world.PartyAt(slot):null;
    if(actor==null)return false;
    g.ui.Modals.Clear();g.leftWorldPress=g.boxDragging=false;
    pointerCaptured=true;dragging=false;pressSlot=slot;dropSlot=slot;pressActorId=actor.Id;pressPoint=g.cursor;pressExtend=extend;
    return true;
   }
   if(!pointerCaptured)return false;
   if(held)
   {
    if(!dragging&&V.Distance(new V(pressPoint.X,pressPoint.Y),new V(g.cursor.X,g.cursor.Y))>DragThreshold)dragging=true;
    if(dragging)dropSlot=DropSlotAt(g.cursor);
    return true;
   }
   if(up)
   {
    var actor=PressActor();bool wasDragging=dragging;int destination=wasDragging?DropSlotAt(g.cursor):dropSlot;bool modifier=pressExtend;
    CancelInteraction();
    if(actor==null)return true;
    int from=g.world.PartyIndex(actor);
    if(wasDragging)
    {
     if(destination>=0&&destination<g.world.PartyCount&&from>=0)
     {
      string beforeLeader=g.world.PartyLeader.Id;
      if(g.world.MovePartySlot(from,destination))
      {
       var leader=g.world.PartyLeader;string note=$"{actor.Name} moved to party slot {destination+1}.";
       if(leader.Id!=beforeLeader)note+=$" {leader.Name} now leads the party.";
       g.Notify(note);
      }
     }
     return true;
    }
    // Resolve the actor's current slot by ID, not the stale press index. This makes a
    // click safe even if party order changed between frames for another reason.
    int current=g.world.PartyIndex(actor);if(current>=0)g.PortraitClick(current,modifier);
    return true;
   }
   return true;
  }

  Rectangle SelectAllRectangle(Rectangle rail){var m=Metrics(rail);return new(rail.X+m.Pad,rail.Bottom-79,rail.Width-m.Pad*2,30);}
  Rectangle TacticsRectangle(Rectangle rail){var m=Metrics(rail);return new(rail.X+m.Pad,rail.Bottom-43,rail.Width-m.Pad*2,30);}

  public bool HandleRightPress()
  {
   if(g.title)return false;
   if(g.ui.Dialogue.IsOpen||g.ui.Modals.ReactionOpen||g.ui.Modals.MapNoteOpen||g.ui.Modals.JournalNoteOpen)return true;
   if(TacticsRectangle(g.ui.Layout.RightRail).Contains(g.cursor))
   {
    var tacticsActor=g.PrimaryPartyActor;g.CycleTacticsProfile(tacticsActor);return true;
   }
   int slot=SlotAt(g.cursor);var actor=slot>=0?g.world.PartyAt(slot):null;
   if(actor==null)return false;if(g.ui.Store.IsOpen)return true;if(g.ui.Services.IsOpen)return true;if(g.ui.Rest.IsOpen)return true;
   CancelInteraction();g.ui.Modals.Clear();g.OpenPortraitInventory(slot);return true;
  }

  public void Draw(Rectangle rail)
  {
   g.Center("EXPEDITION",new(rail.X+3,rail.Y+11,rail.Width-6,22),Gold,.44f,true);
   var party=g.world.Party.ToArray();
   for(int slot=0;slot<SlotCount;slot++)
   {
    var r=SlotRectangle(slot);var actor=slot<party.Length?party[slot]:null;
    if(actor==null)DrawEmpty(r,slot);else DrawActorCard(r,slot,actor);
   }
   var all=SelectAllRectangle(rail);var tactics=TacticsRectangle(rail);
   g.Button(all,"SELECT ALL [G]",g.SelectAll,g.Selection.Count()==g.world.Party.Count(a=>a.Alive),.43f);
   g.Button(tactics,g.TacticsEnabled?"TACTICS: ON":"TACTICS: OFF",g.ToggleTactics,g.TacticsEnabled,.41f);
   if(tactics.Contains(g.cursor))
   {
    var actor=g.PrimaryPartyActor;g.tooltip=$"Turn-based party automation is {(g.TacticsEnabled?"enabled":"disabled")}. Left-click toggles globally; Ctrl+A also toggles. Right-click cycles {actor.Name}'s profile (currently {World.TacticsProfileName(actor.Tactics)}).";
   }
   DrawDragOverlay(rail);
  }

  void DrawEmpty(Rectangle r,int slot)
  {
   g.Box(r,new Color(18,20,20),Gold*.24f);g.Border(new(r.X+3,r.Y+3,r.Width-6,r.Height-6),Gold*.10f);
   g.Center($"{slot+1}",new(r.X,r.Center.Y-14,r.Width,20),Gold*.38f,.55f);
   g.Center("EMPTY",new(r.X,r.Center.Y+8,r.Width,18),Muted*.30f,.39f);
  }

  void DrawActorCard(Rectangle r,int slot,Actor a)
  {
   bool selected=g.IsSelected(a),active=g.world.Mode==Mode.Combat&&g.world.Active==a;
   int infoH=30,portraitH=Math.Max(50,r.Height-infoH-6);var portrait=new Rectangle(r.X+3,r.Y+3,r.Width-6,portraitH);
   g.Box(r,new Color(14,17,18),Gold*.42f);g.Portrait(a,portrait);g.CovenantFrame(r,false);
   // Selection and initiative are deliberately separate visual channels: teal inner selection,
   // bright double outer turn frame. They remain legible when both apply simultaneously.
   if(selected)g.Border(new(r.X+2,r.Y+2,r.Width-4,r.Height-4),Teal,3);
   if(g.ui.Dialogue.IsOpen&&g.ui.Dialogue.ActivePartySpeakerId==a.Id)g.Border(new(r.X+5,r.Y+5,r.Width-10,r.Height-10),Bright,2);
   if(active){g.Border(r,Bright,3);g.Border(new(r.X+5,r.Y+5,r.Width-10,r.Height-10),Gold,2);DrawTurnCorners(r);
    if(!g.preferences.ReducedMotion&&g.elapsed-g.turnBeganAt<.45f)g.Border(new(r.X+8,r.Y+8,r.Width-16,r.Height-16),Bright*(1-(g.elapsed-g.turnBeganAt)/.45f),2);}
   else g.Border(r,Gold*.48f,1);

   if(a.Dead){g.Fill(portrait,Color.Black*.50f);g.Center("DEAD",new(portrait.X,portrait.Center.Y-10,portrait.Width,22),Red,.58f,true);}
   else if(a.Unconscious){g.Fill(portrait,new Color(20,20,20)*.34f);g.Center("DOWN",new(portrait.X,portrait.Center.Y-10,portrait.Width,22),Muted,.52f,true);}

   var hpColor=HpColor(a);int hpY=r.Bottom-7;g.Bar(new(r.X+5,hpY,r.Width-10,4),a.MaxHp<=0?0:a.Hp/(float)a.MaxHp,hpColor);
   int textY=portrait.Bottom+1;string name=$"{slot+1} {a.Name}";if(g.Measure(name,.47f)>r.Width-11)name=a.Name;
   g.Text(name,r.X+6,textY,a.Dead?Red:Bright,.47f);
   g.Text($"{Math.Max(0,a.Hp)}/{a.MaxHp} HP",r.X+6,textY+15,hpColor,.39f);

   if(slot==0)DrawLeaderMarker(a,portrait);
   DrawBadges(a,portrait);
   if(r.Contains(g.cursor)&&!BadgeAt(a,portrait,g.cursor)&&!(slot==0&&LeaderMarkerRect(portrait).Contains(g.cursor)))
   {
    g.tooltip=PortraitTip(a,slot,selected,active);g.ui.Cursors.Set(UiCursorKind.Select);
   }
  }

  void DrawDragOverlay(Rectangle rail)
  {
   if(!dragging)return;var actor=PressActor();if(actor==null)return;
   int target=DropSlotAt(g.cursor);dropSlot=target;
   Color cue=target>=0?Bright:Red;
   if(target>=0)
   {
    var targetRect=SlotRectangle(target);int y=target>pressSlot?targetRect.Bottom+1:targetRect.Y-2;
    y=Math.Clamp(y,rail.Y+34,rail.Bottom-90);g.Fill(new(targetRect.X-2,y-2,targetRect.Width+4,4),cue);
    g.Fill(new(targetRect.X-5,y-5,7,10),cue);g.Fill(new(targetRect.Right-2,y-5,7,10),cue);
   }
   int w=Math.Min(74,rail.Width-18),h=68;var ghost=new Rectangle(g.cursor.X-w/2,g.cursor.Y-h/2,w,h);
   g.Box(ghost,new Color(10,13,15)*.90f,cue);g.Portrait(actor,new(ghost.X+3,ghost.Y+3,ghost.Width-6,ghost.Height-22));
   g.Center(actor.Name,new(ghost.X+2,ghost.Bottom-17,ghost.Width-4,14),cue,.35f);
   g.tooltip=target>=0?$"Release to move {actor.Name} to party slot {target+1}.":"Release outside the party rail to cancel reordering.";
   g.ui.Cursors.Set(target>=0?UiCursorKind.Select:UiCursorKind.Invalid,target>=0?"R":"!",cue);
  }


  Rectangle LeaderMarkerRect(Rectangle portrait)=>new(portrait.X+3,portrait.Y+3,20,18);
  void DrawLeaderMarker(Actor a,Rectangle portrait)
  {
   var r=LeaderMarkerRect(portrait);Color cue=a.Alive?Gold:Muted;g.Box(r,new Color(12,15,16)*.94f,cue);
   // A small crown-like chevron reads clearly at every supported UI scale without new art assets.
   var c=new V(r.Center.X,r.Center.Y);g.Line(c+new V(-6,3),c+new V(-4,-4),cue,2);g.Line(c+new V(-4,-4),c+new V(0,0),cue,2);g.Line(c+new V(0,0),c+new V(4,-4),cue,2);g.Line(c+new V(4,-4),c+new V(6,3),cue,2);g.Line(c+new V(-6,3),c+new V(6,3),cue,2);
   if(r.Contains(g.cursor))
   {
    var fallback=g.world.AvailablePartyLeader;
    g.tooltip=a.Alive?$"Party leader: {a.Name}. Slot 1 is the default dialogue speaker and first formation position. Drag portraits to change leadership.":$"Nominal party leader: {a.Name}. While unavailable, {(fallback==null?"no companion can lead":fallback.Name+" is the temporary available leader")}.";
   }
  }

  Color HpColor(Actor a)
  {
   if(a.Dead)return Red*.55f;if(a.Unconscious)return Red;
   float hp=a.MaxHp<=0?0:a.Hp/(float)a.MaxHp;return hp<=.25f?Red:hp<=.50f?Gold:Teal;
  }

  List<Badge> Badges(Actor a)
  {
   var badges=new List<Badge>();
   if(a.Dead)badges.Add(new(Glyph.Dead,"DEAD","Dead: this character cannot act and is not restored by ordinary recovery.",Red));
   else if(a.Unconscious)badges.Add(new(Glyph.Unconscious,"DOWN","Unconscious: at 0 HP. The character remains inspectable but cannot receive orders.",Red));
   if(a.Concentrating)
   {
    var c=a.Character.FirstConditionWithTag("concentration");string what=c?.Source??c?.Name??"a spell";
    badges.Add(new(Glyph.Concentration,"CONC",$"Concentrating on {what}. Losing concentration ends the maintained effect.",new Color(115,195,220)));
   }
   foreach(var c in a.Character.Conditions.Where(c=>c.Id!="unconscious"&&c.Id!="dead"&&!c.Tags.Any(t=>t.Equals("concentration",StringComparison.OrdinalIgnoreCase))).Take(3))
    badges.Add(new(Glyph.Condition,c.Name.Length>0?c.Name[..1].ToUpperInvariant():"!",$"{c.Name}{(string.IsNullOrWhiteSpace(c.Source)?"":" - "+c.Source)}",Gold));
   if(a.CanLevelUp)badges.Add(new(Glyph.LevelUp,"LEVEL","Level up available. Open Character Record to review advancement.",Bright));
   return badges;
  }

  void DrawBadges(Actor a,Rectangle portrait)
  {
   var badges=Badges(a);if(badges.Count==0)return;int size=17,gap=2,x=portrait.Right-size-3,y=portrait.Bottom-size-3;
   for(int i=badges.Count-1;i>=0;i--)
   {
    var b=badges[i];var r=new Rectangle(x,y,size,size);DrawBadge(r,b);
    if(r.Contains(g.cursor))g.tooltip=b.Tip;x-=size+gap;if(x<portrait.X+2){x=portrait.Right-size-3;y-=size+gap;}
   }
  }

  bool BadgeAt(Actor a,Rectangle portrait,Point p)
  {
   var badges=Badges(a);if(badges.Count==0)return false;int size=17,gap=2,x=portrait.Right-size-3,y=portrait.Bottom-size-3;
   for(int i=badges.Count-1;i>=0;i--){var r=new Rectangle(x,y,size,size);if(r.Contains(p))return true;x-=size+gap;if(x<portrait.X+2){x=portrait.Right-size-3;y-=size+gap;}}
   return false;
  }

  void DrawBadge(Rectangle r,Badge badge)
  {
   g.Box(r,new Color(11,14,16)*.94f,badge.Color);var c=new V(r.Center.X,r.Center.Y);
   switch(badge.Glyph)
   {
    case Glyph.Guard:
     g.Line(c+new V(-5,-6),c+new V(5,-6),badge.Color,2);g.Line(c+new V(-5,-6),c+new V(-4,2),badge.Color,2);g.Line(c+new V(5,-6),c+new V(4,2),badge.Color,2);g.Line(c+new V(-4,2),c+new V(0,6),badge.Color,2);g.Line(c+new V(4,2),c+new V(0,6),badge.Color,2);break;
    case Glyph.Concentration:
     for(int i=0;i<4;i++){float t=i*MathF.PI/2;g.Line(c+new V(MathF.Cos(t),MathF.Sin(t))*3,c+new V(MathF.Cos(t),MathF.Sin(t))*7,badge.Color,2);}g.batch.Draw(g.circle,new Rectangle(r.Center.X-3,r.Center.Y-3,6,6),badge.Color);break;
    case Glyph.Unconscious:g.Text("Z",r.X+4,r.Y+1,badge.Color,.43f);break;
    case Glyph.Dead:g.Line(c+new V(-5,-5),c+new V(5,5),badge.Color,2);g.Line(c+new V(5,-5),c+new V(-5,5),badge.Color,2);break;
    case Glyph.LevelUp:g.Line(c+new V(0,6),c+new V(0,-5),badge.Color,2);g.Line(c+new V(0,-5),c+new V(-4,-1),badge.Color,2);g.Line(c+new V(0,-5),c+new V(4,-1),badge.Color,2);break;
    default:g.Center(badge.Label,new(r.X,r.Y+1,r.Width,r.Height-1),badge.Color,.36f);break;
   }
  }

  void DrawTurnCorners(Rectangle r)
  {
   const int n=9;g.Fill(new(r.X,r.Y,n,3),Bright);g.Fill(new(r.X,r.Y,3,n),Bright);g.Fill(new(r.Right-n,r.Y,n,3),Bright);g.Fill(new(r.Right-3,r.Y,3,n),Bright);
   g.Fill(new(r.X,r.Bottom-3,n,3),Bright);g.Fill(new(r.X,r.Bottom-n,3,n),Bright);g.Fill(new(r.Right-n,r.Bottom-3,n,3),Bright);g.Fill(new(r.Right-3,r.Bottom-n,3,n),Bright);
  }

  string PortraitTip(Actor a,int slot,bool selected,bool active)
  {
   var states=new List<string>();if(a.Dead)states.Add("Dead");else if(a.Unconscious)states.Add("Unconscious");
   if(a.Concentrating)states.Add("Concentrating");if(a.CanLevelUp)states.Add("Level up available");
   states.AddRange(a.Character.Conditions.Where(c=>c.Id!="unconscious"&&c.Id!="dead"&&!c.Tags.Any(t=>t.Equals("concentration",StringComparison.OrdinalIgnoreCase))).Select(c=>c.Name).Take(3));
   string state=states.Count==0?"No major conditions":string.Join(", ",states.Distinct());
   string subclass=string.IsNullOrWhiteSpace(a.Character.SubclassName)?"":$" / {a.Character.SubclassName}";
   string leadership=slot==0?" Party leader: default dialogue speaker and first formation position.":"";
   string tactics=$" Tactics: {World.TacticsProfileName(a.Tactics)}{(g.TacticsEnabled?" (automation on)":" (automation off)")}.";
   return $"Slot {slot+1}: {a.Name} - {a.Class}{subclass}. HP {a.Hp}/{a.MaxHp}. {state}."+leadership+tactics+" "+(active?"Current combatant. ":"")+(selected?"Selected. ":"")+"Click selects; Shift/Ctrl modifies; double-click centers; right-click opens Inventory; drag reorders.";
  }
 }
}
