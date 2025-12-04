using System.IO;
using UnityEngine;

namespace SaveSystem
{
    public static class FileSystem
    {
        public static void SaveBytes(string filename, byte[] bytes)
        {
            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(path, bytes);
        }

        public static byte[] LoadBytes(string filename)
        {
            string path = Path.Combine(Application.persistentDataPath, filename);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}