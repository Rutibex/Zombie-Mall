using Ashen.Core;
using System.Text.Json.Nodes;

static class CampaignTests
{
 public static void Run(Action<bool,string> check)
 {
  var c=CampaignLoader.Load(CampaignLoader.BundledFixturePath);
  check(CampaignLoader.Validate(c).Count==0,"Phase 3 bundled campaign passes strict validation");
  var w=World.New(c);
  check(w.Campaign?.Id==c.Id&&w.CurrentTravelDestinationId=="loc.lower.01"&&w.CurrentAreaName=="Campaign Test Room","Phase 3 world created from authored area definitions");
  check(w.PartyCount==2&&w.Actors.Count==3&&w.Features.Count==4,"Phase 3 definitions create actor and feature placements");
  check(w.Quests.Count==1&&w.Quests.All(q=>q.Id!="recover-ember-heart")&&w.Stores.Count==0&&w.Services.Count==0&&!w.Log.Any(l=>l.Contains("Ember Heart")),"Phase 3 fixture is independent of the original campaign");
  check(w.PartyLeader.Equipment.Weapon!.Definition.Name=="Crowbar"&&!Items.TryGet("item.melee.006",out _),"Phase 3 campaign gear does not mutate the global legacy item catalogue");
  var isolated=CampaignLoader.Clone(c);isolated.Items[0].Name="Different Crowbar";var other=World.New(isolated);
  check(other.PartyLeader.Equipment.Weapon!.Definition.Name=="Different Crowbar"&&w.PartyLeader.Equipment.Weapon!.Definition.Name=="Crowbar","Phase 3 simultaneous worlds isolate definitions with the same stable item IDs");
  c.Items[0].Name="External mutation";
  check(w.PartyLeader.Equipment.Weapon!.Definition.Name=="Crowbar","Phase 3 new world snapshots definitions independently of its caller");
  var box=w.Features.Single(f=>f.Id=="feature.fixture.supply-box");w.Chosen.Pos=new(9,18);w.Reveal();
  check(w.TakeContainerItem(w.Chosen,box,0,1)&&w.Chosen.Inventory.Items[0].Definition.Name=="Canned Beans"&&box.Contents.Items[0].Quantity==2,"Phase 3 custom container item splitting retains bound definitions");
  check(w.Quests[0].Objectives.Single(o=>o.Id=="objective.fixture.supplies").Completed,"Phase 3 item objective updates after a real loot transfer");
  check(w.TryGetDialogue("dialogue.fixture.survivor",out var conversation)&&conversation.Nodes.Length==2,"Phase 3 dialogue graph resolves through the world campaign");
  w.ApplyDialogueConsequence(conversation.Nodes[0].Choices[1].Consequence);
  check(w.Quests[0].Objectives.Single(o=>o.Id=="objective.fixture.speak").Completed,"Phase 3 authored dialogue flag updates the objective");
  var radio=w.Features.Single(f=>f.Id=="feature.fixture.radio");w.Chosen.Pos=new(9,15);w.Reveal();
  check(w.Interact(radio)&&w.HasDialogueFlag("flag.fixture.radio"),"Phase 3 registered event effect dispatches from an actual world interaction");
  int logs=w.Log.Count;check(!w.Interact(radio)&&!w.RunCampaignEvent(radio.CampaignEventId)&&w.ExecutedCampaignEvents.Count==1&&w.Log.Count==logs,"Phase 3 once-only event cannot repeat or duplicate its output");
  var door=w.Features.Single(f=>f.Id=="feature.fixture.door");w.Chosen.Pos=new(15,16);w.Reveal();
  check(w.Interact(door)&&w.Quests[0].Objectives.Single(o=>o.Id=="objective.fixture.door").Completed,"Phase 3 stable feature ID drives objective completion");
  w.Chosen.Pos=new(21,16);w.Reveal();if(w.Mode!=Mode.Combat)w.StartCombat(0);
  foreach(var enemy in w.Actors.Where(a=>a.Enemy))enemy.Hp=0;w.CheckEnd();
  check(w.Mode==Mode.Explore&&w.Gold==0&&w.Party.All(a=>a.Character.Experience==0),"Phase 3 encounter rewards come from data, without inherited fantasy XP or currency");
  check(w.Quests[0].Status==QuestStatus.Completed,"Phase 3 all five data-defined objectives complete through existing game actions");
  string save=Path.Combine(Path.GetTempPath(),"zombie-campaign-"+Guid.NewGuid()+".json");
  try
  {
   w.Save(save);var loaded=World.Load(save);
   check(loaded.Version==World.CurrentSaveVersion&&loaded.Campaign?.Id==w.Campaign?.Id&&loaded.Chosen.Equipment.Weapon!.Definition.Name=="Crowbar","Phase 3 save snapshot round-trips custom gear without external files");
   check(loaded.Quests.Count==1&&loaded.Quests[0].Status==QuestStatus.Completed&&loaded.ExecutedCampaignEvents.Count==1&&loaded.Features.Single(f=>f.Id==door.Id).Used,"Phase 3 objective, event, feature and dialogue progress survives loading");
   check(loaded.Features.Single(f=>f.Id==box.Id).Contents.Items[0].Quantity==2&&loaded.Party.Sum(a=>a.Inventory.Count("item.commodity.001"))==1,"Phase 3 saved custom item quantities persist");
   var root=JsonNode.Parse(File.ReadAllText(save))!.AsObject();root["Campaign"]!["SchemaVersion"]=99;File.WriteAllText(save,root.ToJsonString());bool rejected=false;try{World.Load(save);}catch(CampaignValidationException){rejected=true;}check(rejected,"Phase 3 unsupported embedded campaign schema is rejected before binding items");
  }
  finally{File.Delete(save);}
  var heavy=CampaignLoader.Load(CampaignLoader.BundledFixturePath);heavy.Items[0].DamageDie=12;var heavyWorld=World.New(heavy);
  check(heavyWorld.PartyLeader.Die==12&&heavyWorld.PreviewAttack(heavyWorld.PartyLeader,heavyWorld.Actors.First(a=>a.Enemy),AttackKind.Basic).MaxDamage==15,"Phase 3 campaign weapon die feeds actor and attack previews");
  var replayable=CampaignLoader.Load(CampaignLoader.BundledFixturePath);replayable.Events[0].Once=false;var repeatWorld=World.New(replayable);var repeatRadio=repeatWorld.Features.First(f=>f.Kind=="campaign-event");repeatWorld.Chosen.Pos=new(9,15);repeatWorld.Reveal();
  check(repeatWorld.Interact(repeatRadio)&&repeatWorld.Interact(repeatRadio),"Phase 3 repeatable authored events stay interactable");
  var legacy=World.New();check(legacy.CurrentTravelDestinationId=="saint-orlans-crypt"&&legacy.PartyCount==6&&legacy.Campaign==null,"Phase 3 legacy campaign still has its separate authored factory");
  void Invalid(string label,Action<CampaignDefinition> edit,string code)
  {var bad=CampaignLoader.Load(CampaignLoader.BundledFixturePath);edit(bad);check(CampaignLoader.Validate(bad).Any(i=>i.Code==code),"Phase 3 rejects "+label);}
  Invalid("duplicate stable IDs",x=>x.Items[1].Id=x.Items[0].Id,"duplicate");
  Invalid("unknown item references",x=>x.Actors[0].WeaponId="missing.item","reference");
  Invalid("unknown effect handlers",x=>x.Events[0].Effects[0].Handler="execute-code","handler");
  Invalid("out-of-map placement",x=>x.Areas[0].Actors[0].Position=new(-2,3),"position");
  Invalid("invalid source page ranges",x=>x.Items[0].Source.PageEnd=999,"source");
  Invalid("invalid stack quantities",x=>x.Areas[0].Features[0].Contents[0].Quantity=0,"range");
  Invalid("missing encounter members",x=>x.Encounters[0].ActorIds=["missing.actor"],"reference");
  Invalid("non-enemy encounter members",x=>x.Encounters[0].ActorIds=[x.Actors[0].Id],"encounter");
  Invalid("invalid objective targets",x=>x.Objectives[0].Trigger=CampaignObjectiveTrigger.ItemOwned,"reference");
  Invalid("missing party",x=>x.Actors.ForEach(a=>a.Enemy=true),"party");
  Invalid("invalid equipment slots",x=>x.Items[0].Slot="Boots","slot");
  Invalid("null nested actor collections",x=>x.Areas[0].Actors=null!,"collection");
  Invalid("objectives with unproduced flags",x=>x.Objectives[0].TargetId="flag.missing","reference");
  Invalid("dialogue choices without an exit",x=>x.Dialogues[0].Nodes[0].Choices[0]=x.Dialogues[0].Nodes[0].Choices[0] with{NextNodeId=""},"dialogue");
  Invalid("invalid schema versions",x=>x.SchemaVersion=99,"schema");
  Invalid("dangling dialogue branches",x=>x.Dialogues[0].Nodes[0].Choices[0]=x.Dialogues[0].Nodes[0].Choices[0] with{NextNodeId="missing"},"dialogue");
  Invalid("invalid enum values",x=>x.Items[0].Kind=(ItemKind)999,"range");
  var input=File.ReadAllText(CampaignLoader.BundledFixturePath);bool unknown=false;
  try{CampaignLoader.Parse(input.Replace("\"schemaVersion\": 1","\"misspelledSchema\": 1"));}catch(System.Text.Json.JsonException){unknown=true;}
  check(unknown,"Phase 3 rejects unknown JSON property names instead of silently ignoring typos");
  string legacySave=Path.Combine(Path.GetTempPath(),"zombie-v15-"+Guid.NewGuid()+".json");
  try{legacy.Save(legacySave);var root=JsonNode.Parse(File.ReadAllText(legacySave))!.AsObject();root["Version"]=15;root.Remove("Campaign");File.WriteAllText(legacySave,root.ToJsonString());check(World.Load(legacySave).Version==World.CurrentSaveVersion,"Phase 3 migrates a v15 legacy adventure to v16 without adding mall content");}finally{File.Delete(legacySave);}
 }
}
