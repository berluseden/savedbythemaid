import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object para el Panel de Administración
 * ============================================
 * 
 * Proporciona métodos para interactuar con todas las secciones del panel admin:
 * - Login/Autenticación
 * - Dashboard
 * - Gestión de Reservas (Bookings)
 * - Empleados
 * - Servicios
 */
export class AdminPage extends BasePage {
  // =====================================================
  // LOCATORS - Login
  // =====================================================
  readonly loginEmail: Locator;
  readonly loginPassword: Locator;
  readonly loginButton: Locator;
  readonly loginError: Locator;
  readonly rememberMeCheckbox: Locator;

  // =====================================================
  // LOCATORS - Navigation (Sidebar)
  // =====================================================
  readonly dashboardLink: Locator;
  readonly bookingsLink: Locator;
  readonly employeesLink: Locator;
  readonly servicesLink: Locator;
  readonly pricingLink: Locator;
  readonly usersLink: Locator;
  readonly serviceAreasLink: Locator;
  readonly signOutButton: Locator;
  readonly mobileMenuButton: Locator;

  // =====================================================
  // LOCATORS - Dashboard
  // =====================================================
  readonly dashboardHeading: Locator;
  readonly totalBookingsCard: Locator;
  readonly pendingBookingsCard: Locator;
  readonly recentBookingsTable: Locator;

  // =====================================================
  // LOCATORS - Bookings Page
  // =====================================================
  readonly bookingsHeading: Locator;
  readonly searchInput: Locator;
  readonly statusFilter: Locator;
  readonly dateFilter: Locator;
  readonly bookingsTable: Locator;
  readonly bookingRows: Locator;
  readonly noResultsMessage: Locator;
  readonly paginationPrev: Locator;
  readonly paginationNext: Locator;

  // =====================================================
  // LOCATORS - Status Filter Cards
  // =====================================================
  readonly pendingReviewCard: Locator;
  readonly confirmedCard: Locator;
  readonly inProgressCard: Locator;
  readonly completedCard: Locator;
  readonly cancelledCard: Locator;

  // =====================================================
  // LOCATORS - Booking Actions
  // =====================================================
  readonly viewDetailsButton: Locator;
  readonly approveButton: Locator;
  readonly rejectButton: Locator;
  readonly startServiceButton: Locator;
  readonly completeServiceButton: Locator;

  // =====================================================
  // LOCATORS - Booking Detail Modal
  // =====================================================
  readonly detailModal: Locator;
  readonly modalCloseButton: Locator;
  readonly modalConfirmationNumber: Locator;
  readonly modalCustomerName: Locator;
  readonly modalConfirmButton: Locator;
  readonly modalCancelButton: Locator;
  readonly modalStartButton: Locator;
  readonly modalCompleteButton: Locator;

  // =====================================================
  // LOCATORS - User Menu
  // =====================================================
  readonly userMenuButton: Locator;
  readonly userMenuDropdown: Locator;
  readonly profileLink: Locator;

