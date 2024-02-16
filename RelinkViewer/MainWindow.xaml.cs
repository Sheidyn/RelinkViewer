using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;

namespace RelinkViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// This class handles the main window operations, including UI initialization,
    /// downloading a file list, building a directory tree, and updating the UI.
    /// </summary>
    public partial class MainWindow : Window
    {
        private IConfigurationRoot Configuration;
        private string settingsFilePath = "settings.ini";

        /// <summary>
        /// Constructor for MainWindow. Sets up event listeners and initializes the UI components.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Event handler for the window loaded event. Starts the asynchronous loading of the file list.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrCreateSettings();
            await LoadFileListAsync();
        }

        private void LoadOrCreateSettings()
        {
            var configurationBuilder = new ConfigurationBuilder().AddIniFile(settingsFilePath, optional: true, reloadOnChange: true);
            Configuration = configurationBuilder.Build();

            // Attempt to load the game folder path from the configuration
            string gameFolderPath = Configuration["GameFolderPath"];

            // Check if the game folder path is set and if the specific .exe exists
            if (!string.IsNullOrEmpty(gameFolderPath) && File.Exists(Path.Combine(gameFolderPath, "granblue_fantasy_relink.exe")))
            {
                // The .exe exists, proceed with application logic
            }
            else
            {
                // The .exe doesn't exist or the path is not set; prompt the user for a valid game folder
                PromptForGameFolder();
            }
        }

        private void PromptForGameFolder()
        {
            bool validPathSelected = false;
            while (!validPathSelected)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select the game folder containing 'granblue_fantasy_relink.exe':",
                    UseDescriptionForTitle = true, // This property requires .NET Framework 4.6.2 or later
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    if (File.Exists(Path.Combine(dialog.SelectedPath, "granblue_fantasy_relink.exe")))
                    {
                        // Valid path; update the configuration and break the loop
                        Configuration["GameFolderPath"] = dialog.SelectedPath;
                        File.WriteAllText(settingsFilePath, $"GameFolderPath={dialog.SelectedPath}");
                        validPathSelected = true;
                    }
                    else
                    {
                        MessageBox.Show("The selected folder does not contain 'granblue_fantasy_relink.exe'. Please select the correct game folder.", "Invalid Folder Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                        // The loop will continue, prompting the user again
                    }
                }
                else
                {
                    MessageBox.Show("A game folder path containing 'granblue_fantasy_relink.exe' is required to proceed.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                    return; // Exit the method to prevent further execution
                }
            }
        }
        /// <summary>
        /// Asynchronously downloads the file list and builds the directory tree.
        /// Updates the UI to reflect the current operation status and shows the resulting tree.
        /// </summary>
        private async Task LoadFileListAsync()
        {
            try
            {
                StatusText.Text = "Downloading file list...";
                var fileContent = await DownloadFileListAsync("https://raw.githubusercontent.com/Nenkai/GBFRDataTools/master/GBFRDataTools/filelist.txt", "filelist.txt");
                StatusText.Text = "Building directory tree...";
                var rootNode = await Task.Run(() => BuildDirectoryTree(fileContent, new Progress<double>(UpdateProgressBar)));
                UpdateUI(rootNode);
            }
            catch (Exception ex)
            {
                UpdateStatusText($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the main UI with the built directory tree.
        /// </summary>
        /// <param name="rootNode">The root node of the directory tree to display.</param>
        private void UpdateUI(DirectoryNode rootNode)
        {
            var treeViewItem = CreateTreeViewItem(rootNode); // Convert the entire structure to TreeViewItems

            Dispatcher.Invoke(() =>
            {
                DirectoryTreeView.Items.Clear(); // Clear existing items, if any
                DirectoryTreeView.Items.Add(treeViewItem); // Add the fully constructed tree
                DirectoryTreeView.Visibility = Visibility.Visible;
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                StatusText.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Downloads the file list from a specified URL. If the file already exists locally, it uses the local copy.
        /// </summary>
        /// <param name="url">The URL to download the file list from.</param>
        /// <param name="localPath">The local path where the file list is saved.</param>
        /// <returns>A string containing the file list content.</returns>
        private async Task<string> DownloadFileListAsync(string url, string localPath)
        {
            if (!File.Exists(localPath))
            {
                using var client = new HttpClient();
                var content = await client.GetStringAsync(url);
                await File.WriteAllTextAsync(localPath, content);
            }
            return await File.ReadAllTextAsync(localPath);
        }

        /// <summary>
        /// Updates the status text displayed on the UI.
        /// </summary>
        /// <param name="text">The text to display as the status.</param>
        private void UpdateStatusText(string text)
        {
            Dispatcher.Invoke(() => StatusText.Text = text);
        }

        /// <summary>
        /// Recursively creates TreeViewItems from the directory node structure.
        /// </summary>
        /// <param name="node">The current directory node to convert.</param>
        /// <returns>A TreeViewItem representing the directory node.</returns>
        private TreeViewItem CreateTreeViewItem(DirectoryNode node)
        {
            var treeViewItem = new TreeViewItem
            {
                Header = CreateItemHeader(node.Name, GetIconPathForNode(node)), // Assuming CreateItemHeader is defined as before
                Tag = node // Store the node in the Tag property for easy access later
            };

            // Add a dummy node if this node has children to show the expand arrow
            if (node.Children.Any())
            {
                treeViewItem.Items.Add(null); // null indicates unloaded children
                treeViewItem.Expanded += TreeViewItem_Expanded; // Event handler for expanding the node
            }

            return treeViewItem;
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem.Items.Count == 1 && treeViewItem.Items[0] == null)
            {
                // Remove the dummy node
                treeViewItem.Items.Clear();

                // Assuming the DirectoryNode is stored in the Tag property
                var node = treeViewItem.Tag as DirectoryNode;

                // Load and add the child nodes
                foreach (var childNode in node.Children)
                {
                    var childItem = CreateTreeViewItem(childNode);
                    treeViewItem.Items.Add(childItem);
                }
            }
        }

        private string GetIconPathForNode(DirectoryNode node)
        {
            // Assuming that if a name contains a '.', it's a file; otherwise, it's a directory.
            bool isFile = node.Name.Contains('.');
            if (!isFile)
            {
                // It's a directory
                return "Resources/folderIcon.png"; // Path to your folder icon
            }
            else
            {
                // It's a file, determine the icon based on the extension
                string extension = System.IO.Path.GetExtension(node.Name).ToLower();
                switch (extension)
                {
                    case ".txt":
                        return "Resources/textFileIcon.png"; // Path to your text file icon
                                                             // Add more cases as needed for different file types
                    default:
                        return "Resources/fileIcon.png"; // Path to a generic file icon
                }
            }
        }


        private StackPanel CreateItemHeader(string text, string imagePath)
        {
            // Create a StackPanel to hold the icon and text
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            // Create and add the icon
            var image = new Image
            {
                Source = new BitmapImage(new Uri(imagePath, UriKind.Relative)),
                Width = 16, // Set the size as needed
                Height = 16,
                Margin = new Thickness(2)
            };
            stack.Children.Add(image);

            // Create and add the text
            var txtBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(2)
            };
            stack.Children.Add(txtBlock);

            return stack;
        }

        /// <summary>
        /// Builds the directory tree from the file content, reporting progress through the provided IProgress interface.
        /// </summary>
        /// <param name="fileContent">The file content to parse into a directory tree.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The root node of the constructed directory tree.</returns>
        private DirectoryNode BuildDirectoryTree(string fileContent, IProgress<double> progress)
        {
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var rootNode = new DirectoryNode { Name = "data" };
            int totalLines = lines.Length;
            int updateFrequency = totalLines / 100; // Update progress at most 100 times.

            for (int i = 0; i < totalLines; i++)
            {
                var line = lines[i];
                var parts = line.Split('/');
                var currentNode = rootNode;

                foreach (var part in parts)
                {
                    var nextNode = currentNode.Children.FirstOrDefault(c => c.Name == part) ?? new DirectoryNode { Name = part };
                    if (!currentNode.Children.Contains(nextNode))
                    {
                        currentNode.Children.Add(nextNode);
                    }
                    currentNode = nextNode;
                }

                if (i % updateFrequency == 0 || i == totalLines - 1)
                {
                    double progressPercentage = (double)i / totalLines;
                    progress.Report(progressPercentage);
                }
            }
            return rootNode;
        }

        /// <summary>
        /// Updates the progress bar based on the progress of the directory tree building.
        /// </summary>
        /// <param name="progress">The progress percentage to set.</param>
        private void UpdateProgressBar(double progress)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => LoadingProgressBar.Value = progress * 100);
            }
            else
            {
                LoadingProgressBar.Value = progress * 100;
            }
        }
    }
}
