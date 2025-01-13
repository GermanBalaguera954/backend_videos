using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(50, ErrorMessage = "El nombre de usuario no puede tener más de 50 caracteres.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        [MaxLength(100, ErrorMessage = "El email no puede tener más de 100 caracteres.")]
        public string Email { get; set; }

        // El campo Password es solo necesario para la creación y actualización, no para la obtención de usuarios
        public string? Password { get; set; }  // Lo dejamos como opcional para crear o actualizar

        [MaxLength(20, ErrorMessage = "El rol no puede tener más de 20 caracteres.")]
        public string Role { get; set; }
    }
}
