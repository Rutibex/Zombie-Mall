using Ashen.Core;
using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed class ReactionPrompt
 {
  readonly CovenantGame g;
  public ReactionPrompt(CovenantGame game)=>g=game;

  public void Draw()
  {
   var offer=g.world.PendingReaction;if(offer==null)return;
   g.ui.Modals.Open(UiModalId.Reaction);
   var blocker=new Rectangle(0,0,g.W,H);g.Fill(blocker,Color.Black*.56f);g.buttons.Add((blocker,()=>{}));
   int width=Math.Min(720,g.ui.Layout.CenterBounds.Width-80),height=360;
   var r=new Rectangle(g.ui.Layout.CenterBounds.Center.X-width/2,g.MapRect.Center.Y-height/2,width,height);
   g.Box(r,new Color(14,18,21)*.99f,Bright);g.Border(new(r.X+5,r.Y+5,r.Width-10,r.Height-10),Gold,2);
   g.Center("REACTION INTERRUPT",new(r.X+20,r.Y+19,r.Width-40,27),Gold,.62f,true);
   g.Center(offer.Title,new(r.X+20,r.Y+57,r.Width-40,34),Bright,.86f,true);
   g.Center($"{offer.ReactorName}  •  Reaction {(g.world.Actors.FirstOrDefault(a=>a.Id==offer.ReactorId)?.Economy.ReactionAvailable==true?"READY":"SPENT")}",new(r.X+20,r.Y+96,r.Width-40,24),Teal,.53f);
   g.Paragraph(offer.Description,r.X+42,r.Y+133,r.Width-84,Muted,.67f,23);

   var use=new Rectangle(r.X+42,r.Bottom-105,250,45);var decline=new Rectangle(r.Right-292,r.Bottom-105,250,45);
   g.Button(use,offer.AcceptLabel+" [ENTER/R]",()=>g.ResolveReaction(true),true,.61f);
   g.Button(decline,offer.DeclineLabel+" [ESC]",()=>g.ResolveReaction(false),false,.61f);

   var policy=new Rectangle(r.X+42,r.Bottom-48,r.Width-84,30);var current=g.ReactionPolicyFor(offer.Kind);
   g.Button(policy,$"FUTURE {ReactionLabel(offer.Kind).ToUpperInvariant()} PROMPTS: {ReactionPolicyText(current)}  (CLICK TO CHANGE)",()=>g.CycleReactionPolicy(offer.Kind),false,.45f);
   if(policy.Contains(g.cursor))g.tooltip="ASK opens this interrupt. AUTO accepts valid reactions immediately. NEVER declines them automatically. The current interrupt still requires your decision.";
   g.ui.Cursors.Set(UiCursorKind.Select);
  }
 }
}
