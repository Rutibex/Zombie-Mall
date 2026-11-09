using Ashen.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 V areaMapCenter=new(World.Width/2f,World.Height/2f);
 float areaMapZoom=1f;
 Rectangle areaMapViewport;
 bool areaMapLeftPan,areaMapLeftPanned,areaMapMiddlePan;
 Point areaMapPanOrigin;
 V areaMapPanCenter;
 bool mapShowLocations=true,mapShowQuests=true,mapShowNotes=true,mapShowParty=true;
 string mapNoteDraft="",editingMapNoteId="";
 WorldPoint mapNotePosition;
 const int MapNoteMaximumLength=160;
 bool AreaMapActive=>!title&&ui.Screens.Active==UiScreenId.Map;
 bool MapNoteEditorOpen=>ui.Modals.MapNoteOpen;

 void ResetAreaMapState()
 {
  areaMapZoom=1f;areaMapLeftPan=areaMapLeftPanned=areaMapMiddlePan=false;editingMapNoteId="";mapNoteDraft="";
  CenterAreaMapOnParty();
 }
 void CenterAreaMapOnParty()
 {
  var party=world.Party.Where(a=>a.Alive).ToList();
  if(party.Count==0){areaMapCenter=new(World.Width/2f,World.Height/2f);return;}
  areaMapCenter=new(party.Average(a=>a.Position.X),party.Average(a=>a.Position.Y));ClampAreaMapCenter();
 }

 Rectangle AreaMapViewport(Rectangle r)=>new(r.X+30,r.Y+187,r.Width-60,Math.Max(220,r.Height-280));
 Rectangle AreaMapInfo(Rectangle r)=>new(r.X+30,r.Bottom-82,r.Width-60,52);
 float AreaMapBaseCell(Rectangle viewport)=>Math.Max(2,Math.Min((viewport.Width-26)/(float)World.Width,(viewport.Height-26)/(float)World.Height));
 float AreaMapCell(Rectangle viewport)=>AreaMapBaseCell(viewport)*areaMapZoom;
 V AreaMapScreen(WorldPoint point)
 {
  float cell=AreaMapCell(areaMapViewport);return new V(areaMapViewport.Center.X,areaMapViewport.Center.Y)+(new V(point.X,point.Y)-areaMapCenter)*cell;
 }
 WorldPoint AreaMapWorld(Point point)
 {
  float cell=AreaMapCell(areaMapViewport);if(cell<=0)return new(areaMapCenter.X,areaMapCenter.Y);
  var worldPoint=areaMapCenter+(new V(point.X-areaMapViewport.Center.X,point.Y-areaMapViewport.Center.Y)/cell);return new(worldPoint.X,worldPoint.Y);
 }
 void ClampAreaMapCenter()
 {
  if(areaMapViewport.Width<=0||areaMapViewport.Height<=0)return;float cell=AreaMapCell(areaMapViewport);if(cell<=0)return;
  float hx=areaMapViewport.Width/(2f*cell),hy=areaMapViewport.Height/(2f*cell),cx=World.Width/2f,cy=World.Height/2f;
  float minX=hx>=cx?cx:Math.Max(0,cx-(cx-hx)),maxX=hx>=cx?cx:Math.Min(World.Width,cx+(cx-hx));
  float minY=hy>=cy?cy:Math.Max(0,cy-(cy-hy)),maxY=hy>=cy?cy:Math.Min(World.Height,cy+(cy-hy));
  areaMapCenter.X=Math.Clamp(areaMapCenter.X,minX,maxX);areaMapCenter.Y=Math.Clamp(areaMapCenter.Y,minY,maxY);
 }
 void SetAreaMapZoom(float value,Point anchor)
 {
  if(areaMapViewport.Width<=0||!areaMapViewport.Contains(anchor))return;var before=AreaMapWorld(anchor);areaMapZoom=Math.Clamp(value,1f,4.5f);var after=AreaMapWorld(anchor);areaMapCenter+=new V(before.X-after.X,before.Y-after.Y);ClampAreaMapCenter();
 }
 bool HandleAreaMapWheel(int wheel)
 {
  if(!AreaMapActive||MapNoteEditorOpen||wheel==0||!areaMapViewport.Contains(cursor))return false;
  SetAreaMapZoom(areaMapZoom+wheel/120f*.22f,cursor);return true;
 }
 void UpdateAreaMapKeyboard(float dt)
 {
  if(!AreaMapActive||MapNoteEditorOpen)return;float step=dt*10f/Math.Max(1f,areaMapZoom);
  if(BindingHeld(InputAction.PanLeft))areaMapCenter.X-=step;if(BindingHeld(InputAction.PanRight))areaMapCenter.X+=step;if(BindingHeld(InputAction.PanUp))areaMapCenter.Y-=step;if(BindingHeld(InputAction.PanDown))areaMapCenter.Y+=step;
  if(Pressed(Keys.OemPlus)||Pressed(Keys.Add))SetAreaMapZoom(areaMapZoom+.3f,areaMapViewport.Center);
  if(Pressed(Keys.OemMinus)||Pressed(Keys.Subtract))SetAreaMapZoom(areaMapZoom-.3f,areaMapViewport.Center);
  ClampAreaMapCenter();
  bool middle=mouse.MiddleButton==ButtonState.Pressed,oldMiddle=oldMouse.MiddleButton==ButtonState.Pressed;
  if(middle&&!oldMiddle&&areaMapViewport.Contains(cursor)){areaMapMiddlePan=true;areaMapPanOrigin=cursor;areaMapPanCenter=areaMapCenter;}
  if(middle&&oldMiddle&&areaMapMiddlePan)
  {
   float cell=AreaMapCell(areaMapViewport);areaMapCenter=areaMapPanCenter-new V(cursor.X-areaMapPanOrigin.X,cursor.Y-areaMapPanOrigin.Y)/cell;ClampAreaMapCenter();
  }
  if(!middle)areaMapMiddlePan=false;
 }
 bool HandleAreaMapLeftPointer(bool down,bool held,bool up)
 {
  if(!AreaMapActive||MapNoteEditorOpen)return false;
  if(down&&areaMapViewport.Contains(cursor)){areaMapLeftPan=true;areaMapLeftPanned=false;areaMapPanOrigin=cursor;areaMapPanCenter=areaMapCenter;return true;}
  if(held&&areaMapLeftPan)
  {
   if(V.Distance(new V(areaMapPanOrigin.X,areaMapPanOrigin.Y),new V(cursor.X,cursor.Y))>3)areaMapLeftPanned=true;
   if(areaMapLeftPanned){float cell=AreaMapCell(areaMapViewport);areaMapCenter=areaMapPanCenter-new V(cursor.X-areaMapPanOrigin.X,cursor.Y-areaMapPanOrigin.Y)/cell;ClampAreaMapCenter();}return true;
  }
  if(up&&areaMapLeftPan){areaMapLeftPan=false;return true;}return false;
 }
 MapNote? MapNoteAt(Point point)
 {
  if(!mapShowNotes)return null;
  MapNote? best=null;float distance=13f;
  foreach(var note in world.MapNotes){var p=AreaMapScreen(note.Position);float d=V.Distance(p,new V(point.X,point.Y));if(d<distance){distance=d;best=note;}}return best;
 }
 bool HandleAreaMapRightPress()
 {
  if(!AreaMapActive||MapNoteEditorOpen||!areaMapViewport.Contains(cursor))return false;
  var existing=MapNoteAt(cursor);if(existing!=null){OpenMapNoteEditor(existing.Position,existing);return true;}
  var point=AreaMapWorld(cursor);if(!world.CanPlaceMapNote(point)){Notify("Map notes can only be placed on explored ground.");return true;}OpenMapNoteEditor(point,null);return true;
 }
 void OpenMapNoteEditor(WorldPoint point,MapNote? note)
 {
  mapNotePosition=point;editingMapNoteId=note?.Id??"";mapNoteDraft=note?.Text??"";ui.Modals.Open(UiModalId.MapNote);
 }
 void CancelMapNoteEditor(){if(MapNoteEditorOpen)ui.Modals.Close();editingMapNoteId="";mapNoteDraft="";}
 void CommitMapNoteEditor()
 {
  string text=mapNoteDraft.Trim();if(text.Length==0){Notify("Enter some text for the map note.");return;}
  bool ok;if(editingMapNoteId.Length>0)ok=world.UpdateMapNote(editingMapNoteId,text);else ok=world.AddMapNote(mapNotePosition,text)!=null;
  if(!ok){Notify("That map note could not be saved.");return;}Notify(editingMapNoteId.Length>0?"Map note updated.":"Map note added.");CancelMapNoteEditor();
 }
 void DeleteEditingMapNote()
 {
  if(editingMapNoteId.Length==0)return;if(world.DeleteMapNote(editingMapNoteId))Notify("Map note deleted.");CancelMapNoteEditor();
 }
 void HandleAreaMapNoteKeyboard()
 {
  if(!MapNoteEditorOpen)return;
  if(Pressed(Keys.Escape)){CancelMapNoteEditor();return;}
  if(Pressed(Keys.Enter)){CommitMapNoteEditor();return;}
  if(Pressed(Keys.Back)&&mapNoteDraft.Length>0)mapNoteDraft=mapNoteDraft[..^1];
 }
 void AreaMapTextInput(object? sender,TextInputEventArgs e)
 {
  // This single Window.TextInput hook is retained for IIP35 compatibility and routes
  // text to whichever persistent-note editor currently owns keyboard focus.
  if(characterCreationOpen){CharacterCreationTextInput(e);return;}
  if(SaveNameEditorOpen){SaveBrowserTextInput(e);return;}
  if(JournalNoteEditorOpen){JournalTextInput(e);return;}
  if(!MapNoteEditorOpen)return;char c=e.Character;if(c<' '||c=='\u007f'||mapNoteDraft.Length>=MapNoteMaximumLength)return;mapNoteDraft+=c;
 }
 bool MapMarkerFilter(MapMarkerDefinition marker)=>marker.Kind==MapMarkerKind.Quest?mapShowQuests:mapShowLocations;

 void DrawAreaMapIip35(Rectangle r)
 {
  int x=r.X+30,y=r.Y+88;areaMapViewport=AreaMapViewport(r);ClampAreaMapCenter();
  Button(new(r.Right-207,y-3,177,34),"WORLD MAP",()=>ui.Screens.OpenChild(UiScreenId.WorldMap),false,.54f);
  Text(world.CurrentAreaName.ToUpperInvariant(),x,y,Gold,.72f,true);
  Text($"Explored {world.Explored.Distinct().Count()} cells   ·   {world.MapNotes.Count} note{(world.MapNotes.Count==1?"":"s")}",x,y+31,Muted,.49f);

  int fy=y+58,fw=118,gap=7;
  Button(new(x,fy,fw,31),"LOCATIONS",()=>mapShowLocations=!mapShowLocations,mapShowLocations,.43f);
  Button(new(x+(fw+gap),fy,fw,31),"QUESTS",()=>mapShowQuests=!mapShowQuests,mapShowQuests,.43f);
  Button(new(x+(fw+gap)*2,fy,fw,31),"NOTES",()=>mapShowNotes=!mapShowNotes,mapShowNotes,.43f);
  Button(new(x+(fw+gap)*3,fy,fw,31),"PARTY",()=>mapShowParty=!mapShowParty,mapShowParty,.43f);
  Button(new(x+(fw+gap)*4,fy,142,31),"CENTER PARTY",CenterAreaMapOnParty,false,.42f);
  Button(new(r.Right-96,fy,30,31),"-",()=>SetAreaMapZoom(areaMapZoom-.3f,areaMapViewport.Center),false,.62f);
  Button(new(r.Right-60,fy,30,31),"+",()=>SetAreaMapZoom(areaMapZoom+.3f,areaMapViewport.Center),false,.62f);

  Box(areaMapViewport,new Color(8,11,12),Gold*.50f);Border(new(areaMapViewport.X+4,areaMapViewport.Y+4,areaMapViewport.Width-8,areaMapViewport.Height-8),Gold*.14f);
  DrawAreaMapGeometry();
  var hover=DrawAreaMapPinsAndParty();
  var info=AreaMapInfo(r);Box(info,new Color(16,19,19),Gold*.36f);
  if(hover is {} h){Text(h.Title,info.X+12,info.Y+7,h.Color,.52f,true);Text(h.Detail,info.X+12,info.Y+29,Muted,.44f);}
  else Text("Drag to pan · Wheel or +/- to zoom · Right-click explored ground to add/edit a note",info.X+12,info.Y+17,Muted,.46f);
  Text($"{areaMapZoom:0.0}x",info.Right-49,info.Y+17,Gold,.42f);
  if(MapNoteEditorOpen)DrawMapNoteEditor(r);
 }
 void DrawAreaMapGeometry()
 {
  var explored=world.Explored.Distinct().ToHashSet();float cell=AreaMapCell(areaMapViewport);
  foreach(var c in explored)
  {
   var center=AreaMapScreen(c.Center);var tile=new Rectangle((int)MathF.Floor(center.X-cell/2),(int)MathF.Floor(center.Y-cell/2),Math.Max(1,(int)MathF.Ceiling(cell)+1),Math.Max(1,(int)MathF.Ceiling(cell)+1));if(!tile.Intersects(areaMapViewport))continue;
   Fill(Rectangle.Intersect(tile,areaMapViewport),world.Visible.Contains(c)?new Color(112,109,89):new Color(55,61,60));
   foreach(var n in c.Neighbors().Where(n=>!explored.Contains(n)))
   {
    if(n.X<c.X)Fill(new(tile.Left,tile.Top,Math.Max(1,(int)(cell*.08f)),tile.Height),new Color(21,24,23));
    else if(n.X>c.X)Fill(new(tile.Right-Math.Max(1,(int)(cell*.08f)),tile.Top,Math.Max(1,(int)(cell*.08f)),tile.Height),new Color(21,24,23));
    else if(n.Y<c.Y)Fill(new(tile.Left,tile.Top,tile.Width,Math.Max(1,(int)(cell*.08f))),new Color(21,24,23));
    else Fill(new(tile.Left,tile.Bottom-Math.Max(1,(int)(cell*.08f)),tile.Width,Math.Max(1,(int)(cell*.08f))),new Color(21,24,23));
   }
  }
  foreach(var door in world.Features.Where(f=>f.Kind=="door"&&world.IsFeatureDiscovered(f)))
  {
   var p=AreaMapScreen(door.Pos.Center);if(!MapPointVisible(p,9))continue;int s=Math.Clamp((int)(cell*.35f),3,8);Fill(new((int)p.X-s,(int)p.Y-2,s*2+1,4),door.Used?Muted*.55f:Gold*.85f);
  }
 }
 bool MapPointVisible(V p,int padding=0)=>p.X>=areaMapViewport.Left+padding&&p.X<areaMapViewport.Right-padding&&p.Y>=areaMapViewport.Top+padding&&p.Y<areaMapViewport.Bottom-padding;
 (string Title,string Detail,Color Color)? DrawAreaMapPinsAndParty()
 {
  (string Title,string Detail,Color Color)? hover=null;float hoverDistance=15f;var mousePoint=new V(cursor.X,cursor.Y);
  foreach(var marker in world.DiscoveredMapMarkers().Where(MapMarkerFilter))
  {
   var p=AreaMapScreen(marker.Position);if(!MapPointVisible(p,12))continue;Color color=marker.Kind switch{MapMarkerKind.Quest=>new Color(228,185,74),MapMarkerKind.Exit=>Bright,MapMarkerKind.Service=>Teal,_=>Gold};int s=marker.Kind==MapMarkerKind.Quest?8:6;
   batch.Draw(circle,new Rectangle((int)p.X-s,(int)p.Y-s,s*2,s*2),color*.78f);Border(new((int)p.X-s-2,(int)p.Y-s-2,s*2+4,s*2+4),color*.52f);
   float d=V.Distance(p,mousePoint);if(d<hoverDistance){hoverDistance=d;hover=(marker.Label,$"{marker.Kind}: {marker.Description}",color);}
  }
  if(mapShowNotes)foreach(var note in world.MapNotes)
  {
   var p=AreaMapScreen(note.Position);if(!MapPointVisible(p,12))continue;Color c=new(173,202,219);batch.Draw(circle,new Rectangle((int)p.X-6,(int)p.Y-8,12,12),c*.9f);Line(p+new V(0,2),p+new V(0,9),c,2);
   float d=V.Distance(p,mousePoint);if(d<hoverDistance){hoverDistance=d;hover=("USER NOTE",note.Text,c);}
  }
  if(mapShowParty)
  {
   foreach(var actor in world.Party)
   {
    var p=AreaMapScreen(actor.Position);if(!MapPointVisible(p,16))continue;bool selected=IsSelected(actor),leader=actor.Id==world.PartyLeader.Id;Color c=!actor.Alive?Muted*.5f:selected?Bright:Teal;int s=selected?6:5;
    batch.Draw(circle,new Rectangle((int)p.X-s,(int)p.Y-s,s*2,s*2),c);Border(new((int)p.X-s-2,(int)p.Y-s-2,s*2+4,s*2+4),leader?Gold:Color.Black*.75f,leader?2:1);
    if(leader)
    {
     float dir=actor.FacingLeft?-1f:1f;Line(p+new V(dir*7,0),p+new V(dir*14,0),Gold,2);Line(p+new V(dir*14,0),p+new V(dir*10,-4),Gold,2);Line(p+new V(dir*14,0),p+new V(dir*10,4),Gold,2);
    }
    float d=V.Distance(p,mousePoint);if(d<hoverDistance){hoverDistance=d;hover=(actor.Name,$"Party slot {world.PartyIndex(actor)+1}{(leader?" · leader":"")}{(!actor.Alive?" · incapacitated":"")}",c);}
   }
  }
  if(hover is {} h)
  {
   var labelWidth=Math.Clamp((int)Measure(h.Title,.48f,true)+22,105,290);var label=new Rectangle(Math.Clamp(cursor.X+13,areaMapViewport.Left+5,areaMapViewport.Right-labelWidth-5),Math.Clamp(cursor.Y-30,areaMapViewport.Top+5,areaMapViewport.Bottom-31),labelWidth,27);Box(label,new Color(12,15,15)*.96f,h.Color*.7f);Center(h.Title,new(label.X,label.Y+6,label.Width,18),Bright,.45f,true);
  }
  return hover;
 }
 void DrawMapNoteEditor(Rectangle r)
 {
  buttons.Add((ui.Layout.Screen,()=>{}));
  var modal=new Rectangle(r.Center.X-330,r.Center.Y-128,660,256);Box(modal,new Color(15,18,18),Gold);Border(new(modal.X+5,modal.Y+5,modal.Width-10,modal.Height-10),Gold*.28f);
  Center(editingMapNoteId.Length>0?"EDIT MAP NOTE":"ADD MAP NOTE",new(modal.X,modal.Y+18,modal.Width,28),Bright,.62f,true);
  Text($"Explored location: {mapNotePosition.X:0.0}, {mapNotePosition.Y:0.0}",modal.X+24,modal.Y+60,Muted,.46f);
  var field=new Rectangle(modal.X+24,modal.Y+89,modal.Width-48,64);Box(field,new Color(7,9,10),Gold*.45f);string shown=mapNoteDraft.Length==0?"Type a note...":mapNoteDraft;Paragraph(shown+(mapNoteDraft.Length<MapNoteMaximumLength&&((int)(elapsed*2)%2==0)?"|":""),field.X+12,field.Y+11,field.Width-24,mapNoteDraft.Length==0?Muted*.55f:Bright,.50f,21);
  Text($"{mapNoteDraft.Length}/{MapNoteMaximumLength}   Enter saves · Esc cancels",modal.X+24,modal.Y+161,Muted,.40f);
  int by=modal.Bottom-55;CommandButton(new(modal.X+24,by,150,34),"SAVE",CommitMapNoteEditor,mapNoteDraft.Trim().Length>0,"Enter note text before saving.",false,.52f);
  if(editingMapNoteId.Length>0)Button(new(modal.X+184,by,150,34),"DELETE",DeleteEditingMapNote,false,.50f);
  Button(new(modal.Right-174,by,150,34),"CANCEL",CancelMapNoteEditor,false,.50f);
 }
}
