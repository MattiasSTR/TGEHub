using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using TGELoader.Project.Open;
using System.Windows.Media;

namespace TGELoader.Project
{
    public static class Utility
    {
        public static void RunPremake(string aWorkingDirectory)
        {
            var premakeExePath = Path.Combine(aWorkingDirectory, @"Premake\premake5.exe");

            if (!File.Exists(premakeExePath))
            {
                throw new FileNotFoundException($"Premake executable not found: {premakeExePath}");
            }

            var premakeArgs = "--file=Source/Game/premake5.lua vs2022";

            using var premakeProcess = new Process();
            premakeProcess.StartInfo.FileName = premakeExePath;
            premakeProcess.StartInfo.Arguments = premakeArgs;
            premakeProcess.StartInfo.WorkingDirectory = aWorkingDirectory;
            premakeProcess.StartInfo.UseShellExecute = false;
            premakeProcess.StartInfo.RedirectStandardOutput = true;
            premakeProcess.StartInfo.RedirectStandardError = true;
            premakeProcess.Start();

            var output = premakeProcess.StandardOutput.ReadToEnd();
            var error = premakeProcess.StandardError.ReadToEnd();
            premakeProcess.WaitForExit();

            if (premakeProcess.ExitCode != 0)
            {
                throw new Exception($"Premake execution failed: {error}");
            }

            Debug.Write(output);
        }

        public static void OpenSolution(string aPath, bool aShowMessageBox)
        {
            var solutionFile = Directory
        .GetFiles(aPath, "*.sln", SearchOption.TopDirectoryOnly)
        .FirstOrDefault();

            if (!string.IsNullOrEmpty(solutionFile))
            {
                if (aShowMessageBox)
                {
                    var result = MessageBox.Show(
                    "Do you want to open the new project?",
                    "Open Project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = solutionFile,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = solutionFile,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                var result = MessageBox.Show(
                    "No solution found. Do you want to run premake?",
                    "No Solution",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    RunPremake(aPath);
                    OpenSolution(aPath, aShowMessageBox);
                }

            }
        }

        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        public static void CopyDirectory(string aSourceDirectory, string aDestinationDirectory)
        {
            foreach (var dir in Directory.GetDirectories(aSourceDirectory, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(aSourceDirectory, aDestinationDirectory));
            }

            foreach (var file in Directory.GetFiles(aSourceDirectory, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(aSourceDirectory, aDestinationDirectory), true);
            }
        }

    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)

        {
            return _canExecute == null || _canExecute(parameter);
        }


        public void Execute(object parameter)

        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    public class Base : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string aPropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(aPropertyName));
        }
    }

    public static class Serializer
    {
        public static void ToFile<T>(T aInstance, string aPath)
        {
            try
            {
                using var fs = new FileStream(aPath, FileMode.Create);
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(fs, aInstance);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        internal static T FromFile<T>(string aPath)
        {
            try
            {
                using var fs = new FileStream(aPath, FileMode.Open);
                var serializer = new DataContractSerializer(typeof (T));
                T instance = (T)serializer.ReadObject(fs);
                return instance;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return default(T);
            }
        }
    }

    public static class TempPathProvider
    {
        private static readonly string myTempDirectory = Path.Combine(Path.GetTempPath(), "TGEHub_Temp");

        public static string TempDirectory => myTempDirectory;

        public static void EnsureTempDirectoryExists()
        {
            if (!Directory.Exists(myTempDirectory))
            {
                Directory.CreateDirectory(myTempDirectory);
            }
        }

        public static void CleanTempDirectory()
        {
            if (Directory.Exists(myTempDirectory))
            {
                foreach (var file in Directory.GetFiles(myTempDirectory, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(myTempDirectory, true);
            }
        }
    }
}
