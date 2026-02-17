using System.Diagnostics;
using System.IO.Compression;

class Program
{
    static void Main(string[] args)
    {
        string fileName = "RCL.zip"; // Replace with your file name
        string extractPath = Directory.GetCurrentDirectory();
        string zipPath = Path.Combine(extractPath, fileName);
        string tempExtractPath = Path.Combine(Path.GetTempPath(), "RCL_Update_" + Guid.NewGuid().ToString());

        Console.Write("Starting the Upgrade\n");

        try
        {
            // Create temporary extraction directory
            Directory.CreateDirectory(tempExtractPath);

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
                    string tempDestinationPath = Path.Combine(tempExtractPath, entry.FullName);

                    // Create subdirectories if they don't exist
                    string tempDirectory = Path.GetDirectoryName(tempDestinationPath);
                    if (!string.IsNullOrEmpty(tempDirectory) && !Directory.Exists(tempDirectory))
                    {
                        Directory.CreateDirectory(tempDirectory);
                    }

                    // Extract to temporary location
                    if (!entry.FullName.EndsWith("/"))
                    {
                        entry.ExtractToFile(tempDestinationPath, true);
                        extractedSize += entry.Length;
                        Console.WriteLine($"Extracting {entry.FullName} - {extractedSize * 100 / totalSize}% complete");
                    }
                }
            }

            // Move files from temporary location to final location
            MoveFilesFromTemp(tempExtractPath, extractPath);

            // Delete RCL.zip after extraction
            try
            {
                File.Delete(zipPath);
                Console.WriteLine($"Deleted: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete {fileName} - {ex.Message}");
            }

            // Delete update.xml if it exists
            string updateXmlPath = Path.Combine(extractPath, "update.xml");
            if (File.Exists(updateXmlPath))
            {
                try
                {
                    File.Delete(updateXmlPath);
                    Console.WriteLine($"Deleted: update.xml");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not delete update.xml - {ex.Message}");
                }
            }

            // Rename update_new.xml to update.xml
            string updateNewXmlPath = Path.Combine(extractPath, "update_new.xml");
            if (File.Exists(updateNewXmlPath))
            {
                try
                {
                    File.Move(updateNewXmlPath, updateXmlPath, true);
                    Console.WriteLine($"Renamed: update_new.xml to update.xml");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not rename update_new.xml - {ex.Message}");
                }
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("Update Successful!");
            Console.WriteLine("Files have been updated successfully.");
            Console.WriteLine("========================================\n");

            Console.WriteLine("Launching RCL.exe...");
            RunRCL();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during update: {ex.Message}");
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
            }
            catch { }
        }
    }

    private static void MoveFilesFromTemp(string sourceDir, string targetDir)
    {
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(targetDir, fileName);

            try
            {
                File.Copy(file, targetFile, true);
                Console.WriteLine($"Updated: {fileName}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not update {fileName} (file in use) - {ex.Message}");
            }
        }

        // Recursively process subdirectories
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string targetSubDir = Path.Combine(targetDir, dirName);

            if (!Directory.Exists(targetSubDir))
            {
                Directory.CreateDirectory(targetSubDir);
            }

            MoveFilesFromTemp(subDir, targetSubDir);
        }
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
    }
}

