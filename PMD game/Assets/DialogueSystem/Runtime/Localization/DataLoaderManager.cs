using System.Collections.Generic;
using System.Linq;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public static class DataLoaderManager
    {
        private static List<IDialogueDataLoader> _loaders = new List<IDialogueDataLoader>();
        
        // Static constructor to register default loaders
        static DataLoaderManager()
        {
            RegisterLoader(new CsvLoader());
            RegisterLoader(new JsonLoader());
            RegisterLoader(new XmLloader());
        }

        public static void RegisterLoader(IDialogueDataLoader loader)
        {
            if (!_loaders.Contains(loader))
            {
                _loaders.Add(loader);
                Debug.Log($"Registered data loader: {loader.GetType().Name}");
            }
        }

        public static void UnregisterLoader(IDialogueDataLoader loader)
        {
            _loaders.Remove(loader);
        }

        public static LocalizationDatabase Load(string filePath)
        {
            var suitableLoader = _loaders.FirstOrDefault(loader => loader.CanLoad(filePath));

            if (suitableLoader != null)
            {
                Debug.Log($"Loading localization data with {suitableLoader.GetType().Name} from: {filePath}");
                return suitableLoader.Load(filePath);
            }
            
            Debug.LogError($"No suitable loader found for: {filePath}");
            Debug.LogWarning($"Supported formats: {string.Join(", ", GetSupportedExtensions())}");

            return new LocalizationDatabase();
        }

        public static string[] GetSupportedExtensions()
        {
            return _loaders.SelectMany(loader =>
            {
                if (loader is CsvLoader) return new[] { "*.csv" };
                if (loader is JsonLoader) return new[] { "*.json" };
                if (loader is XmLloader) return new[] { "*.xml" };
                return new string[0];
            }).ToArray();
        }

        public static int GetLoaderCount()
        {
            return _loaders.Count;
        }
        
        public static List<string> GetAvailableLoaders()
        {
            return _loaders.Select(loader => loader.GetType().Name).ToList();
        }
    }
}
