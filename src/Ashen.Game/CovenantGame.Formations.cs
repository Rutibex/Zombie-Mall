using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 // IIP19: five Infinity-style quick slots are deliberately separate from the formation
 // catalogue. A slot is a user-facing shortcut; a preset is the shape assigned to it.
 readonly FormationPreset[] formationQuickSlots=
 [
  FormationPreset.Wedge,
  FormationPreset.Line,
  FormationPreset.Column,
  FormationPreset.Box,
  FormationPreset.Arc
 ];
 int activeFormationSlot;
 int configuringFormationSlot=-1;
 string contextHeadingOverride="";

 FormationPreset FormationAtQuickSlot(int slot)=>formationQuickSlots[Math.Clamp(slot,0,formationQuickSlots.Length-1)];
 bool IsActiveFormationSlot(int slot)=>slot==activeFormationSlot&&groupFormation==FormationAtQuickSlot(slot);

 void ActivateFormationQuickSlot(int slot)
 {
  if(slot<0||slot>=formationQuickSlots.Length)return;
  activeFormationSlot=slot;groupFormation=formationQuickSlots[slot];
  Notify($"Formation {slot+1}: {FormationPatterns.Name(groupFormation)}. Portrait order determines formation position.");
 }

 void AssignFormationQuickSlot(int slot,FormationPreset preset)
 {
  if(slot<0||slot>=formationQuickSlots.Length)return;
  formationQuickSlots[slot]=preset;
  // If the player reconfigures the active quick slot, the live formation changes with it.
  if(activeFormationSlot==slot)groupFormation=preset;
  Notify($"Formation slot {slot+1} assigned to {FormationPatterns.Name(preset)}.");
 }

 void OpenFormationQuickSlotConfiguration(int slot,Point anchor)
 {
  if(slot<0||slot>=formationQuickSlots.Length||Selection.Count()<2)return;
  configuringFormationSlot=slot;contextHeadingOverride=$"FORMATION SLOT {slot+1}";
  contextActor=null;contextFeature=null;contextScreen=anchor;contextCommands.Clear();
  foreach(var preset in FormationPatterns.Catalogue)
  {
   var captured=preset;
   string marker=formationQuickSlots[slot]==captured?"* ":"";
   contextCommands.Add(($"{marker}{FormationPatterns.Name(captured)}",()=>AssignFormationQuickSlot(slot,captured),true,FormationPatterns.Hint(captured)));
  }
  int height=39+contextCommands.Count*33;
  contextRect=new(Math.Clamp(contextScreen.X,MapRect.Left,MapRect.Right-238),Math.Clamp(contextScreen.Y,MapRect.Top,MapRect.Bottom-height),238,height);
  contextOpen=true;leftWorldPress=boxDragging=false;
 }


 // IIP20: right-drag on open terrain places a formation anchor and faces the final
 // shape toward the drag vector. A short right-click still opens the ordinary context menu.
 bool rightFormationPress,rightFormationDragging;
 Point rightFormationPressScreen;
 WorldPoint rightFormationAnchor,rightFormationFacing=new(1,0);
 bool FormationDragActive=>rightFormationPress||rightFormationDragging;

 bool BeginFormationRightPress()
 {
  if(title||UiBlocksWorld||paused||world.Mode!=Mode.Explore||Selection.Count()<2||!MapRect.Contains(cursor)||MiniRect.Contains(cursor))return false;
  if(PickActor()!=null)return false;
  var point=MouseWorld();if(ui.Highlights.FeatureAt(point.Cell)!=null)return false;
  rightFormationPress=true;rightFormationDragging=false;rightFormationPressScreen=cursor;rightFormationAnchor=point;
  var leader=SelectionLeader;var initial=leader==null?new WorldPoint(1,0):point-leader.Position;float len=initial.Distance(new(0,0));
  rightFormationFacing=len>.001f?initial*(1/len):new WorldPoint(1,0);
  contextOpen=false;ResetFormationContext();return true;
 }
 void UpdateFormationRightPointer(bool held,bool up)
 {
  if(!rightFormationPress)return;
  if(held)
  {
   var delta=new Microsoft.Xna.Framework.Vector2(cursor.X-rightFormationPressScreen.X,cursor.Y-rightFormationPressScreen.Y);
   if(!rightFormationDragging&&delta.Length()>8)rightFormationDragging=true;
   if(rightFormationDragging)
   {
    var vector=MouseWorld()-rightFormationAnchor;float len=vector.Distance(new(0,0));if(len>.12f)rightFormationFacing=vector*(1/len);
   }
  }
  if(!up)return;
  bool dragged=rightFormationDragging;var anchor=rightFormationAnchor;var facing=rightFormationFacing;
  rightFormationPress=rightFormationDragging=false;
  if(dragged)
  {
   var leader=SelectionLeader;string reason=leader==null?"Select a living companion first.":world.FormationMovementReason(leader,anchor);
   IssueMove(anchor,facing);if(reason=="")Notify($"{FormationPatterns.Name(groupFormation)} formation ordered. The party will compress through tight passages and reform when clear.");
  }
  else OpenContext();
 }
 void CancelFormationRightPointer(){rightFormationPress=rightFormationDragging=false;}

 IEnumerable<(Actor Actor,WorldPoint Target,bool Compressed)> FormationPreview(WorldPoint anchor,WorldPoint facing)
  =>world.PreviewFormation(anchor,selectedParty,groupFormation,facing);

 void DrawFormationDragPreview()
 {
  if(!rightFormationDragging)return;
  var anchor=Screen(rightFormationAnchor);var facingPoint=Screen(rightFormationAnchor+rightFormationFacing*1.35f);
  GroundRing(anchor,20*zoom,Gold);Line(anchor,facingPoint,Gold,3);GroundRing(facingPoint,6*zoom,Gold);
  bool anyCompressed=false;
  foreach(var entry in FormationPreview(rightFormationAnchor,rightFormationFacing))
  {
   anyCompressed|=entry.Compressed;var target=Screen(entry.Target);
   GroundRing(target,18*zoom,entry.Compressed?Gold*.78f:Teal*.78f);Text(entry.Actor.Name,target.X-22,target.Y+9,Muted,.52f);
  }
  var label=anyCompressed?"TIGHT DESTINATION - COLUMN":"RIGHT-DRAG - SET FORMATION FACING";
  var r=new Rectangle((int)anchor.X-145,(int)anchor.Y-54,290,30);Box(r,new Color(12,17,20)*.94f,Gold*.72f);Center(label,r,Bright,.55f);
  ui.Cursors.Set(UiCursorKind.Move,"R",Gold);
 }

 void ResetFormationContext()
 {
  configuringFormationSlot=-1;contextHeadingOverride="";
 }
}
