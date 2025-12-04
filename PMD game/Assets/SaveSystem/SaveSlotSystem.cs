using System;
using System.Collections.Generic;

namespace SaveSystem
{
    [Serializable]
    public class SaveSlotSettings
    {
        public string SlotName;
        public SaveFileType? FileType;
        public SaveEncryption? Encryption;
    }
    
    [Serializable]
    public class SaveSlotMeta
    {
        public string SlotName;
        public string Timestamp;
        public float Playtime;
        public string ThumbnailPath;
    }

    [Serializable]
    public class SerializationWrapper
    {
        public List<SerializableKeyValuePair> Pairs = new();

        public SerializationWrapper()
        {
            
        }

        public SerializationWrapper(Dictionary<string, Dictionary<string, object>> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                Pairs.Add(new SerializableKeyValuePair(kvp.Key, kvp.Value));
            }
        }

        public Dictionary<string, Dictionary<string, object>> Data
        {
            get
            {
                var d = new Dictionary<string, Dictionary<string, object>>();
                foreach (var kv in Pairs)
                {
                    d[kv.Key] = kv.Value;
                }
                return d;
            }
        }
    }

    [Serializable]
    public class SerializableKeyValuePair
    {
        public string Key;
        public Dictionary<string, object> Value;

        public SerializableKeyValuePair()
        {
            
        }

        public SerializableKeyValuePair(string key, Dictionary<string, object> value)
        {
            Key = key;
            Value = value;
        }
    }
}
