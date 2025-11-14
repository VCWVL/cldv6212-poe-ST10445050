namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class User
    {
        public int Id { get; set; }  // Primary key
        public string FirstName { get; set; }  // First Name
        public string LastName { get; set; }   // Last Name
        public string Email { get; set; }      // Email
        public string Phone { get; set; }      // Phone number
        
        public string Password { get; set; }   // Password (hashed)
        public string Role { get; set; }       // Admin or Customer
    }
}
