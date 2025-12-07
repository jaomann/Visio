using System.Diagnostics;

namespace Visio
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Debug.WriteLine($"[ERRO NÃO TRATADO] {exception?.Message}");
            Debug.WriteLine($"[STACK TRACE] {exception?.StackTrace}");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Debug.WriteLine($"[ERRO ASYNC NÃO TRATADO] {e.Exception?.Message}");
            Debug.WriteLine($"[STACK TRACE] {e.Exception?.StackTrace}");
            e.SetObserved();
        }
    }
}
