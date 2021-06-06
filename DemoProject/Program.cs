using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using DapperPlus;

namespace DemoProject
{
    public partial class UserDTO
    {
        public Guid Id { get; set; }
        public Guid XId { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDie { get; set; }
        public double? Height { get; set; }
        public double? ExampleProperty1 { get; }
    }
 

    public interface IConnectionProvider
    {
        IDbConnection CreateNew();
    }

    public class SqlAttribute : Attribute
    {
        public string Query { get; }

        public SqlAttribute(string query)
        {
            Query = query;
        }
    }

    public interface IUserService
    {
        [Sql("select * from Users where Id=@id")]
        UserDTO GetUser(Guid id);

        
        [Sql("select * from Users")]
        IEnumerable<UserDTO> GetUsers();

        [Sql("insert into Users values (@Id,@Name)")]
        void NewUser(UserDTO user);
    }
    

    public class ImplementationGenerator
    {
        private string Type2String(Type type)
        {
            if (type == typeof(void)) return "void";
            if (type.GenericTypeArguments.Length > 0)
            {
                var index = type.FullName?.IndexOf("`", StringComparison.InvariantCultureIgnoreCase) ?? 0;
                return type.FullName?.Substring(0, index) + "<" +
                       string.Join(",", type.GenericTypeArguments.Select(Type2String)) + ">";
            }
            return type.ToString();
        }
        public static string MethodNameForReaderAndType(Type propertyType)
        {
            if (propertyType == typeof(int)) return nameof(IDataReader.GetInt32);
            if (propertyType == typeof(long)) return nameof(IDataReader.GetInt64);
            if (propertyType == typeof(short)) return nameof(IDataReader.GetInt16);
            if (propertyType == typeof(byte)) return nameof(IDataReader.GetByte);
            if (propertyType == typeof(bool)) return nameof(IDataReader.GetBoolean);
            if (propertyType == typeof(char)) return nameof(IDataReader.GetChar);
            if (propertyType == typeof(string)) return nameof(IDataReader.GetString);
            if (propertyType == typeof(decimal)) return nameof(IDataReader.GetDecimal);
            if (propertyType == typeof(double)) return nameof(IDataReader.GetDouble);
            if (propertyType == typeof(float)) return nameof(IDataReader.GetFloat);
            if (propertyType == typeof(DateTime)) return nameof(IDataReader.GetDateTime);
            if (propertyType == typeof(Guid)) return nameof(IDataReader.GetGuid);

            return nameof(IDataReader.GetValue) + "<" + (propertyType.FullName ?? propertyType.Name) + ">";
            throw new NotImplementedException(propertyType.FullName);
        }
        public void GenerateImplementation(Type interfaceType , string classPath)
        {
            var writer = new IndentedTextWriter(new StreamWriter(classPath));
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Data;");
            writer.WriteLine($"namespace {interfaceType.Namespace}");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine($"public partial class {interfaceType.Name.Substring(1)}Implementation : {interfaceType.FullName}");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("private readonly IConnectionProvider _connectionProvider;");
            writer.WriteLine("public UserServiceImplementation(IConnectionProvider connectionProvider)");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("_connectionProvider = connectionProvider;");
            writer.Indent--;
            writer.WriteLine("}");
            var sqlAttribute = typeof(SqlAttribute);
            var methods = interfaceType.GetMethods()
                .Where(x => x.CustomAttributes.Any(y => y.AttributeType == sqlAttribute)).ToArray();
            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                writer.Write($"public {Type2String(returnType)} {method.Name}(");
                bool first = true;
                foreach (var parameter in method.GetParameters())
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        writer.Write(",");
                    }
                    writer.Write($"{Type2String(parameter.ParameterType)} {parameter.Name}");
                }
                writer.WriteLine(")");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine("using (var connection=_connectionProvider.CreateNew())");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine("connection.Open();");
                writer.WriteLine("using (var cmd= connection.CreateCommand())");
                var sqlAttributeValue = method.GetCustomAttribute<SqlAttribute>().Query;
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine($"cmd.CommandText = \"{sqlAttributeValue}\";");

