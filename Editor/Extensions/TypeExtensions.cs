using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Extensions
{
    public static class TypeExtensions
    {
        public static List<Type> GetTypesUpUntilBaseClass<BaseClass>(
            this Type type, bool includeBaseClass = true)
        {
            List<Type> types = new List<Type>();
            while (typeof(BaseClass).IsAssignableFrom(type))
            {
                if (type == typeof(BaseClass) && !includeBaseClass)
                    break;

                types.Add(type);

                type = type.BaseType;
                
                if (type == typeof(BaseClass) && includeBaseClass)
                    break;
            }
            return types;
        }
        
        public static List<FieldInfo> GetFieldsUpUntilBaseClass<BaseClass>(
            this Type type, bool includeBaseClass = true)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            while (typeof(BaseClass).IsAssignableFrom(type))
            {
                if (type == typeof(BaseClass) && !includeBaseClass)
                    break;

                fields.AddRange(type.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

                type = type.BaseType;
                
                if (type == typeof(BaseClass) && includeBaseClass)
                    break;
            }
            return fields;
        }

        public static List<FieldInfo> GetFieldsUpUntilBaseClass<BaseClass, FieldType>(
            this Type type, bool includeBaseClass = true)
        {
            List<FieldInfo> fields = GetFieldsUpUntilBaseClass<BaseClass>(type, includeBaseClass);

            for (int i = fields.Count - 1; i >= 0; i--)
            {
                if (!typeof(FieldType).IsAssignableFrom(fields[i].FieldType))
                    fields.RemoveAt(i);
            }
            return fields;
        }

        public static List<FieldType> GetFieldValuesUpUntilBaseClass<BaseClass, FieldType>(
            this Type type, object instance, bool includeBaseClass = true)
        {
            List<FieldInfo> fields = GetFieldsUpUntilBaseClass<BaseClass, FieldType>(
                type, includeBaseClass);

            List<FieldType> values = new List<FieldType>();
            for (int i = 0; i < fields.Count; i++)
            {
                values.Add((FieldType)fields[i].GetValue(instance));
            }
            return values;
        }

        public static FieldInfo GetDeclaringFieldUpUntilBaseClass<BaseClass, FieldType>(
            this Type type, object instance, FieldType value, bool includeBaseClass = true)
        {
            List<FieldInfo> fields = GetFieldsUpUntilBaseClass<BaseClass, FieldType>(
                type, includeBaseClass);

            FieldType fieldValue;
            for (int i = 0; i < fields.Count; i++)
            {
                fieldValue = (FieldType)fields[i].GetValue(instance);
                if (Equals(fieldValue, value))
                    return fields[i];
            }

            return null;
        }

        public static string GetNameOfDeclaringField<BaseClass, FieldType>(
            this Type type, object instance, FieldType value, bool capitalize = false)
        {
            FieldInfo declaringField = type
                .GetDeclaringFieldUpUntilBaseClass<BaseClass, FieldType>(instance, value);

            if (declaringField == null)
                return null;

            return GetFieldName(type, declaringField, capitalize);
        }

        public static string GetFieldName(this Type type, FieldInfo fieldInfo, bool capitalize = false)
        {
            string name = fieldInfo.Name;

            if (!capitalize)
                return name;

            if (name.Length <= 1)
                return name.ToUpper();

            return char.ToUpper(name[0]) + name.Substring(1);
        }
        
        public static List<FieldInfo> GetAllAssignableFields<FieldType>(this Type type, BindingFlags bindingFlags)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            FieldInfo[] fieldCandidates = type.GetFields(bindingFlags);
            for (int i = 0; i < fieldCandidates.Length; i++)
            {
                if (typeof(FieldType).IsAssignableFrom(fieldCandidates[i].FieldType))
                    result.Add(fieldCandidates[i]);
            }
            return result;
        }

        public static Type[] GetAllAssignableClasses(
            this Type type, bool includeAbstract = true, bool includeItself = false)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => type.IsAssignableFrom(t) && (t != type || includeItself) && (includeAbstract || !t.IsAbstract))
                .ToArray();
        }

        public static Type[] GetAllClassesWithAttribute<T>(bool includeAbstract = true)
            where T : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.HasAttribute<T>() && (includeAbstract || !t.IsAbstract)).ToArray();
        }

        public static MethodInfo[] GetAllMethodsWithAttribute<T>(
            this Type type, bool includeAbstract = true,
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            where T : Attribute
        {
            return type.GetMethods(bindingFlags)
                .Where(t => t.HasAttribute<T>() && (includeAbstract || !t.IsAbstract)).ToArray();
        }

        public static void ExecuteStaticMethod(this Type type, string name, params object[] arguments)
        {
            MethodInfo method = type.GetMethod(name);
            if (method == null || !method.IsStatic)
                return;

            method.Invoke(null, arguments);
        }

        public static MethodInfo GetMethodIncludingFromBaseClasses(this Type type, string name)
        {
            MethodInfo methodInfo = null;
            Type baseType = type;
            while (methodInfo == null)
            {
                methodInfo = baseType.GetMethod(
                    name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo != null)
                    return methodInfo;
                
                baseType = baseType.BaseType;
                if (baseType == null)
                    break;
            }

            return null;
        }
        
        /// <summary>
        /// Courtesy of codeproject.com:
        /// https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect
        /// </summary>
        public static bool IsList(this Type type)
        {
            if (null == type)
                throw new ArgumentNullException(nameof(type));

            if (typeof(IList).IsAssignableFrom(type))
                return true;
            
            foreach (Type it in type.GetInterfaces())
            {
                if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition())
                    return true;
            }
            return false;
        }

        public static List<FieldInfo> GetSerializableFields(this Type type, bool includeParents = true)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (includeParents)
                flags |= BindingFlags.FlattenHierarchy;
            
            FieldInfo[] allFields = type.GetFields(flags);
            List<FieldInfo> serializableFields = new List<FieldInfo>();
            foreach (FieldInfo fieldInfo in allFields)
            {
                // Public fields get serialized regardless.
                if (fieldInfo.IsPublic)
                {
                    serializableFields.Add(fieldInfo);
                    continue;
                }

                // Private fields only get serialized if they have a certain attribute.
                if (fieldInfo.HasAttribute<SerializeField>() || fieldInfo.HasAttribute<SerializeReference>())
                    serializableFields.Add(fieldInfo);
            }

            return serializableFields;
        }
    }
}
