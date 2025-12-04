using System.ComponentModel.DataAnnotations;
namespace MVC_project.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(15,ErrorMessage ="The name too long")]
        public string FirstName {get; set;}

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(15, ErrorMessage = "The name too long")]
        public string LastName { get; set; }

        [Key]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email {get; set;}

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Password confirmation required")]
        [Compare("Password", ErrorMessage = "The passwords do not match")]
        public string ConfirmPassword { get; set; }

    }
}
