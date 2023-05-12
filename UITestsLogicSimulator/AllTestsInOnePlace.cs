using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using LogicSimulator.Models;
using LogicSimulator.ViewModels;
using LogicSimulator.Views;
using LogicSimulator.Views.Shapes;
using System.Text;
using Button = Avalonia.Controls.Button;

namespace UITestsLogicSimulator {
    public class AllTestsInOnePlace {
        private readonly LauncherWindow launcherWindow = AvaloniaApp.GetMainWindow();
        private readonly MainWindow mainWindow = LauncherWindowViewModel.GetMW;
        private readonly Mapper map = ViewModelBase.map;

        private readonly Canvas canv;
        private readonly ListBox gates;



        bool first_log = true;
        readonly string path = "../../../TestLog.txt";
        private void Log(string? message) {
            message ??= "null";
            if (first_log) {
                File.WriteAllText(path, message + "\n");
                first_log = false;
            }  else File.AppendAllText(path, message + "\n");
        }

        public AllTestsInOnePlace() {
            var buttons = launcherWindow.GetVisualDescendants().OfType<Button>();
            var new_proj = buttons.First(x => (string) x.Content == "Создать новый проект");
            new_proj.Command.Execute(null);
            // Только таки имбовая возможность создать проект, но никогда не определять ему файл
            // сохранения, от чего данные unit-test'ы никогда не появлияют на файловую систему :D

            var vis_arr = mainWindow.GetVisualDescendants();
            canv = vis_arr.OfType<Canvas>().First(x => (string?) x.Tag == "Scene");
            // canv.PointerEnter
            // И тут выясняется, что я в принципе не могу имитировать клики по холсту, по этому
            // придётся воздействовать на приложение через Mapper на прямую.

            gates = vis_arr.OfType<ListBox>().First(x => x.Name == "Gates");
            map.sim.Stop(); // чтобы в холостую не работало, я сам задам количество тиков методом Ticks здесь
        }



        private IGate? Click(Control target, double x, double y) {
            var pos = new Point(x, y);
            map.Press(target, pos);
            int mode = map.Release(target, pos);
            // Log("Tapped: " + map.tapped + " | " + mode);
            if (map.tapped && mode == 1) {
                var tpos = map.tap_pos;
                var newy = map.GenSelectedItem();
                newy.Move(tpos);
                map.AddItem(newy);
                return newy;
            }
            return null;
        }
        private void Move(Control a, Control b) {
            map.Move(a, new());
            map.Press(a, new());
            int mode = map.Release(b, new(100, 100), false); // В себе уже имеет map.Move(target, pos2)
            // Log("Moved: " + map.tapped + " | " + mode);
        }
        private string Export() {
            map.Export();
            var scheme = map.current_scheme;
            if (scheme == null) return "Scheme not defined";

            scheme.Created = 123;
            scheme.Modified = 456;
            return Utils.Obj2json(scheme.Export());
        }
        private void SelectGate(int id) => gates.SelectedIndex = id; // Хоть что-то хотя бы возможно сделать чисто через визуальную часть, а не в обход обёрток, нюхающих ивенты ;'-}
        private void Ticks(int count) {
            while (count-- > 0) map.sim.TopSecretPublicTickMethod();
        }
        private static void SaveProject() { // Чтобы только посмотреть, что всё соединилось как надо
            var proj = ViewModelBase.TopSecretGetProj() ?? throw new Exception("А где проект? :/");
            proj.SetDir("../../..");
            proj.FileName = "tested";
            proj.Save();
        }
        private void NewScheme() {
            Export();
            var button = mainWindow.GetVisualDescendants().OfType<Button>().Last(x => x.Name == "NewScheme");
            button.Command.Execute(null);
            var button2 = mainWindow.GetVisualDescendants().OfType<Button>().Last(x => x.Name == "OpenScheme");
            button2.Command.Execute(null);
        }
        private void ImportScheme(string data) {
            object yeah = Utils.Json2obj(data) ?? new Exception("Что-то не то в JSON");
            var proj = ViewModelBase.TopSecretGetProj() ?? throw new Exception("А где проект? :/");
            Scheme clone = new(proj, yeah);
            var scheme = map.current_scheme ?? throw new Exception("А где схема? :/");
            scheme.Update(clone.items, clone.joins, clone.states);
            map.ImportScheme(false);
        }



