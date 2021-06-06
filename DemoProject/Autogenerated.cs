using System;
using System.Data;
namespace DemoProject
{
    public partial class UserServiceImplementation : DemoProject.IUserService
    {
        private readonly IConnectionProvider _connectionProvider;
        public UserServiceImplementation(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }
        public DemoProject.UserDTO GetUser(System.Guid id)
        {
            using (var connection=_connectionProvider.CreateNew())
            {
                connection.Open();
                using (var cmd= connection.CreateCommand())
                {
                    cmd.CommandText = "select * from Users where Id=@id";
                    //Parameters
                    IDbDataParameter p_id = cmd.CreateParameter();
                    p_id.ParameterName = "id";
                    p_id.Value = id;
                    cmd.Parameters.Add(p_id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var row = new DemoProject.UserDTO();
                        var _Id=reader.GetOrdinal("Id");
                        var _XId=reader.GetOrdinal("XId");
                        var _Name=reader.GetOrdinal("Name");
                        var _DateOfBirth=reader.GetOrdinal("DateOfBirth");
                        var _DateOfDie=reader.GetOrdinal("DateOfDie");
                        var _Height=reader.GetOrdinal("Height");
                        var _ExampleProperty1=reader.GetOrdinal("ExampleProperty1");
                        if (reader.IsDBNull(_Id))
                            throw new Exception("Column Id got null value");
                        row.Id = reader.GetGuid(_Id);
                        if (reader.IsDBNull(_XId))
                            throw new Exception("Column XId got null value");
                        row.XId = reader.GetGuid(_XId);
                        if (reader.IsDBNull(_Name))
                            throw new Exception("Column Name got null value");
                        row.Name = reader.GetString(_Name);
                        if (reader.IsDBNull(_DateOfBirth))
                            throw new Exception("Column DateOfBirth got null value");
                        row.DateOfBirth = reader.GetDateTime(_DateOfBirth);
                        row.DateOfDie = reader.GetDateTime(_DateOfDie);
                        row.Height = reader.GetDouble(_Height);
                        //TODO Property ExampleProperty1 has no setter
                        return row;
                    }
                }
            }
        }
        public System.Collections.Generic.IEnumerable<DemoProject.UserDTO> GetUsers()
        {
            using (var connection=_connectionProvider.CreateNew())
            {
                connection.Open();
                using (var cmd= connection.CreateCommand())
                {
                    cmd.CommandText = "select * from Users";
                    //Parameters
                    using (var reader = cmd.ExecuteReader())
                    {
                        var _Id=reader.GetOrdinal("Id");
                        var _XId=reader.GetOrdinal("XId");
                        var _Name=reader.GetOrdinal("Name");
                        var _DateOfBirth=reader.GetOrdinal("DateOfBirth");
                        var _DateOfDie=reader.GetOrdinal("DateOfDie");
                        var _Height=reader.GetOrdinal("Height");
                        var _ExampleProperty1=reader.GetOrdinal("ExampleProperty1");
                        while (reader.Read())
                        {
                            var row = new DemoProject.UserDTO();
                            //TODO
                            if (reader.IsDBNull(_Id))
                                throw new Exception("Column Id got null value");
                            row.Id = reader.GetGuid(_Id);
                            if (reader.IsDBNull(_XId))
                                throw new Exception("Column XId got null value");
                            row.XId = reader.GetGuid(_XId);
                            if (reader.IsDBNull(_Name))
                                throw new Exception("Column Name got null value");
                            row.Name = reader.GetString(_Name);
                            if (reader.IsDBNull(_DateOfBirth))
                                throw new Exception("Column DateOfBirth got null value");
                            row.DateOfBirth = reader.GetDateTime(_DateOfBirth);
                            row.DateOfDie = reader.GetDateTime(_DateOfDie);
                            row.Height = reader.GetDouble(_Height);
                            //TODO Property ExampleProperty1 has no setter
                            yield return row;
                        }
                    }
                }
            }
        }
        public void NewUser(DemoProject.UserDTO user)
        {
            using (var connection=_connectionProvider.CreateNew())
            {
                connection.Open();
                using (var cmd= connection.CreateCommand())
                {
                    cmd.CommandText = "insert into Users values (@Id,@Name)";
                    //Parameters
                    IDbDataParameter p_user = cmd.CreateParameter();
                    p_user.ParameterName = "user";
                    p_user.Value = user;
                    cmd.Parameters.Add(p_user);
                    int result=cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
