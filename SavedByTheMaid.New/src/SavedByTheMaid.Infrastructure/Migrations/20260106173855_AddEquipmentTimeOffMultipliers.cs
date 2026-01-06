using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SavedByTheMaid.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentTimeOffMultipliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DirtLevel",
                table: "ServiceOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "ServiceOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FloorLevel",
                table: "ServiceOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasElevator",
                table: "ServiceOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPets",
                table: "ServiceOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxOccurrences",
                table: "ServiceOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PreferredEndTime",
                table: "ServiceOrders",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PreferredStartTime",
                table: "ServiceOrders",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndDate",
                table: "ServiceOrders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrencePattern",
                table: "ServiceOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SquareFootage",
                table: "ServiceOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentAmount",
                table: "ServiceMeets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentReason",
                table: "ServiceMeets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AdjustmentStatus",
                table: "ServiceMeets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckInLatitude",
                table: "ServiceMeets",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckInLongitude",
                table: "ServiceMeets",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "ServiceMeets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckOutLatitude",
                table: "ServiceMeets",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckOutLongitude",
                table: "ServiceMeets",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "ServiceMeets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFeedback",
                table: "ServiceMeets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CustomerRating",
                table: "ServiceMeets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotosJson",
                table: "ServiceMeets",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ServiceAreaId",
                table: "ServiceMeets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDailyHours",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDailyServices",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "CleaningPlaceRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "CleaningPlaceRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeTimeOffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsAllDay = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PriceMultipliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecurrenceDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoomServiceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceOrderRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceTypeEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceMeets_ServiceAreaId_ScheduledStart",
                table: "ServiceMeets",
                columns: new[] { "ServiceAreaId", "ScheduledStart" });

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
                name: "IX_EmployeeTimeOffs_EmployeeId_StartDateTime_EndDateTime",
                table: "EmployeeTimeOffs",
                columns: new[] { "EmployeeId", "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceMultipliers_ServiceTypeId",
                table: "PriceMultipliers",
                column: "ServiceTypeId");

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
                name: "IX_ServiceOrderRooms_CleaningPlaceRoomId",
                table: "ServiceOrderRooms",
                column: "CleaningPlaceRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderRooms_ServiceOrderId",
                table: "ServiceOrderRooms",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypeEquipment_EquipmentId",
                table: "ServiceTypeEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypeEquipment_ServiceTypeId_EquipmentId",
                table: "ServiceTypeEquipment",
                columns: new[] { "ServiceTypeId", "EquipmentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceMeets_ServiceAreas_ServiceAreaId",
                table: "ServiceMeets",
                column: "ServiceAreaId",
                principalTable: "ServiceAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceMeets_ServiceAreas_ServiceAreaId",
                table: "ServiceMeets");

            migrationBuilder.DropTable(
                name: "EmployeeEquipment");

            migrationBuilder.DropTable(
                name: "EmployeeTimeOffs");

            migrationBuilder.DropTable(
                name: "PriceMultipliers");

            migrationBuilder.DropTable(
                name: "RecurrenceDiscounts");

            migrationBuilder.DropTable(
                name: "RoomServiceTypes");

            migrationBuilder.DropTable(
                name: "ServiceOrderRooms");

            migrationBuilder.DropTable(
                name: "ServiceTypeEquipment");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_ServiceMeets_ServiceAreaId_ScheduledStart",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "DirtLevel",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "FloorLevel",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "HasElevator",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "HasPets",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "MaxOccurrences",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "PreferredEndTime",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "PreferredStartTime",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "SquareFootage",
                table: "ServiceOrders");

            migrationBuilder.DropColumn(
                name: "AdjustmentAmount",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "AdjustmentReason",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "AdjustmentStatus",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckInLatitude",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckInLongitude",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckOutLatitude",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckOutLongitude",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CustomerFeedback",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "CustomerRating",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "PhotosJson",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "ServiceAreaId",
                table: "ServiceMeets");

            migrationBuilder.DropColumn(
                name: "MaxDailyHours",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MaxDailyServices",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "CleaningPlaceRooms");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "CleaningPlaceRooms");
        }
    }
}
