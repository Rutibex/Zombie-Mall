namespace Ashen.Core;
public sealed class CampaignLowerMall
{
 public string ConcourseAreaId {get;set;}="";
 public List<LowerMallSite> Sites {get;set;}=[];
 public List<LowerMallMonster> Monsters {get;set;}=[];
}
public sealed class LowerMallSite
{
 public int Number {get;set;}
 public string Level {get;set;}="lower";
 [System.Text.Json.Serialization.JsonIgnore] public int DisplayNumber=>Level=="exterior"?Number-300:Level=="roof"?Number-200:Level=="upper"?Number-100:Number;
 [System.Text.Json.Serialization.JsonIgnore] public string Prefix=>$"{Level}.{DisplayNumber:00}";
 public string AreaId {get;set;}="";
 public string Name {get;set;}="";
 public SourceReference Source {get;set;}=new();
 public string BookText {get;set;}="";
 public int SecureXp {get;set;}
 public int EncounterSides {get;set;}=6;
 public string BackEncounterId {get;set;}="";
 public List<LowerMallAction> Actions {get;set;}=[];
 public List<LowerMallEncounter> Encounters {get;set;}=[];
}
public sealed class LowerMallAction
{
 public string Id {get;set;}="";
 public string AuditId {get;set;}="";
 public string FeatureId {get;set;}="";
 public string Name {get;set;}="";
 public string Text {get;set;}="";
 public string Kind {get;set;}="lore";
 public string Effect {get;set;}="";
 public string Skill {get;set;}="Investigation";
 public string Save {get;set;}="Dexterity";
 public int Dc {get;set;}
 public int ForceDc {get;set;}
 public int Minutes {get;set;}=5;
 public int Xp {get;set;}
 public int DamageDice {get;set;}
 public int DamageDie {get;set;}=6;
 public DamageType DamageType {get;set;}=DamageType.Bludgeoning;
 public int TensionDice {get;set;}
 public int TensionDie {get;set;}=6;
 public string Condition {get;set;}="";
 public int DurationSeconds {get;set;}=60;
 public string EncounterId {get;set;}="";
 public string RequiredActionId {get;set;}="";
 public string CostItemId {get;set;}="";
 public string NpcActorId {get;set;}="";
}
public sealed class LowerMallEncounter
{
 public int Low {get;set;}=1;
 public int High {get;set;}=6;
 public string EncounterId {get;set;}="";
 public string Text {get;set;}="";
 public int Dice {get;set;}
 public int Die {get;set;}=4;
 public int Bonus {get;set;}=1;
}
public sealed class LowerMallMonster
{
 public int RewardXp {get;set;}
 public string ActorId {get;set;}="";
 public List<string> Traits {get;set;}=[];
}
public sealed class LowerMallRelease
{
 public string EncounterId {get;set;}="";
 public int Count {get;set;}
 public string Trigger {get;set;}="entry";
}
public sealed class LowerMallState
{
 public Dictionary<string,int> LootRolls {get;set;}=[];
 public Dictionary<string,int> SearchAttempts {get;set;}=[];
 public int Number {get;set;}
 public int EnteredMinute {get;set;}
 public int EncounterRoll {get;set;}
 public string HazardId {get;set;}="";
 public bool HazardResolved {get;set;}
 public int StockRoll {get;set;}
 public int BranchRoll {get;set;}
 public int NextHazardMinute {get;set;}=-1;
 public Dictionary<string,int> DailyUses {get;set;}=[];
 public int BonusRoll {get;set;}
 public int NextEventMinute {get;set;}=-1;
 public int EchoRoll {get;set;}
 public int CarAttempts {get;set;}
 public Dictionary<string,int> OuterRolls {get;set;}=[];
 public Dictionary<string,double> OuterTimers {get;set;}=[];
 public List<string> OuterSignalUnits {get;set;}=[];
 public Dictionary<string,int> ExteriorRolls {get;set;}=[];
 public Dictionary<string,double> ExteriorTimers {get;set;}=[];
 public Dictionary<string,int> RoofTimers {get;set;}=[];
 public Dictionary<string,int> RoofRolls {get;set;}=[];
 public List<string> RoofSignalUnits {get;set;}=[];
 public int FoodCycle {get;set;}
 public int CarLimit {get;set;}
 public bool Quiet {get;set;}=true;
 public bool Secured {get;set;}
 public List<LowerMallRelease> Releases {get;set;}=[];
 public List<string> Resolved {get;set;}=[];
 public List<string> Credits {get;set;}=[];
 public List<string> Events {get;set;}=[];
 public int FruitMinute {get;set;}=-1;
 public int HarvestMinute {get;set;}=-1;
}
public sealed class LowerMallEffect
{
 public string ActorId {get;set;}="";
 public string Condition {get;set;}="";
 public double EndsAt {get;set;}
}
public static class LowerMallRules
{
 public static readonly HashSet<string> Traits=["ext31-tire","ext31-oil","ext31-mechanic","ext31-burning","ext31-manager","ext31-radio","ext31-screamer","ext31-revive","ext31-gasbag","ext31-vault","ext31-perfect","ext31-paint","ext31-brute","ext31-cop","ext31-rats","ext31-crate","ext31-water","ext30-pistol","ext30-shotgun","ext30-regen","ext30-screech","ext30-brute","ext30-exploder","ext30-revive","ext30-rats","ext30-frost","ext30-frozen","ext30-murphy","ext30-doughnut","ext30-grease","ext30-flammable","roof29-drowned","roof29-clinger","roof29-bird","roof29-drinker","roof29-toast","roof29-gasbag","roof29-ventcrawler","roof29-repeater","roof29-drop","roof29-biker","roof29-screamer","roof29-guard","roof29-maw","roof29-hungerling","roof29-feral","roof29-wet","roof29-horror","roof-crawler","roof-officer","roof-guard","roof-howarth","roof-vent","roof-technician","roof-drone","roof-drowned","roof-waterthing","roof-rat","roof-shadow","roof-ooze","roof-scorched","roof-shelterrat","roof-elder","roof-shotgun","roof-vest","roof-brute","roof-vine","roof-revolver","roof-subject","roof-screamer","roof-clinger","roof-adman","final27-puppeteer","final27-monitor","final27-gourmand","final27-revenant","final27-cape","final27-collector","final27-sales","final27-plush","final27-cleave","final27-grocery","final27-child","final27-fungal","final27-paper","final27-inkspawn","final27-receiver","final27-cyber","final27-pack","final27-alpha","final27-dough","final27-demon","final27-wax","final27-scorched","final27-lurker","gallery-nocrit","gallery-composer","gallery-costume","gallery-parrot","gallery-python","gallery-small","gallery-collector","gallery-baker","gallery-cobbler","gallery-bluntresist","gallery-caffeine","gallery-groover","gallery-charge","gallery-glitched","gallery-boss","gallery-glitcher","gallery-screamer","gallery-comic-collector","gallery-panel","gallery-porcelain","gallery-art","gallery-spores","gallery-gasbag","gallery-grease","gallery-feral","gallery-frozen","gallery-ink","upper-fast","upper-screamer","upper-hartley","upper-chant","upper-diseaseburst","upper-eye","upper-static","upper-swarm","upper-burst8","upper-burst6","upper-areaonly","upper-fireburst","upper-groan","upper-slogan","upper-electricresist","upper-drone","upper-cobbler","grapple","acid","bile","psychic","spores","maggots","swarm","lifesteal","butcher","cold","mannequin","finish","ceramic","regenerate","bloom","sonic","layered","flurry","ink","jitter","charge","cyber","stun","scratch","rooted","reinforce","infection","human","fear","bleed","ambush","coach","hoarder","dormant","mindterror","knight","mask","visions","luggage","suitcase","shortcircuit","frozen","fireweak","poison","scream","sporebrute","vineboss","slashresist","cultist","silent","drummer","conductor","mountain","model","reflection","gasbody","minute-revive","deathwail","two-dice","camper","final-mold","final-herbalist","final-small","final-doll","final-screamer","final-collector","final-ambush","final-smile","final-feast","final-acid","final-chant","final-immovable","final-disease","final-meatheal","final-eyeless","final-display","final-still","final-cleave","final-manager","final-fresh","final-archivist","final-patient","final-spitter","final-disperse","final-grease","final-bloated","final-managercall","final-wax","final-slow","final-weaver","final-reflection","final-bleed"];
 public static void Validate(CampaignDefinition c,Action<string,string,string> error)
 {
  var d=c.LowerMall;if(d==null)return;
  if(c.SchemaVersion<20||c.Story==null||c.Shelter==null||!c.Areas.Any(a=>a.Id==d.ConcourseAreaId))error("lower","chapter","Schema 20, refuge and a real concourse are required.");
  if(d.Sites==null||d.Monsters==null||d.Sites.Any(s=>s==null)||d.Monsters.Any(m=>m==null)){error("lower","collections","Null chapter data.");return;}
  int expected=c.SchemaVersion>=29?159:c.SchemaVersion>=28?146:c.SchemaVersion>=27?133:c.SchemaVersion>=26?121:c.SchemaVersion>=25?108:c.SchemaVersion>=24?90:c.SchemaVersion>=23?72:c.SchemaVersion>=22?54:c.SchemaVersion>=21?36:18;
  if(d.Sites.Count!=expected||!d.Sites.Select(s=>s.Number).Order().SequenceEqual(c.SchemaVersion>=28?Enumerable.Range(1,54).Concat(Enumerable.Range(101,54)).Concat(Enumerable.Range(201,25)).Concat(Enumerable.Range(301,c.SchemaVersion>=29?26:13)):c.SchemaVersion>=26?Enumerable.Range(1,54).Concat(Enumerable.Range(101,54)).Concat(Enumerable.Range(201,c.SchemaVersion>=27?25:13)):c.SchemaVersion>=23?Enumerable.Range(1,54).Concat(Enumerable.Range(101,c.SchemaVersion>=25?54:c.SchemaVersion>=24?36:18)):Enumerable.Range(1,expected))||d.Sites.Select(s=>s.AreaId).Distinct().Count()!=expected)error("lower","sites","Consecutive authored lower-mall locations are required.");
  var ids=new HashSet<string>();foreach(var s in d.Sites)
  {
   var area=c.Areas.FirstOrDefault(a=>a.Id==s.AreaId);if(area==null||s.Source==null||s.Level is not ("lower" or "upper" or "roof" or "exterior")||s.Level=="exterior"&&(c.SchemaVersion<28||s.Number<301||s.Number>(c.SchemaVersion>=29?326:313))||s.Level=="roof"&&(c.SchemaVersion<26||s.Number<201||s.Number>(c.SchemaVersion>=27?225:213))||s.Level=="upper"&&(c.SchemaVersion<23||s.Number<101||s.Number>(c.SchemaVersion>=25?154:c.SchemaVersion>=24?136:118))||s.Level=="lower"&&s.Number>54||s.Source.PageStart<(s.Level=="exterior"?128:s.Level=="roof"?445:s.Level=="upper"?333:195)||s.Source.PageEnd>(s.Level=="exterior"?(c.SchemaVersion>=29?193:160):s.Level=="roof"?(c.SchemaVersion>=27?508:479):s.Level=="upper"?(s.Number>136?443:s.Number>118?399:362):s.Number>36?330:s.Number>18?285:241)||s.BookText.Length<1000||s.SecureXp is <0 or >500||s.Actions==null||s.Encounters==null||s.Actions.Any(a=>a==null)){error("lower",s.Name,"Invalid source or location.");continue;}
   foreach(var a in s.Actions)
   {
    if(!ids.Add(a.Id)||string.IsNullOrWhiteSpace(a.Id)||!area.Features.Any(f=>f.Id==a.FeatureId)||a.Kind is not ("loot" or "lore" or "hazard" or "device" or "backroom" or "reference" or "event")||!Enum.TryParse<Skill>(a.Skill,out _)||!Enum.TryParse<Ability>(a.Save,out _)||a.Dc is <0 or >25||a.ForceDc is <0 or >25||a.Minutes is <0 or >60||a.Xp<0||a.Xp>(s.Level=="roof"?1000:500)||a.DamageDice is <0 or >6||a.DamageDie is <4 or >12||a.TensionDice is <0 or >5||a.TensionDie is <4 or >8||a.DurationSeconds is <0 or >604800||a.EncounterId!=""&&!c.Encounters.Any(e=>e.Id==a.EncounterId&&e.AreaId==s.AreaId)||a.RequiredActionId!=""&&!s.Actions.Any(x=>x.Id==a.RequiredActionId)||a.CostItemId!=""&&!c.Items.Any(i=>i.Id==a.CostItemId)||a.NpcActorId!=""&&!c.Actors.Any(n=>n.Id==a.NpcActorId&&n.Recruitable))error("lower",a.Id,"Invalid bounded physical action.");
   }
   if(!s.Actions.Any(a=>a.Kind=="hazard")||!c.Encounters.Any(e=>e.Id==s.BackEncounterId&&e.AreaId==s.AreaId))error("lower",s.Name,"Missing hazard or back-room threat.");
   foreach(var e in s.Encounters)if(e.Low<1||e.High>(s.Level is "upper" or "roof" or "exterior"?s.EncounterSides:6)||e.Low>e.High||e.Dice is <0 or >2||e.Die<2||e.Die>(s.Level is "upper" or "roof" or "exterior"?8:6)||e.Bonus<0||e.Bonus>(s.Level is "upper" or "roof" or "exterior"?8:3)||e.EncounterId!=""&&!c.Encounters.Any(x=>x.Id==e.EncounterId&&x.AreaId==s.AreaId&&x.RewardXp==0&&x.RewardCurrency==0))error("lower",s.Name,"Invalid encounter roll.");
  }
  if(d.Monsters.Select(m=>m.ActorId).Distinct().Count()!=d.Monsters.Count||d.Monsters.Any(m=>!c.Actors.Any(a=>a.Id==m.ActorId&&a.Enemy)||m.RewardXp is <0 or >500||m.Traits==null||m.Traits.Any(t=>!Traits.Contains(t))))error("lower","monsters","Invalid reserved monster or trait.");
 }
}
