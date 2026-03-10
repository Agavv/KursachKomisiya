namespace 
    
    API.DTOs.Auth
{
    public class ChangePasswordDto
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }
}
