using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TGELoader.Project.Open
{
    /// <summary>
    /// Interaction logic for OpenView.xaml
    /// </summary>
    public partial class OpenView : UserControl
    {
        public OpenView()
        {
            InitializeComponent();
        }

        private void OnListBoxItem_Mouse_DoubleClick(object sender, MouseEventArgs e)
        {
            OpenProject.Open((ProjectData)projectsListBox.SelectedItem, true, false);
        }

        private void OnAddExisting_Button_Click(Object sender, RoutedEventArgs e)
        {
            OpenProject.AddExistingProject();
        }

        private void OnEllipsis_Button_Click(Object sender, RoutedEventArgs e)
        {
            // We need make sure that the button also selects the correct index
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                ListBoxItem listBoxItem = Utility.FindAncestor<ListBoxItem>(clickedButton);

                if (listBoxItem != null)
                {
                    int index = projectsListBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);

                    projectsListBox.SelectedIndex = index;

                    Button button = (Button)sender;
                    ContextMenu contextMenu = button.ContextMenu;
                    contextMenu.IsOpen = true;
                }
            }
        }

        private void OnShowInExplorer_Button_Click(Object sender, RoutedEventArgs e)
        {
            var selectedProject = (ProjectData)projectsListBox.SelectedItem;
            var path = selectedProject.ProjectPath;

            if (Directory.Exists(path) || File.Exists(path))
            {
                Process.Start("explorer.exe", path);
            }
        }

        private void OnRunPremake_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedProject = (ProjectData)projectsListBox.SelectedItem;
            var path = selectedProject.ProjectPath;
            Utility.RunPremake(path);
        }

        private async void OnGenerateTurnIn_Button_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            mainWindow.IsEnabled = false;

            try
            {
                await OpenProject.GenerateTurnIn((ProjectData)projectsListBox.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                mainWindow.IsEnabled = true;
            }
        }

        //private void OnRename_Button_Click(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show("Comming soon!");

        //    // I need to create a way for the user to input a new name

        //    //var selectedProject = (ProjectData)projectsListBox.SelectedItem;
        //    //var path = selectedProject.ProjectPath;

        //    //try
        //    //{
        //    //    string parentDirectory = Directory.GetParent(path).FullName;
        //    //    string newPath = Path.Combine(parentDirectory, newFolderName);

        //    //    Directory.Move(path, newPath);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Console.WriteLine($"Error renaming folder: {ex.Message}");
        //    //}
        //}

        private void OnRemoveFromList_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                    "Are you sure you want to remove the project?\nThis will not delete it from the disk",
                    "Remove Project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                OpenProject.RemoveProject((ProjectData)projectsListBox.SelectedItem);
            }
        }

        private void OnDeleteProject_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                    "Are you sure you want to delete the project?\nThis will permanently remove it from the disk",
                    "Delete Project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var selectedProject = (ProjectData)projectsListBox.SelectedItem;
                var path = selectedProject.ProjectPath;
                Directory.Delete(path, true);
                OpenProject.ReadProjectData();
            }
        }
    }
}