                writer.WriteLine("//Parameters");
                foreach (var parameter in method.GetParameters())
                {
                    writer.WriteLine($"IDbDataParameter p_{parameter.Name} = cmd.CreateParameter();");
                    writer.WriteLine($"p_{parameter.Name}.ParameterName = \"{parameter.Name}\";");
                    writer.WriteLine($"p_{parameter.Name}.Value = {parameter.Name};");
                    writer.WriteLine($"cmd.Parameters.Add(p_{parameter.Name});");
                }
              
              
                //yield returning 
                if (method.ReturnType.GenericTypeArguments.Length > 0)
                {
                    writer.WriteLine("using (var reader = cmd.ExecuteReader())");
                    writer.WriteLine("{");
                    writer.Indent++;
                    var innerType = method.ReturnType.GenericTypeArguments[0];
                    var props = innerType.GetProperties();
                    foreach (var prop in props)
                    {
                        writer.WriteLine($"var _{prop.Name}=reader.GetOrdinal(\"{prop.Name}\");");
                    }
                    writer.WriteLine("while (reader.Read())");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine($"var row = new {Type2String(innerType)}();");
                    writer.WriteLine("//TODO");

                    foreach (var prop in props)
                    {
                        WriteProperty(writer, prop);
                    }

                    writer.WriteLine("yield return row;");
                    writer.Indent--;
                    writer.WriteLine("}");

                    writer.Indent--;
                    writer.WriteLine("}");
                    ;
                }
                else if (method.ReturnType == typeof(void))
                {
                    writer.WriteLine("int result=cmd.ExecuteNonQuery();");
                }
                //object returning
                else
                {
                    writer.WriteLine("using (var reader = cmd.ExecuteReader())");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("if (!reader.Read()) return null;");
                    writer.WriteLine($"var row = new {Type2String(method.ReturnType)}();");
                    var props = method.ReturnType.GetProperties();
                    foreach (var prop in props)
                    {
                        writer.WriteLine($"var _{prop.Name}=reader.GetOrdinal(\"{prop.Name}\");");
                    }
                    foreach (var prop in props)
                    {
                        WriteProperty(writer, prop);
                    }
                    writer.WriteLine("return row;");

                    writer.Indent--;
                    writer.WriteLine("}");
                }
                ;
                
             
                writer.Indent--;
                writer.WriteLine("}");
                writer.Indent--;
                writer.WriteLine("}");
                writer.Indent--;
                writer.WriteLine("}");
            }
            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
            writer.Flush();
            writer.Close();
        }

        private void WriteProperty(IndentedTextWriter writer, PropertyInfo prop)
        {
            if (!prop.CanWrite)
            {
                writer.WriteLine($"//TODO Property {prop.Name} has no setter");
                return;
            }
            var pType = prop.PropertyType;
            if (IsNullable(pType, out var resultType))
            {
                
            }
            else
            {
                writer.WriteLine($"if (reader.IsDBNull(_{prop.Name}))");
                writer.Indent++;
                writer.WriteLine($"throw new Exception(\"Column {prop.Name} got null value\");");
                writer.Indent--;
            }

            resultType = resultType ?? pType;
            writer.WriteLine(
                $"row.{prop.Name} = reader.{MethodNameForReaderAndType(resultType)}(_{prop.Name});");
        }

        private bool IsNullable(Type type, out Type underLyingType)
        {
            underLyingType = Nullable.GetUnderlyingType(type);
            return underLyingType != null;
        }
    }
    class Program
    {
        
        static void Main(string[] args)
        {
            string classPath = @"D:\SSD\PROJECTS\DapperPlus\DemoProject\Autogenerated.cs";
            var i=new ImplementationGenerator();
            i.GenerateImplementation(typeof(IUserService), classPath);

            return;
            App.SetClassPath(@"D:\SSD\PROJECTS\DapperPlus\DemoProject\Autogenerated.cs");
            //App.Register(typeof(Program).Assembly, type => true | type.Name.EndsWith("DTO"));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                App.Register(assembly, type => true);
            }


            App.GenerateIfNeeded();
            DbConnection connection = null;
            //var users = connection?.Execute("select * from Users").Select(UserDTO.Parse);
        }
    }
}
