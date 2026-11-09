namespace Ashen.Game;

public sealed partial class CovenantGame
{
 enum UiModalId { Context, Reaction, Information, MapNote, JournalNote, SaveName }
 sealed class ModalManager
 {
  UiModalId? active;
  public bool IsOpen=>active.HasValue;
  public bool ContextOpen=>active==UiModalId.Context;
  public bool ReactionOpen=>active==UiModalId.Reaction;
  public bool InformationOpen=>active==UiModalId.Information;
  public bool MapNoteOpen=>active==UiModalId.MapNote;
  public bool JournalNoteOpen=>active==UiModalId.JournalNote;
  public bool SaveNameOpen=>active==UiModalId.SaveName;
  public void Open(UiModalId id)=>active=id;
  public bool Close(){if(!active.HasValue)return false;active=null;return true;}
  public void Clear()=>active=null;
 }
}
