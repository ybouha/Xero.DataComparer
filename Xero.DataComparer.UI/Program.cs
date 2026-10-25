namespace Xero.DataComparer.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Any unexpected exception is logged and reported, the window stays usable.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ErrorLog.Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ErrorLog.Write(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => { ErrorLog.Write(e.Exception); e.SetObserved(); };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
