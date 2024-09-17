using System.Diagnostics;
using System.IO.Compression;

class Program
{
    static void Main(string[] args)
    {
        string fileName = "RCL.zip"; // Replace with your file name
        string extractPath = Directory.GetCurrentDirectory();
        string zipPath = Path.Combine(extractPath, fileName);
        Console.Write("Starting the Upgrade\n");
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
        {
            long totalSize = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                totalSize += entry.Length;
            }

            long extractedSize = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                {
                    entry.ExtractToFile(destinationPath, true);
                    extractedSize += entry.Length;
                    Console.WriteLine($"Extracting {entry.FullName} - {extractedSize * 100 / totalSize}% complete");
                }
            }
        }
        Console.WriteLine("Update complete!");
        RunRCL();
    }

    private static void RunRCL()
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        // Specify the executable name
        string executableName = "RCL.exe";

        // Combine the directory and executable name to get the full path
        string executablePath = Path.Combine(currentDirectory, executableName);

        // Create a new process start info
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = currentDirectory
        };
        Process process = new Process
        {
            StartInfo = startInfo
        };
        process.Start();
        File.Delete(Path.Combine(currentDirectory, "update_new.xml"));
        File.Delete(Path.Combine(currentDirectory, "RCL.zip"));
    }
}

