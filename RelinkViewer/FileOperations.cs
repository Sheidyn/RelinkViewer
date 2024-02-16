using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

public static class FileOperations
{
    public static async Task ExtractFileAsync(string fileToExtract)
    {
        var gbfrDataToolsPath = ConfigurationManager.Instance.GBFRDataToolsPath;
        var gameFolderPath = ConfigurationManager.Instance.GameFolderPath;
        var outputFolderPath = ConfigurationManager.Instance.OutputFolderPath;

        // Ensure the output directory exists
        EnsureDirectoryExists(outputFolderPath);

        // Construct the command arguments
        string arguments = $"extract -f \"{fileToExtract}\" -i \"{Path.Combine(gameFolderPath, "data.i")}\" -o \"{outputFolderPath}\"";

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = gbfrDataToolsPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(gbfrDataToolsPath); // Set working directory if needed

                process.Start();

                // Asynchronously read the output
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                // Process results
                if (process.ExitCode == 0)
                {
                    MessageBox.Show($"File extracted successfully to {outputFolderPath}", "Extraction Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to extract file: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Exception encountered: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

}
