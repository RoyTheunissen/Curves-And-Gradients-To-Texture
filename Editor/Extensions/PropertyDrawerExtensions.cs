using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = System.Object;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Extensions
{
    public static class PropertyDrawerExtensions 
    {
        public static object GetActualObjectByPath(SerializedObject serializedObject, string path)
        {
            return GetActualObjectByPath(serializedObject.targetObject, path);
        }

        public static object GetActualObjectByPath(Object owner, string path)
        {
            // Sample paths:    connections.Array.data[0].to
            //                  connection.to
            //                  to

            string[] pathSections = path.Split('.');

            object value = owner;
            for (int i = 0; i < pathSections.Length; i++)
            {
                Type valueType = value.GetType();

                if (valueType.IsArray || valueType.IsList())
                {
                    // Parse the next section which contains the index. 
                    string indexPathSection = pathSections[i + 1];
                    indexPathSection = Regex.Replace(indexPathSection, @"\D", "");
                    int index = int.Parse(indexPathSection);

                    if (valueType.IsArray)
                    {
                        // Get the value from the array.
                        Array array = value as Array;
                        value = array.GetValue(index);
                    }
                    else
                    {
                        // Get the value from the list.
                        IList list = value as IList;
                        value = list[index];
                    }
                    
                    // We can now skip the next section which is the one with the index.
                    i++;
                    continue;
                }
                
                // Go deeper down the hierarchy by searching in the current value for a field with
                // the same name as the current path section and then getting that value.
                FieldInfo fieldInfo = valueType.GetField(
                    pathSections[i], BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                value = fieldInfo.GetValue(value);
            }

            return value;
        }

        public static object GetParentObject(
            this PropertyDrawer propertyDrawer, SerializedProperty property)
        {
            string path = property.propertyPath;
            int indexOfLastSeparator = path.LastIndexOf(".", StringComparison.Ordinal);

            // No separators means it's a root object and there's no parent.
            if (indexOfLastSeparator == -1)
                return property.serializedObject.targetObject;

            string pathExcludingLastObject = path.Substring(0, indexOfLastSeparator);
            return GetActualObjectByPath(property.serializedObject, pathExcludingLastObject);
        }

        public static T GetActualObject<T>(
            this PropertyDrawer propertyDrawer, FieldInfo fieldInfo, SerializedProperty property)
            where T : class
        {
            return property.GetValue<T>();
        }
    }
}
