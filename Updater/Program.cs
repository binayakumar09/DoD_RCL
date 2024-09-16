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
    }
}

