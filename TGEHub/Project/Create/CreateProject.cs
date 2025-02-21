using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace TGELoader.Project.Create;

internal class CreateProject : Base
{
    private RelayCommand browseCommand;

    private string myProjectName = "NewProject";
    private string myProjectPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\TGEProjects";
    private string myUtilityProjectName = "CommonUtilities";

    private string myErrorMessage = string.Empty;
    private string myMessage = string.Empty;

    private bool myDoNotCreateUtilityProject = true;
    private bool myCreateUtilityProject;

    private bool myIsValid;

    private bool myInProgress;

    private static readonly SemaphoreSlim FILELOCK = new(1, 1);

    public CreateProject()
    {
        ValidatePath();
    }

    public string ProjectName
    {
        get => myProjectName;
        set
        {
            if (myProjectName != value)
            {
                myProjectName = value;
                ValidatePath();
                OnPropertyChanged(nameof(ProjectName));
            }
        }
    }

    public string ProjectPath
    {
        get => myProjectPath;
        set
        {
            if (myProjectPath != value)
            {
                myProjectPath = value;
                ValidatePath();
                OnPropertyChanged(nameof(ProjectPath));
            }
        }
    }

    public bool IsValid
    {
        get => myIsValid;
        set
        {
            if (myIsValid != value)
            {
                myIsValid = value;
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(CreateEnabled));
            }
        }
    }

    public bool InProgress
    {
        get => myInProgress;
        set
        {
            if (myInProgress != value)
            {
                myInProgress = value;
                OnPropertyChanged(nameof(InProgress));
                OnPropertyChanged(nameof(CreateEnabled));
            }
        }
    }

    public bool CreateEnabled
    {
        get
        {
            if (CreateUtilityProject)
            {
                var tempPath = TempPathProvider.TempDirectory;
                if (!Directory.Exists(tempPath)) ErrorMessage = "Add .h, .cpp or .hpp files";
                return IsValid && !InProgress && Directory.Exists(tempPath);
            }

            ErrorMessage = string.Empty;
            return IsValid && !InProgress;
        }
    }

    public string Message
    {
        get => myMessage;
        set
        {
            if (myMessage != value)
            {
                myMessage = value;
                OnPropertyChanged(nameof(Message));
            }
        }
    }

    public string ErrorMessage
    {
        get => myErrorMessage;
        set
        {
            if (myErrorMessage != value)
            {
                myErrorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
    }

    public bool DoNotCreateUtilityProject
    {
        get => myDoNotCreateUtilityProject;
        set
        {
            if (myDoNotCreateUtilityProject != value)
            {
                myDoNotCreateUtilityProject = value;
                OnPropertyChanged(nameof(DoNotCreateUtilityProject));
                OnPropertyChanged(nameof(CreateEnabled));
                if (myDoNotCreateUtilityProject) CreateUtilityProject = false;
            }
        }
    }

    public bool CreateUtilityProject
    {
        get => myCreateUtilityProject;
        set
        {
            if (myCreateUtilityProject != value)
            {
                myCreateUtilityProject = value;
                OnPropertyChanged(nameof(CreateUtilityProject));
                OnPropertyChanged(nameof(CreateEnabled));
                if (myCreateUtilityProject) DoNotCreateUtilityProject = false;
            }
        }
    }

    public string UtilityProjectName
    {
        get => myUtilityProjectName;
        set
        {
            myUtilityProjectName = value;
            ValidatePath();
            OnPropertyChanged(nameof(UtilityProjectName));
        }
    }

    public void UpdateCreateEnabledAfterFilesAdded()
    {
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(CreateEnabled));
    }

    // Move from command to just a as datacontext in createview
    public RelayCommand BrowseCommand => browseCommand ??= new RelayCommand(BrowseForPath);

    private void BrowseForPath(object parameter)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            Title = "Select Project Folder",
            FileName = "Select Folder"
        };

        if (dialog.ShowDialog() == true) 
        { 
            ProjectPath = Path.GetDirectoryName(dialog.FileName) ?? ProjectPath; 
        }
    }


    public bool ValidatePath()
    {
        var path = ProjectPath;
        if (!Path.EndsInDirectorySeparator(path)) path += @"\";
        path += $@"{ProjectName}\";

        IsValid = false;
        if (string.IsNullOrWhiteSpace(ProjectName.Trim()))
        {
            ErrorMessage = "Type in a project name";
        }
        else if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
        {
            ErrorMessage = "Invalid character(s) used in project name";
        }
        else if (string.IsNullOrWhiteSpace(ProjectPath.Trim()))
        {
            ErrorMessage = "Select a valid project folder";
        }
        else if (ProjectPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
        {
            ErrorMessage = "Invalid character(s) used in project path";
        }
        else if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
        {
            ErrorMessage = "Selected project path already exists and is not empty";
        }
        else if (UtilityProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
        {
            ErrorMessage = "Invalid character(s) used in solution name";
        }
        else if (string.IsNullOrWhiteSpace(UtilityProjectName.Trim()))
        {
            ErrorMessage = "Type in a solution name";
        }
        else
        {
            ErrorMessage = string.Empty;
            IsValid = true;
        }

        return IsValid;
    }

    public async Task<string> Create()
    {
        InProgress = true;
        ValidatePath();
        if (!IsValid)
        {
            InProgress = false;
            return string.Empty;
        }

        if (!Path.EndsInDirectorySeparator(ProjectPath))
        {
            ProjectPath += @"\";
        }

        var path = $@"{ProjectPath}{ProjectName}\";

        try
        {
            UpdateMessage("Creating Project: Adding Directory");
            if (!Directory.Exists(path)) 
            {
                Directory.CreateDirectory(path); 
            }

            var thisExePath = AppDomain.CurrentDomain.BaseDirectory;
            var localTGEFolder = Path.Combine(thisExePath, @"Files\TGE");

            if (!Directory.Exists(localTGEFolder))
            {
                InProgress = false;
                throw new DirectoryNotFoundException($"Source folder does not exist: {localTGEFolder}");
            }

            // TODO: Consistency with Async & Task.Run

            // Copy TGE
            UpdateMessage("Creating Project: Copying TGE");
            await Task.Run(() => CopyTGE(localTGEFolder, path));

            // Copy and create Bin
            UpdateMessage("Creating Project: Creating Bin");
            await Task.Run(() => SetupProjectEnvironment(path, localTGEFolder));


            if (CreateUtilityProject)
            {
                await Task.Run(() => AddUtilityProjectFiles(path, thisExePath));

                await UpdatePremakeFilesAsync(path);
            }

            // Update workspace name
            UpdateMessage("Creating Project: Changing Workspace Name");
            await ReplaceInFileAsync(
                Path.Combine(path, @"Source\Game\premake5.lua"),
                @"workspace\s+""Game""",
                $@"workspace ""{ProjectName}"""
            );

            // Update engine name
            UpdateMessage("Creating Project: Changing Engine Name");
            await ReplaceInFileAsync(
                Path.Combine(path, @"Source\Game\source\main.cpp"),
                @"winconf.myApplicationName = L""TGE - Amazing Game""",
                $@"winconf.myApplicationName = L""{ProjectName}"""
            );

            UpdateMessage("Creating Project: Running Premake");
            await Task.Run(() => Utility.RunPremake(path));

            var project = new Project(ProjectName, path);
            await Task.Run(() => Serializer.ToFile(project, path + $"{ProjectName}" + Project.Extension));

            UpdateMessage(string.Empty);
            InProgress = false;
            return path;
        }
        catch (Exception ex)
        {
            InProgress = false;
            Debug.WriteLine($"Error in CreateAsync: {ex.Message}");
            UpdateMessage($"Error: {ex.Message}");
            return string.Empty;
        }
    }

    private void CopyTGE(string aSourceDirectory, string aDestinationDirectory)
    {
        var dir = new DirectoryInfo(aSourceDirectory);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {aSourceDirectory}");
        }

        var dirs = dir.GetDirectories();
        var files = dir.GetFiles();

        if (!Directory.Exists(aDestinationDirectory)) 
        { 
            Directory.CreateDirectory(aDestinationDirectory); 
        }

        var totalFiles = files.Length;
        var totalDirs = dirs.Length;
        var filesCopied = 0;
        var dirsCopied = 0;

        UpdateMessage($"Creating Project: Copying TGE {filesCopied + dirsCopied}/{totalFiles + totalDirs}");

        foreach (var file in files)
        {
            var tempPath = Path.Combine(aDestinationDirectory, file.Name);
            file.CopyTo(tempPath, false);
            filesCopied++;
            UpdateMessage(
                $"Creating Project: Copying TGE {filesCopied + dirsCopied}/{totalFiles + totalDirs} - " +
                $"Files: {filesCopied}/{totalFiles}, Directories: {dirsCopied}/{totalDirs}");
        }

        foreach (var subdir in dirs)
        {
            var tempPath = Path.Combine(aDestinationDirectory, subdir.Name);
            CopyTGE(subdir.FullName, tempPath);
            dirsCopied++;
            UpdateMessage(
                $"Creating Project: Copying TGE {filesCopied + dirsCopied}/{totalFiles + totalDirs} - " +
                $"Files: {filesCopied}/{totalFiles}, Directories: {dirsCopied}/{totalDirs}");
        }
    }

    private void SetupProjectEnvironment(string aDestinationPath, string aSourceFolder)
    {
        var binPath = Path.Combine(aDestinationPath, "Bin");
        if (!Directory.Exists(binPath))
        {
            Directory.CreateDirectory(binPath);
        }

        var dllSourcePath = Path.Combine(aSourceFolder, @"Dependencies\dll");
        if (!Directory.Exists(dllSourcePath))
        {
            throw new DirectoryNotFoundException($"DLL source folder does not exist: {dllSourcePath}");
        }

        foreach (var dllFile in Directory.GetFiles(dllSourcePath, "*.dll"))
        {
            var destFile = Path.Combine(binPath, Path.GetFileName(dllFile));
            File.Copy(dllFile, destFile, true);
        }
    }

    private void AddUtilityProjectFiles(string aProjectPath, string aExePath)
    {
        var utilityProjectPath = Path.Combine(aProjectPath, "Source", UtilityProjectName);
        var nestedProjectFolderPath = Path.Combine(utilityProjectPath, UtilityProjectName);
        var sourceFilePath = Path.Combine(aExePath, "Files", "premake5.lua");
        var destinationFilePath = Path.Combine(utilityProjectPath, "premake5.lua");

        try
        {
            // Create folders
            if (!Directory.Exists(utilityProjectPath))
            {
                Directory.CreateDirectory(utilityProjectPath);
            }

            if (!Directory.Exists(nestedProjectFolderPath))
            {
                Directory.CreateDirectory(nestedProjectFolderPath);
            }

            // Copy over premake5.lua
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

            // Add .h, .cpp & .hpp files from temp path to UtilityProjectPath
            var tempDirectory = TempPathProvider.TempDirectory;
            var tempFiles = Directory.GetFiles(tempDirectory);
            foreach (var file in tempFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    destinationFilePath = Path.Combine(nestedProjectFolderPath, fileName);

                    File.Copy(file, destinationFilePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            TempPathProvider.CleanTempDirectory();
        } 
        catch(Exception ex) 
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public async Task UpdatePremakeFilesAsync(string aPathToProject)
    {
        // Rename .Lua in utility project
        await ReplaceInFileAsync(
            Path.Combine(aPathToProject, $@"Source\{UtilityProjectName}\premake5.lua"),
            @"CommonUtilities",
            $@"{UtilityProjectName}");

        // Add Common.lua
        await AddLineAfterPatternAsync(
            Path.Combine(aPathToProject, @"Premake\Common.lua"),
            @"dirs\[""engine_assets""]\s*=\s*os.realpath\(dirs\.root \.\. ""EngineAssets/""\)",
            $@"dirs[""utilities""]  = os.realpath(dirs.root .. ""Source/{UtilityProjectName}"")");

        // Add to premake5
        await AddLineAfterPatternAsync(
            Path.Combine(aPathToProject, @"Source\Game\premake5.lua"),
            @"include \(dirs.engine\)",
            @"include (dirs.utilities)");

        // Change premake5 in Game
        await ReplaceInFileAsync(
            Path.Combine(aPathToProject, @"Source\Game\premake5.lua"),
            @"links {""External"", ""Engine""}",
            $@"links {{""External"", ""Engine"", ""{UtilityProjectName}""}}");

        // Change premake5 in Game
        await ReplaceInFileAsync(
            Path.Combine(aPathToProject, @"Source\Game\premake5.lua"),
            @"includedirs { dirs.external, dirs.engine }",
            @"includedirs { dirs.external, dirs.engine, dirs.utilities }");
    }

    private async Task ReplaceInFileAsync(string aFilePath, string aPattern, string aReplacement)
    {
        await FILELOCK.WaitAsync();

        try
        {
            if (!File.Exists(aFilePath)) 
            { 
                throw new FileNotFoundException($"The file was not found: {aFilePath}"); 
            }

            var content = await File.ReadAllTextAsync(aFilePath);
            content = Regex.Replace(content, aPattern, aReplacement);
            await File.WriteAllTextAsync(aFilePath, content);
        }

        finally
        {
            FILELOCK.Release();
        }
    }

    private async Task AddLineAfterPatternAsync(string aFilePath, string aPattern, string aNewLine)
    {
        await FILELOCK.WaitAsync();
        try
        {
            if (!File.Exists(aFilePath)) 
            { 
                throw new FileNotFoundException($"The file was not found: {aFilePath}"); 
            }

            var lines = (await File.ReadAllLinesAsync(aFilePath)).ToList();

            for (var i = 0; i < lines.Count; i++)
            {
                if (Regex.IsMatch(lines[i], aPattern))
                {
                    lines.Insert(i + 1, aNewLine);
                    break;
                }
            }

            await File.WriteAllLinesAsync(aFilePath, lines);
        }
        finally
        {
            FILELOCK.Release();
        }
    }

    private void UpdateMessage(string aMessage)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        { 
            Message = aMessage; 
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() => Message = aMessage);
        }
    }
}