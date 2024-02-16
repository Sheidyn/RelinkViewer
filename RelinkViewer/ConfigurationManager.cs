using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text;

public class ConfigurationManager
{
    private static ConfigurationManager _instance;
    public static ConfigurationManager Instance => _instance ?? (_instance = new ConfigurationManager());

    private IConfigurationRoot Configuration;
    private readonly string settingsFilePath = "settings.ini";

    // Properties to store the paths
    public string GameFolderPath { get; private set; }
    public string GBFRDataToolsPath { get; private set; }
    public string OutputFolderPath { get; private set; }

    public ConfigurationManager()
    {
        LoadOrCreateConfiguration();
    }

    private void LoadOrCreateConfiguration()
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddIniFile(settingsFilePath, optional: true, reloadOnChange: true);
        Configuration = builder.Build();

        // Load or prompt for GameFolderPath
        GameFolderPath = Configuration["Paths:GameFolderPath"];
        if (string.IsNullOrEmpty(GameFolderPath) || !Directory.Exists(GameFolderPath))
        {
            PromptForGameFolderPath();
            // Save the path to the configuration after prompting
            Configuration["GameFolderPath"] = GameFolderPath;
            SaveConfiguration();
        }

        // Load or prompt for GBFRDataToolsPath
        GBFRDataToolsPath = Configuration["Paths:GBFRDataToolsPath"];
        if (string.IsNullOrEmpty(GBFRDataToolsPath) || !File.Exists(GBFRDataToolsPath))
        {
            PromptForGBFRDataToolsPath();
            // Save the path to the configuration after prompting
            Configuration["GBFRDataToolsPath"] = GBFRDataToolsPath;
            SaveConfiguration();
        }

        OutputFolderPath = Configuration["Paths:OutputFolderPath"];
        if (string.IsNullOrEmpty(OutputFolderPath))
        {
            // Set to default next to the .exe location with "/output" appended
            OutputFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
            Configuration["OutputFolderPath"] = OutputFolderPath;
            SaveConfiguration();
        }
    }

    private void PromptForGameFolderPath()
    {
        using (var dialog = new CommonOpenFileDialog())
        {
            dialog.IsFolderPicker = true;
            dialog.Title = "Select the Game Folder";

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                // Assuming validation for the existence of 'granblue_fantasy_relink.exe' within the directory
                if (File.Exists(Path.Combine(dialog.FileName, "granblue_fantasy_relink.exe")))
                {
                    GameFolderPath = dialog.FileName; // Update the property
                    Configuration["GameFolderPath"] = dialog.FileName;
                    SaveConfiguration(); // Save changes to settings.ini
                }
                else
                {
                    MessageBox.Show($"The folder must contain 'granblue_fantasy_relink.exe'.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Error);
                    PromptForGameFolderPath(); // Prompt again if validation fails
                }
            }
            else
            {
                MessageBox.Show("A game folder path is required to proceed.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Optionally, allow the user to try again or handle differently
            }
        }
    }

    private void PromptForGBFRDataToolsPath()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe", // Filter to only show executable files
            Title = "Select GBFRDataTools.exe",
        };

        if (openFileDialog.ShowDialog() == true) // Check if the user selected a file
        {
            var selectedPath = openFileDialog.FileName;
            if (Path.GetFileName(selectedPath).Equals("GBFRDataTools.exe", StringComparison.OrdinalIgnoreCase))
            {
                // Valid executable selected; update the configuration
                GBFRDataToolsPath = selectedPath; // Update the property
                Configuration["GBFRDataToolsPath"] = selectedPath;
                SaveConfiguration(); // Save changes to settings.ini
            }
            else
            {
                MessageBox.Show("Please select 'GBFRDataTools.exe'.", "Invalid File Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                PromptForGBFRDataToolsPath(); // Re-prompt if the selected file is not GBFRDataTools.exe
            }
        }
        else
        {
            MessageBox.Show("GBFRDataTools executable path is required to proceed.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return; // Exit the method to prevent further execution
        }
    }

    private void SaveConfiguration()
    {
        // Build the content for the INI file with sections and key-value pairs
        var sb = new StringBuilder();

        sb.AppendLine("[Paths]");
        sb.AppendLine($"GameFolderPath={GameFolderPath}");
        sb.AppendLine($"GBFRDataToolsPath={GBFRDataToolsPath}");
        sb.AppendLine($"OutputFolderPath={OutputFolderPath}");

        // Other sections and settings can be added similarly
        // sb.AppendLine("[OtherSection]");
        // sb.AppendLine("OtherKey=OtherValue");

        // Write the content to the settings.ini file
        File.WriteAllText(settingsFilePath, sb.ToString());
    }

    // Additional methods as needed
}