        private string ComplexSolution() {
            var sim = map.sim;
            sim.ComparativeTestMode = true;

            var inputs = sim.GetSwitches();
            var outputs = sim.GetLightBulbs();
            int L = inputs.Length;
            int steps = 1 << L;

            StringBuilder sb = new();
            for (int step = 0; step < steps; step++) {
                for (int i = 0; i < L; i++) inputs[i].SetState((step & 1 << i) > 0);
                if (step > 0) sb.Append('|');
                int hits = 0;
                Ticks(1);
                while (hits++ < 1024 && sim.SomethingHasChanged) Ticks(1);
                foreach (var output in outputs) sb.Append(output.GetState() ? '1' : '0');
                sb.Append("_t" + hits);
            }
            return sb.ToString();
        }



        [Fact]
        public void GeneralTest() {
            Task.Delay(10).GetAwaiter().GetResult();

            SelectGate(0); // AND-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? gate = Click(canv, 200, 200);
            Assert.NotNull(gate);
            var data = Export();
            Assert.Equal("{\"name\": \"Newy\", \"created\": 123, \"modified\": 456, \"items\": [{\"id\": 0, \"pos\": \"$p$200,200\", \"size\": \"$s$87,86\", \"base_size\": 25}], \"joins\": [], \"states\": \"00\"}", data);

            SelectGate(3); // XOR-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? gate2 = Click(canv, 300, 300);
            Assert.NotNull(gate2);

            Move(gate.SecretGetPin(2), gate2.SecretGetPin(0)); // Соединяем gate и gate2

            data = Export();
            Assert.Equal("{\"name\": \"Newy\", \"created\": 123, \"modified\": 456, \"items\": [{\"id\": 0, \"pos\": \"$p$200,200\", \"size\": \"$s$87,86\", \"base_size\": 25}, {\"id\": 3, \"pos\": \"$p$300,300\", \"size\": \"$s$87,86\", \"base_size\": 25}], \"joins\": [[0, 2, \"Out\", 1, 0, \"In\"]], \"states\": \"000\"}", data);

            SelectGate(5); // Switch-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? button = Click(canv, 100, 150);
            IGate? button2 = Click(canv, 100, 250);
            IGate? button3 = Click(canv, 100, 350);
            Assert.NotNull(button);
            Assert.NotNull(button2);
            Assert.NotNull(button3);

            Move(button.SecretGetPin(0), gate.SecretGetPin(0));
            Move(button2.SecretGetPin(0), gate.SecretGetPin(1));
            Move(button3.SecretGetPin(0), gate2.SecretGetPin(1));

            SelectGate(7); // LightBulb-gate
            Task.Delay(1).GetAwaiter().GetResult();

            IGate? ball = Click(canv, 400, 300);
            Assert.NotNull(ball);

            Move(gate2.SecretGetPin(2), ball.SecretGetPin(0));

            var input = (Switch) button;
            var input2 = (Switch) button2;
            var input3 = (Switch) button3;
            var output = (LightBulb) ball;

            StringBuilder sb = new();
            for (int i = 0; i < 8; i++) {
                input.SetState((i & 4) > 0);
                input2.SetState((i & 2) > 0);
                input3.SetState((i & 1) > 0);
                if (i > 0) sb.Append('|');
                for (int tick = 0; tick < 5; tick++) {
                    Ticks(1);
                    sb.Append(output.GetState() ? '1' : '0');
                }
            }
            Assert.Equal("00000|00111|11000|00111|11000|00111|11011|11000", sb.ToString());

            NewScheme();
            Task.Delay(1).GetAwaiter().GetResult();

            ImportScheme("{\"name\": \"Для тестирования\", \"created\": 1683838621, \"modified\": 1683839324, \"items\": [{\"id\": 5, \"pos\": \"$p$149,242\", \"size\": \"$s$75,75\", \"base_size\": 25, \"state\": false}, {\"id\": 5, \"pos\": \"$p$153,330\", \"size\": \"$s$75,75\", \"base_size\": 25, \"state\": false}, {\"id\": 5, \"pos\": \"$p$152,414\", \"size\": \"$s$75,75\", \"base_size\": 25, \"state\": false}, {\"id\": 5, \"pos\": \"$p$149,497\", \"size\": \"$s$75,75\", \"base_size\": 25, \"state\": false}, {\"id\": 9, \"pos\": \"$p$587,328\", \"size\": \"$s$105,105\", \"base_size\": 25, \"state\": \"0.1.0.1.0.0\"}, {\"id\": 3, \"pos\": \"$p$339,236\", \"size\": \"$s$90,90\", \"base_size\": 25}, {\"id\": 3, \"pos\": \"$p$348,336\", \"size\": \"$s$90,90\", \"base_size\": 25}, {\"id\": 3, \"pos\": \"$p$352,444\", \"size\": \"$s$90,90\", \"base_size\": 25}, {\"id\": 3, \"pos\": \"$p$355,546\", \"size\": \"$s$90,90\", \"base_size\": 25}, {\"id\": 9, \"pos\": \"$p$594,460\", \"size\": \"$s$105,105\", \"base_size\": 25, \"state\": \"0.0.1.0.0.0\"}, {\"id\": 3, \"pos\": \"$p$591,182\", \"size\": \"$s$90,90\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$749,199\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$750,276\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$751,354\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$751,430\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$752,506\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 7, \"pos\": \"$p$755,584\", \"size\": \"$s$75,75\", \"base_size\": 25}, {\"id\": 1, \"pos\": \"$p$592,596\", \"size\": \"$s$90,90\", \"base_size\": 25}], \"joins\": [[0, 0, \"Out\", 5, 0, \"In\"], [1, 0, \"Out\", 6, 0, \"In\"], [2, 0, \"Out\", 7, 0, \"In\"], [3, 0, \"Out\", 8, 0, \"In\"], [4, 3, \"Out\", 6, 1, \"In\"], [11, 0, \"In\", 4, 3, \"Out\"], [4, 4, \"Out\", 10, 0, \"In\"], [12, 0, \"In\", 4, 4, \"Out\"], [13, 0, \"In\", 4, 5, \"Out\"], [4, 5, \"Out\", 17, 0, \"In\"], [4, 0, \"In\", 5, 2, \"Out\"], [6, 2, \"Out\", 4, 1, \"In\"], [6, 2, \"Out\", 9, 2, \"In\"], [7, 2, \"Out\", 4, 2, \"In\"], [7, 2, \"Out\", 9, 1, \"In\"], [8, 2, \"Out\", 9, 0, \"In\"], [9, 3, \"Out\", 10, 1, \"In\"], [14, 0, \"In\", 9, 3, \"Out\"], [15, 0, \"In\", 9, 4, \"Out\"], [9, 4, \"Out\", 17, 1, \"In\"], [16, 0, \"In\", 9, 5, \"Out\"], [9, 5, \"Out\", 7, 1, \"In\"], [10, 2, \"Out\", 5, 1, \"In\"], [17, 2, \"Out\", 8, 1, \"In\"]], \"states\": \"00000000000000000000000\"}");
            var res = ComplexSolution();
            Assert.Equal("110001_t4|001010_t10|111011_t8|011011_t5|100100_t9|011111_t10|101110_t8|001110_t5|010001_t6|110001_t5|001110_t11|111111_t8|000100_t8|100100_t5|011011_t11|101010_t8", res);

            NewScheme();
            Task.Delay(1).GetAwaiter().GetResult();

            ImportScheme("{\"name\": \"Для тестирования #2\", \"created\": 1683843280, \"modified\": 1683850250, \"items\": [{\"id\": 3, \"pos\": \"$p$551,364\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$551,407\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$551,450\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$554,494\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$556,539\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$551,320\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 3, \"pos\": \"$p$551,277\", \"size\": \"$s$45,43\", \"base_size\": 11.662684505243613}, {\"id\": 10, \"pos\": \"$p$438,181\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$437,236\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$438,291\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$437,348\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$436,402\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$435,457\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 10, \"pos\": \"$p$436,512\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652165}, {\"id\": 5, \"pos\": \"$p$77,262\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767926, \"state\": false}, {\"id\": 5, \"pos\": \"$p$77,313\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767926, \"state\": false}, {\"id\": 5, \"pos\": \"$p$79,360\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767926, \"state\": false}, {\"id\": 5, \"pos\": \"$p$79,408\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767926, \"state\": false}, {\"id\": 5, \"pos\": \"$p$441,584\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767926, \"state\": false}, {\"id\": 2, \"pos\": \"$p$124,288\", \"size\": \"$s$20,20\", \"base_size\": 5.984801234229215}, {\"id\": 0, \"pos\": \"$p$163,288\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$164,266\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$163,309\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$165,380\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$165,401\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$164,423\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229203}, {\"id\": 0, \"pos\": \"$p$255,256\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$254,279\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$256,316\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$256,337\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$256,373\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$256,395\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$257,429\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 0, \"pos\": \"$p$257,452\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229196}, {\"id\": 2, \"pos\": \"$p$227,384\", \"size\": \"$s$19,19\", \"base_size\": 5.984801234229196}, {\"id\": 2, \"pos\": \"$p$226,267\", \"size\": \"$s$19,19\", \"base_size\": 5.984801234229196}, {\"id\": 2, \"pos\": \"$p$282,288\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,244\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$328,256\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,269\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,283\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$282,267\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,306\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$326,319\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$326,331\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,345\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$285,327\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$286,348\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$326,366\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,379\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,392\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,405\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,427\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$328,440\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$327,453\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 0, \"pos\": \"$p$326,467\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$285,383\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$284,410\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$285,441\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 2, \"pos\": \"$p$285,464\", \"size\": \"$s$21,20\", \"base_size\": 5.984801234229173}, {\"id\": 1, \"pos\": \"$p$737,222\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$782,271\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$656,224\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$850,384\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$637,361\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$744,517\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$650,520\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$736,271\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767866}, {\"id\": 7, \"pos\": \"$p$691,270\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$691,315\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$782,315\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$782,359\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$736,360\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$691,360\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$690,404\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$782,404\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$782,449\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$736,449\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 7, \"pos\": \"$p$690,448\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$638,382\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 1, \"pos\": \"$p$851,406\", \"size\": \"$s$45,44\", \"base_size\": 12.828952955767829}, {\"id\": 0, \"pos\": \"$p$302,303\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730391}, {\"id\": 0, \"pos\": \"$p$302,242\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730391}, {\"id\": 0, \"pos\": \"$p$280,239\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730391}, {\"id\": 10, \"pos\": \"$p$448,180\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$449,291\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$444,457\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$445,512\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$449,236\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$447,347\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 10, \"pos\": \"$p$445,402\", \"size\": \"$s$23,54\", \"base_size\": 6.583281357652062}, {\"id\": 0, \"pos\": \"$p$302,425\", \"size\": \"$s$14,14\", \"base_size\": 4.496469747730372}, {\"id\": 0, \"pos\": \"$p$301,363\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730363}, {\"id\": 0, \"pos\": \"$p$280,362\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730363}, {\"id\": 2, \"pos\": \"$p$200,347\", \"size\": \"$s$19,18\", \"base_size\": 5.440728394753737}, {\"id\": 0, \"pos\": \"$p$280,301\", \"size\": \"$s$15,15\", \"base_size\": 4.496469747730348}], \"joins\": [[70, 0, \"In\", 0, 2, \"Out\"], [0, 2, \"Out\", 60, 1, \"In\"], [0, 2, \"Out\", 63, 0, \"In\"], [72, 0, \"In\", 1, 2, \"Out\"], [1, 2, \"Out\", 79, 0, \"In\"], [1, 2, \"Out\", 80, 0, \"In\"], [2, 2, \"Out\", 66, 0, \"In\"], [74, 0, \"In\", 2, 2, \"Out\"], [2, 2, \"Out\", 79, 1, \"In\"], [75, 0, \"In\", 3, 2, \"Out\"], [65, 0, \"In\", 3, 2, \"Out\"], [3, 2, \"Out\", 80, 1, \"In\"], [77, 0, \"In\", 4, 2, \"Out\"], [4, 2, \"Out\", 66, 1, \"In\"], [4, 2, \"Out\", 65, 1, \"In\"], [69, 0, \"In\", 5, 2, \"Out\"], [5, 2, \"Out\", 64, 0, \"In\"], [5, 2, \"Out\", 62, 1, \"In\"], [67, 0, \"In\", 6, 2, \"Out\"], [6, 2, \"Out\", 62, 0, \"In\"], [6, 2, \"Out\", 60, 0, \"In\"], [7, 8, \"Out\", 6, 0, \"In\"], [8, 8, \"Out\", 5, 0, \"In\"], [9, 8, \"Out\", 0, 0, \"In\"], [10, 8, \"Out\", 1, 0, \"In\"], [11, 8, \"Out\", 2, 0, \"In\"], [12, 8, \"Out\", 3, 0, \"In\"], [13, 8, \"Out\", 4, 0, \"In\"], [14, 0, \"Out\", 23, 1, \"In\"], [14, 0, \"Out\", 21, 0, \"In\"], [24, 1, \"In\", 15, 0, \"Out\"], [15, 0, \"Out\", 20, 0, \"In\"], [16, 0, \"Out\", 25, 0, \"In\"], [16, 0, \"Out\", 22, 0, \"In\"], [94, 0, \"In\", 16, 0, \"Out\"], [95, 1, \"In\", 16, 0, \"Out\"], [19, 0, \"In\", 17, 0, \"Out\"], [17, 0, \"Out\", 25, 1, \"In\"], [17, 0, \"Out\", 24, 0, \"In\"], [17, 0, \"Out\", 23, 0, \"In\"], [17, 0, \"Out\", 93, 1, \"In\"], [18, 0, \"Out\", 4, 1, \"In\"], [3, 1, \"In\", 18, 0, \"Out\"], [18, 0, \"Out\", 2, 1, \"In\"], [1, 1, \"In\", 18, 0, \"Out\"], [18, 0, \"Out\", 0, 1, \"In\"], [5, 1, \"In\", 18, 0, \"Out\"], [18, 0, \"Out\", 6, 1, \"In\"], [19, 1, \"Out\", 21, 1, \"In\"], [19, 1, \"Out\", 20, 1, \"In\"], [19, 1, \"Out\", 22, 1, \"In\"], [83, 0, \"In\", 19, 1, \"Out\"], [19, 1, \"Out\", 95, 0, \"In\"], [20, 2, \"Out\", 27, 1, \"In\"], [20, 2, \"Out\", 29, 0, \"In\"], [21, 2, \"Out\", 26, 0, \"In\"], [21, 2, \"Out\", 28, 0, \"In\"], [22, 2, \"Out\", 35, 0, \"In\"], [22, 2, \"Out\", 28, 1, \"In\"], [22, 2, \"Out\", 29, 1, \"In\"], [23, 2, \"Out\", 30, 0, \"In\"], [23, 2, \"Out\", 32, 0, \"In\"], [24, 2, \"Out\", 31, 1, \"In\"], [24, 2, \"Out\", 33, 0, \"In\"], [25, 2, \"Out\", 34, 0, \"In\"], [25, 2, \"Out\", 32, 1, \"In\"], [25, 2, \"Out\", 33, 1, \"In\"], [91, 0, \"In\", 25, 2, \"Out\"], [41, 0, \"In\", 26, 2, \"Out\"], [26, 2, \"Out\", 38, 0, \"In\"], [26, 2, \"Out\", 40, 0, \"In\"], [36, 0, \"In\", 27, 2, \"Out\"], [27, 2, \"Out\", 40, 1, \"In\"], [27, 2, \"Out\", 39, 1, \"In\"], [46, 0, \"In\", 28, 2, \"Out\"], [28, 2, \"Out\", 43, 0, \"In\"], [45, 0, \"In\", 28, 2, \"Out\"], [29, 2, \"Out\", 47, 0, \"In\"], [29, 2, \"Out\", 44, 1, \"In\"], [29, 2, \"Out\", 45, 1, \"In\"], [30, 2, \"Out\", 56, 0, \"In\"], [30, 2, \"Out\", 49, 0, \"In\"], [30, 2, \"Out\", 51, 0, \"In\"], [57, 0, \"In\", 31, 2, \"Out\"], [31, 2, \"Out\", 51, 1, \"In\"], [50, 1, \"In\", 31, 2, \"Out\"], [58, 0, \"In\", 32, 2, \"Out\"], [32, 2, \"Out\", 53, 0, \"In\"], [32, 2, \"Out\", 55, 0, \"In\"], [59, 0, \"In\", 33, 2, \"Out\"], [33, 2, \"Out\", 55, 1, \"In\"], [33, 2, \"Out\", 54, 1, \"In\"], [30, 1, \"In\", 34, 1, \"Out\"], [31, 0, \"In\", 34, 1, \"Out\"], [35, 1, \"Out\", 27, 0, \"In\"], [35, 1, \"Out\", 26, 1, \"In\"], [83, 1, \"In\", 35, 1, \"Out\"], [36, 1, \"Out\", 37, 1, \"In\"], [36, 1, \"Out\", 38, 1, \"In\"], [37, 2, \"Out\", 7, 0, \"In\"], [37, 2, \"Out\", 8, 0, \"In\"], [9, 0, \"In\", 37, 2, \"Out\"], [11, 0, \"In\", 37, 2, \"Out\"], [12, 0, \"In\", 37, 2, \"Out\"], [13, 0, \"In\", 37, 2, \"Out\"], [9, 1, \"In\", 38, 2, \"Out\"], [12, 1, \"In\", 38, 2, \"Out\"], [39, 2, \"Out\", 7, 1, \"In\"], [39, 2, \"Out\", 9, 2, \"In\"], [39, 2, \"Out\", 10, 0, \"In\"], [39, 2, \"Out\", 11, 1, \"In\"], [39, 2, \"Out\", 13, 1, \"In\"], [40, 2, \"Out\", 7, 2, \"In\"], [40, 2, \"Out\", 9, 3, \"In\"], [40, 2, \"Out\", 10, 1, \"In\"], [40, 2, \"Out\", 12, 2, \"In\"], [40, 2, \"Out\", 13, 2, \"In\"], [41, 1, \"Out\", 39, 0, \"In\"], [41, 1, \"Out\", 82, 1, \"In\"], [42, 2, \"Out\", 8, 1, \"In\"], [42, 2, \"Out\", 9, 4, \"In\"], [42, 2, \"Out\", 10, 2, \"In\"], [42, 2, \"Out\", 12, 3, \"In\"], [43, 2, \"Out\", 7, 3, \"In\"], [43, 2, \"Out\", 8, 2, \"In\"], [43, 2, \"Out\", 10, 3, \"In\"], [43, 2, \"Out\", 12, 4, \"In\"], [43, 2, \"Out\", 13, 3, \"In\"], [44, 2, \"Out\", 7, 4, \"In\"], [44, 2, \"Out\", 8, 3, \"In\"], [44, 2, \"Out\", 10, 4, \"In\"], [44, 2, \"Out\", 11, 2, \"In\"], [44, 2, \"Out\", 12, 5, \"In\"], [13, 4, \"In\", 44, 2, \"Out\"], [45, 2, \"Out\", 7, 5, \"In\"], [45, 2, \"Out\", 9, 5, \"In\"], [45, 2, \"Out\", 12, 6, \"In\"], [46, 1, \"Out\", 44, 0, \"In\"], [46, 1, \"Out\", 81, 1, \"In\"], [47, 1, \"Out\", 43, 1, \"In\"], [47, 1, \"Out\", 42, 1, \"In\"], [48, 2, \"Out\", 7, 6, \"In\"], [48, 2, \"Out\", 8, 4, \"In\"], [48, 2, \"Out\", 9, 6, \"In\"], [48, 2, \"Out\", 11, 3, \"In\"], [13, 5, \"In\", 48, 2, \"Out\"], [10, 5, \"In\", 48, 2, \"Out\"], [86, 0, \"In\", 48, 2, \"Out\"], [49, 2, \"Out\", 84, 0, \"In\"], [49, 2, \"Out\", 8, 5, \"In\"], [49, 2, \"Out\", 85, 0, \"In\"], [49, 2, \"Out\", 10, 6, \"In\"], [49, 2, \"Out\", 86, 1, \"In\"], [49, 2, \"Out\", 13, 6, \"In\"], [50, 2, \"Out\", 84, 1, \"In\"], [50, 2, \"Out\", 8, 6, \"In\"], [50, 2, \"Out\", 86, 2, \"In\"], [50, 2, \"Out\", 11, 4, \"In\"], [50, 2, \"Out\", 89, 0, \"In\"], [50, 2, \"Out\", 85, 1, \"In\"], [51, 2, \"Out\", 88, 0, \"In\"], [51, 2, \"Out\", 89, 1, \"In\"], [51, 2, \"Out\", 11, 5, \"In\"], [51, 2, \"Out\", 86, 3, \"In\"], [51, 2, \"Out\", 87, 0, \"In\"], [52, 2, \"Out\", 88, 1, \"In\"], [52, 2, \"Out\", 87, 1, \"In\"], [52, 2, \"Out\", 84, 3, \"In\"], [52, 2, \"Out\", 11, 6, \"In\"], [53, 2, \"Out\", 85, 2, \"In\"], [53, 2, \"Out\", 87, 2, \"In\"], [53, 2, \"Out\", 86, 4, \"In\"], [53, 2, \"Out\", 90, 0, \"In\"], [53, 2, \"Out\", 89, 2, \"In\"], [54, 2, \"Out\", 84, 5, \"In\"], [54, 2, \"Out\", 88, 2, \"In\"], [54, 2, \"Out\", 89, 3, \"In\"], [54, 2, \"Out\", 90, 1, \"In\"], [54, 2, \"Out\", 87, 3, \"In\"], [55, 2, \"Out\", 84, 6, \"In\"], [55, 2, \"Out\", 88, 3, \"In\"], [55, 2, \"Out\", 90, 2, \"In\"], [55, 2, \"Out\", 89, 4, \"In\"], [56, 1, \"Out\", 50, 0, \"In\"], [56, 1, \"Out\", 92, 1, \"In\"], [48, 1, \"In\", 57, 1, \"Out\"], [49, 1, \"In\", 57, 1, \"Out\"], [58, 1, \"Out\", 54, 0, \"In\"], [58, 1, \"Out\", 91, 1, \"In\"], [59, 1, \"Out\", 52, 1, \"In\"], [59, 1, \"Out\", 53, 1, \"In\"], [61, 0, \"In\", 60, 2, \"Out\"], [68, 0, \"In\", 62, 2, \"Out\"], [71, 0, \"In\", 63, 2, \"Out\"], [73, 0, \"In\", 64, 2, \"Out\"], [65, 2, \"Out\", 76, 0, \"In\"], [78, 0, \"In\", 66, 2, \"Out\"], [79, 2, \"Out\", 64, 1, \"In\"], [63, 1, \"In\", 80, 2, \"Out\"], [81, 2, \"Out\", 42, 0, \"In\"], [82, 2, \"Out\", 37, 0, \"In\"], [83, 2, \"Out\", 82, 0, \"In\"], [84, 8, \"Out\", 7, 7, \"In\"], [85, 8, \"Out\", 9, 7, \"In\"], [86, 8, \"Out\", 12, 7, \"In\"], [87, 8, \"Out\", 13, 7, \"In\"], [8, 7, \"In\", 88, 8, \"Out\"], [10, 7, \"In\", 89, 8, \"Out\"], [11, 7, \"In\", 90, 8, \"Out\"], [91, 2, \"Out\", 52, 0, \"In\"], [92, 2, \"Out\", 48, 0, \"In\"], [93, 2, \"Out\", 92, 0, \"In\"], [94, 1, \"Out\", 93, 0, \"In\"], [95, 2, \"Out\", 81, 0, \"In\"]], \"states\": \"01011111111011100000100000000000000111100010000110000000011111111111101100000000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"}");
            res = ComplexSolution();
            Assert.Equal("1111110111111_t1|1000110001100_t11|1110111110111_t10|1110111101111_t8|1011111101100_t8|1111011101111_t9|1111011111111_t10|1110110001100_t10|1111111111111_t9|1111111101111_t9|1111111111101_t10|0011011111111_t10|1111000110111_t10|1000111111111_t11|1111011110111_t11|1111011110001_t10|0000011100000_t10|1111011110111_t10|0011010101100_t9|0011000110001_t9|1110000110111_t8|1000110110001_t10|1000110000000_t10|0011011110111_t10|0000000000000_t11|0000000110001_t11|0000000000111_t11|1110110000000_t10|1000111101100_t10|1111000100000_t11|1000110001100_t11|1000110001111_t10", res);

            Log("res: " + res);

            Log("Export: " + Export());
            Log("ОК!");
            SaveProject();
        }
    }
}