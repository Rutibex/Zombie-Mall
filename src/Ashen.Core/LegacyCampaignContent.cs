namespace Ashen.Core;

// Retained authored regression adventure. The new campaign loader does not call this factory.
public sealed partial class World
{
 static World CreateLegacyCampaign()
 {
  var w=new World();var floor=new HashSet<Cell>();
  void Room(int x,int y,int width,int height){for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)floor.Add(new(xx,yy));}
  Room(2,12,10,9);Room(11,15,8,3);Room(18,9,10,12);Room(21,4,3,7);Room(17,2,11,5);
  Room(27,13,7,3);Room(33,8,10,11);Room(36,18,3,7);Room(31,24,13,6);
  Room(24,20,3,7);Room(25,24,7,3);
  floor.Remove(new(16,15));floor.Remove(new(16,17));w.Floors=floor.ToList();
  w.Actors=[
   new(){Id="knight",Name="Garrick",Portrait=0,Pos=new(6,16),Weapon="sword",Tactics=TacticsProfile.Defensive,Character=CharacterTemplates.Companion("knight")},
   new(){Id="ranger",Name="Sylva",Portrait=1,Pos=new(5,15),Weapon="bow",Tactics=TacticsProfile.Ranged,Character=CharacterTemplates.Companion("ranger")},
   new(){Id="mage",Name="Mira",Portrait=2,Pos=new(4,16),Weapon="staff",Tactics=TacticsProfile.Ranged,Character=CharacterTemplates.Companion("mage")},
   new(){Id="cleric",Name="Orren",Portrait=3,Pos=new(5,17),Weapon="mace",Tactics=TacticsProfile.Support,Character=CharacterTemplates.Companion("cleric")},
   // IIP5: all six party members now have distinct portrait frames. World body sprites remain independent.
   new(){Id="rogue",Name="Rook",Portrait=4,Pos=new(6,15),Weapon="sword",Tactics=TacticsProfile.Aggressive,Character=CharacterTemplates.Companion("rogue")},
   new(){Id="bard",Name="Elara",Portrait=5,Pos=new(6,17),Weapon="bow",Tactics=TacticsProfile.Support,Character=CharacterTemplates.Companion("bard")}];
  w.PartyOrder=["knight","ranger","mage","cleric","rogue","bard"];
  foreach(var member in w.Party)w.AddItem(member,"healing-potion",1);
  // IIP24: ranged weapon users begin with a real equipped quiver stack. Ammo is physical and persistent.
  foreach(var member in w.Party.Where(a=>Items.Get(a.Weapon).RequiresAmmunition))member.Equipment.Set(EquipmentSlot.Ammo1,w.CreateItemStack("arrows",40));
  void Enemy(string id,string name,string role,int x,int y,int hp,int ac,int group,int range=1){w.Actors.Add(new(){Id=id,Name=name,CreatureKind=role,Pos=new(x,y),Enemy=true,Group=group,Character=CharacterTemplates.Enemy(role,hp,ac,range)});}
  Enemy("s1","Crypt sentinel","Skeleton",21,13,15,12,0);Enemy("s2","Bone archer","Archer",25,11,12,12,0,6);Enemy("s3","Crypt sentinel","Skeleton",25,18,15,12,0);
  Enemy("g1","Hollow acolyte","Wraith",37,11,20,13,1,5);Enemy("g2","Gravebound","Skeleton",35,15,21,14,1);Enemy("g3","Gravebound","Skeleton",40,16,21,14,1);
  Enemy("boss","The Ashen Prior","Boss",39,27,52,15,2,5);Enemy("b1","Oathless guard","Skeleton",34,26,23,14,2);Enemy("b2","Oathless guard","Skeleton",36,28,23,14,2);
  // IIP26: corpses carry durable loot. Equipped enemy gear also remains lootable from the corpse panel.
  foreach(var enemy in w.Actors.Where(a=>a.Enemy))
  {
   if(enemy.CreatureKind=="Archer")w.AddItem(enemy,"arrows",8);
   else if(enemy.CreatureKind=="Wraith")w.AddItem(enemy,"healing-potion",1);
   else if(enemy.CreatureKind=="Boss")w.AddItem(enemy,"wand-sparks",1);
  }
  w.Features=[new(){Kind="exit",Name="Stairs to daylight",Pos=new(3,16)},new(){Kind="npc",Name="Warden Serel",Pos=new(8,14),DialogueId="warden-serel",DialoguePortrait=2},new(){Kind="chest",Name="Abandoned supplies",Pos=new(9,19),Loot="silver"},new(){Kind="door",Name="Ironbound door",Pos=new(16,16)},new(){Kind="chest",Name="Warden's coffer",Pos=new(19,3),Loot="ward"},new(){Kind="shrine",Name="Shrine of the last dawn",Pos=new(25,3)},new(){Kind="chest",Name="Acolyte's reliquary",Pos=new(41,9),Loot="seal"},new(){Kind="relic",Name="The Ember Heart",Pos=new(41,27),Loot="relic"}];
  w.Features.First(f=>f.Name=="Warden's coffer").Locked=true;
  var trapped=w.Features.First(f=>f.Name=="Acolyte's reliquary");trapped.DetectedTrap=true;trapped.Disarmable=true;
  foreach(var feature in w.Features.Where(f=>!string.IsNullOrWhiteSpace(f.Loot)))
  {
   feature.Gold=20;feature.Contents.Add(w.CreateItemStack(feature.Loot));if(feature.Kind=="chest")feature.Contents.Add(w.CreateItemStack("healing-potion",2));
   if(feature.Name=="Abandoned supplies")feature.Contents.Add(w.CreateItemStack("arrows",20));
   if(feature.Name=="Warden's coffer")feature.Contents.Add(w.CreateItemStack("shield"));
   if(feature.Name=="Acolyte's reliquary")feature.Contents.Add(w.CreateItemStack("ember-ring"));
  }
  w.AssignMissingItemIds();foreach(var member in w.Party)
  {
   if(member.Equipment.Weapon is {} weapon)member.QuickAccess.WeaponInstanceIds[0]=weapon.InstanceId;var potion=member.Inventory.Items.FirstOrDefault(x=>x.ItemId=="healing-potion");if(potion!=null)member.QuickAccess.ItemInstanceIds[0]=potion.InstanceId;
   var source=member.Character.Spellcasting.FirstOrDefault();if(source!=null){var first=source.Cantrips.FirstOrDefault()??source.PreparedSpells.FirstOrDefault()??source.KnownSpells.FirstOrDefault();if(!string.IsNullOrWhiteSpace(first))member.QuickAccess.SpellBindings[0]=new(){SourceId=source.SourceId,SpellId=first};}
   member.QuickAccess.Normalize(member);
  }
  w.InitializeTravelState();w.InitializeJournalState();w.InitializeCompanionConversationState();w.InitializeStoreState();w.InitializeServiceState();w.InitializeRestState();w.LogQuest("The stolen Ember Heart lies beneath Saint Orlan's crypt.");w.LogQuest("Recover it from the Ashen Prior, then return it to the Ember Hearth in Saint Orlan.");w.Message(MessageCategory.System,"Click to move. Click nearby objects to interact. I: inventory.");w.Reveal();return w;
 }
}
