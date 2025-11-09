//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TowerFall;
//using Monocle;
//using Microsoft.Xna.Framework;

//namespace TFModFortRiseAiSimple
//{
//  public class DebugPathRenderer : Entity
//  {
//    public override void Render()
//    {
//      base.Render();

//      if (AISi.agents[1].debugPath == null || AISi.agents[1].debugPath.Count == 0)
//        return;

//      foreach (var cell in AISi.agents[1].debugPath)
//      {
//        float x = cell.X * AISiAgentLevelChase.BLOCK_SIZE;
//        float y = cell.Y * AISiAgentLevelChase.BLOCK_SIZE;

//        Color color = Color.Yellow;
//        //if (cell.Equals(AISi.agents[1].debugPath[0]))
//        if (cell.Equals(AISi.agents[1].debugPath.First()))
//          color = Color.Green;
//        if (cell.Equals(AISi.agents[1].debugPath.Last()))
//          //else if (cell.Equals(AISi.agents[1].debugPath[1]))
//          color = Color.Red;

//        Draw.Rect(x, y, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, color);
//      }

//      //Player player = AISi.agents[1].player;
//      //if (player != null)
//      //{
//      //  Point p = AISi.agents[1].WorldToCell(player.Position);
//      //  Draw.Rect(p.X * AISiAgentLevelChase.BLOCK_SIZE, p.Y * AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, Color.Blue * 0.5f);
//      //}

//      //Player enemy = AISi.agents[1].enemy;
//      //if (enemy != null)
//      //{
//      //  Point p = AISi.agents[1].WorldToCell(enemy.Position);
//      //  Draw.Rect(p.X * AISiAgentLevelChase.BLOCK_SIZE, p.Y * AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, AISiAgentLevelChase.BLOCK_SIZE, Color.Green * 0.5f);
//      //}
//    }
//  }
//}
