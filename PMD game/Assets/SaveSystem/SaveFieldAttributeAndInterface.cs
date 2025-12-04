using System;
using System.Collections.Generic;

namespace SaveSystem
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveFieldAttribute : Attribute
    {
        
    }

    public interface ISaveable
    {
        string SaveKey { get; } //Unique ID
        Dictionary<string, object> CaptureState();
        void RestoreState(Dictionary<string, object> state);
    }
}
