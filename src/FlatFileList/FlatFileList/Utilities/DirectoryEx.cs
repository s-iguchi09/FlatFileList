using System.IO;

namespace FlatFileList.Utilities
{
    public static class DirectoryEx
    {
        public static IEnumerable<string> GetAllFiles(string path)
        {
            return GetAllFiles(path, _ => true);
        }

        public static IEnumerable<string> GetAllFiles(string path, Func<string, bool> predicate)
        {
            if (File.Exists(path))
            {
                yield return path;
            }

            foreach (var fileName in SafeGetFiles(path))
            {
                yield return fileName;
            }


            foreach (var directoryName in SafeGetDirectories(path))
            {
                if (!predicate(directoryName))
                {
                    continue;
                }

                foreach (var fileName in GetAllFiles(directoryName))
                {
                    yield return fileName;
                }
            }
        }

        private static IEnumerable<string> SafeGetDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch
            {
                return [];
            }
        }

        private static IEnumerable<string> SafeGetFiles(string path)
        {
            try
            {
                return Directory.GetFiles(path);
            }
            catch
            {
                return [];
            }
        }
    }
}
