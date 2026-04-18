using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace SavedByTheMaid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdditionalServiceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalServiceTypes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    FirstName = table.Column<string>(type: "longtext", nullable: true),
                    LastName = table.Column<string>(type: "longtext", nullable: true),
                    Address = table.Column<string>(type: "longtext", nullable: true),
                    City = table.Column<string>(type: "longtext", nullable: true),
                    State = table.Column<string>(type: "longtext", nullable: true),
                    ZipCode = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CleaningPlaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningPlaces", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecurrenceDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RecurrenceType = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceDiscounts", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAreas", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerBedroom = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PricePerBathroom = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    MinutesPerBedroom = table.Column<int>(type: "int", nullable: false),
                    MinutesPerBathroom = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(255)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Token = table.Column<string>(type: "varchar(255)", nullable: false),
                    JwtId = table.Column<string>(type: "longtext", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CleaningPlaceRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    BaseMinutes = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CleaningPlaceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningPlaceRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningPlaceRooms_CleaningPlaces_CleaningPlaceId",
                        column: x => x.CleaningPlaceId,
                        principalTable: "CleaningPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(type: "longtext", nullable: false),
                    LastName = table.Column<string>(type: "longtext", nullable: false),
                    Email = table.Column<string>(type: "longtext", nullable: true),
                    Phone = table.Column<string>(type: "longtext", nullable: true),
                    Address = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: true),
                    PrimaryServiceAreaId = table.Column<int>(type: "int", nullable: true),
                    MaxDailyHours = table.Column<int>(type: "int", nullable: true),
                    MaxDailyServices = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_ServiceAreas_PrimaryServiceAreaId",
                        column: x => x.PrimaryServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceAreaZips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ZipCode = table.Column<string>(type: "varchar(255)", nullable: false),
                    City = table.Column<string>(type: "longtext", nullable: true),
                    State = table.Column<string>(type: "longtext", nullable: true),
                    County = table.Column<string>(type: "longtext", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsFullCoverage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SurchargeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinimumMinutes = table.Column<int>(type: "int", nullable: true),
                    AvailableDaysJson = table.Column<string>(type: "longtext", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true),
                    ServiceAreaId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAreaZips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAreaZips_ServiceAreas_ServiceAreaId",
                        column: x => x.ServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PriceMultipliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    ConditionType = table.Column<int>(type: "int", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AppliesToTime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AppliesToPrice = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceMultipliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceMultipliers_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<string>(type: "varchar(255)", nullable: true),
                    ServiceAreaId = table.Column<int>(type: "int", nullable: true),
                    ZipCode = table.Column<string>(type: "longtext", nullable: false),
                    Address = table.Column<string>(type: "longtext", nullable: false),
                    AddressLine2 = table.Column<string>(type: "longtext", nullable: true),
                    City = table.Column<string>(type: "longtext", nullable: true),
                    State = table.Column<string>(type: "longtext", nullable: true),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    CleaningPlaceId = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    SquareFootage = table.Column<int>(type: "int", nullable: true),
                    DirtLevel = table.Column<int>(type: "int", nullable: false),
                    HasPets = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FloorLevel = table.Column<int>(type: "int", nullable: true),
                    HasElevator = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderStatus = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    RecurrenceType = table.Column<int>(type: "int", nullable: false),
                    RecurrenceEndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MaxOccurrences = table.Column<int>(type: "int", nullable: true),
                    RecurrencePattern = table.Column<string>(type: "longtext", nullable: true),
                    ContactName = table.Column<string>(type: "longtext", nullable: true),
                    ContactPhone = table.Column<string>(type: "longtext", nullable: true),
                    ContactEmail = table.Column<string>(type: "longtext", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "longtext", nullable: true),
                    PreferredStartTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    PreferredEndTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrders_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceOrders_CleaningPlaces_CleaningPlaceId",
                        column: x => x.CleaningPlaceId,
                        principalTable: "CleaningPlaces",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceOrders_ServiceAreas_ServiceAreaId",
                        column: x => x.ServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceOrders_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceTypeEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypeEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceTypeEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceTypeEquipment_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomServiceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CleaningPlaceRoomId = table.Column<int>(type: "int", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    BaseMinutesOverride = table.Column<int>(type: "int", nullable: true),
                    BasePriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomServiceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomServiceTypes_CleaningPlaceRooms_CleaningPlaceRoomId",
                        column: x => x.CleaningPlaceRoomId,
                        principalTable: "CleaningPlaceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomServiceTypes_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEquipment_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSchedules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeServiceAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ServiceAreaId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeServiceAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeServiceAreas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeServiceAreas_ServiceAreas_ServiceAreaId",
                        column: x => x.ServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeTimeOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTimeOffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTimeOffs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SlotOccupancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SlotStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SlotEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OccupancyType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotOccupancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotOccupancies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceOrderId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<string>(type: "varchar(255)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReasonCode = table.Column<string>(type: "longtext", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_AspNetUsers_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderStatusHistories_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceMeets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceOrderId = table.Column<int>(type: "int", nullable: false),
                    AssignedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ServiceAreaId = table.Column<int>(type: "int", nullable: true),
                    ScheduledStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CheckInLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckInLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CheckOutLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckOutLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdjustmentStatus = table.Column<int>(type: "int", nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AdjustmentReason = table.Column<string>(type: "longtext", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true),
                    CancellationReason = table.Column<string>(type: "longtext", nullable: true),
                    PhotosJson = table.Column<string>(type: "longtext", nullable: true),
                    CustomerRating = table.Column<int>(type: "int", nullable: true),
                    CustomerFeedback = table.Column<string>(type: "longtext", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceMeets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceMeets_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceMeets_ServiceAreas_ServiceAreaId",
                        column: x => x.ServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceMeets_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceOrderId = table.Column<int>(type: "int", nullable: false),
                    AdditionalServiceTypeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderItems_AdditionalServiceTypes_AdditionalServiceTy~",
                        column: x => x.AdditionalServiceTypeId,
                        principalTable: "AdditionalServiceTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceOrderItems_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceOrderRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceOrderId = table.Column<int>(type: "int", nullable: false),
                    CleaningPlaceRoomId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CalculatedMinutes = table.Column<int>(type: "int", nullable: false),
                    CalculatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOrderRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceOrderRooms_CleaningPlaceRooms_CleaningPlaceRoomId",
                        column: x => x.CleaningPlaceRoomId,
                        principalTable: "CleaningPlaceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceOrderRooms_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SoftReserves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    CustomerId = table.Column<string>(type: "varchar(255)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ServiceAreaId = table.Column<int>(type: "int", nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExtensionCount = table.Column<int>(type: "int", nullable: false),
                    ServiceOrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftReserves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftReserves_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SoftReserves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftReserves_ServiceAreas_ServiceAreaId",
                        column: x => x.ServiceAreaId,
                        principalTable: "ServiceAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftReserves_ServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "ServiceOrders",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MeetStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ServiceMeetId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<string>(type: "varchar(255)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReasonCode = table.Column<string>(type: "longtext", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetStatusHistories_AspNetUsers_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MeetStatusHistories_ServiceMeets_ServiceMeetId",
                        column: x => x.ServiceMeetId,
                        principalTable: "ServiceMeets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CleaningPlaceRooms_CleaningPlaceId",
                table: "CleaningPlaceRooms",
                column: "CleaningPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEquipment_EmployeeId_EquipmentId",
                table: "EmployeeEquipment",
                columns: new[] { "EmployeeId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEquipment_EquipmentId",
                table: "EmployeeEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PrimaryServiceAreaId",
                table: "Employees",
                column: "PrimaryServiceAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSchedules_EmployeeId_DayOfWeek",
                table: "EmployeeSchedules",
                columns: new[] { "EmployeeId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeServiceAreas_EmployeeId_ServiceAreaId",
                table: "EmployeeServiceAreas",
                columns: new[] { "EmployeeId", "ServiceAreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeServiceAreas_ServiceAreaId",
                table: "EmployeeServiceAreas",
                column: "ServiceAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimeOffs_EmployeeId_StartDateTime_EndDateTime",
                table: "EmployeeTimeOffs",
                columns: new[] { "EmployeeId", "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetStatusHistories_ChangedAt",
                table: "MeetStatusHistories",
                column: "ChangedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_MeetStatusHistories_ChangedById",
                table: "MeetStatusHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_MeetStatusHistories_ServiceMeetId",
                table: "MeetStatusHistories",
                column: "ServiceMeetId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ChangedAt",
                table: "OrderStatusHistories",
                column: "ChangedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ChangedById",
                table: "OrderStatusHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ServiceOrderId",
                table: "OrderStatusHistories",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceMultipliers_ServiceTypeId",
                table: "PriceMultipliers",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Revoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomServiceTypes_CleaningPlaceRoomId_ServiceTypeId",
                table: "RoomServiceTypes",
                columns: new[] { "CleaningPlaceRoomId", "ServiceTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomServiceTypes_ServiceTypeId",
                table: "RoomServiceTypes",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreaZips_ServiceAreaId",
                table: "ServiceAreaZips",
                column: "ServiceAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceAreaZips_ZipCode",
                table: "ServiceAreaZips",
                column: "ZipCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeets_AssignedEmployeeId_ScheduledStart",
                table: "ServiceMeets",
                columns: new[] { "AssignedEmployeeId", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeets_ServiceAreaId_ScheduledStart",
                table: "ServiceMeets",
                columns: new[] { "ServiceAreaId", "ScheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeets_ServiceOrderId",
                table: "ServiceMeets",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeets_Status",
                table: "ServiceMeets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItems_AdditionalServiceTypeId",
                table: "ServiceOrderItems",
                column: "AdditionalServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderItems_ServiceOrderId",
                table: "ServiceOrderItems",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderRooms_CleaningPlaceRoomId",
                table: "ServiceOrderRooms",
                column: "CleaningPlaceRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderRooms_ServiceOrderId",
                table: "ServiceOrderRooms",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CleaningPlaceId",
                table: "ServiceOrders",
                column: "CleaningPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CreatedAt",
                table: "ServiceOrders",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_CustomerId",
                table: "ServiceOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_OrderStatus_CreatedAt",
                table: "ServiceOrders",
                columns: new[] { "OrderStatus", "CreatedAt" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_ServiceAreaId",
                table: "ServiceOrders",
                column: "ServiceAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_ServiceTypeId",
                table: "ServiceOrders",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypeEquipment_EquipmentId",
                table: "ServiceTypeEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypeEquipment_ServiceTypeId_EquipmentId",
                table: "ServiceTypeEquipment",
                columns: new[] { "ServiceTypeId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlotOccupancies_EmployeeId_SlotStart",
                table: "SlotOccupancies",
                columns: new[] { "EmployeeId", "SlotStart" },
                unique: true,
                filter: "`IsDeleted` = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SlotOccupancies_ExpiresAt_OccupancyType",
                table: "SlotOccupancies",
                columns: new[] { "ExpiresAt", "OccupancyType" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotOccupancies_OccupancyType_ReferenceId",
                table: "SlotOccupancies",
                columns: new[] { "OccupancyType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_CustomerId",
                table: "SoftReserves",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_EmployeeId_ScheduledStart_ScheduledEnd",
                table: "SoftReserves",
                columns: new[] { "EmployeeId", "ScheduledStart", "ScheduledEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_ExpiresAt_Status",
                table: "SoftReserves",
                columns: new[] { "ExpiresAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_ServiceAreaId",
                table: "SoftReserves",
                column: "ServiceAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_ServiceOrderId",
                table: "SoftReserves",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_SessionId",
                table: "SoftReserves",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EmployeeEquipment");

            migrationBuilder.DropTable(
                name: "EmployeeSchedules");

            migrationBuilder.DropTable(
                name: "EmployeeServiceAreas");

            migrationBuilder.DropTable(
                name: "EmployeeTimeOffs");

            migrationBuilder.DropTable(
                name: "MeetStatusHistories");

            migrationBuilder.DropTable(
                name: "OrderStatusHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PriceMultipliers");

            migrationBuilder.DropTable(
                name: "RecurrenceDiscounts");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoomServiceTypes");

            migrationBuilder.DropTable(
                name: "ServiceAreaZips");

            migrationBuilder.DropTable(
                name: "ServiceOrderItems");

            migrationBuilder.DropTable(
                name: "ServiceOrderRooms");

            migrationBuilder.DropTable(
                name: "ServiceTypeEquipment");

            migrationBuilder.DropTable(
                name: "SlotOccupancies");

            migrationBuilder.DropTable(
                name: "SoftReserves");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ServiceMeets");

            migrationBuilder.DropTable(
                name: "AdditionalServiceTypes");

            migrationBuilder.DropTable(
                name: "CleaningPlaceRooms");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "ServiceOrders");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CleaningPlaces");

            migrationBuilder.DropTable(
                name: "ServiceAreas");

            migrationBuilder.DropTable(
                name: "ServiceTypes");
        }
    }
}
