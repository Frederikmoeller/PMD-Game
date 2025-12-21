using System;

namespace SaveSystem
{
    public enum SaveFileType
    {
        Json,
    }

    public enum SaveEncryption
    {
        None,
        SimpleXor,
        Aes
    }
    
    [Serializable]
    public class SaveSystemSettings
    {
        public SaveFileType DefaultFileType = SaveFileType.Json;
        public SaveEncryption DefaultEncryption = SaveEncryption.None;
        public string AutoSaveSlot = "AutoSave";
        public float AutoSaveInterval = 300f;
    }
    
}
