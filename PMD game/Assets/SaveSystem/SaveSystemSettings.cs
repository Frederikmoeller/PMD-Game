using System;

namespace SaveSystem
{
    public enum SaveFileType
    {
        JSON,
    }

    public enum SaveEncryption
    {
        None,
        SimpleXOR,
        AES
    }
    
    [Serializable]
    public class SaveSystemSettings
    {
        public SaveFileType DefaultFileType = SaveFileType.JSON;
        public SaveEncryption DefaultEncryption = SaveEncryption.None;
        public string AutoSaveSlot = "AutoSave";
        public float AutoSaveInterval = 300f;
    }
    
}
