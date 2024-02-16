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
        ConfigurationManager config;
        DirectoryNode rootDirectoryNode;
        private string filielist;
        //Scrolling variables
        private bool isAutoScrolling = false;
        private Point autoScrollStartPoint;
        private ScrollViewer autoScrollTarget;
        private System.Windows.Threading.DispatcherTimer autoScrollTimer;
        private double autoScrollSpeed = 0;
        //
        /// <summary>
        /// Constructor for MainWindow. Sets up event listeners and initializes the UI components.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the search timer with a debounce interval
            SearchTextBox.Visibility = Visibility.Collapsed;
            config = ConfigurationManager.Instance;
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Event handler for the window loaded event. Starts the asynchronous loading of the file list.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFileListAsync();
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
                filielist = await DownloadFileListAsync("https://raw.githubusercontent.com/Nenkai/GBFRDataTools/master/GBFRDataTools/filelist.txt", "filelist.txt");

                //var archive = new DataArchive();
                //archive.Init(fileContent);

                StatusText.Text = "Building directory tree...";
                rootDirectoryNode = await Task.Run(() => BuildDirectoryTree(filielist, new Progress<double>(UpdateProgressBar)));
                UpdateUI(rootDirectoryNode);
                SearchTextBox.Visibility = Visibility.Visible;
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
                // Use CreateItemHeader for a consistent look, including the icon
                Header = CreateItemHeader(node.Name, GetIconPathForNode(node)),
                // Store the node in the Tag property for easy reference later
                Tag = node
            };

            // This setup for lazy loading of children remains unchanged
            if (node.Children.Any())
            {
                treeViewItem.Items.Add(null); // Placeholder for lazy loading
                treeViewItem.Expanded += TreeViewItem_Expanded; // Event handler for expanding the node
            }

            // Always create the context menu and add general options
            var contextMenu = new ContextMenu();

            // Add "Copy Name" and "Copy Full Path" menu items
            AddContextMenuItem(contextMenu, "Copy Name", () => Clipboard.SetText(node.Name));
            AddContextMenuItem(contextMenu, "Copy Full Path", () => Clipboard.SetText(node.FullPath));

            // Conditionally add "Extract" option for non-directory nodes
            if (!node.IsDirectory)
            {
                AddContextMenuItem(contextMenu, "Extract", () => ExtractFile(node));
            }

            // Assign the constructed menu to the TreeViewItem
            treeViewItem.ContextMenu = contextMenu;

            return treeViewItem;
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Find the TreeViewItem that received the right-click
            var treeViewItem = FindAncestorOrSelf<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                e.Handled = true; // Prevent further processing, ensuring context menu applies to the right item
            }
        }

        // Helper method to traverse up the visual tree to find the TreeViewItem
        public static T FindAncestorOrSelf<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T objTyped)
                {
                    return objTyped;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }


        private void AddContextMenuItem(ContextMenu menu, string header, Action action)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += (sender, e) =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy {header.ToLower()} to clipboard: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            menu.Items.Add(menuItem);
        }

        private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as TreeViewItem;
            if (item.Items.Count == 1 && item.Items[0] is string && (string)item.Items[0] == "Loading...")
            {
                item.Items.Clear();
                // Load and add the actual child items to the item here
            }
        }


        private async void ExtractFile(DirectoryNode node)
        {
            string fileToExtract = node.FullPath;
            await FileOperations.ExtractFileAsync(fileToExtract);
        }

        private void FileNode_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem && treeViewItem.Tag is DirectoryNode node && !node.IsDirectory)
            {
                e.Handled = true; // Prevent the event from bubbling up
                                  // Execute your file-specific operation here
            }
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

        #region Mousewheel funcionality
        private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var treeView = sender as UIElement;
            var scrollViewer = FindScrollViewerParent(treeView);

            if (scrollViewer != null)
            {
                if (e.Delta > 0)
                {
                    scrollViewer.LineUp();
                }
                else
                {
                    scrollViewer.LineDown();
                }
                e.Handled = true;
            }
        }

        private ScrollViewer FindScrollViewerParent(DependencyObject child)
        {
            var parentDepObj = VisualTreeHelper.GetParent(child);
            while (parentDepObj != null && !(parentDepObj is ScrollViewer))
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
            }
            return parentDepObj as ScrollViewer;
        }

        private void TreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                Mouse.OverrideCursor = Cursors.Hand;

                autoScrollTarget = FindScrollViewerParent(sender as DependencyObject);
                if (autoScrollTarget != null)
                {
                    isAutoScrolling = true;
                    autoScrollStartPoint = e.GetPosition(autoScrollTarget);
                    (sender as UIElement).CaptureMouse();

                    autoScrollTimer = new System.Windows.Threading.DispatcherTimer();
                    autoScrollTimer.Interval = TimeSpan.FromMilliseconds(1); // Adjust for smoother or faster scrolling
                    autoScrollTimer.Tick += AutoScrollTimer_Tick;
                    autoScrollTimer.Start();

                    e.Handled = true;
                }
            }
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isAutoScrolling)
            {
                var currentPosition = e.GetPosition(autoScrollTarget);
                // Calculate speed based on vertical distance from the start point
                autoScrollSpeed = (currentPosition.Y - autoScrollStartPoint.Y) * 0.1; // Adjust multiplier for sensitivity
            }
        }

        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            if (autoScrollTarget != null && isAutoScrolling)
            {
                autoScrollTarget.ScrollToVerticalOffset(autoScrollTarget.VerticalOffset + autoScrollSpeed);
            }
        }

        private void TreeView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                Mouse.OverrideCursor = null;
                isAutoScrolling = false;
                if (autoScrollTimer != null)
                {
                    autoScrollTimer.Stop();
                    autoScrollTimer.Tick -= AutoScrollTimer_Tick;
                    autoScrollTimer = null;
                }
                (sender as UIElement).ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion


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
            // Initialize the rootNode with an empty path or a specific root path if needed
            var rootNode = new DirectoryNode("data", "");
            int totalLines = lines.Length;
            int updateFrequency = totalLines / 100; // Update progress at most 100 times.

            for (int i = 0; i < totalLines; i++)
            {
                var line = lines[i];
                var parts = line.Split('/');
                var currentNode = rootNode;
                string currentPath = ""; // Keep track of the current path

                for (int j = 0; j < parts.Length; j++)
                {
                    var part = parts[j];
                    // Update the current path
                    currentPath = j == 0 ? part : $"{currentPath}/{part}";

                    var nextNode = currentNode.Children.FirstOrDefault(c => c.Name == part);
                    if (nextNode == null)
                    {
                        // Create the new node with its name and the cumulative path
                        nextNode = new DirectoryNode(part, currentPath);
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



        #region Searchbar

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Trim();

            if (searchText.Length >= 3) // Conduct search with at least 3 characters
            {
                var searchResults = SearchDirectoryTree(rootDirectoryNode, searchText);
                SearchResultsListView.ItemsSource = searchResults;

                // Toggle visibility to show search results and hide the original TreeView
                SearchResultsListView.Visibility = Visibility.Visible;
                DirectoryTreeView.Visibility = Visibility.Collapsed;
            }
            else
            {
                // No search or cleared search, show the original TreeView
                SearchResultsListView.Visibility = Visibility.Collapsed;
                DirectoryTreeView.Visibility = Visibility.Visible;
            }
        }

        private List<DirectoryNode> SearchDirectoryTree(DirectoryNode root, string searchText)
        {
            var results = new List<DirectoryNode>();
            SearchDirectoryNode(root, searchText.ToLower(), results);
            return results;
        }

        private void SearchDirectoryNode(DirectoryNode node, string searchText, List<DirectoryNode> results)
        {
            // Simple case-insensitive search in the node's name
            if (node.Name.ToLower().Contains(searchText))
            {
                results.Add(node);
            }

            foreach (var child in node.Children)
            {
                SearchDirectoryNode(child, searchText, results);
            }
        }
        private void ExtractMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                // Assuming the DataContext of the ListViewItem is a DirectoryNode object
                DirectoryNode node = ((FrameworkElement)menuItem.DataContext).DataContext as DirectoryNode;
                if (node != null)
                {
                    ExtractFile(node);
                }
            }
        }


        private void SearchResultsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsListView.SelectedItem is DirectoryNode selectedNode)
            {
                SelectItemByPath("data/" + selectedNode.FullPath);
                // Collapse the ListView to hide the search results
                SearchResultsListView.Visibility = Visibility.Collapsed;
                // Optionally, make sure the TreeView is visible if it was not
                DirectoryTreeView.Visibility = Visibility.Visible;
            }
        }

        private void SelectItemByPath(string path)
        {
            // Split the path to get individual parts.
            var parts = path.Split('/');
            ItemsControl currentItem = DirectoryTreeView;

            foreach (var part in parts)
            {
                bool found = false;

                // Wait for the item containers to be generated.
                currentItem.ApplyTemplate();
                ItemsPresenter itemsPresenter = (ItemsPresenter)currentItem.Template.FindName("ItemsHost", currentItem);
                itemsPresenter?.ApplyTemplate();
                currentItem.UpdateLayout();

                for (int i = 0; i < currentItem.Items.Count; i++)
                {
                    if (currentItem.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem treeViewItem)
                    {
                        var node = (DirectoryNode)treeViewItem.Tag;
                        if (node.Name.Equals(part, StringComparison.OrdinalIgnoreCase))
                        {
                            // Expand the current node and proceed to search within its children.
                            treeViewItem.IsExpanded = true;
                            currentItem = treeViewItem;
                            found = true;

                            // If it's the last part of the path, select it.
                            if (part == parts.Last())
                            {
                                treeViewItem.IsSelected = true;
                                treeViewItem.BringIntoView();
                            }
                            break;
                        }
                    }
                }

                if (!found) break; // Break the loop if the path part wasn't found.
            }
        }

        #endregion

    }
}
