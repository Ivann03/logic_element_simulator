using Avalonia;
using LogicSimulator.Views.Logical_elements;

namespace LogicSimulator.Models {
    public class Locontrol {
        public readonly int num;
        public Func parent;
        public readonly string tag;

        public Locontrol(Func parent, int n, string tag) {
            this.parent = parent;
            num = n; 
            this.tag = tag;
        }

        public Point GetPos() => parent.GetPinPos(num);
    }
}
