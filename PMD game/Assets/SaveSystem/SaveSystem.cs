using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace SaveSystem
{

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;

        public SaveSystemSettings GlobalSettings = new();

        private float _autoSaveTimer = 0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (!(GlobalSettings.AutoSaveInterval > 0)) return;
            _autoSaveTimer += Time.deltaTime;
            if (!(_autoSaveTimer >= GlobalSettings.AutoSaveInterval)) return;
            AutoSave();
            _autoSaveTimer = 0f;
        }

        public void SaveSlot(string slotName, SaveSlotSettings slotSettings = null)
        {
            var allSaveables = GetAllSaveables();
            var slotData = new Dictionary<string, Dictionary<string, object>>();

            foreach (var s in allSaveables)
            {
                slotData[s.SaveKey] = s.CaptureState();
            }

            byte[] dataBytes = SerializeData(slotData, slotSettings);
            
            FileSystem.SaveBytes(slotName, dataBytes);
            Debug.Log($"Saved slot '{slotName}' ({GetFileType(slotSettings)}, {GetEncryption(slotSettings)})");
        }

        public void LoadSlot(string slotName, SaveSlotSettings slotSettings = null)
        {
            byte[] dataBytes = FileSystem.LoadBytes(slotName);
            if (dataBytes == null)
            {
                Debug.LogWarning($"Slot '{slotName}' not found!");
                return;
            }

            SaveEncryption encryption = GetEncryption(slotSettings);
            if (encryption != SaveEncryption.None)
            {
                dataBytes = encryption switch
                {
                    SaveEncryption.SimpleXor => SimpleXor(dataBytes),
                    SaveEncryption.Aes => AES_Decrypt(dataBytes),
                    _ => dataBytes
                };
            }

            SaveFileType fileType = GetFileType(slotSettings);
            Dictionary<string, Dictionary<string, object>> slotData = fileType switch
            {
                SaveFileType.Json => JsonUtility
                    .FromJson<SerializationWrapper>(System.Text.Encoding.UTF8.GetString(dataBytes)).Data,
                _ => throw new Exception("Unsupported file type")
            };

            var allSaveables = GetAllSaveables();
            foreach (var s in allSaveables)
            {
                if (slotData.TryGetValue(s.SaveKey, out var state))
                {
                    s.RestoreState(state);
                }
            }
            
            Debug.Log($"Loaded slot '{slotName}' ({fileType}, {encryption})");
        }

        public void DeleteSlot(string slotName)
        {
            string path = Path.Combine(Application.persistentDataPath, slotName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void AutoSave()
        {
            SaveSlot(GlobalSettings.AutoSaveSlot);
        }

        private ISaveable[] GetAllSaveables()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>().ToArray();
        }

        private SaveFileType GetFileType(SaveSlotSettings slotSettings)
        {
            return slotSettings?.FileType ?? GlobalSettings.DefaultFileType;
        }

        private SaveEncryption GetEncryption(SaveSlotSettings slotSettings)
        {
            return slotSettings?.Encryption ?? GlobalSettings.DefaultEncryption;
        }

        private byte[] SerializeData(Dictionary<string, Dictionary<string, object>> data, SaveSlotSettings slotSettings)
        {
            SaveFileType fileType = GetFileType(slotSettings);
            byte[] bytes = fileType switch
            {
                SaveFileType.Json => System.Text.Encoding.UTF8.GetBytes(
                    JsonUtility.ToJson(new SerializationWrapper(data), true)),
                _ => throw new Exception("Unsupported file type")
            };

            SaveEncryption encryption = GetEncryption(slotSettings);
            if (encryption != SaveEncryption.None)
            {
                bytes = ApplyEncryption(bytes, encryption);
            }

            return bytes;
        }

        private byte[] ApplyEncryption(byte[] data, SaveEncryption encryption)
        {
            return encryption switch
            {
                SaveEncryption.SimpleXor => SimpleXor(data),
                SaveEncryption.Aes => AES_Encrypt(data),
                _ => data
            };
        }

        private byte[] SimpleXor(byte[] data)
        {
            byte key = 0xAA;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key;
            }

            return data;
        }
        
        private byte[] AES_Encrypt(byte[] data) => data; // placeholder
        private byte[] AES_Decrypt(byte[] data) => data; // placeholder
    }
}
