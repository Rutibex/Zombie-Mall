using Microsoft.Xna.Framework;
namespace Ashen.Game;

public sealed partial class CovenantGame
{
 sealed record InformationModel(string Id,string Title,string Category,string Description,IReadOnlyList<string> Facts,string Footer="Right-click or Esc to close",ItemInspection? Item=null);
 sealed record InformationHotspot(Rectangle Bounds,InformationModel Model,string Tooltip);

 sealed class InformationManager
 {
  readonly CovenantGame g;
  readonly List<InformationHotspot> hotspots=[];
  InformationModel? open;
  public InformationManager(CovenantGame game)=>g=game;
  public bool IsOpen=>g.ui.Modals.InformationOpen&&open!=null;
  public InformationModel? OpenModel=>open;

  // Draw builds the next input frame's inspection map. Update intentionally consumes
  // the hotspots from the previous completed frame, exactly like the existing button list.
  public void BeginFrame()=>hotspots.Clear();

  public void Register(Rectangle bounds,string id,string title,string category,string tooltip,string description,params string[] facts)
  {
   var model=new InformationModel(id,title,category,description,facts.Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray());
   hotspots.Add(new(bounds,model,tooltip));
   if(bounds.Contains(g.cursor))g.ui.Tooltips.Offer(id,tooltip);
  }

  public void Register(Rectangle bounds,InformationModel model,string tooltip)
  {
   hotspots.Add(new(bounds,model,tooltip));
   if(bounds.Contains(g.cursor))g.ui.Tooltips.Offer(model.Id,tooltip);
  }

  public bool HandleRightPress(Point p)
  {
   if(g.ui.Modals.IsOpen)return false;
   var target=hotspots.AsEnumerable().Reverse().FirstOrDefault(x=>x.Bounds.Contains(p));
   if(target==null)return false;
   Open(target.Model);return true;
  }

  public void Open(InformationModel model)
  {
   open=model;g.contextOpen=false;g.leftWorldPress=g.boxDragging=false;g.ui.Modals.Open(UiModalId.Information);
  }

  public bool Close()
  {
   if(!g.ui.Modals.InformationOpen)return false;
   open=null;g.ui.Modals.Close();return true;
  }

  public void Draw()
  {
   if(!IsOpen||open==null)return;
   var model=open;
   if(model.Item!=null){g.DrawItemInformation(model);return;}
   g.Fill(g.ui.Layout.CenterBounds,Color.Black*.67f);
   g.buttons.Add((g.ui.Layout.CenterBounds,()=>Close()));

   int width=Math.Min(760,g.ui.Layout.CenterBounds.Width-70),height=Math.Min(585,g.ui.Layout.CenterBounds.Height-70);
   var r=new Rectangle(g.ui.Layout.CenterBounds.Center.X-width/2,g.ui.Layout.CenterBounds.Center.Y-height/2,width,height);
   g.batch.Draw(g.paper,r,Color.White);g.Border(r,Gold,3);g.Border(new(r.X+7,r.Y+7,r.Width-14,r.Height-14),Gold*.45f,1);
   g.Text("RIGHT-CLICK INFORMATION",r.X+30,r.Y+24,new Color(90,66,32),.53f,true);
   g.Text(model.Title,r.X+30,r.Y+67,Ink,.86f,true);
   g.Text(model.Category.ToUpperInvariant(),r.X+31,r.Y+111,new Color(97,66,34),.56f);
   g.Fill(new(r.X+30,r.Y+142,r.Width-60,2),new Color(112,84,48));

   int y=r.Y+164;
   foreach(var line in g.Wrap(model.Description,r.Width-72,.70f))
   {
    g.Text(line,r.X+36,y,Ink,.70f);y+=24;if(y>r.Bottom-158)break;
   }
   if(model.Facts.Count>0&&y<r.Bottom-142)
   {
    y+=12;g.Text("DETAILS",r.X+36,y,new Color(97,66,34),.55f,true);y+=30;
    foreach(var fact in model.Facts)
    {
     foreach(var line in g.Wrap("• "+fact,r.Width-78,.62f))
     {
      if(y>r.Bottom-92)break;g.Text(line,r.X+42,y,Ink,.62f);y+=21;
     }
     y+=3;if(y>r.Bottom-92)break;
    }
   }

   var close=new Rectangle(r.Right-154,r.Bottom-58,118,34);
   g.Button(close,"DONE",()=>Close(),true,.60f);
   g.Text(model.Footer,r.X+34,r.Bottom-50,new Color(104,83,54),.46f);
  }
 }
}
