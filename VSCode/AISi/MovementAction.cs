//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace TFModFortRiseAiSimple.AISi
//{
//  // --- Description d’un mouvement possible ---
//  class MovementAction
//  {
//    public string Name;
//    public int DeltaX;
//    public int DeltaY;
//    public float Cost;
//    public Func<Point, AISiAgentLevelChase, bool> Condition; // Peut-on le faire ?
//    public Func<Point, AISiAgentLevelChase, Point> Result;   // Résultat du mouvement

//    public MovementAction(string name, int dx, int dy, float cost,
//        Func<Point, AISiAgentLevelChase, bool> cond,
//        Func<Point, AISiAgentLevelChase, Point> res)
//    {
//      Name = name;
//      DeltaX = dx;
//      DeltaY = dy;
//      Cost = cost;
//      Condition = cond;
//      Result = res;
//    }
//  }
//}
