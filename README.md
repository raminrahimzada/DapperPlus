# This is my fun project
## The main purpose of this project is to write something like that

```cs
 public interface IUserService
    {
        [Sql("select * from Users where Id=@id")]
        UserDTO GetUser(Guid id);
        
        [Sql("select * from Users")]
        IEnumerable<UserDTO> GetUsers();

        [Sql("insert into Users values (@Id,@Name)")]
        void NewUser(UserDTO user);
    }
```

and in compile time library should generate its implementations without asking me anything
```cs
//This  class should be generated on compile time
public partial class UserServiceImplementation : IUserService
{
  ...
```

## Project is still development. You can see currently generated code in [here](https://github.com/raminrahimzada/DapperPlus/blob/main/DemoProject/Autogenerated.cs) <br/>
## Feel free to suggest anything interesting
