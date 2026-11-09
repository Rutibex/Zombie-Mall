namespace Ashen.Core;

// IIP19 keeps the first five numeric values stable because older saves, screenshots and
// tests already use them. The expanded catalogue gives the five quick slots something
// meaningful to configure rather than making every visible button permanently hard-wired.
public enum FormationPreset
{
 Wedge=0, Line=1, Column=2, Box=3, Arc=4,
 Diamond=5, EchelonLeft=6, EchelonRight=7, Loose=8
}

public readonly record struct FormationOffset(float Back,float Side);

public static class FormationPatterns
{
 static readonly FormationOffset[] Wedge=[new(0,0),new(.85f,.75f),new(.85f,-.75f),new(1.70f,1.15f),new(1.70f,0),new(1.70f,-1.15f)];
 static readonly FormationOffset[] Line=[new(0,0),new(0,.82f),new(0,-.82f),new(0,1.64f),new(0,-1.64f),new(0,2.46f)];
 static readonly FormationOffset[] Column=[new(0,0),new(.82f,0),new(1.64f,0),new(2.46f,0),new(3.28f,0),new(4.10f,0)];
 static readonly FormationOffset[] Box=[new(0,0),new(.82f,.72f),new(.82f,-.72f),new(1.64f,.72f),new(1.64f,-.72f),new(2.46f,0)];
 static readonly FormationOffset[] Arc=[new(0,0),new(.42f,.82f),new(.42f,-.82f),new(.92f,1.52f),new(.92f,-1.52f),new(1.20f,0)];
 static readonly FormationOffset[] Diamond=[new(0,0),new(.82f,.82f),new(.82f,-.82f),new(1.64f,0),new(2.46f,.82f),new(2.46f,-.82f)];
 static readonly FormationOffset[] EchelonLeft=[new(0,0),new(.70f,-.70f),new(1.40f,-1.40f),new(2.10f,-2.10f),new(2.80f,-2.80f),new(3.50f,-3.50f)];
 static readonly FormationOffset[] EchelonRight=[new(0,0),new(.70f,.70f),new(1.40f,1.40f),new(2.10f,2.10f),new(2.80f,2.80f),new(3.50f,3.50f)];
 static readonly FormationOffset[] Loose=[new(0,0),new(1.15f,1.20f),new(1.05f,-1.25f),new(2.25f,1.75f),new(2.35f,-1.70f),new(2.85f,.10f)];

 public static IReadOnlyList<FormationPreset> Catalogue {get;}=Enum.GetValues<FormationPreset>();
 public static IReadOnlyList<FormationOffset> Slots(FormationPreset preset)=>preset switch
 {
  FormationPreset.Line=>Line,
  FormationPreset.Column=>Column,
  FormationPreset.Box=>Box,
  FormationPreset.Arc=>Arc,
  FormationPreset.Diamond=>Diamond,
  FormationPreset.EchelonLeft=>EchelonLeft,
  FormationPreset.EchelonRight=>EchelonRight,
  FormationPreset.Loose=>Loose,
  _=>Wedge
 };
 public static string Name(FormationPreset preset)=>preset switch
 {
  FormationPreset.Line=>"Line",
  FormationPreset.Column=>"Column",
  FormationPreset.Box=>"Box",
  FormationPreset.Arc=>"Arc",
  FormationPreset.Diamond=>"Diamond",
  FormationPreset.EchelonLeft=>"Echelon Left",
  FormationPreset.EchelonRight=>"Echelon Right",
  FormationPreset.Loose=>"Loose",
  _=>"Wedge"
 };
 public static string ShortName(FormationPreset preset)=>preset switch
 {
  FormationPreset.EchelonLeft=>"ECH L",
  FormationPreset.EchelonRight=>"ECH R",
  _=>Name(preset).ToUpperInvariant()
 };
 public static string Hint(FormationPreset preset)=>preset switch
 {
  FormationPreset.Line=>"A broad horizontal line. Useful for ranged parties and wide corridors.",
  FormationPreset.Column=>"A narrow marching column. Useful for doors and tight passages.",
  FormationPreset.Box=>"A compact two-rank block that keeps the party close together.",
  FormationPreset.Arc=>"A shallow crescent with the leader forward and the flanks spread.",
  FormationPreset.Diamond=>"A compact diamond with a protected centerline and balanced flanks.",
  FormationPreset.EchelonLeft=>"A diagonal line trailing to the leader's left; useful when one flank needs depth.",
  FormationPreset.EchelonRight=>"A diagonal line trailing to the leader's right; useful when one flank needs depth.",
  FormationPreset.Loose=>"A widely spaced travel formation that reduces crowding in open rooms.",
  _=>"The classic travel wedge: leader forward, companions staggered behind."
 };
}
