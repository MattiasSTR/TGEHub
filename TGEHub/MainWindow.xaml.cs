using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TGELoader.Project;

namespace TGELoader
{
    public partial class MainWindow : Window
    {
        private readonly CubicEase myEasing = new CubicEase() { EasingMode = EasingMode.EaseInOut };

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            TempPathProvider.CleanTempDirectory();
        }

        private void OnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender == openProjectButton)
            {
                if (createProjectButton.IsChecked == true)
                {
                    createProjectButton.IsChecked = false;
                    AnimateToOpenProject();
                    openProjectView.IsEnabled = true;
                    createProjectView.IsEnabled = false;
                }

                openProjectButton.IsChecked = true;
            }
            else
            {
                if (openProjectButton.IsChecked == true)
                {
                    openProjectButton.IsChecked = false;
                    AnimateToCreateProject();
                    openProjectView.IsEnabled = false;
                    createProjectView.IsEnabled = true;
                }

                createProjectButton.IsChecked = true;
            }
        }

        private void AnimateToCreateProject()
        {
            var highlightAnimation = new DoubleAnimation(200, 400, new Duration(TimeSpan.FromSeconds(0.1)));
            highlightAnimation.EasingFunction = myEasing;
            highlightAnimation.Completed += (s, e) =>
            {
                var marginAnimation = new ThicknessAnimation(new Thickness(0), new Thickness(-800, 0, 0, 0), new Duration(TimeSpan.FromSeconds(0.5)));
                browserContent.BeginAnimation(MarginProperty, marginAnimation);
            };
            highlightRect.BeginAnimation(Canvas.LeftProperty, highlightAnimation);
        }

        private void AnimateToOpenProject()
        {
            var highlightAnimation = new DoubleAnimation(400, 200, new Duration(TimeSpan.FromSeconds(0.1)));
            highlightAnimation.EasingFunction = myEasing;
            highlightAnimation.Completed += (s, e) =>
            {
                var marginAnimation = new ThicknessAnimation(new Thickness(-800, 0, 0, 0), new Thickness(0), new Duration(TimeSpan.FromSeconds(0.5)));
                browserContent.BeginAnimation(MarginProperty, marginAnimation);
            };
            highlightRect.BeginAnimation(Canvas.LeftProperty, highlightAnimation);
        }
    }
}