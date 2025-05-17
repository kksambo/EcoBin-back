namespace WasteManagement.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } 

        public double Points { get; set; } = 0; 
        public double amount { get; set; } = 0;

        public AppUser(string name, string email, string password, string phoneNumber, string role)
        {
            Name = name;
            Email = email;
            Password = password;
            PhoneNumber = phoneNumber;
            Role = role;
        }

        public AppUser() { } 
    }
}