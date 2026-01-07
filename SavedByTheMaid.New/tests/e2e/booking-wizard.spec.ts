import { test, expect } from '@playwright/test';
import { BookingPage } from '../pages/BookingPage';

test.describe('Booking Wizard - E2E Critical Flows', () => {
  let bookingPage: BookingPage;

  test.beforeEach(async ({ page }) => {
    bookingPage = new BookingPage(page);
  });

  test.skip('TC-E2E-001: Usuario completa reserva exitosamente (Happy Path)', async ({ page }) => {
    // TODO: Actualizar Page Object locators para coincidir con componentes reales de BookingPage.tsx
    
    // Ejecutar flujo completo
    const result = await bookingPage.completeBookingFlow({
      zipCode: '10001',
      serviceName: 'Deep Clean',
      bedrooms: 2,
      bathrooms: 2,
      squareFeet: 1500,
      dayOffset: 1, // Mañana
      timeSlot: '09:00 AM',
      contact: {
        firstName: 'Juan',
        lastName: 'Pérez',
        email: `test+${Date.now()}@qa.com`, // Email único
        phone: '555-1234',
        address: '123 Main St, New York, NY'
      }
    });

    // Validaciones finales
    expect(result.confirmation.serviceOrderId).toBeGreaterThan(0);
    expect(result.confirmation.total).toBeGreaterThan(0);
    expect(result.estimate.total).toBeCloseTo(result.confirmation.total, 2);

    // Debe estar en página de éxito
    await expect(page).toHaveURL(/success|confirmation/i);
    
    // Screenshot final
    await bookingPage.takeScreenshot('booking-success');
  });

  test.skip('TC-E2E-002: Usuario cancela SoftReserve haciendo Back', async ({ page }) => {
    await bookingPage.gotoBooking();
    
    // Completar hasta Schedule
    await bookingPage.checkCoverage('10001');
    await bookingPage.selectService('Deep Clean');
    await bookingPage.fillPropertyDetails({
      bedrooms: 2,
      bathrooms: 2,
      squareFeet: 1500
    });
    
    // Crear SoftReserve
    const reserve = await bookingPage.selectTimeSlot(1, '10:00 AM');
    expect(reserve.softReserveId).toBeGreaterThan(0);
    
    // Regresar (esto debe cancelar el SoftReserve)
    await bookingPage.cancelSoftReserve();
    
    // El slot debe estar disponible nuevamente
    const timeSlot = page.getByRole('button', { name: /10:00 AM/i });
    await expect(timeSlot).toBeEnabled();
    await expect(timeSlot).not.toHaveClass(/selected|reserved/);
  });

  test.skip('TC-NEG-005: ZIP code sin cobertura muestra mensaje de error', async ({ page }) => {
    await bookingPage.gotoBooking();
    
    // Ingresar ZIP inválido
    const hasCoverage = await bookingPage.checkCoverage('00000');
    
    expect(hasCoverage).toBe(false);
    
    // Mensaje de error debe ser visible
    await expect(
      page.getByText(/lo sentimos|sorry|not.*serve/i)
    ).toBeVisible();
    
    // Botón Next debe estar deshabilitado
    await expect(bookingPage.nextButton).toBeDisabled();
  });

  test.skip('TC-NEG-006: Sistema previene valores inválidos en detalles de propiedad', async ({ page }) => {
    await bookingPage.gotoBooking();
    
    await bookingPage.checkCoverage('10001');
    await bookingPage.selectService('Standard Clean');
    
    // Intentar ingresar valores inválidos
    await test.step('Valores negativos son prevenidos', async () => {
      // HTML5 validation debería prevenir esto
      await bookingPage.bedroomsInput.fill('-5');
      await bookingPage.bathroomsInput.fill('0');
      
      // Intentar avanzar
      await bookingPage.getEstimateBtn.click();
      
      // Debe mostrar error o no permitir avanzar
      const isNextDisabled = await bookingPage.nextButton.isDisabled();
      const hasError = await page.getByText(/invalid|inválido|error/i).isVisible();
      
      expect(isNextDisabled || hasError).toBe(true);
    });
  });

  test.skip('TC-UX-009: Mensajes de error son claros y en español', async ({ page }) => {
    await bookingPage.gotoBooking();
    
    // Simular error de red
    await page.route('**/api/booking/coverage/**', route => 
      route.abort('failed')
    );
    
    await bookingPage.zipInput.fill('12345');
    await bookingPage.checkCoverageBtn.click();
    
    // Debe mostrar mensaje amigable, no stack trace
    await expect(
      page.getByText(/error de conexión|network error|try again/i)
    ).toBeVisible();
    
    // No debe mostrar código técnico
    const bodyText = await page.textContent('body');
    expect(bodyText).not.toContain('500');
    expect(bodyText).not.toContain('stack trace');
    expect(bodyText).not.toContain('Exception');
  });

  test.skip('TC-EDGE-007: Concurrencia - Solo un usuario puede reservar el mismo slot', async ({ browser }) => {
    test.slow();
    
    // Crear 2 contextos (2 usuarios simultáneos)
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();
    
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();
    
    const booking1 = new BookingPage(page1);
    const booking2 = new BookingPage(page2);
    
    // Ambos completan hasta el paso de Schedule
    await Promise.all([
      booking1.gotoBooking(),
      booking2.gotoBooking()
    ]);
    
    await Promise.all([
      booking1.checkCoverage('10001'),
      booking2.checkCoverage('10001')
    ]);
    
    await Promise.all([
      booking1.selectService('Deep Clean'),
      booking2.selectService('Deep Clean')
    ]);
    
    await Promise.all([
      booking1.fillPropertyDetails({ bedrooms: 2, bathrooms: 2, squareFeet: 1500 }),
      booking2.fillPropertyDetails({ bedrooms: 2, bathrooms: 2, squareFeet: 1500 })
    ]);
    
    // Ambos intentan reservar el MISMO slot al MISMO tiempo
    const results = await Promise.allSettled([
      booking1.selectTimeSlot(2, '14:00'),
      booking2.selectTimeSlot(2, '14:00')
    ]);
    
    // Validar que solo UNO tuvo éxito
    const successCount = results.filter(r => r.status === 'fulfilled').length;
    const failureCount = results.filter(r => r.status === 'rejected').length;
    
    expect(successCount).toBe(1);
    expect(failureCount).toBe(1);
    
    // El que falló debe ver mensaje de error
    const failedIndex = results[0].status === 'rejected' ? 0 : 1;
    const failedPage = failedIndex === 0 ? page1 : page2;
    
    await expect(
      failedPage.getByText(/no longer available|ya no disponible|slot.*taken/i)
    ).toBeVisible({ timeout: 10000 });
    
    await context1.close();
    await context2.close();
  });
});

