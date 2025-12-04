// DialogueSystem/Localization/XMLloader.cs
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Localization
{
    public class XMLloader : IDialogueDataLoader
    {
        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".xml";
        }

        public LocalizationDatabase Load(string filePath)
        {
            var db = new LocalizationDatabase();
            
            // Handle both absolute paths and StreamingAssets paths
            string fullPath = filePath;
            if (!Path.IsPathRooted(filePath))
            {
                fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            }

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"XML file not found: {fullPath}");
                return db;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fullPath);
                
                XmlNodeList entryNodes = doc.SelectNodes("//entry");
                if (entryNodes == null || entryNodes.Count == 0)
                {
                    Debug.LogWarning("No localization entries found in XML file");
                    return db;
                }
                
                foreach (XmlNode entryNode in entryNodes)
                {
                    string key = entryNode.Attributes?["key"]?.Value;
                    if (string.IsNullOrEmpty(key))
                    {
                        Debug.LogWarning("Skipping XML entry with null key");
                        continue;
                    }
                    
                    db.data[key] = new Dictionary<string, string>();
                    
                    foreach (XmlNode translationNode in entryNode.SelectNodes("translation"))
                    {
                        string lang = translationNode.Attributes?["lang"]?.Value;
                        string text = translationNode.InnerText;
                        
                        if (!string.IsNullOrEmpty(lang))
                        {
                            db.data[key][lang] = text;
                        }
                    }
                    
                    // Alternative format: direct language attributes on entry
                    if (entryNode.Attributes?["en"] != null)
                    {
                        foreach (XmlAttribute attr in entryNode.Attributes)
                        {
                            if (attr.Name != "key" && attr.Name != "id")
                            {
                                db.data[key][attr.Name] = attr.Value;
                            }
                        }
                    }
                }
                
                Debug.Log($"XML loaded successfully: {db.data.Count} keys");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse XML file '{filePath}': {e.Message}");
            }
            
            return db;
        }
    }
}
