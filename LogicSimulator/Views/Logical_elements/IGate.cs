using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using LogicSimulator.Models;
using System.Collections.Generic;

namespace LogicSimulator.Views.Logical_elements {
    public interface IGate {
        public int CountIns { get; }
        public int CountOuts { get; }
        public UserControl GetSelf();

        public Point GetPos();
        public Size GetSize();
        public Size GetBodySize();
        public void Move(Point pos, bool global = false);
        public void SavePose();
        public Point GetPose();
        public Rect GetBounds();

        public Locontrol GetPin(Ellipse finded);
        public Point GetPinPos(int n);

        public void AddJoin(Connected join);
        public void RemoveJoin(Connected join);
        public void ClearJoins();
        public void SetJoinColor(int o_num, bool value);
        public bool ContainsJoin(Connected join);

        public void Brain(ref bool[] ins, ref bool[] outs);
        public int[][] GetPinData();
        public void LogicUpdate(Dictionary<IGate, Meta> ids, Meta me);

        public int TypeId { get; }
        public object Export();
        public List<object[]> ExportJoins(Dictionary<IGate, int> to_num);
        public void Import(Dictionary<string, object> dict);

        public Ellipse SecretGetPin(int n);
    }
}