test.describe.skip('Booking Wizard - Navigation & UX', () => {
  // TODO: Actualizar locators para coincidir con BookingPage.tsx real
  let bookingPage: BookingPage;

  test.beforeEach(async ({ page }) => {
    bookingPage = new BookingPage(page);
    await bookingPage.gotoBooking();
  });

  test('Usuario puede navegar Back/Next sin perder datos', async ({ page }) => {
    await bookingPage.checkCoverage('10001');
    await bookingPage.selectService('Standard Clean');
    
    // Llenar detalles
    await bookingPage.bedroomsInput.fill('3');
    await bookingPage.bathroomsInput.fill('2');
    
    // Regresar
    await bookingPage.backButton.click();
    
    // Avanzar de nuevo
    await bookingPage.nextButton.click();
    
    // Los datos deben persistir
    await expect(bookingPage.bedroomsInput).toHaveValue('3');
    await expect(bookingPage.bathroomsInput).toHaveValue('2');
  });

  test('Wizard muestra progreso visual correcto', async ({ page }) => {
    await bookingPage.checkCoverage('10001');
    
    // Step 1 debe estar marcado como completo
    const step1 = page.locator('[data-step="1"]').or(page.locator('.step-1'));
    await expect(step1).toHaveClass(/completed|active/);
    
    await bookingPage.selectService('Deep Clean');
    
    // Step 2 debe estar activo
    const step2 = page.locator('[data-step="2"]').or(page.locator('.step-2'));
    await expect(step2).toHaveClass(/completed|active/);
  });
});
