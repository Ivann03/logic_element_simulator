using Avalonia.Controls;
using Avalonia;
using LogicSimulator.ViewModels;
using LogicSimulator.Views.Logical_elements;
using System;
using System.Collections.Generic;
using DynamicData;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.LogicalTree;
using System.Linq;
using Entry = LogicSimulator.Views.Logical_elements.Entry;
using Avalonia.Input;

namespace LogicSimulator.Models {
    public class Fuctoin {
        readonly Line marker = new() { Tag = "Marker", ZIndex = 2, IsVisible = false, Stroke = Brushes.Black, StrokeThickness = 3 };
        readonly Rectangle marker2 = new() { Tag = "Marker", Classes = new("anim"), ZIndex = 2, IsVisible = false, Stroke = Brushes.MediumAquamarine, StrokeThickness = 3 };
        
        public Line Marker { get => marker; }
        public Rectangle Marker2 { get => marker2; }

        public readonly Imitator sim = new(); 

        public Canvas canv = new();

        private Func? marked_item;
        private Connected? marked_line;

        private void UpdateMarker() {
            marker2.IsVisible = marked_item != null || marked_line != null;

            if (marked_item != null) {
                var bound = marked_item.GetBounds();
                marker2.Margin = new(bound.X, bound.Y);
                marker2.Width = bound.Width;
                marker2.Height = bound.Height;
                marked_line = null;
            }

            if (marked_line != null) {
                var line = marked_line.line;
                var A = line.StartPoint;
                var B = line.EndPoint;
                marker2.Margin = new(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y));
                marker2.Width = Math.Abs(A.X - B.X);
                marker2.Height = Math.Abs(A.Y - B.Y);
            }
        }

        private int selected_item = 0;
        public int SelectedItem { get => selected_item; set => selected_item = value; }

        private static Func CreateItem(int n) {
            return n switch {
                0 => new AND(),
                1 => new OR(),
                2 => new NOT(),
                3 => new XOR(),
                4 => new Entry(),
                5 => new Exit(),
                6 => new MULTIPLEXER_3(),
                7 => new NAND_2(),
                8 => new FlipFlop(),
                _ => new AND(),
            };
        }

        public Func[] item_types = Enumerable.Range(0, 9).Select(CreateItem).ToArray();

        public Func GenSelectedItem() => CreateItem(selected_item);

        readonly List<Func> items = new();
        private void AddToMap(IControl item) {
            canv.Children.Add(item);
        }

        public void AddItem(Func item) {
            items.Add(item);
            sim.AddItem(item);
            AddToMap(item.GetSelf());
        }
        public void RemoveItem(Func item) {
            if (marked_item != null) {
                marked_item = null;
                UpdateMarker();
            }
            if (marked_line != null && item.ContainsJoin(marked_line)) {
                marked_line = null;
                UpdateMarker();
            }

            items.Remove(item);
            sim.RemoveItem(item);

            item.ClearJoins();
            ((Control) item).Remove();
        }
        public void RemoveAll() {
            foreach (var item in items.ToArray()) RemoveItem(item);
            sim.Clear();
        }

        private void SaveAllPoses() {
            foreach (var item in items) item.SavePose();
        }

        int mode = 0;

        private static int CalcMode(string? tag) {
            if (tag == null) return 0;
            return tag switch {
                "Scene" => 1,
                "Body" => 2,
                "Deleter" => 4,
                "In" => 5,
                "Out" => 6,
                "IO" => 7,
                "Join" => 8,
                "Pin" or _ => 0,
            };
        }
        private void UpdateMode(Control item) => mode = CalcMode((string?) item.Tag);
        
        private static bool IsMode(Control item, string[] mods) {
            var name = (string?) item.Tag;
            if (name == null) return false;
            return mods.IndexOf(name) != -1;
        }

        private static UserControl? GetUC(Control item) {
            while (item.Parent != null) {
                if (item is UserControl @UC) return @UC;
                item = (Control) item.Parent;
            }
            return null;
        }
        private static Func? GetGate(Control item) {
            var UC = GetUC(item);
            if (UC is Func @gate) return @gate;
            return null;
        }

        Point moved_pos;
        Func? moved_item;
        Point item_old_pos;

        Ellipse? marker_circle;
        Locontrol? start_dist;
        int marker_mode;

        Line? old_join;
        bool join_start;
        bool delete_join = false;

        public bool lock_self_connect = true;

