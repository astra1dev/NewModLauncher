using Avalonia;
using System;
using System.IO;

namespace NewModLauncher
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Logger.Info("Launcher Starting...");
            
            var resourceNames = typeof(Program).Assembly.GetManifestResourceNames();
            Logger.Log("INFO", "Embedded Resources: " + string.Join(", ", resourceNames));

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                LogException(ex, "Exception in Main startup block");
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();


        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                string terminating = e.IsTerminating ? "[FATAL]" : "[RECOVERABLE]";
                string senderType = sender?.GetType().FullName ?? "null";
                string moreInfo = $"Sender: {senderType}\n";
                moreInfo += $"AppDomain: {AppDomain.CurrentDomain.FriendlyName}\n";
                moreInfo += $"OS: {Environment.OSVersion}\n";
                moreInfo += $"CLR: {Environment.Version}\n";
                moreInfo += $"IsTerminating: {e.IsTerminating}\n";
                moreInfo += $"Time: {DateTime.Now}\n";

                if (e.ExceptionObject is Exception ex)
                {
                    LogException(ex, "UnhandledException: " + terminating + "\n" + moreInfo);
                }
                else
                {
                    Logger.Log("ERROR", $"Unhandled non-Exception object: {e.ExceptionObject}\n" + moreInfo);
                    try
                    {
                        Console.WriteLine($"[CRITICAL] Unhandled non-Exception: {e.ExceptionObject}\n{moreInfo}");
                    }
                    catch { }
                }
            }
            catch (Exception handlerEx)
            {
                try
                {
                    File.AppendAllText("crash.log", $"[CRASH in UnhandledExceptionHandler] {handlerEx}\n");
                }
                catch { }
            }
        }
        private static void LogException(Exception ex, string context = null)
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                string logContent =
                    $"[{DateTime.Now}] {context ?? "Unhandled Exception"}:\n" +
                    $"Type: {ex.GetType().FullName}\n" +
                    $"Message: {ex.Message}\n" +
                    $"StackTrace: {ex.StackTrace}\n\n";
                File.AppendAllText(logPath, logContent);
                Logger.Log("ERROR", logContent);
            }
            catch { }
        }
    }
}
