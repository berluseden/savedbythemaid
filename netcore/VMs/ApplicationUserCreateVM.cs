using System.ComponentModel.DataAnnotations;

namespace netcore.VMs
{
    public class ApplicationUserCreateVM
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Super Admin")]
        public bool IsSuperAdmin { get; set; }

        // Roles
        [Display(Name = "Dashboard")]
        public bool HomeRole { get; set; }

        [Display(Name = "Usuarios")]
        public bool ApplicationUserRole { get; set; }

        [Display(Name = "Servicios")]
        public bool CleaningRole { get; set; }

        [Display(Name = "Reservas")]
        public bool BookingRole { get; set; }

        [Display(Name = "Inv. Catálogos")]
        public bool InventoryCatalogRole { get; set; }

        [Display(Name = "Inv. Transacciones")]
        public bool InventoryTransactionRole { get; set; }

        [Display(Name = "Reportes")]
        public bool ReportsRole { get; set; }
    }
}
