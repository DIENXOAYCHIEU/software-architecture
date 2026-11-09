namespace AuthService.Domain.Entities;

public class User{
	public Guid Id {get; set;}
	public string Username {get; set;} = string.Empty;
	public string Password {get; set;}= string.Empty;

	public User(
		string username,
		string password
		){
		this.Id = Guid.NewGuid();
		this.Username=username;
		this.Password=password;
	}
}