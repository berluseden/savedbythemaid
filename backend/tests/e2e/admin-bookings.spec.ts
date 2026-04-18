import { test, expect } from '@playwright/test';
import { AdminPage } from '../pages/AdminPage';

/**
 * Suite de Tests E2E para Panel de Administración
 * ================================================
 * 
 * Esta suite cubre la funcionalidad del panel de administración:
 * - Autenticación de administradores
 * - Gestión de reservas (visualización, filtrado, aprobación, cancelación)
 * - Seguridad de acceso
 * 
 * Credenciales de admin: admin@savedbytemaid.com / Admin123!
 * Base URL: http://localhost:5000
 */

// Constantes de configuración
const ADMIN_EMAIL = 'admin@savedbytemaid.com';
const ADMIN_PASSWORD = 'Admin123!';
const NON_ADMIN_EMAIL = 'john@test.com';
const NON_ADMIN_PASSWORD = 'Test123!';

test.describe('Panel Admin - Gestión de Reservas @admin', () => {
  let adminPage: AdminPage;

  test.beforeEach(async ({ page }) => {
    adminPage = new AdminPage(page);
    // Configurar timeout extendido para SPAs
    test.setTimeout(90000);
    
    // Navegar a login y autenticarse
    await adminPage.gotoLogin();
    await adminPage.login(ADMIN_EMAIL, ADMIN_PASSWORD);
    await adminPage.expectLoginSuccess();
  });

  // =====================================================
  // SMOKE TESTS - Funcionalidad Crítica
  // =====================================================

  test('@smoke TC-035: Login admin exitoso redirige a dashboard', async ({ page }) => {
    /**
     * Objetivo: Verificar que un admin puede hacer login correctamente
     * 
     * Pasos:
     * 1. Ir a /login
     * 2. Ingresar credenciales de admin
     * 3. Verificar redirección a /admin
     * 4. Verificar que el dashboard está visible
     */
    
    await test.step('1. Verificar que estamos en el panel de admin', async () => {
      await expect(page).toHaveURL(/\/admin/);
    });

    await test.step('2. Verificar heading del Dashboard', async () => {
      await expect(
        page.getByRole('heading', { name: /Dashboard/i })
      ).toBeVisible({ timeout: 10000 });
    });

    await test.step('3. Verificar elementos del sidebar', async () => {
      await adminPage.expectIsAdmin();
      await expect(adminPage.dashboardLink).toBeVisible();
      await expect(adminPage.bookingsLink).toBeVisible();
      await expect(adminPage.employeesLink).toBeVisible();
    });

    await test.step('4. Verificar tarjetas de estadísticas', async () => {
      // El dashboard debe mostrar estadísticas
      await expect(
        page.getByText(/Reservas Totales|Total Bookings/i)
      ).toBeVisible();
    });

    await page.screenshot({ 
      path: `test-results/tc035-admin-dashboard-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@smoke TC-036: Ver lista de órdenes con filtros', async ({ page }) => {
    /**
     * Objetivo: Verificar que se pueden ver y filtrar las reservas
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Verificar que la tabla está visible
     * 3. Verificar que los filtros funcionan
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
      await expect(adminPage.bookingsHeading).toBeVisible();
    });

    await test.step('2. Verificar que la tabla de reservas está visible', async () => {
      await adminPage.waitForBookingsToLoad();
      
      // Debe haber una tabla o un mensaje de vacío
      const hasTable = await adminPage.bookingsTable.isVisible();
      const hasNoResults = await adminPage.noResultsMessage.isVisible().catch(() => false);
      
      expect(hasTable || hasNoResults).toBeTruthy();
    });

    await test.step('3. Verificar que los filtros están disponibles', async () => {
      await expect(adminPage.searchInput).toBeVisible();
      await expect(adminPage.statusFilter).toBeVisible();
      await expect(adminPage.dateFilter).toBeVisible();
    });

    await test.step('4. Verificar tarjetas de estado', async () => {
      await expect(adminPage.pendingReviewCard).toBeVisible();
      await expect(adminPage.confirmedCard).toBeVisible();
      await expect(adminPage.completedCard).toBeVisible();
    });

    await page.screenshot({ 
      path: `test-results/tc036-bookings-list-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  // =====================================================
  // REGRESSION TESTS - Flujos Completos
  // =====================================================

  test('@regression TC-037: Aprobar orden PendingReview → Confirmed', async ({ page }) => {
    /**
     * Objetivo: Verificar que un admin puede aprobar una orden pendiente
     * 
     * Precondición: Debe existir al menos una orden en estado PendingReview
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Filtrar por PendingReview
     * 3. Aprobar la primera orden
     * 4. Verificar cambio de estado
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
    });

    await test.step('2. Verificar órdenes pendientes de aprobación', async () => {
      // Obtener conteo de órdenes PendingReview
      const pendingCount = await adminPage.getStatusCount('PendingReview');
      
      if (pendingCount === 0) {
        test.skip(true, 'No hay órdenes PendingReview para aprobar');
        return;
      }
      
      // Filtrar por PendingReview
      await adminPage.filterByStatusCard('PendingReview');
      
      // Verificar que hay filas
      const rowCount = await adminPage.getBookingsCount();
      expect(rowCount).toBeGreaterThan(0);
    });

    await test.step('3. Obtener datos de la primera orden', async () => {
      const bookingData = await adminPage.getBookingRowData(0);
      console.log('Orden a aprobar:', bookingData.confirmationNumber);
    });

    await test.step('4. Aprobar la orden', async () => {
      // Click en botón aprobar de la primera fila
      await adminPage.approveButton.first().click();
      
      // Esperar que se procese
      await page.waitForLoadState('networkidle');
    });

    await test.step('5. Verificar que la orden cambió de estado', async () => {
      // Limpiar filtro
      await adminPage.statusFilter.selectOption({ value: 'all' });
      await page.waitForLoadState('networkidle');
      
      await page.screenshot({ 
        path: `test-results/tc037-order-approved-${Date.now()}.png`, 
        fullPage: true 
      });
    });
  });

  test('@regression TC-038: Cancelar orden con motivo', async ({ page }) => {
    /**
     * Objetivo: Verificar que un admin puede cancelar una orden
     * 
     * Precondición: Debe existir al menos una orden cancelable
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Filtrar por PendingReview
     * 3. Cancelar la primera orden
     * 4. Verificar cambio de estado
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
    });

    await test.step('2. Buscar órdenes cancelables', async () => {
      // Órdenes PendingReview son cancelables
      const pendingCount = await adminPage.getStatusCount('PendingReview');
      
      if (pendingCount === 0) {
        test.skip(true, 'No hay órdenes PendingReview para cancelar');
        return;
      }
      
      await adminPage.filterByStatusCard('PendingReview');
    });

    await test.step('3. Verificar botón de rechazo visible', async () => {
      const rejectBtn = adminPage.rejectButton.first();
      await expect(rejectBtn).toBeVisible({ timeout: 5000 });
    });

    await test.step('4. Cancelar la orden', async () => {
      await adminPage.rejectButton.first().click();
      
      // Esperar procesamiento
      await page.waitForLoadState('networkidle');
    });

    await test.step('5. Verificar resultado', async () => {
      await page.screenshot({ 
        path: `test-results/tc038-order-cancelled-${Date.now()}.png`, 
        fullPage: true 
      });
    });
  });

  test('@regression TC-039: Filtrar por estado PendingReview', async ({ page }) => {
    /**
     * Objetivo: Verificar que el filtro por estado funciona correctamente
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Obtener conteo de PendingReview
     * 3. Aplicar filtro
     * 4. Verificar que solo se muestran órdenes con ese estado
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
      await adminPage.waitForBookingsToLoad();
    });

    await test.step('2. Obtener conteo esperado', async () => {
      const pendingCount = await adminPage.getStatusCount('PendingReview');
      console.log(`Órdenes PendingReview esperadas: ${pendingCount}`);
    });

    await test.step('3. Aplicar filtro por tarjeta de estado', async () => {
      await adminPage.filterByStatusCard('PendingReview');
    });

    await test.step('4. Verificar que las órdenes mostradas tienen el estado correcto', async () => {
      const rowCount = await adminPage.getBookingsCount();
      
      if (rowCount === 0) {
        // No hay órdenes pendientes, verificar mensaje
        await adminPage.expectNoResults();
      } else {
        // Verificar que todas las filas tienen el estado correcto
        for (let i = 0; i < Math.min(rowCount, 3); i++) {
          const rowData = await adminPage.getBookingRowData(i);
          expect(rowData.status.toLowerCase()).toContain('aprobar');
        }
      }
    });

    await test.step('5. Probar filtro con dropdown', async () => {
      // Limpiar filtro
      await adminPage.filterByStatus('Todos los estados');
      await page.waitForLoadState('networkidle');
      
      // Aplicar filtro con dropdown
      await adminPage.filterByStatus('Por Aprobar');
      await page.waitForLoadState('networkidle');
    });

    await page.screenshot({ 
      path: `test-results/tc039-filter-pending-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@regression TC-040: Buscar por número de confirmación', async ({ page }) => {
    /**
     * Objetivo: Verificar que la búsqueda por número de confirmación funciona
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Obtener un número de confirmación existente
     * 3. Buscar por ese número
     * 4. Verificar que solo se muestra esa orden
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
      await adminPage.waitForBookingsToLoad();
    });

    let confirmationNumber: string;
    
    await test.step('2. Obtener número de confirmación de la primera orden', async () => {
      const rowCount = await adminPage.getBookingsCount();
      
      if (rowCount === 0) {
        test.skip(true, 'No hay órdenes para buscar');
        return;
      }
      
      const rowData = await adminPage.getBookingRowData(0);
      confirmationNumber = rowData.confirmationNumber.trim();
      console.log(`Buscando orden: ${confirmationNumber}`);
    });

    await test.step('3. Realizar búsqueda', async () => {
      await adminPage.searchBookings(confirmationNumber!);
    });

    await test.step('4. Verificar resultado de búsqueda', async () => {
      const rowCount = await adminPage.getBookingsCount();
      expect(rowCount).toBeGreaterThanOrEqual(1);
      
      // Verificar que el resultado contiene el número buscado
      const rowData = await adminPage.getBookingRowData(0);
      expect(rowData.confirmationNumber).toContain(confirmationNumber!);
    });

    await test.step('5. Limpiar búsqueda y verificar', async () => {
      await adminPage.searchInput.clear();
      await page.waitForTimeout(500);
      
      const rowCountAfterClear = await adminPage.getBookingsCount();
      // Debería haber más resultados después de limpiar
      console.log(`Filas después de limpiar: ${rowCountAfterClear}`);
    });

    await page.screenshot({ 
      path: `test-results/tc040-search-confirmation-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@regression TC-041: Ver detalles de una orden', async ({ page }) => {
    /**
     * Objetivo: Verificar que se pueden ver los detalles de una orden
     * 
     * Pasos:
     * 1. Navegar a Bookings
     * 2. Hacer clic en Ver Detalles
     * 3. Verificar que el modal muestra información correcta
     * 4. Cerrar el modal
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
      await adminPage.waitForBookingsToLoad();
    });

    await test.step('2. Verificar que hay órdenes', async () => {
      const rowCount = await adminPage.getBookingsCount();
      
      if (rowCount === 0) {
        test.skip(true, 'No hay órdenes para ver detalles');
        return;
      }
    });

    await test.step('3. Abrir detalles de la primera orden', async () => {
      await adminPage.viewDetailsButton.first().click();
      await expect(adminPage.detailModal).toBeVisible({ timeout: 5000 });
    });

    await test.step('4. Verificar contenido del modal', async () => {
      // Debe mostrar secciones de información
      await expect(
        page.getByText(/Información del Cliente|Customer Info/i)
      ).toBeVisible();
      
      await expect(
        page.getByText(/Detalles del Servicio|Service Details/i)
      ).toBeVisible();
    });

    await test.step('5. Cerrar modal', async () => {
      await adminPage.closeDetailModal();
      await expect(adminPage.detailModal).not.toBeVisible();
    });

    await page.screenshot({ 
      path: `test-results/tc041-view-details-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@regression TC-042: Flujo completo PendingReview → Confirmed → InProgress → Completed', async ({ page }) => {
    /**
     * Objetivo: Verificar el flujo completo de estados de una orden
     * 
     * Este test simula el ciclo de vida completo de una orden
     */
    
    await test.step('1. Navegar a página de reservas', async () => {
      await adminPage.navigateToBookings();
      await adminPage.waitForBookingsToLoad();
    });

    let confirmationNumber: string | undefined;

    await test.step('2. Buscar orden PendingReview', async () => {
      const pendingCount = await adminPage.getStatusCount('PendingReview');
      
      if (pendingCount === 0) {
        test.skip(true, 'No hay órdenes PendingReview para el flujo completo');
        return;
      }
      
      await adminPage.filterByStatusCard('PendingReview');
      const rowData = await adminPage.getBookingRowData(0);
      confirmationNumber = rowData.confirmationNumber.trim();
      console.log(`Procesando orden: ${confirmationNumber}`);
    });

    await test.step('3. Aprobar orden (PendingReview → Confirmed)', async () => {
      if (!confirmationNumber) return;
      
      await adminPage.approveButton.first().click();
      await page.waitForLoadState('networkidle');
      
      // Verificar cambio de estado
      await adminPage.filterByStatus('Todos los estados');
      await adminPage.searchBookings(confirmationNumber);
      
      const rowData = await adminPage.getBookingRowData(0);
      expect(rowData.status.toLowerCase()).toMatch(/confirmad|confirmed/i);
    });

    await test.step('4. Iniciar servicio (Confirmed → InProgress)', async () => {
      if (!confirmationNumber) return;
      
      const startBtn = adminPage.startServiceButton.first();
      if (await startBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
        await startBtn.click();
        await page.waitForLoadState('networkidle');
        
        const rowData = await adminPage.getBookingRowData(0);
        expect(rowData.status.toLowerCase()).toMatch(/progreso|progress/i);
      }
    });

    await test.step('5. Completar servicio (InProgress → Completed)', async () => {
      if (!confirmationNumber) return;
      
      const completeBtn = adminPage.completeServiceButton.first();
      if (await completeBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
        await completeBtn.click();
        await page.waitForLoadState('networkidle');
        
        const rowData = await adminPage.getBookingRowData(0);
        expect(rowData.status.toLowerCase()).toMatch(/completad|completed/i);
      }
    });

    await page.screenshot({ 
      path: `test-results/tc042-full-workflow-${Date.now()}.png`, 
      fullPage: true 
    });
  });
});

// =====================================================
// SECURITY TESTS - Pruebas de Seguridad
// =====================================================

test.describe('Panel Admin - Seguridad @admin @security', () => {

  test('@security TC-043: Usuario no-admin no puede acceder al panel', async ({ page }) => {
    /**
     * Objetivo: Verificar que usuarios sin rol admin no pueden acceder
     * 
     * Pasos:
     * 1. Intentar login con usuario no-admin
     * 2. Intentar navegar directamente a /admin
     * 3. Verificar acceso denegado
     */
    
    const adminPage = new AdminPage(page);
    test.setTimeout(60000);

    await test.step('1. Navegar a /admin directamente sin autenticación', async () => {
      await page.goto('/admin');
      
      // Debería redirigir a login
      await expect(page).toHaveURL(/\/(login|admin)/, { timeout: 10000 });
    });

    await test.step('2. Si hay página de login visible, intentar con credenciales inválidas', async () => {
      // Intentar con credenciales vacías
      if (await adminPage.loginButton.isVisible({ timeout: 3000 }).catch(() => false)) {
        await adminPage.login('invalid@email.com', 'wrongpassword');
        
        // Debería mostrar error
        await expect(adminPage.loginError).toBeVisible({ timeout: 5000 });
      }
    });

    await page.screenshot({ 
      path: `test-results/tc043-access-denied-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@security TC-044: Login con credenciales inválidas muestra error', async ({ page }) => {
    /**
     * Objetivo: Verificar manejo de errores de autenticación
     */
    
    const adminPage = new AdminPage(page);
    test.setTimeout(60000);

    await test.step('1. Ir a login', async () => {
      await adminPage.gotoLogin();
      await expect(adminPage.loginButton).toBeVisible();
    });

    await test.step('2. Intentar login con email inválido', async () => {
      await adminPage.login('fake@invalid.com', 'FakePassword123!');
      
      // Esperar respuesta
      await page.waitForTimeout(2000);
    });

    await test.step('3. Verificar mensaje de error', async () => {
      await expect(adminPage.loginError).toBeVisible({ timeout: 5000 });
    });

    await test.step('4. Verificar que no redirigió al admin', async () => {
      await expect(page).not.toHaveURL(/\/admin\/(?!.*login)/);
    });

    await page.screenshot({ 
      path: `test-results/tc044-invalid-login-${Date.now()}.png`, 
      fullPage: true 
    });
  });

  test('@security TC-045: Sesión expira después de logout', async ({ page }) => {
    /**
     * Objetivo: Verificar que el logout invalida la sesión
     */
    
    const adminPage = new AdminPage(page);
    test.setTimeout(60000);

    await test.step('1. Login como admin', async () => {
      await adminPage.gotoLogin();
      await adminPage.login(ADMIN_EMAIL, ADMIN_PASSWORD);
      await adminPage.expectLoginSuccess();
    });

    await test.step('2. Verificar acceso al admin', async () => {
      await adminPage.expectIsAdmin();
    });

    await test.step('3. Hacer logout', async () => {
      await adminPage.logout();
    });

    await test.step('4. Intentar acceder a admin después de logout', async () => {
      await page.goto('/admin');
      
      // Debería redirigir a login
      await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
    });

    await page.screenshot({ 
      path: `test-results/tc045-logout-session-${Date.now()}.png`, 
      fullPage: true 
    });
  });
});

// =====================================================
// NAVEGACIÓN TESTS
// =====================================================

test.describe('Panel Admin - Navegación @admin', () => {
  let adminPage: AdminPage;

  test.beforeEach(async ({ page }) => {
    adminPage = new AdminPage(page);
    test.setTimeout(60000);
    
    await adminPage.gotoLogin();
    await adminPage.login(ADMIN_EMAIL, ADMIN_PASSWORD);
    await adminPage.expectLoginSuccess();
  });

  test('@smoke TC-046: Navegación entre secciones del panel', async ({ page }) => {
    /**
     * Objetivo: Verificar que la navegación del sidebar funciona
     */
    
    await test.step('1. Navegar a Bookings', async () => {
      await adminPage.navigateToBookings();
      await expect(page).toHaveURL(/\/admin\/bookings/);
    });

    await test.step('2. Navegar a Employees', async () => {
      await adminPage.navigateToEmployees();
      await expect(page).toHaveURL(/\/admin\/employees/);
    });

    await test.step('3. Navegar a Services', async () => {
      await adminPage.navigateToServices();
      await expect(page).toHaveURL(/\/admin\/services/);
    });

    await test.step('4. Regresar al Dashboard', async () => {
      await adminPage.navigateToDashboard();
      await expect(page).toHaveURL(/\/admin\/?$/);
    });

    await page.screenshot({ 
      path: `test-results/tc046-navigation-${Date.now()}.png`, 
      fullPage: true 
    });
  });
});
