using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DapperPlus
{
    public class App
    {

        public static bool GenerateIfNeeded()
        {
            Debug.WriteLine("generating source...");
            var writer = new IndentedTextWriter(new StreamWriter(ClassPath));
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Data.Common;");
          
            foreach (var type in TypeList)
            {
                var typeName = type.Name;
                typeName = typeName.Replace('+', '.');
                var typeFullName = type.FullName ?? "";
                typeFullName = typeFullName.Replace('+', '.');
                writer.WriteLine($"namespace {type.Namespace}");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine($"partial class {typeName}");   
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine($"public static {typeFullName} Parse(DbDataReader reader)");
                
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine($"var result = new {typeFullName}();");
                var props = type.GetProperties();
                foreach (var prop in props)
                {
                    writer.WriteLine("// Property " + prop.Name);
                    var ordinal = $"v{prop.Name}";
                    writer.WriteLine($"var {ordinal} = reader.GetOrdinal(nameof({typeFullName}.{prop.Name}));");
                    var t = prop.PropertyType;
                    if (t.IsGenericType&&t.Name== "Nullable`1")
                    {
                        var innerType = t.GenericTypeArguments[0];
                        writer.WriteLine(
                            $"result.{prop.Name} = reader.IsDBNull({ordinal}) ? ({innerType}?) null : reader.{MethodNameForReaderAndType(innerType)}({ordinal});");
                    }
                    else
                    {
                        writer.WriteLine($"result.{prop.Name} = reader.{MethodNameForReaderAndType(prop.PropertyType)}({ordinal});");
                    }
                }
                writer.WriteLine("return result;");
                writer.Indent--;
                writer.WriteLine("}");
                writer.Indent--;
                writer.WriteLine("}");
                writer.Indent--;
                writer.WriteLine("}");
            }
            writer.Flush();
            writer.Close();
            return true;
        }

        public static string MethodNameForReaderAndType(Type propertyType)
        {
            if (propertyType == typeof(int)) return nameof(DbDataReader.GetInt32);
            if (propertyType == typeof(long)) return nameof(DbDataReader.GetInt64);
            if (propertyType == typeof(short)) return nameof(DbDataReader.GetInt16);
            if (propertyType == typeof(byte)) return nameof(DbDataReader.GetByte);
            if (propertyType == typeof(bool)) return nameof(DbDataReader.GetBoolean);
            if (propertyType == typeof(char)) return nameof(DbDataReader.GetChar);
            if (propertyType == typeof(string)) return nameof(DbDataReader.GetString);
            if (propertyType == typeof(decimal)) return nameof(DbDataReader.GetDecimal);
            if (propertyType == typeof(double)) return nameof(DbDataReader.GetDouble);
            if (propertyType == typeof(float)) return nameof(DbDataReader.GetFloat);
            if (propertyType == typeof(DateTime)) return nameof(DbDataReader.GetDateTime);
            if (propertyType == typeof(Guid)) return nameof(DbDataReader.GetGuid);

            return nameof(DbDataReader.GetFieldValue) + "<" + (propertyType.FullName ?? propertyType.Name) + ">";
            throw new NotImplementedException(propertyType.FullName);
        }

        public static string ClassPath { get; set; }
        private static readonly HashSet<Type> TypeList = new HashSet<Type>();

        public static void Register<T>()
        {
            var type = typeof(T);
            if (type.IsAbstract) throw new Exception("Non Abstract class required");
            TypeList.Add(type);
        }

        public static void Register(Assembly assembly,Func<Type,bool> filter)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (filter(type))
                {
                    if (type.IsAbstract) continue;
                    if (string.IsNullOrWhiteSpace(type.Namespace)) continue;
                    if (type.IsNested && !type.IsPublic) continue;
                    if (type.IsSpecialName) continue;
                    if (type.ContainsGenericParameters) continue;
                    TypeList.Add(type);
                }
            }
        }
        public static void SetClassPath(string classPath)
        {
            ClassPath=classPath;
        }
    }
}