        public void Press(Control item, Point pos) {
            UpdateMode(item);

            moved_pos = pos;
            moved_item = GetGate(item);
            tapped = true;
            if (moved_item != null) item_old_pos = moved_item.GetPos();

            switch (mode) {
            case 1:
                SaveAllPoses();
                break;
            case 5 or 6 or 7:
                if (marker_circle == null) break;
                var gate = GetGate(marker_circle) ?? throw new Exception("Неизвестно");
                start_dist = gate.GetPin(marker_circle);

                var circle_pos = start_dist.GetPos();
                marker.StartPoint = marker.EndPoint = circle_pos;
                marker.IsVisible = true;
                marker_mode = mode;
                break;
            case 8:
                if (item is not Line @join) break;
                Connected.arrow_to_join.TryGetValue(@join, out var @join2);
                if (@join2 == null) break;

                if (marked_line == @join2) {
                    marked_line = null;
                    UpdateMarker();
                }

                var dist_a = @join.StartPoint.Hypot(pos);
                var dist_b = @join.EndPoint.Hypot(pos);
                join_start = dist_a > dist_b;
                old_join = @join;

                marker.StartPoint = join_start ? @join.StartPoint : pos;
                marker.EndPoint = join_start ? pos : @join.EndPoint;
                marker_mode = CalcMode(join_start ? @join2.A.tag : @join2.B.tag);

                marker.IsVisible = true;
                @join.IsVisible = false;
                break;
            }

            Move(item, pos);
        }

        public void FixItem(ref Control res, Point pos, IEnumerable<ILogical> items) {
            foreach (var logic in items) {
                var item = (Control) logic;
                var tb = item.TransformedBounds;
                if (tb != null && tb.Value.Bounds.TransformToAABB(tb.Value.Transform).Contains(pos) && (string?) item.Tag != "Join") res = item; // Гениально! Апгрейд прошёл успешно :D
                FixItem(ref res, pos, item.GetLogicalChildren());
            }
        }
        public void Move(Control item, Point pos, bool use_fix = true) {
            
            if (use_fix && (mode == 5 || mode == 6 || mode == 7 || mode == 8)) {
                var tb = canv.TransformedBounds;
                if (tb != null) {
                    item = new Canvas() { Tag = "Scene" };
                    var bounds = tb.Value.Bounds.TransformToAABB(tb.Value.Transform);
                    FixItem(ref item, pos + bounds.TopLeft, canv.Children);
                }
            }

            string[] mods = new[] { "In", "Out", "IO" };
            var tag = (string?) item.Tag;
            if (IsMode(item, mods) && item is Ellipse @ellipse
                && !(marker_mode == 5 && tag == "In" || marker_mode == 6 && tag == "Out" ||
                lock_self_connect && moved_item == GetGate(item))) { 
                if (marker_circle != null && marker_circle != @ellipse) { 
                    marker_circle.Fill = new SolidColorBrush(Color.Parse("#0000"));
                    marker_circle.Stroke = Brushes.Black;
                }
                marker_circle = @ellipse;
                @ellipse.Fill = Brushes.Red;
                @ellipse.Stroke = Brushes.Red;
            } else if (marker_circle != null) {
                marker_circle.Fill = new SolidColorBrush(Color.Parse("#0000"));
                marker_circle.Stroke = Brushes.Black;
                marker_circle = null;
            }

            if (mode == 8) delete_join = (string?) item.Tag == "Deleter";

            var delta = pos - moved_pos;
            if (delta.X == 0 && delta.Y == 0) return;

            if (Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2) > 9) tapped = false;

