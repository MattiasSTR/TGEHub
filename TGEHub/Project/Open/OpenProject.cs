using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TGELoader.Project.Open
{
    [DataContract]
    public class ProjectData 
    {
        [DataMember] public required string ProjectName { get; set; }
        [DataMember] public required string ProjectPath { get; set; }
        [DataMember] public DateTime Date {get; set; }

        public string FullPath { get => $"{ProjectPath}{ProjectName}{Project.Extension}"; }
    }

    [DataContract]
    public class ProjectDataList
    {
        [DataMember] public required List<ProjectData> Projects { get; set; }
    }

    class OpenProject
    {
        private static readonly string myApplicationDataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\TGELoader\";
        private static readonly string myProjectDataPath = string.Empty;
        private static readonly ObservableCollection<ProjectData> myProjects = new ObservableCollection<ProjectData>();

        public static ReadOnlyObservableCollection<ProjectData> Projects { get; }

        static OpenProject()
        {
            try
            {
                if (!Directory.Exists(myApplicationDataPath)) 
                { 
                    Directory.CreateDirectory(myApplicationDataPath);
                }

                myProjectDataPath = $@"{myApplicationDataPath}ProjectData.xml";
                Projects = new ReadOnlyObservableCollection<ProjectData>(myProjects);
                ReadProjectData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void ReadProjectData()
        {
            if (File.Exists(myProjectDataPath))
            {
                var projects = Serializer.FromFile<ProjectDataList>(myProjectDataPath).Projects
                                         .OrderByDescending(x => x.Date)
                                         .ToList();
                myProjects.Clear();

                foreach (var project in projects)
                {
                    if (File.Exists(project.FullPath))  // Ensuring the project file exists
                    {
                        myProjects.Add(project);
                    }
                }
            }
        }

        public static void Open(ProjectData aProjectData, bool aOpenSolution, bool aShowMessageBox)
        {
            ReadProjectData();
            var project = myProjects.FirstOrDefault(x => x.FullPath == aProjectData.FullPath);
            if (project != null)
            {
                project.Date = DateTime.Now;
            }
            else
            {
                project = aProjectData;
                project.Date = DateTime.Now;
                myProjects.Add(project);
            }

            WriteProjectData();
            if (aOpenSolution)
            {
                Utility.OpenSolution(project.ProjectPath, aShowMessageBox);
            }
            ReadProjectData();
        }

        private static void WriteProjectData()
        {
            var projects = myProjects.OrderBy(x => x.Date).ToList();
            Serializer.ToFile(new ProjectDataList() { Projects = projects }, myProjectDataPath);
        }

        public static void RemoveProject(ProjectData aProjectToRemove)
        {
            var project = myProjects.FirstOrDefault(p => p.FullPath == aProjectToRemove.FullPath);
            if (project != null)
            {
                myProjects.Remove(project);

                File.Delete(project.FullPath);

                WriteProjectData();
                ReadProjectData();
            }
        }

        public static void AddExistingProject()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false, // Allows selecting a folder
                CheckPathExists = true,
                ValidateNames = false,   // Needed for selecting folders
                FileName = "Select Folder"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string folderPath = string.Empty;
                folderPath = Path.GetDirectoryName(dialog.FileName) ?? folderPath;

                string folderName = string.Empty;
                folderName = Path.GetFileName(folderPath) ?? folderName;

                if (!Path.EndsInDirectorySeparator(folderPath))
                {
                    folderPath += @"\";
                }

                if (File.Exists(folderPath + folderName + Project.Extension))
                {
                    MessageBox.Show("Project already in the list");
                    return;
                }

                string[] foldersToCheck = { "EngineAssets", "Premake", "Source", "Dependencies" };
                string fileToCheck = "generate_game.bat";

                bool isTGEProject = IsPathTGEProject(folderPath, foldersToCheck, fileToCheck);
                if (isTGEProject)
                {
                    

                    var project = new Project(folderName, folderPath);
                    Serializer.ToFile(project, folderPath + $"{folderName}" + Project.Extension);

                    var resultRunPremake = MessageBox.Show(
                    "Do you want to run Premake?",
                    "Run Premake",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                    if (resultRunPremake == MessageBoxResult.Yes)
                    {
                        Utility.RunPremake(folderPath);
                        Open(new ProjectData() { ProjectName = folderName, ProjectPath = folderPath }, true, true);
                    }

                    Open(new ProjectData() { ProjectName = folderName, ProjectPath = folderPath }, false, false);
                }
                else
                {
                    MessageBox.Show("Selected path does not contain a TGE project", "Failed");
                }
            }
        }

        private static bool IsPathTGEProject(string aPath, string[] someRequiredFolders, string aRequiredFile)
        {
            bool foldersExist = true;
            foreach (var folder in someRequiredFolders)
            {
                string folderPath = Path.Combine(aPath, folder);
                if (!Directory.Exists(folderPath))
                {
                    foldersExist = false;
                    break;
                }
            }

            string filePath = Path.Combine(aPath, aRequiredFile);
            bool fileExists = File.Exists(filePath);

            return foldersExist && fileExists;
        }

        public static async Task GenerateTurnIn(ProjectData aProjectToTurnIn)
        {
            var project = myProjects.FirstOrDefault(p => p.FullPath == aProjectToTurnIn.FullPath);
            var sourcePath = project.ProjectPath;

            var saveDialog = new SaveFileDialog
            {
                Filter = "Zip Files (*.zip)|*.zip",
                Title = "Select Destination and File Name"
            };

            string zipFilePath = string.Empty;

            if (saveDialog.ShowDialog() == true)
            {
                zipFilePath = saveDialog.FileName;
            }

            if (string.IsNullOrEmpty(zipFilePath))
            {
                MessageBox.Show("No destination selected. Operation cancelled.");
                return;
            }

            MessageBox.Show("Starting the process.\nPlease do not exit the application until completed");

            try
            {
                string folderName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));

                if (!Path.EndsInDirectorySeparator(folderName))
                {
                    folderName += @"\";
                }

                TempPathProvider.CleanTempDirectory();
                string tempDirectory = TempPathProvider.TempDirectory;
                TempPathProvider.EnsureTempDirectoryExists();

                await Task.Run(() => Utility.CopyDirectory(sourcePath, Path.Combine(tempDirectory, folderName)));

                await Task.Run(() => DeleteFilesForTurnIn(Path.Combine(tempDirectory, folderName)));

                await Task.Run(() => ZipFile.CreateFromDirectory(tempDirectory, zipFilePath));

                TempPathProvider.CleanTempDirectory();

                if (Directory.Exists(Path.GetDirectoryName(zipFilePath)) || File.Exists(Path.GetDirectoryName(zipFilePath)))
                {
                    Process.Start("explorer.exe", Path.GetDirectoryName(zipFilePath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private static void DeleteFilesForTurnIn(string aDirectory)
        {
            try
            {
                string[] foldersToDelete = { ".vs", "Doc", "Lib", "Local", "Temp" };
                string[] filePatternsToDelete = { "*.sln", "*.pdb", "*.idb", $"*{Project.Extension}" };
                string tutorialsFolder = Path.Combine(aDirectory, "Source", "Tutorials");

                foreach (string folderName in foldersToDelete)
                {
                    string folderPath = Path.Combine(aDirectory, folderName);
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                        Console.WriteLine($"Deleted folder: {folderPath}");
                    }
                }

                foreach (string pattern in filePatternsToDelete)
                {
                    foreach (string file in Directory.EnumerateFiles(aDirectory, pattern, SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }

                if (Directory.Exists(tutorialsFolder))
                {
                    Directory.Delete(tutorialsFolder, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while cleaning the directory: {ex.Message}");
            }
        }
    }
}
