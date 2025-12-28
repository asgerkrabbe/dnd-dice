using System;
using System.IO;
using System.Windows;

namespace DiceRoller.Wpf;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DnDDice",
        "app_errors.log");

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        DispatcherUnhandledException += HandleDispatcherException;
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogError("Unhandled Exception", exception);
        
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{exception?.Message}\n\nThe app will now close.\n\nCheck {LogPath} for details.",
            "Fatal Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void HandleDispatcherException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher Exception", e.Exception);
        
        MessageBox.Show(
            $"An error occurred:\n\n{e.Exception.Message}\n\nCheck {LogPath} for details.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        e.Handled = true;
    }

    private static void LogError(string context, Exception? exception)
    {
        try
        {
            var logDir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var message = $"[{timestamp}] {context}\n" +
                          $"Message: {exception?.Message}\n" +
                          $"Type: {exception?.GetType().FullName}\n" +
                          $"StackTrace: {exception?.StackTrace}\n" +
                          $"---\n";

            File.AppendAllText(LogPath, message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log error: {ex.Message}");
        }
    }
}
