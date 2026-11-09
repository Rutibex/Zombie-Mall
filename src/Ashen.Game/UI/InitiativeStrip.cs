using Ashen.Core;
using Microsoft.Xna.Framework;
using V=Microsoft.Xna.Framework.Vector2;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class InitiativeStrip
 {
  readonly CovenantGame g;
  Rectangle bounds;
  public InitiativeStrip(CovenantGame game)=>g=game;
  public bool Contains(Point p)=>g.world.Mode==Mode.Combat&&bounds.Contains(p);

  public void Draw(Rectangle worldViewport)
  {
   if(g.world.Mode!=Mode.Combat){bounds=Rectangle.Empty;return;}
   var order=g.world.InitiativeSequence();if(order.Count==0){bounds=Rectangle.Empty;return;}
   int pad=Math.Max(6,(int)Math.Round(8*g.ui.Layout.Scale));
   int height=Math.Max(66,(int)Math.Round(72*g.ui.Layout.Scale));
   bounds=new Rectangle(worldViewport.X+pad,worldViewport.Y+pad,worldViewport.Width-pad*2,height);
   g.Box(bounds,new Color(10,14,17)*.95f,Gold*.72f);g.buttons.Add((bounds,()=>{}));

   int roundW=Math.Max(96,(int)Math.Round(112*g.ui.Layout.Scale));
   var roundRect=new Rectangle(bounds.X+6,bounds.Y+6,roundW,bounds.Height-12);
   g.Box(roundRect,new Color(18,22,24),Gold*.66f);
   g.Center($"ROUND {g.world.Round}",new(roundRect.X+3,roundRect.Y+8,roundRect.Width-6,20),Bright,.58f,true);
   g.Center($"{order.Count} IN ORDER",new(roundRect.X+3,roundRect.Y+31,roundRect.Width-6,16),Muted,.36f);
   if(roundRect.Contains(g.cursor))g.tooltip=$"Combat round {g.world.Round}. Initiative owner: {g.world.Active?.Name??"none"}. The strip lists the current combatant first, then upcoming turns.";

   int x=roundRect.Right+7,available=bounds.Right-7-x;
   int minCard=Math.Max(88,(int)Math.Round(94*g.ui.Layout.Scale));
   int maxCard=Math.Max(minCard,(int)Math.Round(126*g.ui.Layout.Scale));
   int gap=Math.Max(3,(int)Math.Round(4*g.ui.Layout.Scale));
   int maxCards=Math.Max(1,(available+gap)/(minCard+gap));
   bool overflow=order.Count>maxCards;
   int visibleCount=overflow?Math.Max(1,maxCards-1):order.Count;
   int slots=visibleCount+(overflow?1:0);
   int cardW=Math.Clamp((available-gap*Math.Max(0,slots-1))/Math.Max(1,slots),minCard,maxCard);

   for(int i=0;i<visibleCount;i++)
   {
    var a=order[i];var r=new Rectangle(x,bounds.Y+6,cardW,bounds.Height-12);DrawCard(r,a,i);x+=cardW+gap;
   }
   if(overflow)
   {
    int hidden=order.Count-visibleCount;var r=new Rectangle(x,bounds.Y+6,Math.Max(minCard,Math.Min(cardW,bounds.Right-7-x)),bounds.Height-12);
    g.Box(r,new Color(17,20,22),Gold*.42f);g.Center($"+{hidden} MORE",new(r.X+2,r.Y+12,r.Width-4,18),Gold,.45f,true);g.Center("UPCOMING",new(r.X+2,r.Y+34,r.Width-4,14),Muted,.31f);
    if(r.Contains(g.cursor))g.tooltip=$"{hidden} additional combatant{(hidden==1?"":"s")} remain later in the initiative cycle. Their order updates automatically as participants enter or leave combat.";
   }
  }

  void DrawCard(Rectangle r,Actor a,int offset)
  {
   bool active=offset==0&&g.world.Active==a;Color edge=active?Bright:a.Enemy?Red:Teal;
   g.Box(r,active?new Color(31,28,20):new Color(16,20,22),edge*(active?1f:.72f));
   if(active){g.Border(r,Bright,2);g.Border(new(r.X+3,r.Y+3,r.Width-6,r.Height-6),Gold,1);}
   int icon=Math.Min(34,r.Height-16);var art=new Rectangle(r.X+5,r.Y+5,icon,icon);
   if(!a.Enemy)g.Portrait(a,art);
   else{var (atlas,index)=g.Body(a);g.batch.Draw(g.tex[atlas],art,g.bodyBounds[atlas][index],Color.White);}
   string turn=active?"NOW":offset==1?"NEXT":"+"+offset;
   g.Text(turn,r.X+icon+10,r.Y+6,active?Bright:Muted,.39f);
   g.Text($"INIT {a.Initiative}",r.X+icon+10,r.Y+22,Gold,.36f);
   string name=a.Name;while(name.Length>5&&g.Measure(name,.37f)>r.Width-10)name=name[..^1];
   if(name!=a.Name)name=name.Length>1?name[..^1]+".":name;
   g.Text(name,r.X+5,r.Bottom-17,a.Enemy?new Color(230,176,161):Bright,.37f);
   g.Bar(new(r.X+5,r.Bottom-5,r.Width-10,3),a.MaxHp<=0?0:a.Hp/(float)a.MaxHp,a.Hp<=a.MaxHp/4?Red:a.Hp<=a.MaxHp/2?Gold:Teal);

   if(r.Contains(g.cursor))
   {
    string status=a.Character.Conditions.Count==0?"No conditions":string.Join(", ",a.Character.Conditions.Take(3).Select(c=>c.Name));
    g.tooltip=$"{a.Name} — initiative {a.Initiative}; HP {a.Hp}/{a.MaxHp}; AC {a.Ac}. {(active?"Current turn.":$"Upcoming position +{offset}.")} {status}. Click to focus{(a.Enemy?".":" and inspect without transferring initiative.")}";
    g.ui.Cursors.Set(UiCursorKind.Select);
   }
   var focus=a;g.buttons.Add((r,()=>ActivateCard(focus)));
  }

  public void ActivateForCheck(Actor actor)=>ActivateCard(actor);

  void ActivateCard(Actor actor)
  {
   string? owner=g.world.Active?.Id;
   g.FocusActor(actor);
   if(!actor.Enemy)
   {
    int slot=g.world.PartyIndex(actor);if(slot>=0)g.SelectCharacter(slot);
   }
   // Selection/inspection is intentionally separate from initiative ownership.
   if(g.uiCheck&&owner!=null&&g.world.Active?.Id!=owner)throw new InvalidOperationException("Initiative card inspection transferred initiative ownership.");
  }
 }
}
