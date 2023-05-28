using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;

namespace LogicSimulator.Models {
    public class Connected {
        public static readonly Dictionary<Line, Connected> arrow_to_join = new();

        public Connected(Locontrol a, Locontrol b) {
            A = a; B = b; Update();
            a.parent.AddJoin(this);
            if (a.parent != b.parent) b.parent.AddJoin(this);
            arrow_to_join[line] = this;
        }
        public Locontrol A { get; set; }
        public Locontrol B { get; set; }
        public Line line = new() { Tag = "Join", ZIndex = 2, Stroke = Brushes.Black, StrokeThickness = 3 };

        public void Update() {
            line.StartPoint = A.GetPos();
            line.EndPoint = B.GetPos();
        }
        public void Delete() {
            arrow_to_join.Remove(line);
            line.Remove();
            A.parent.RemoveJoin(this);
            B.parent.RemoveJoin(this);
        }
    }
}
