using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using LogicSimulator;
using LogicSimulator.Views;

namespace UITestsLogicSimulator {
    public static class AvaloniaApp {
        public static void Stop() {
            var app = GetApp();
            if (app is IDisposable disposable) Dispatcher.UIThread.Post(disposable.Dispose);

            Dispatcher.UIThread.Post(() => app.Shutdown());
        }

        public static LauncherWindow GetMainWindow() => (LauncherWindow) GetApp().MainWindow;

        public static IClassicDesktopStyleApplicationLifetime GetApp() {
            var app = Application.Current ?? throw new Exception("Приложение не найдено");
            var life = app.ApplicationLifetime ?? throw new Exception("Приложение не найдено");
            return (IClassicDesktopStyleApplicationLifetime) life;
        }

        public static AppBuilder BuildAvaloniaApp() {
            App.lock_inc_build = true;
            return AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .UseHeadless(); 
        }
    }
}
