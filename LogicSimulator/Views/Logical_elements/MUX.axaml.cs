using Avalonia.Controls;
using System.ComponentModel;

namespace LogicSimulator.Views.Shapes {
    public partial class MUX: Bord, Func, INotifyPropertyChanged {
        public override int TypeId => 4;

        public override UserControl GetSelf() => this;
        protected override Func GetSelfI => this;
        protected override int[][] Sides => new int[][] {
            System.Array.Empty<int>(),
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 1 },
            new int[] { 0, 0, 0 }
        };

        protected override void Init() => InitializeComponent();

       
        public void Brain(ref bool[] ins, ref bool[] outs) {
            int num = (ins[8] ? 4 : 0) | (ins[9] ? 2 : 0) | (ins[10] ? 1 : 0);
            outs[0] = ins[num];
        }
    }
}
