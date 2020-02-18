using System.IO;

namespace Wox.Plugin.SharedCommands
{
    public static class FilesFolders
    {
        public static void Copy(this string sourcePath, string targetPath)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourcePath);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourcePath);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(targetPath, file.Name);
                file.CopyTo(temppath, false);
            }

            // Recursively copy subdirectories by calling itself on each subdirectory until there are no more to copy
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(targetPath, subdir.Name);
                Copy(subdir.FullName, temppath);
            }

        }
    }
}
