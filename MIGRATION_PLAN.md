# 🚀 SavedByTheMaid - Plan de Implementación Completo

**Fecha de inicio:** 6 de Enero 2026  
**Estado:** En Progreso  
**Progreso actual:** ~35-40%

---

## 📋 Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Limpieza del Sistema](#limpieza-del-sistema)
3. [Estado Actual](#estado-actual)
4. [Fase 1 - MVP Funcional](#fase-1---mvp-funcional)
5. [Fase 2 - Operación](#fase-2---operación)
6. [Fase 3 - Avanzado](#fase-3---avanzado)
7. [Modelo de Datos](#modelo-de-datos)
8. [Checklist de Progreso](#checklist-de-progreso)

---

## Resumen Ejecutivo

Sistema de booking para servicios de limpieza con:
- **Proyecto Único** (netcore) - Puerto 5001
  - Área `Client` → Portal Cliente (ruta `/`)
  - Controladores Admin → (ruta `/admin`)
- **Base de Datos** - MySQL (SavedByMaidIM)
- **Identity** - Unificado con SSO

---

## Limpieza del Sistema

### 🔄 Consolidación de Proyectos (EService.Web → netcore)

Actualmente tenemos **2 proyectos web separados**:
- `EService.Web` - Portal Cliente (puerto 5000)
- `netcore` - Panel Admin (puerto 5001)

**Decisión:** Consolidar en UN solo proyecto usando **Áreas de ASP.NET Core**

#### Nueva Estructura:

```
netcore/                          # Proyecto único
├── Areas/
│   └── Client/                   # Área para clientes
│       ├── Controllers/
│       │   ├── HomeController.cs
│       │   ├── BookingController.cs
│       │   ├── CartController.cs
│       │   └── AccountController.cs
│       └── Views/
│           ├── Home/
│           ├── Booking/
│           ├── Cart/
│           ├── Account/
│           └── Shared/
│               └── _ClientLayout.cshtml
├── Controllers/                  # Admin (sin área)
├── Views/                        # Admin views
└── wwwroot/                      # Recursos compartidos + cliente
```

#### URLs Resultantes:
| Ruta | Descripción |
|------|-------------|
| `/` | Landing page cliente |
| `/booking` | Wizard de reserva |
| `/cart` | Carrito del cliente |
| `/account/login` | Login cliente |
| `/admin` | Dashboard admin |
| `/admin/employees` | CRUD empleadas |
| `/admin/servicetypes` | CRUD servicios |

#### Beneficios:
- ✅ Un solo proyecto = menos mantenimiento
- ✅ Comparten servicios, modelos, Identity
- ✅ Un solo `Program.cs`
- ✅ Deploy más simple
- ✅ SSO automático (misma cookie)

#### Tareas de Consolidación:
| # | Tarea | Estado |
|---|-------|--------|
| C.1 | Crear estructura de Área `Client` | ✅ |
| C.2 | Mover controladores de EService.Web | ✅ |
| C.3 | Mover vistas de EService.Web | ✅ |
| C.4 | Mover wwwroot (CSS, JS, imágenes) | ✅ |
| C.5 | Crear `_ClientLayout.cshtml` (ecoMaid) | ✅ |
| C.6 | Mover controladores admin a `/admin` | ✅ |
| C.7 | Configurar rutas en Program.cs | ✅ |
| C.8 | Actualizar referencias en vistas | ✅ |
| C.9 | Eliminar proyecto EService.Web | ✅ |
| C.10 | Actualizar JempSoft.sln | ✅ |

**✅ CONSOLIDACIÓN COMPLETADA - 6 Ene 2026**

---

### ❌ Módulos a Ocultar (Inventario/Warehouse)

El sistema heredó módulos de inventario que **NO** aplican al negocio de limpieza.
Se ocultarán del menú (no se eliminan por si se necesitan en el futuro).

#### Secciones a Ocultar del Admin Panel:

**CATÁLOGOS (Inventario):**
- [x] Ubicaciones (Branch, Warehouse) ✅
- [x] Productos (Product) ✅
- [x] Contactos (Customer, Vendor) ✅

**TRANSACCIONES (Inventario):**
- [x] Compras (PurchaseOrder, Receiving) ✅
- [x] Ventas (SalesOrder, Shipment) ✅
- [x] Existencias (Stock) ✅
- [x] Transferencias (TransferOut, TransferIn) ✅

#### Archivo Modificado:
- `netcore/Views/Shared/_AdminLTE4Sidebar.cshtml` ✅
  - Comentado sección `InventoryCatalogRole`
  - Comentado sección `InventoryTransactionRole`

#### Tablas que permanecen (no se eliminan):
- Branch, Warehouse, Product, Customer, Vendor
- PurchaseOrder, Receiving, SalesOrder, Shipment
- TransferOut, TransferIn, Stock

---

## Estado Actual

### ✅ Completado
- [x] Catálogo base (CleaningPlaces, Rooms, ServiceTypes)
- [x] Relaciones many-to-many (Place→Rooms, Room→Services)
- [x] AdditionalServiceTypes (extras)
- [x] Employees básico
- [x] EmployeeSchedules básico
- [x] CartItems, ServiceOrders, ServiceContactsInfo
- [x] ServiceMeeting básico
- [x] ASP.NET Identity unificado
- [x] Admin Panel CRUD completo
- [x] Booking Wizard pasos 1-3
- [x] **Ocultar módulos de Inventario del menú** ✅ (6 Ene 2026)
- [x] **Consolidación de proyectos** ✅ (6 Ene 2026)

### 🟡 Parcial
- [ ] EmployeeSchedules (falta StartTime/EndTime)
- [ ] ServiceOrders (falta PaymentStatus, OrderStatus, ZoneId)
- [ ] ServiceMeeting (falta ScheduledStart/End, MeetStatus)
- [ ] Booking Wizard paso 4 (disponibilidad real)

### 🔴 Pendiente
- [ ] Zonas/ServiceAreas (simplificado por ZIP)
- [ ] SoftReserves (anti-colisión)
- [ ] Recurrencia
- [ ] Notificaciones
- [ ] Skills/Equipment

---

## Fase 1 - MVP Funcional

**Duración estimada:** 2-3 semanas  
**Objetivo:** Booking completo funcional end-to-end

### 1.0 Estrategia de Zonas (Simplificada por ZIP)

**Concepto:**
- Zona = Grupo de códigos postales (ZIPs)
- ZIP es obligatorio en el booking
- El sistema determina la zona automáticamente por ZIP

**Ventajas:**
- ✅ Más preciso que "ciudad"
- ✅ No necesita mapas ni geocodificación
- ✅ Escala fácil (solo agregar ZIPs a una zona)
- ✅ Implementación rápida

**Flujo:**
1. Cliente ingresa dirección + ZIP (obligatorio)
2. Sistema busca ZIP en `ServiceAreaZips` → obtiene `ServiceAreaId`
3. Sistema filtra empleadas por `EmployeeServiceAreas`
4. Se guarda `ServiceAreaId` + `ZipCode` en la orden

### 1.1 Modelo de Datos - Nuevas Tablas

```sql
-- Zonas de servicio (simplificado por ZIP)
CREATE TABLE ServiceAreas (
    ServiceAreaId INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,         -- "Zona Norte", "Centro", etc.
    State VARCHAR(50),                  -- Estado/Provincia (opcional)
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- ZIPs por zona (cada ZIP solo puede estar en una zona)
CREATE TABLE ServiceAreaZips (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ServiceAreaId INT NOT NULL,
    ZipCode VARCHAR(10) NOT NULL,
    FOREIGN KEY (ServiceAreaId) REFERENCES ServiceAreas(ServiceAreaId),
    UNIQUE KEY (ZipCode)                -- Un ZIP = Una zona
);

-- Empleadas por zona (una empleada puede cubrir múltiples zonas)
CREATE TABLE EmployeeServiceAreas (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    EmployeeId INT NOT NULL,
    ServiceAreaId INT NOT NULL,
    IsPrimary TINYINT(1) DEFAULT 0,     -- Zona principal de la empleada
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId),
    FOREIGN KEY (ServiceAreaId) REFERENCES ServiceAreas(ServiceAreaId),
    UNIQUE KEY (EmployeeId, ServiceAreaId)
);

-- Reservas temporales (anti-colisión)
CREATE TABLE SoftReserves (
    SoftReserveId INT PRIMARY KEY AUTO_INCREMENT,
    SessionId VARCHAR(100),             -- Para usuarios no autenticados
    CustomerId INT NULL,                -- Para usuarios autenticados
    EmployeeId INT NOT NULL,
    ScheduledStart DATETIME NOT NULL,
    ScheduledEnd DATETIME NOT NULL,
    ExpiresAt DATETIME NOT NULL,        -- TTL (ej: 10 minutos)
    Status ENUM('Active', 'Converted', 'Expired') DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId),
    INDEX idx_employee_time (EmployeeId, ScheduledStart, ScheduledEnd),
    INDEX idx_expires (ExpiresAt, Status)
);
```

### 1.2 Alteraciones a Tablas Existentes

```sql
-- Employees: agregar zona principal
ALTER TABLE Employees 
ADD COLUMN PrimaryServiceAreaId INT NULL,
ADD FOREIGN KEY (PrimaryServiceAreaId) REFERENCES ServiceAreas(ServiceAreaId);

-- EmployeeSchedules: mejorar disponibilidad
ALTER TABLE EmployeeSchedules
ADD COLUMN StartTime TIME NOT NULL DEFAULT '08:00:00',
ADD COLUMN EndTime TIME NOT NULL DEFAULT '18:00:00',
ADD COLUMN BufferMinutes INT NOT NULL DEFAULT 30;

-- ServiceTypes: agregar tiempo base
ALTER TABLE ServiceTypes
ADD COLUMN EstimatedMinutes INT NOT NULL DEFAULT 60,
ADD COLUMN Description TEXT;

-- ServiceOrders: agregar campos faltantes
ALTER TABLE ServiceOrders
ADD COLUMN ServiceAreaId INT NULL,
ADD COLUMN ZipCode VARCHAR(10),
ADD COLUMN PaymentStatus ENUM('Pending', 'Paid', 'Failed', 'Refunded') DEFAULT 'Pending',
ADD COLUMN OrderStatus ENUM('Draft', 'Confirmed', 'InProgress', 'Completed', 'Cancelled') DEFAULT 'Draft',
ADD COLUMN RecurrenceType ENUM('Once', 'Weekly', 'Biweekly', 'Monthly') DEFAULT 'Once',
ADD COLUMN Source ENUM('Web', 'Admin', 'Phone') DEFAULT 'Web',
ADD FOREIGN KEY (ServiceAreaId) REFERENCES ServiceAreas(ServiceAreaId);

-- ServiceMeeting: agregar campos operativos
ALTER TABLE ServiceMeeting
ADD COLUMN ScheduledStart DATETIME,
ADD COLUMN ScheduledEnd DATETIME,
ADD COLUMN ActualStart DATETIME NULL,
ADD COLUMN ActualEnd DATETIME NULL,
ADD COLUMN AssignedEmployeeId INT NULL,
ADD COLUMN MeetStatus ENUM('Scheduled', 'Assigned', 'OnTheWay', 'InProgress', 'Completed', 'Cancelled', 'NoShow') DEFAULT 'Scheduled',
ADD COLUMN Notes TEXT,
ADD FOREIGN KEY (AssignedEmployeeId) REFERENCES Employees(EmployeeId);

-- ServiceContactsInfo: agregar zona
ALTER TABLE ServiceContactsInfo
ADD COLUMN ZipCode VARCHAR(10),
ADD COLUMN ServiceAreaId INT NULL,
ADD FOREIGN KEY (ServiceAreaId) REFERENCES ServiceAreas(ServiceAreaId);
```

### 1.3 Tareas de Implementación

| # | Tarea | Archivo(s) | Estado |
|---|-------|------------|--------|
| 1.1 | Crear migraciones SQL | init-db/ | ⬜ |
| 1.2 | Crear modelos C# para nuevas tablas | JempSoft.Core/Models/ | ⬜ |
| 1.3 | Actualizar DbContext | JempSoft.Core/Data/ | ⬜ |
| 1.4 | CRUD ServiceAreas (Admin) | netcore/Controllers/ + Views/ | ⬜ |
| 1.5 | CRUD ServiceAreaZips (Admin) | netcore/Controllers/ + Views/ | ⬜ |
| 1.6 | UI asignar zonas a empleadas | netcore/Views/Employees/ | ⬜ |
| 1.7 | Mejorar UI Edit CleaningPlaces | netcore/Views/CleaningPlaces/ | 🟡 |
| 1.8 | Mejorar UI Edit CleaningPlaceRooms | netcore/Views/CleaningPlaceRooms/ | ⬜ |
| 1.9 | Actualizar Booking Wizard - ZIP obligatorio | EService.Web/Views/Booking/ | ⬜ |
| 1.10 | Servicio GetServiceAreaByZip | JempSoft.Applications/ | ⬜ |
| 1.11 | Servicio SoftReserve | JempSoft.Applications/ | ⬜ |
| 1.12 | Disponibilidad filtrada por zona | JempSoft.Applications/Book/ | ⬜ |
| 1.13 | Booking Wizard paso 4 completo | EService.Web/ | ⬜ |
| 1.14 | Checkout con SoftReserve→Confirmed | EService.Web/ | ⬜ |

---

## Fase 2 - Operación

**Duración estimada:** 2-3 semanas  
**Objetivo:** Panel de operación funcional

### 2.1 Tareas

| # | Tarea | Descripción | Estado |
|---|-------|-------------|--------|
| 2.1 | Dashboard operativo | Calendario día/semana con citas | ⬜ |
| 2.2 | Vista mapa de calor | Servicios por zona | ⬜ |
| 2.3 | Check-In/Check-Out empleada | Con timestamps reales | ⬜ |
| 2.4 | Estados de servicio | Botones para cambiar MeetStatus | ⬜ |
| 2.5 | Asignación manual | Dispatcher asigna empleada | ⬜ |
| 2.6 | Métricas básicas | Ocupación, servicios/día | ⬜ |
| 2.7 | Historial por cliente | Ver órdenes anteriores | ⬜ |
| 2.8 | Historial por empleada | Ver servicios realizados | ⬜ |

---

## Fase 3 - Avanzado

**Duración estimada:** 3-4 semanas  
**Objetivo:** Features avanzados y optimización

### 3.1 Recurrencia

```sql
-- Tabla para reglas de recurrencia
CREATE TABLE RecurrenceRules (
    RecurrenceRuleId INT PRIMARY KEY AUTO_INCREMENT,
    ServiceOrderId BIGINT NOT NULL,
    Frequency ENUM('Weekly', 'Biweekly', 'Monthly') NOT NULL,
    DayOfWeek INT, -- 0=Domingo, 1=Lunes, etc.
    PreferredTime TIME,
    DiscountPercent DECIMAL(5,2) DEFAULT 0,
    NextGenerationDate DATE,
    IsActive TINYINT(1) DEFAULT 1,
    FOREIGN KEY (ServiceOrderId) REFERENCES ServiceOrders(Id)
);
```

### 3.2 Multiplicadores de Precio

```sql
CREATE TABLE PricingMultipliers (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(50) NOT NULL,
    MultiplierType ENUM('SquareMeters', 'DirtLevel', 'Pets', 'FirstTime', 'Recurrence') NOT NULL,
    MinValue DECIMAL(10,2),
    MaxValue DECIMAL(10,2),
    Multiplier DECIMAL(5,2) NOT NULL DEFAULT 1.0,
    IsActive TINYINT(1) DEFAULT 1
);
```

### 3.3 Equipment/Skills

```sql
CREATE TABLE Equipment (
    EquipmentId INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    IsActive TINYINT(1) DEFAULT 1
);

CREATE TABLE ServiceTypeEquipment (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ServiceTypeId INT NOT NULL,
    EquipmentId INT NOT NULL,
    IsRequired TINYINT(1) DEFAULT 1,
    FOREIGN KEY (ServiceTypeId) REFERENCES ServiceTypes(ServiceTypeId),
    FOREIGN KEY (EquipmentId) REFERENCES Equipment(EquipmentId)
);

CREATE TABLE EmployeeEquipment (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    EmployeeId INT NOT NULL,
    EquipmentId INT NOT NULL,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId),
    FOREIGN KEY (EquipmentId) REFERENCES Equipment(EquipmentId)
);
```

### 3.4 Otras Tareas Fase 3

| # | Tarea | Estado |
|---|-------|--------|
| 3.1 | Sistema de recurrencia completo | ⬜ |
| 3.2 | Generación automática de citas futuras | ⬜ |
| 3.3 | Descuentos por recurrencia | ⬜ |
| 3.4 | Multiplicadores de precio | ⬜ |
| 3.5 | Equipment/Skills matching | ⬜ |
| 3.6 | Notificaciones email (24h, 2h antes) | ⬜ |
| 3.7 | Políticas de cancelación | ⬜ |
| 3.8 | Reembolsos parciales/totales | ⬜ |
| 3.9 | Calificaciones/Reviews | ⬜ |
| 3.10 | Geofence Check-In (opcional) | ⬜ |

---

## Modelo de Datos

### Diagrama de Relaciones (Simplificado)

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  ServiceAreas   │────▶│ ServiceAreaZips  │     │   Equipment     │
│  (Zonas)        │     │ (ZIPs por zona)  │     │                 │
└────────┬────────┘     └──────────────────┘     └────────┬────────┘
         │                                                │
         ▼                                                ▼
┌─────────────────┐                              ┌─────────────────┐
│EmployeeService  │◀────────────────────────────▶│ServiceType      │
│    Areas        │                              │  Equipment      │
└────────┬────────┘                              └────────┬────────┘
         │                                                │
         ▼                                                ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Employees     │────▶│EmployeeSchedules │     │  ServiceTypes   │
│                 │     │ (Disponibilidad) │     │  (Servicios)    │
└────────┬────────┘     └──────────────────┘     └────────┬────────┘
         │                                                │
         ▼                                                ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  SoftReserves   │     │  ServiceMeeting  │◀────│  ServiceOrders  │
│  (Bloqueo temp) │     │  (Citas)         │     │  (Órdenes)      │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

### Flujo de Booking

```
1. Cliente ingresa dirección + ZIP
         │
         ▼
2. Sistema determina ServiceAreaId por ZIP
         │
         ▼
3. Cliente selecciona: CleaningPlace → Room → ServiceType → Extras
         │
         ▼
4. Sistema calcula: Duración + Precio estimado
         │
         ▼
5. Cliente elige fecha → Sistema muestra slots disponibles
   (Filtra por: Zona + Disponibilidad + Skills)
         │
         ▼
6. Cliente selecciona slot → SoftReserve (TTL 10 min)
         │
         ▼
7. Checkout/Pago
         │
         ├──▶ Éxito: SoftReserve → Confirmed, Crear ServiceMeeting
         │
         └──▶ Fallo/Timeout: SoftReserve expira, libera slot
```

---

## Checklist de Progreso

### Fase 1 - MVP
- [ ] 1.1 Migraciones SQL ejecutadas
- [ ] 1.2 Modelos C# creados
- [ ] 1.3 DbContext actualizado
- [ ] 1.4 CRUD ServiceAreas funcionando
- [ ] 1.5 CRUD ServiceAreaZips funcionando
- [ ] 1.6 Empleadas con zonas asignadas
- [ ] 1.7 UI Edit CleaningPlaces mejorada
- [ ] 1.8 UI Edit CleaningPlaceRooms mejorada
- [ ] 1.9 Booking Wizard con ZIP obligatorio
- [ ] 1.10 Servicio GetServiceAreaByZip
- [ ] 1.11 Servicio SoftReserve implementado
- [ ] 1.12 Disponibilidad filtrada por zona
- [ ] 1.13 Booking Wizard paso 4 completo
- [ ] 1.14 Checkout con SoftReserve funcional
- [ ] 1.15 Pruebas end-to-end booking

### Fase 2 - Operación
- [ ] 2.1 Dashboard operativo
- [ ] 2.2 Calendario de citas
- [ ] 2.3 Check-In/Check-Out
- [ ] 2.4 Estados de servicio
- [ ] 2.5 Asignación manual
- [ ] 2.6 Métricas básicas

### Fase 3 - Avanzado
- [ ] 3.1 Recurrencia
- [ ] 3.2 Multiplicadores de precio
- [ ] 3.3 Equipment/Skills
- [ ] 3.4 Notificaciones
- [ ] 3.5 Cancelaciones/Reembolsos

---

## Notas de Implementación

### Configuración de Entorno
- **MySQL Docker:** `savedbythemaid-mysql`
- **DB:** `SavedByMaidIM`
- **Usuario:** `appuser` / `App@123456`
- **Admin:** `super@admin.com` / `123456`

### Comandos Útiles

```bash
# Ejecutar migraciones
docker exec savedbythemaid-mysql mysql -uappuser -pApp@123456 SavedByMaidIM < migration.sql

# Ver tablas
docker exec savedbythemaid-mysql mysql -uappuser -pApp@123456 SavedByMaidIM -e "SHOW TABLES;"

# Compilar solución
dotnet build JempSoft.sln

# Ejecutar aplicación (Cliente + Admin en un solo proyecto)
dotnet run --project netcore --urls "http://localhost:5001"

# URLs disponibles:
# - http://localhost:5001/          → Portal Cliente
# - http://localhost:5001/admin     → Panel Admin
```

---

**Última actualización:** 6 de Enero 2026
