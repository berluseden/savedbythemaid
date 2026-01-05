using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace netcore.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public partial class ApplicationUser : IdentityUser
    {
        public string profilePictureUrl { get; set; } = "/images/empty_profile.png";
        public bool isSuperAdmin { get; set; } = false;

        // ===========================================
        // ROLES DE ACCESO AL SISTEMA
        // ===========================================

        /// <summary>Dashboard principal</summary>
        [Display(Name = "Dashboard")]
        public bool HomeRole { get; set; } = false;

        /// <summary>Gestión de usuarios y roles</summary>
        [Display(Name = "Usuarios")]
        public bool ApplicationUserRole { get; set; } = false;

        /// <summary>Módulo de servicios de limpieza (Personal, Inmuebles, Servicios)</summary>
        [Display(Name = "Servicios")]
        public bool CleaningRole { get; set; } = false;

        /// <summary>Reservas y citas de servicio</summary>
        [Display(Name = "Reservas")]
        public bool BookingRole { get; set; } = false;

        /// <summary>Catálogos de inventario (Sucursales, Almacenes, Productos, Clientes, Proveedores)</summary>
        [Display(Name = "Inv.Catálogos")]
        public bool InventoryCatalogRole { get; set; } = false;

        /// <summary>Transacciones de inventario (Compras, Ventas, Recepciones, Envíos)</summary>
        [Display(Name = "Inv.Transacciones")]
        public bool InventoryTransactionRole { get; set; } = false;

        /// <summary>Reportes y estadísticas</summary>
        [Display(Name = "Reportes")]
        public bool ReportsRole { get; set; } = false;
    }
}
