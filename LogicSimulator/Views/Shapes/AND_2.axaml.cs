using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using LogicSimulator.Models;
using LogicSimulator.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LogicSimulator.Views.Shapes {
    public partial class AND_2: UserControl, IGate, INotifyPropertyChanged {
        readonly Ellipse[] pins;
        public AND_2() {
            InitializeComponent();
            DataContext = this;

            List<Ellipse> list = new();
            foreach (var logic in this.LogicalChildren[0].LogicalChildren)
                if (logic is Ellipse @ellipse) list.Add(@ellipse);
            if (list.Count != 3) throw new Exception("Чё?!"); // У этой фигуры всегда 3 пина
            pins = list.ToArray();

            joins = new JoinedItems?[pins.Length];
        }

        public UserControl GetSelf() => this;
        public Point GetPos() => new(Margin.Left, Margin.Top);
        public Size GetSize() => new(Width, Height);
        public Size GetBodySize() => new(width, height);

        public void Move(Point pos) {
            Margin = new(pos.X, pos.Y, 0, 0);
            UpdateJoins(false);
        }

        public void Resize(Size size, bool global) {
            double limit = (9 + 32) * 2;
            width = size.Width.Max(limit);
            height = size.Height.Max(limit);
            RecalcSizes();
            UpdateJoins(global);
        }

        /*
         * Обработка размеров внутренностей
         */

        private readonly double base_size = 25;
        private double width = 30 * 3; // Размеры тела, а не всего UserControl
        private double height = 30 * 3;

        public double BaseSize => base_size;
        public double BaseFraction => base_size / 40;
        public double EllipseSize => BaseFraction * 30;

        public Thickness BodyStrokeSize => new(BaseFraction * 3);
        public double EllipseStrokeSize => BaseFraction * 5;
        public double PinStrokeSize => BaseFraction * 6;

        public Thickness BodyMargin => new(base_size, 0, 0, 0);
        public double BodyWidth => width;
        public double BodyHeight => height;
        public CornerRadius BodyRadius => new(width.Min(height) / 3 + BodyStrokeSize.Top);

        public Point[][] PinPoints { get {
            double X = EllipseSize - EllipseStrokeSize / 2;
            double X2 = base_size + width - EllipseStrokeSize / 2;
            double R = BodyRadius.TopLeft;
            double Y_s = R, Y_m = height / 2, Y_e = height - Y_s;
            double min = EllipseSize + BaseFraction * 2;
            // .1..2.
            double Y = Y_s + (Y_e - Y_s) / 4;
            double Y2 = Y_s + (Y_e - Y_s) / 4 * 3;
            if (Y2 - Y < min) { Y = Y_m - min / 2; Y2 = Y_m + min / 2; }
            double PinWidth = base_size - EllipseSize + PinStrokeSize;
            return new Point[][] {
                new Point[] { new(X, Y), new(X + PinWidth, Y) }, // Первый вход
                new Point[] { new(X, Y2), new(X + PinWidth, Y2) }, // Второй вход
                new Point[] { new(X2, Y_m), new(X2 + PinWidth, Y_m) }, // Единственный выход
            };
        } }

        public Thickness[] EllipseMargins { get {
            Point[][] pins = PinPoints;
            double R2 = EllipseSize / 2;
            double X = UC_Width - EllipseSize;
            double Y = pins[0][0].Y - R2;
            double Y2 = pins[1][0].Y - R2;
            double Y3 = pins[2][0].Y - R2;
            return new Thickness[] {
                new(0, Y, 0, 0), // Первый вход
                new(0, Y2, 0, 0), // Второй вход
                new(X, Y3, 0, 0), // Единственный выход
            };
        } }

        public double UC_Width => base_size * 2 + width;
        public double UC_Height => height;

        public double FontSizze => BodyRadius.TopLeft / 1.3;

        public Thickness[] ImageMargins { get {
            double R = BodyRadius.BottomLeft;
            double num = R - R / Math.Sqrt(2);
            return new Thickness[] {
                new(0, 0, num, num), // Картинка с удалителем
                new(num, 0, 0, num), // Картинка с переместителем
            };
        } }

#pragma warning disable CS0108
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0108

        void RecalcSizes() {
            // Log.Write("Size: " + width + " " + height);
            PropertyChanged?.Invoke(this, new(nameof(EllipseSize)));
            PropertyChanged?.Invoke(this, new(nameof(BodyStrokeSize)));
            PropertyChanged?.Invoke(this, new(nameof(EllipseStrokeSize)));
            PropertyChanged?.Invoke(this, new(nameof(PinStrokeSize)));
            PropertyChanged?.Invoke(this, new(nameof(BodyMargin)));
            PropertyChanged?.Invoke(this, new(nameof(BodyWidth)));
            PropertyChanged?.Invoke(this, new(nameof(BodyHeight)));
            PropertyChanged?.Invoke(this, new(nameof(BodyRadius)));
            PropertyChanged?.Invoke(this, new(nameof(EllipseMargins)));
            PropertyChanged?.Invoke(this, new(nameof(PinPoints)));
            PropertyChanged?.Invoke(this, new(nameof(UC_Width)));
            PropertyChanged?.Invoke(this, new(nameof(UC_Height)));
            PropertyChanged?.Invoke(this, new(nameof(FontSizze)));
            PropertyChanged?.Invoke(this, new(nameof(ImageMargins)));
        }

        /*
         * Обработка пинов
         */

        public Distantor GetPin(Ellipse finded, Visual? ref_point) {
            int n = 0;
            foreach (var pin in pins) {
                if (pin == finded) return new(this, n, ref_point, (string?) finded.Tag ?? "");
                n++;
            }
            throw new Exception("Так не бывает");
        }

        public Point GetPinPos(int n, Visual? ref_point) {
            var pin = pins[n];
            return pin.Center(ref_point); // Смотрите Utils ;'-} Там круто сделан метод
        }

        /*
         * Обработка соединений
         */

        // readonly List<JoinedItems> joins = new();
        readonly JoinedItems?[] joins;

        public void AddJoin(JoinedItems join) {
            if (join.A.parent == this) {
                int n = join.A.num;
                joins[n]?.Delete();
                joins[n] = join;
            }
            if (join.B.parent == this) {
                int n = join.B.num;
                joins[n]?.Delete();
                joins[n] = join;
            }
        }

        public void RemoveJoin(JoinedItems join) {
            if (join.A.parent == this) joins[join.A.num] = null;
            if (join.B.parent == this) joins[join.B.num] = null;
        }

        private void UpdateJoins(bool global) {
            foreach (var join in joins)
                if (join != null && (!global || join.A.parent == this)) join.Update();
        }

        public void ClearJoins() {
            foreach (var join in joins) join?.Delete();
        }
    }
}