            switch (mode) {
            case 1:
                foreach (var item_ in items) {
                    var pose = item_.GetPose();
                    item_.Move(pose + delta, true);
                }
                UpdateMarker();
                break;
            case 2:
                if (moved_item == null) break;
                var new_pos = item_old_pos + delta;
                moved_item.Move(new_pos);
                UpdateMarker();
                break;
            case 5 or 6 or 7:
                var end_pos = marker_circle == null ? pos : marker_circle.Center(canv);
                marker.EndPoint = end_pos;
                break;
            case 8:
                if (old_join == null) break;
                var p = marker_circle == null ? pos : marker_circle.Center(canv);
                if (join_start) marker.EndPoint = p;
                else marker.StartPoint = p;
                break;
            }
        }

        public bool tapped = false; 
        public Point tap_pos;

        public int Release(Control item, Point pos, bool use_fix = true) {
            Move(item, pos, use_fix);
           
            switch (mode) {
            case 5 or 6 or 7:
                if (start_dist == null) break;
                if (marker_circle != null) {
                    var gate = GetGate(marker_circle) ?? throw new Exception("Неизвестно");
                    var end_dist = gate.GetPin(marker_circle);
                    var newy = new Connected(start_dist, end_dist);
                    AddToMap(newy.line);
                }
                marker.IsVisible = false;
                marker_mode = 0;
                break;
            case 8:
                if (old_join == null) break;
                Connected.arrow_to_join.TryGetValue(old_join, out var @join);
                if (marker_circle != null && @join != null) {
                    var gate = GetGate(marker_circle) ?? throw new Exception("Неизвестно"); 
                    var p = gate.GetPin(marker_circle);
                    @join.Delete();

                    var newy = join_start ? new Connected(@join.A, p) : new Connected(p, @join.B);
                    AddToMap(newy.line);
                } else old_join.IsVisible = true;

                marker.IsVisible = false;
                marker_mode = 0;
                old_join = null;

                if (delete_join) @join?.Delete();
                delete_join = false;
                break;
            }

            if (tapped) Tapped(item, pos);

            int res_mode = mode;
            mode = 0;
            moved_item = null;
            return res_mode;
        }

        private void Tapped(Control item, Point pos) {
             tap_pos = pos;

            switch (mode) {

            case 2 or 8:
                if (item is Line @line) {
                    if (!Connected.arrow_to_join.TryGetValue(@line, out var @join)) break;
                    marked_item = null;
                    marked_line = @join;
                    UpdateMarker();
                    break;
                }

                if (moved_item == null) break;

                marked_item = moved_item;
                UpdateMarker();
                break;
            }
        }

        public void KeyPressed(Control _, Key key) {
            switch (key) {
            case Key.Up:
            case Key.Left:
            case Key.Right:
            case Key.Down:
                int dx = key == Key.Left ? -1 : key == Key.Right ? 1 : 0;
                int dy = key == Key.Up ? -1 : key == Key.Down ? 1 : 0;
                marked_item?.Move(marked_item.GetPos() + new Point(dx * 10, dy * 10));
                UpdateMarker();
                break;
            case Key.Delete:
                if (marked_item != null) RemoveItem(marked_item);
                if (marked_line != null) {
                    marked_line.Delete();
                    marked_line = null;
                    UpdateMarker();
                }
                break;
            }
        }

        public readonly Treatment filer = new();
        public Diagram? current_scheme;

        public void Export() {
            if (current_scheme == null) return;

            var arr = items.Select(x => x.Export()).ToArray();

            Dictionary<Func, int> item_to_num = new();
            int n = 0;
            foreach (var item in items) item_to_num.Add(item, n++);
            List<object[]> joins = new();
            foreach (var item in items) joins.Add(item.ExportJoins(item_to_num));

            sim.Clean();
            string states = sim.Export();

            try { current_scheme.Update(arr, joins.ToArray(), states); }
            catch (Exception e) { Log.Write("Save error:\n" + e); }
        }

        public void ImportScheme(bool start = true) {
            if (current_scheme == null) return;

            sim.Stop();
            sim.lock_sim = true;

            RemoveAll();

            List<Func> list = new();
            foreach (var item in current_scheme.items) {
                if (item is not Dictionary<string, object> @dict) { Log.Write("Не верный тип элемента: " + item); continue; }

                if (!@dict.TryGetValue("id", out var @value)) { Log.Write("id элемента не обнаружен"); continue; }
                if (@value is not int @id) { Log.Write("Неверный тип id: " + @value); continue; }
                var newy = CreateItem(@id);

                newy.Import(@dict);
                AddItem(newy);
                list.Add(newy);
            }
            var items_arr = list.ToArray();

            List<Connected> joinz = new();
            foreach (var obj in current_scheme.joins) {
                object[] join;
                if (obj is List<object> @j) join = @j.ToArray();
                else if (obj is object[] @j2) join = @j2;
                else { Log.Write("Одно из соединений не того типа: " + obj + " " + Plugin.Obj2json(obj)); continue; }
                if (join.Length != 6 ||
                    join[0] is not int @num_a || join[1] is not int @pin_a || join[2] is not string @tag_a ||
                    join[3] is not int @num_b || join[4] is not int @pin_b || join[5] is not string @tag_b) { Log.Write("Содержимое списка соединения ошибочно"); continue; }

                var newy = new Connected(new(items_arr[@num_a], @pin_a, tag_a), new(items_arr[@num_b], @pin_b, tag_b));
                AddToMap(newy.line);
                joinz.Add(newy);
            }

            foreach (var join in joinz) join.Update();

            sim.Import(current_scheme.states);
            sim.lock_sim = false;
            if (start) sim.Start();
        }
    }
}
