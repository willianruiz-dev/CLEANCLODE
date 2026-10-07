namespace ApiService.Models
{
    public sealed class UserPersonalInfoDto
    {
        public string Document { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
