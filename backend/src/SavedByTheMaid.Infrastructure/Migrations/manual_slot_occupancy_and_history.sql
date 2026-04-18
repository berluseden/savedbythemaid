-- Migración manual: SlotOccupancy + StatusHistory
-- Fecha: 2026-01-19
-- Descripción: Añade tablas para anti-colisión y auditoría de estados

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
    INDEX IX_SlotOccupancies_OccupancyType_ReferenceId (OccupancyType, ReferenceId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Índice UNIQUE compuesto para anti-colisión (CORE del modelo)
-- Garantiza que no existan dos ocupaciones para el mismo slot+empleado
CREATE UNIQUE INDEX IX_SlotOccupancies_EmployeeId_SlotStart 
    ON SlotOccupancies (EmployeeId, SlotStart) 
    WHERE IsDeleted = 0;

-- Nota: MySQL no soporta índices parciales. Alternativa:
-- Usamos un índice compuesto que incluye IsDeleted y la lógica de filtrado se hace en la aplicación
-- O usamos un trigger BEFORE INSERT para validar
ALTER TABLE SlotOccupancies DROP INDEX IF EXISTS IX_SlotOccupancies_EmployeeId_SlotStart;

CREATE UNIQUE INDEX IX_SlotOccupancies_EmployeeId_SlotStart_NotDeleted 
    ON SlotOccupancies (EmployeeId, SlotStart, IsDeleted);

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
-- 4. Migrar órdenes existentes de Draft/Confirmed a PendingReview
-- ============================================
-- OPCIONAL: Migrar órdenes con status Draft (1) a PendingReview (0)
-- UPDATE ServiceOrders SET OrderStatus = 0 WHERE OrderStatus = 1;

-- NOTA: Solo ejecutar si se quiere que órdenes Draft existentes pasen a PendingReview
-- Si se mantienen como están, seguirán funcionando gracias a validTransitions que soporta Draft

-- ============================================
-- 5. Poblar historial inicial para órdenes existentes (OPCIONAL)
-- ============================================
-- INSERT INTO OrderStatusHistories (ServiceOrderId, FromStatus, ToStatus, ChangedById, ReasonCode, Notes, CreatedAt)
-- SELECT Id, NULL, OrderStatus, NULL, 'MIGRATION', 'Estado inicial migrado', CreatedAt
-- FROM ServiceOrders WHERE IsDeleted = 0;

-- INSERT INTO MeetStatusHistories (ServiceMeetId, FromStatus, ToStatus, ChangedById, ReasonCode, Notes, CreatedAt)
-- SELECT Id, NULL, Status, NULL, 'MIGRATION', 'Estado inicial migrado', CreatedAt
-- FROM ServiceMeets WHERE IsDeleted = 0;