  constructor(page: Page) {
    super(page);

    // Login page locators
    this.loginEmail = page.locator('input#email');
    this.loginPassword = page.locator('input#password');
    this.loginButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
    this.loginError = page.locator('.bg-red-50, [role="alert"]');
    this.rememberMeCheckbox = page.getByLabel(/Remember me|Recordarme/i);

    // Navigation locators
    this.dashboardLink = page.getByRole('link', { name: /Dashboard/i });
    this.bookingsLink = page.getByRole('link', { name: /Bookings|Reservas/i });
    this.employeesLink = page.getByRole('link', { name: /Employees|Empleados/i });
    // Use exact match to avoid "Additional Services" match
    this.servicesLink = page.getByRole('link', { name: 'Services', exact: true });
    this.pricingLink = page.getByRole('link', { name: /Pricing|Precios/i });
    this.usersLink = page.getByRole('link', { name: /Users|Usuarios/i });
    this.serviceAreasLink = page.getByRole('link', { name: /Service Areas|Zonas/i });
    this.signOutButton = page.getByRole('button', { name: /Sign Out|Cerrar sesión/i });
    this.mobileMenuButton = page.locator('button').filter({ has: page.locator('svg.lucide-menu') });

    // Dashboard locators
    this.dashboardHeading = page.getByRole('heading', { name: /Dashboard/i });
    this.totalBookingsCard = page.getByText(/Reservas Totales|Total Bookings/i);
    this.pendingBookingsCard = page.getByText(/Pendientes|Pending/i);
    this.recentBookingsTable = page.locator('table').first();

    // Bookings page locators
    this.bookingsHeading = page.getByRole('heading', { name: /Reservas|Bookings/i });
    this.searchInput = page.getByPlaceholder(/Buscar por nombre, ID|Search/i);
    this.statusFilter = page.locator('select').filter({ hasText: /Todos los estados|All/i });
    this.dateFilter = page.locator('input[type="date"]');
    this.bookingsTable = page.locator('table');
    this.bookingRows = page.locator('table tbody tr');
    this.noResultsMessage = page.getByText(/No se encontraron reservas|No bookings found/i);
    this.paginationPrev = page.locator('button').filter({ has: page.locator('svg.lucide-chevron-left') });
    this.paginationNext = page.locator('button').filter({ has: page.locator('svg.lucide-chevron-right') });

    // Status filter cards
    this.pendingReviewCard = page.getByRole('button').filter({ hasText: /Por Aprobar|Pending Review/i });
    this.confirmedCard = page.getByRole('button').filter({ hasText: /Confirmada|Confirmed/i });
    this.inProgressCard = page.getByRole('button').filter({ hasText: /En Progreso|In Progress/i });
    this.completedCard = page.getByRole('button').filter({ hasText: /Completada|Completed/i });
    this.cancelledCard = page.getByRole('button').filter({ hasText: /Cancelada|Cancelled/i });

    // Booking action buttons (in table row)
    this.viewDetailsButton = page.locator('button[title="Ver detalles"]');
    this.approveButton = page.locator('button[title="Aprobar y Confirmar"]');
    this.rejectButton = page.locator('button[title="Rechazar"]');
    this.startServiceButton = page.locator('button[title="Iniciar"]');
    this.completeServiceButton = page.locator('button[title="Completar"]');

    // Detail modal locators
    this.detailModal = page.locator('.fixed.inset-0.z-50');
    this.modalCloseButton = page.locator('.fixed.inset-0 button').filter({ has: page.locator('svg.lucide-x') });
    this.modalConfirmationNumber = page.locator('.fixed.inset-0 h2');
    this.modalCustomerName = page.locator('.fixed.inset-0').getByText(/Sin nombre|[A-Z][a-z]+/);
    this.modalConfirmButton = page.getByRole('button', { name: /Confirmar Reserva|Confirm/i });
    this.modalCancelButton = page.getByRole('button', { name: /Cancelar Reserva|Cancel Booking/i });
    this.modalStartButton = page.getByRole('button', { name: /Iniciar Servicio|Start Service/i });
    this.modalCompleteButton = page.getByRole('button', { name: /Marcar como Completado|Mark as Completed/i });

    // User menu
    this.userMenuButton = page.locator('button').filter({ has: page.locator('.bg-\\[\\#00205B\\].rounded-full') });
    this.userMenuDropdown = page.locator('.absolute.right-0.mt-2.w-48');
    this.profileLink = page.getByRole('link', { name: /My Profile|Mi Perfil/i });
  }

  // =====================================================
  // MÉTODOS - Autenticación
  // =====================================================

