using LogicSimulator.Models;
using ReactiveUI;

namespace LogicSimulator.ViewModels {
    public class ViewModelBase: ReactiveObject {
        public readonly static Fuctoin map = new();
        private static Proect? current_proj;
        protected static Proect? CurrentProj {
            get => current_proj;
            set {
                if (value == null) return;
                current_proj = value;
                map.current_scheme = value.GetFirstScheme();
            }
        }

        public static Proect? TopSecretGetProj() => current_proj;
    }
}