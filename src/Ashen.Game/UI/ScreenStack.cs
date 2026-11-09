namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum UiScreenId
 {
  Map,
  WorldMap,
  Journal,
  Inventory,
  Character,
  Biography,
  CharacterInformation,
  LevelUp,
  Spellbook,
  Abilities,
  Help,
  Settings,
  Chronicle,
  Menu,
  SaveGame,
  LoadGame,
  Expedition
 }

 /// <summary>
 /// IIP9 navigation authority. Root destinations replace one another directly;
 /// child destinations retain a canonical parent and Back/Escape walks toward
 /// that parent before returning to the game world.
 /// </summary>
 sealed class ScreenStack
 {
  readonly List<UiScreenId> stack=[];
  public bool IsOpen=>stack.Count>0;
  public int Depth=>stack.Count;
  public UiScreenId? Active=>stack.Count==0?null:stack[^1];
  public UiScreenId? Root=>stack.Count==0?null:stack[0];
  public UiScreenId? Parent=>stack.Count<2?null:stack[^2];
  public bool HasChild=>stack.Count>1;
  public IReadOnlyList<UiScreenId> Path=>stack;

  public static UiScreenId? ParentOf(UiScreenId id)=>id switch
  {
   UiScreenId.WorldMap=>UiScreenId.Map,
   UiScreenId.Biography=>UiScreenId.Character,
   UiScreenId.CharacterInformation=>UiScreenId.Character,
   UiScreenId.LevelUp=>UiScreenId.Character,
   UiScreenId.Settings=>UiScreenId.Menu,
   UiScreenId.SaveGame=>UiScreenId.Menu,
   UiScreenId.LoadGame=>UiScreenId.Menu,
   _=>null
  };
  public static bool IsChild(UiScreenId id)=>ParentOf(id).HasValue;
  public bool IsFamily(UiScreenId root)=>Root==root;

  static List<UiScreenId> CanonicalPath(UiScreenId id)
  {
   var path=new List<UiScreenId>();var seen=new HashSet<UiScreenId>();UiScreenId? current=id;
   while(current.HasValue)
   {
    if(!seen.Add(current.Value))throw new InvalidOperationException($"Screen parent cycle at {current.Value}.");
    path.Add(current.Value);current=ParentOf(current.Value);
   }
   path.Reverse();return path;
  }

  public void NavigateRoot(UiScreenId id)
  {
   if(IsChild(id))throw new InvalidOperationException($"{id} is a child screen and cannot be opened as a root destination.");
   stack.Clear();stack.Add(id);
  }

  /// <summary>Open any destination by its canonical path. Used by capture/debug entry points.</summary>
  public void Navigate(UiScreenId id)
  {
   stack.Clear();stack.AddRange(CanonicalPath(id));
  }

  public void OpenChild(UiScreenId id)
  {
   if(!IsChild(id))throw new InvalidOperationException($"{id} is not a child screen.");
   stack.Clear();stack.AddRange(CanonicalPath(id));
  }

  // Compatibility alias for older phase call sites; IIP9 code should prefer OpenChild.
  public void Push(UiScreenId id)
  {
   if(IsChild(id))OpenChild(id);else NavigateRoot(id);
  }

  public void ToggleRoot(UiScreenId id)
  {
   if(IsChild(id))throw new InvalidOperationException($"{id} is not a root destination.");
   if(IsFamily(id))ReturnToGame();else NavigateRoot(id);
  }
  public void Toggle(UiScreenId id)
  {
   if(IsChild(id)){if(Active==id)Back();else OpenChild(id);}
   else ToggleRoot(id);
  }

  /// <summary>Back one navigation level. A root screen backs out to the game world.</summary>
  public bool Back()
  {
   if(stack.Count==0)return false;
   stack.RemoveAt(stack.Count-1);return true;
  }
  public bool CloseTop()=>Back();
  public void ReturnToGame()=>stack.Clear();
  public void Clear()=>ReturnToGame();
 }
}