  /**
   * Realiza login con las credenciales proporcionadas
   */
  async login(email: string, password: string): Promise<void> {
    await this.loginEmail.waitFor({ state: 'visible', timeout: 10000 });
    await this.loginEmail.fill(email);
    await this.loginPassword.fill(password);
    await this.loginButton.click();
    
    // Esperar a que el login sea procesado
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Verifica que el login haya sido exitoso
   */
  async expectLoginSuccess(): Promise<void> {
    await expect(this.page).toHaveURL(/\/admin/, { timeout: 15000 });
  }

  /**
   * Verifica que aparezca un error de login
   */
  async expectLoginError(): Promise<void> {
    await expect(this.loginError).toBeVisible({ timeout: 5000 });
  }

  /**
   * Cierra sesión del admin
   */
  async logout(): Promise<void> {
    await this.signOutButton.click();
    await expect(this.page).toHaveURL(/\/login/);
  }

  // =====================================================
  // MÉTODOS - Navegación
  // =====================================================

  /**
   * Navega a la página de login
   */
  async gotoLogin(): Promise<void> {
    await this.goto('/login');
  }

  /**
   * Navega al Dashboard de admin
   */
  async navigateToDashboard(): Promise<void> {
    await this.dashboardLink.click();
    await expect(this.dashboardHeading).toBeVisible();
  }

  /**
   * Navega a la página de reservas
   */
  async navigateToBookings(): Promise<void> {
    await this.bookingsLink.click();
    await expect(this.bookingsHeading).toBeVisible({ timeout: 10000 });
    await this.waitForLoading();
  }

  /**
   * Navega a la página de empleados
   */
  async navigateToEmployees(): Promise<void> {
    await this.employeesLink.click();
    await expect(this.page.getByRole('heading', { name: /Employees|Empleados/i })).toBeVisible();
  }

  /**
   * Navega a la página de servicios
   */
  async navigateToServices(): Promise<void> {
    await this.servicesLink.click();
    await expect(this.page.getByRole('heading', { name: /Services|Servicios/i })).toBeVisible();
  }

  /**
   * Navega a la página de usuarios
   */
  async navigateToUsers(): Promise<void> {
    await this.usersLink.click();
    await expect(this.page.getByRole('heading', { name: /Users|Usuarios/i })).toBeVisible();
  }

  // =====================================================
  // MÉTODOS - Gestión de Reservas
  // =====================================================

  /**
   * Filtra reservas por estado usando el dropdown
   */
  async filterByStatus(status: string): Promise<void> {
    await this.statusFilter.waitFor({ state: 'visible' });
    await this.statusFilter.selectOption({ label: status });
    await this.waitForLoading();
  }

  /**
   * Filtra reservas por estado usando las tarjetas de filtro
   */
  async filterByStatusCard(status: 'PendingReview' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled'): Promise<void> {
    const statusCards: Record<string, Locator> = {
      'PendingReview': this.pendingReviewCard,
      'Confirmed': this.confirmedCard,
      'InProgress': this.inProgressCard,
      'Completed': this.completedCard,
      'Cancelled': this.cancelledCard,
    };
    
    await statusCards[status].click();
    await this.waitForLoading();
  }

  /**
   * Busca reservas por término de búsqueda
   */
  async searchBookings(searchTerm: string): Promise<void> {
    await this.searchInput.waitFor({ state: 'visible' });
    await this.searchInput.fill(searchTerm);
    // La búsqueda es en tiempo real, esperar un momento
    await this.page.waitForTimeout(500);
    await this.waitForLoading();
  }

  /**
   * Filtra reservas por fecha
   */
  async filterByDate(date: string): Promise<void> {
    await this.dateFilter.fill(date);
    await this.waitForLoading();
  }

  /**
   * Obtiene el número de filas de reservas visibles
   */
  async getBookingsCount(): Promise<number> {
    await this.bookingsTable.waitFor({ state: 'visible' });
    return await this.bookingRows.count();
  }

  /**
   * Obtiene los datos de una fila de reserva por índice
   */
  async getBookingRowData(index: number): Promise<{
    confirmationNumber: string;
    customer: string;
    status: string;
    total: string;
  }> {
    const row = this.bookingRows.nth(index);
    const cells = row.locator('td');
    
    return {
      confirmationNumber: await cells.nth(0).textContent() || '',
      customer: await cells.nth(1).locator('p').first().textContent() || '',
      status: await cells.nth(4).locator('span').textContent() || '',
      total: await cells.nth(6).textContent() || '',
    };
  }

  /**
   * Hace clic en Ver Detalles de una reserva por número de confirmación
   */
  async viewBookingDetails(confirmationNumber: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    await row.locator('button[title="Ver detalles"]').click();
    await expect(this.detailModal).toBeVisible();
  }

  /**
   * Aprueba una reserva por número de confirmación
   */
  async approveBooking(confirmationNumber: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    const approveBtn = row.locator('button[title="Aprobar y Confirmar"]');
    
    await expect(approveBtn).toBeVisible();
    await approveBtn.click();
    
    // Esperar actualización
    await this.waitForAPIResponse('/api/admin/orders');
    await this.waitForLoading();
  }

  /**
   * Cancela una reserva por número de confirmación
   */
  async cancelBooking(confirmationNumber: string, reason?: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    const rejectBtn = row.locator('button[title="Rechazar"]');
    
    await expect(rejectBtn).toBeVisible();
    await rejectBtn.click();
    
    // Si hay un modal de confirmación con campo de razón
    const reasonInput = this.page.getByPlaceholder(/Motivo|Reason/i);
    if (await reasonInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      if (reason) {
        await reasonInput.fill(reason);
      }
      await this.page.getByRole('button', { name: /Confirmar|Confirm/i }).click();
    }
    
    // Esperar actualización
    await this.waitForAPIResponse('/api/admin/orders');
    await this.waitForLoading();
  }

  /**
   * Asigna un empleado a una reserva
   */
  async assignEmployee(confirmationNumber: string, employeeName: string): Promise<void> {
    // Primero abrir detalles de la reserva
    await this.viewBookingDetails(confirmationNumber);
    
    // Buscar botón de asignar en la sección de citas
    const assignButton = this.page.getByRole('button', { name: /Asignar/i });
    await assignButton.click();
    
    // Seleccionar empleado del dropdown
    const employeeSelect = this.page.locator('select').filter({ hasText: /Seleccionar empleado/i });
    await employeeSelect.selectOption({ label: employeeName });
    
    // Esperar actualización
    await this.waitForLoading();
    
    // Cerrar modal
    await this.closeDetailModal();
  }

  /**
   * Inicia el servicio de una reserva confirmada
   */
  async startService(confirmationNumber: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    const startBtn = row.locator('button[title="Iniciar"]');
    
    await expect(startBtn).toBeVisible();
    await startBtn.click();
    
    await this.waitForAPIResponse('/api/admin/orders');
    await this.waitForLoading();
  }

  /**
   * Marca una reserva como completada
   */
  async completeService(confirmationNumber: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    const completeBtn = row.locator('button[title="Completar"]');
    
    await expect(completeBtn).toBeVisible();
    await completeBtn.click();
    
    await this.waitForAPIResponse('/api/admin/orders');
    await this.waitForLoading();
  }

  /**
   * Cierra el modal de detalles
   */
  async closeDetailModal(): Promise<void> {
    const closeBtn = this.page.getByRole('button', { name: /Cerrar|Close/i });
    await closeBtn.click();
    await expect(this.detailModal).not.toBeVisible();
  }

  // =====================================================
  // MÉTODOS - Verificaciones
  // =====================================================

  /**
   * Verifica que el panel de admin esté visible
   */
  async expectAdminPanelVisible(): Promise<void> {
    await expect(this.page.getByText(/Admin Panel/i)).toBeVisible();
  }

  /**
   * Verifica que una reserva tenga un estado específico
   */
  async expectBookingStatus(confirmationNumber: string, expectedStatus: string): Promise<void> {
    const row = this.page.locator('tr').filter({ hasText: confirmationNumber });
    const statusBadge = row.locator('span').filter({ hasText: new RegExp(expectedStatus, 'i') });
    await expect(statusBadge).toBeVisible();
  }

  /**
   * Verifica que no haya resultados en la tabla
   */
  async expectNoResults(): Promise<void> {
    await expect(this.noResultsMessage).toBeVisible();
  }

  /**
   * Verifica que la tabla tenga un número específico de filas
   */
  async expectBookingsCount(count: number): Promise<void> {
    const actualCount = await this.getBookingsCount();
    expect(actualCount).toBe(count);
  }

  /**
   * Verifica que el usuario actual sea admin
   */
  async expectIsAdmin(): Promise<void> {
    // Si puede ver el sidebar de admin, es admin
    await expect(this.dashboardLink).toBeVisible();
    await expect(this.bookingsLink).toBeVisible();
  }

  /**
   * Verifica acceso denegado (redirección a login o mensaje de error)
   */
  async expectAccessDenied(): Promise<void> {
    await expect(this.page).toHaveURL(/\/(login|unauthorized|403)/);
  }

  // =====================================================
  // MÉTODOS - Utilidades
  // =====================================================

  /**
   * Espera a que se carguen las reservas
   */
  async waitForBookingsToLoad(): Promise<void> {
    await this.waitForLoading();
    // Esperar que la tabla o el mensaje de vacío aparezcan
    await Promise.race([
      this.bookingsTable.waitFor({ state: 'visible', timeout: 10000 }),
      this.noResultsMessage.waitFor({ state: 'visible', timeout: 10000 }),
    ]);
  }

  /**
   * Obtiene el contador de un estado de la tarjeta de filtro
   */
  async getStatusCount(status: 'PendingReview' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled'): Promise<number> {
    const statusCards: Record<string, Locator> = {
      'PendingReview': this.pendingReviewCard,
      'Confirmed': this.confirmedCard,
      'InProgress': this.inProgressCard,
      'Completed': this.completedCard,
      'Cancelled': this.cancelledCard,
    };
    
    const card = statusCards[status];
    const countText = await card.locator('p.text-2xl').textContent();
    return parseInt(countText || '0', 10);
  }
}
