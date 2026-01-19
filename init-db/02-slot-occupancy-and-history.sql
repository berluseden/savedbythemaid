-- Migración: SlotOccupancy + StatusHistory para MySQL
-- Fecha: 2026-01-19
-- Compatible con MySQL 8.0

-- ============================================
-- 1. Tabla SlotOccupancy (Anti-colisión)
-- ============================================
CREATE TABLE IF NOT EXISTS SlotOccupancies (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EmployeeId INT NOT NULL,
    SlotStart DATETIME(6) NOT NULL,
    SlotEnd DATETIME(6) NOT NULL,
    OccupancyType INT NOT NULL DEFAULT 0 COMMENT '0=SoftReserve, 1=Meeting',
    ReferenceId INT NOT NULL COMMENT 'ID de SoftReserve o ServiceMeet',
    ExpiresAt DATETIME(6) NULL COMMENT 'Solo para SoftReserve',
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL,
    CreatedByUserId VARCHAR(255) NULL,
    UpdatedByUserId VARCHAR(255) NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_SlotOccupancies_Employees 
        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    
    INDEX IX_SlotOccupancies_ExpiresAt_OccupancyType (ExpiresAt, OccupancyType),
    INDEX IX_SlotOccupancies_OccupancyType_ReferenceId (OccupancyType, ReferenceId),
    -- Índice UNIQUE compuesto (EmployeeId, SlotStart) cuando IsDeleted=0
    -- MySQL no soporta índices parciales, usamos índice compuesto
    UNIQUE INDEX IX_SlotOccupancies_AntiCollision (EmployeeId, SlotStart, IsDeleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 2. Tabla OrderStatusHistory (Auditoría)
-- ============================================
CREATE TABLE IF NOT EXISTS OrderStatusHistories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ServiceOrderId INT NOT NULL,
    FromStatus INT NULL COMMENT 'Estado anterior (null si es creación)',
    ToStatus INT NOT NULL COMMENT 'Estado nuevo',
    ChangedById VARCHAR(255) NULL COMMENT 'ID del usuario que cambió',
    ChangedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ReasonCode VARCHAR(50) NULL COMMENT 'Código de razón',
    Notes TEXT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL,
    CreatedByUserId VARCHAR(255) NULL,
    UpdatedByUserId VARCHAR(255) NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_OrderStatusHistories_ServiceOrders 
        FOREIGN KEY (ServiceOrderId) REFERENCES ServiceOrders(Id) ON DELETE CASCADE,
    
    INDEX IX_OrderStatusHistories_ServiceOrderId (ServiceOrderId),
    INDEX IX_OrderStatusHistories_ChangedAt (ChangedAt),
    INDEX IX_OrderStatusHistories_ReasonCode (ReasonCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 3. Tabla MeetStatusHistory (Auditoría)
-- ============================================
CREATE TABLE IF NOT EXISTS MeetStatusHistories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ServiceMeetId INT NOT NULL,
    FromStatus INT NULL COMMENT 'Estado anterior (null si es creación)',
    ToStatus INT NOT NULL COMMENT 'Estado nuevo',
    ChangedById VARCHAR(255) NULL COMMENT 'ID del usuario que cambió',
    ChangedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ReasonCode VARCHAR(50) NULL COMMENT 'Código de razón',
    Notes TEXT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL,
    CreatedByUserId VARCHAR(255) NULL,
    UpdatedByUserId VARCHAR(255) NULL,
    IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_MeetStatusHistories_ServiceMeets 
        FOREIGN KEY (ServiceMeetId) REFERENCES ServiceMeets(Id) ON DELETE CASCADE,
    
    INDEX IX_MeetStatusHistories_ServiceMeetId (ServiceMeetId),
    INDEX IX_MeetStatusHistories_ChangedAt (ChangedAt),
    INDEX IX_MeetStatusHistories_ReasonCode (ReasonCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================
-- 4. Poblar historial inicial para órdenes existentes
-- ============================================
INSERT INTO OrderStatusHistories (ServiceOrderId, FromStatus, ToStatus, ChangedById, ReasonCode, Notes, CreatedAt)
SELECT so.Id, NULL, so.OrderStatus, NULL, 'MIGRATION', 'Estado inicial migrado', so.CreatedAt
FROM ServiceOrders so WHERE so.IsDeleted = 0
ON DUPLICATE KEY UPDATE OrderStatusHistories.Id = OrderStatusHistories.Id;

INSERT INTO MeetStatusHistories (ServiceMeetId, FromStatus, ToStatus, ChangedById, ReasonCode, Notes, CreatedAt)
SELECT sm.Id, NULL, sm.Status, NULL, 'MIGRATION', 'Estado inicial migrado', sm.CreatedAt
FROM ServiceMeets sm WHERE sm.IsDeleted = 0
ON DUPLICATE KEY UPDATE MeetStatusHistories.Id = MeetStatusHistories.Id;
