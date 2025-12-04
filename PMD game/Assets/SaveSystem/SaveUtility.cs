using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace SaveSystem
{
    public static class SaveUtility
    {
        public static Dictionary<string, object> Capture(object obj)
        {
            var result = new Dictionary<string, object>();
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)))
                {
                    result[field.Name] = field.GetValue(obj);
                }
            }
            return result;
        }

        public static void Restore(object obj, Dictionary<string, object> state)
        {
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(SaveFieldAttribute)) && state.TryGetValue(field.Name, out var value))
                {
                    field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                }
            }
        }
    }
}
