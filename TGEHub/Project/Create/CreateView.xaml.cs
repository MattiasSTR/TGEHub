using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TGELoader.Project.Open;

namespace TGELoader.Project.Create;

/// <summary>
///     Interaction logic for CreateView.xaml
/// </summary>
public partial class CreateView : UserControl
{
    public CreateView()
    {
        InitializeComponent();

        TempPathProvider.CleanTempDirectory();
    }

    private async void OnCreate_Button_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as CreateProject;
        if (vm == null)
        {
            MessageBox.Show("Unable to create project. DataContext is not set correctly.");
            return;
        }

        try
        {
            var projectPath = await vm.Create();

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                var solutionFile = Directory
                    .GetFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(solutionFile))
                {
                    vm.ValidatePath();
                    OpenProject.Open(new ProjectData { ProjectName = vm.ProjectName, ProjectPath = projectPath }, true, true);
                }
                else
                {
                    MessageBox.Show("Solution file (.sln) not found in the project directory.");
                }
            }
            else
            {
                MessageBox.Show($"Project creation failed: {vm.Message}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred during project creation: {ex.Message}");
        }
    }

    private void OnAddFiles_Button_Click(object sender, RoutedEventArgs e)
    {
        TempPathProvider.CleanTempDirectory();

        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Header and C++ Files|*.h;*.cpp;*.hpp|All Files|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string[] selectedFiles = openFileDialog.FileNames;
            
            TempPathProvider.EnsureTempDirectoryExists();

            var tempDirectory = TempPathProvider.TempDirectory;

            foreach (var file in selectedFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var destinationFilePath = Path.Combine(tempDirectory, fileName);

                    File.Copy(file, destinationFilePath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            // Update Create Button state
            var createProject = (CreateProject)DataContext;
            createProject.UpdateCreateEnabledAfterFilesAdded();
        }
    }
}