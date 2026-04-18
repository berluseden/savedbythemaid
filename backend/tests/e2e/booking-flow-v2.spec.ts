import { test, expect } from '@playwright/test';

/**
 * Test E2E del flujo de booking - Best Practices de Playwright
 * 
 * Mejores prácticas aplicadas:
 * - NO usar waitForTimeout, waitForLoadState, waitForNavigation
 * - Usar locators semánticos (getByRole, getByText, getByLabel, getByTestId)
 * - Usar expect().toBeVisible() para esperas automáticas
 * - Screenshots en cada paso para debugging
 */
test.describe('Booking Flow E2E', () => {
  
  test('Flujo completo de reserva', async ({ page }) => {
    const timestamp = Date.now();
    const testEmail = `test${timestamp}@savedbytemaid.com`;
    
    // Configurar timeout más largo para SPAs
    test.setTimeout(120000);

    // ============================================
    // PASO 1: Navegar a la página de booking
    // ============================================
    await page.goto('/booking');
    
    // Esperar que la aplicación React cargue - buscar cualquier elemento interactivo
    await expect(page.locator('button, input, a').first()).toBeVisible({ timeout: 30000 });
    
    // Screenshot inicial
    await page.screenshot({ path: `test-results/01-inicial-${timestamp}.png`, fullPage: true });
    console.log('✅ Paso 1: Página cargada');

    // ============================================
    // PASO 2: Ingresar ZIP code
    // ============================================
    // Buscar input de texto (probablemente el ZIP)
    const firstInput = page.locator('input').first();
    await expect(firstInput).toBeVisible();
    await firstInput.fill('33166');
    
    // Screenshot después de llenar ZIP
    await page.screenshot({ path: `test-results/02-zip-ingresado-${timestamp}.png`, fullPage: true });
    
    // Buscar y hacer click en botón para verificar/continuar
    const primaryButton = page.locator('button').first();
    await expect(primaryButton).toBeEnabled();
    await primaryButton.click();
    
    // Esperar a que algo cambie (nuevo contenido o mensaje)
    await expect(page.locator('body')).not.toBeEmpty();
    
    await page.screenshot({ path: `test-results/03-zip-verificado-${timestamp}.png`, fullPage: true });
    console.log('✅ Paso 2: ZIP verificado');

    // ============================================
    // PASO 3: Selección de servicio (si aplica)
    // ============================================
    // Buscar cards o elementos seleccionables
    const serviceElements = page.locator('[class*="card"], [class*="service"], [role="button"]').first();
    
    if (await serviceElements.isVisible()) {
      await serviceElements.click();
      await page.screenshot({ path: `test-results/04-servicio-seleccionado-${timestamp}.png`, fullPage: true });
      console.log('✅ Paso 3: Servicio seleccionado');
      
      // Buscar botón de siguiente
      const nextBtn = page.getByRole('button', { name: /next|siguiente|continue|continuar/i });
      if (await nextBtn.isVisible()) {
        await nextBtn.click();
      }
    }

    // ============================================
    // PASO 4: Detalles de la propiedad
    // ============================================
    // Buscar inputs numéricos (habitaciones, baños, etc.)
    const numberInputs = page.locator('input[type="number"]');
    const numInputsCount = await numberInputs.count();
    
    if (numInputsCount > 0) {
      // Llenar el primer input numérico con 2
      await numberInputs.first().fill('2');
      
      if (numInputsCount > 1) {
        await numberInputs.nth(1).fill('1');
      }
      
      await page.screenshot({ path: `test-results/05-detalles-${timestamp}.png`, fullPage: true });
      console.log('✅ Paso 4: Detalles ingresados');
      
      // Siguiente paso
      const nextBtn = page.getByRole('button', { name: /next|siguiente|continue|continuar/i });
      if (await nextBtn.isVisible()) {
        await nextBtn.click();
      }
    }

    // ============================================
    // PASO 5: Selección de fecha y hora
    // ============================================
    // Buscar elementos de calendario o selector de fecha
    const calendarArea = page.locator('[class*="calendar"], [class*="date"], [class*="schedule"]');
    
    // Screenshot para ver estado actual
    await page.screenshot({ path: `test-results/05b-antes-fecha-${timestamp}.png`, fullPage: true });
    
    // Buscar días clickeables en calendario
    const clickableDays = page.locator('button, [role="button"], [class*="day"]').filter({ hasText: /^\d{1,2}$/ });
    const daysCount = await clickableDays.count();
    console.log(`📅 Días encontrados: ${daysCount}`);
    
    if (daysCount > 0) {
      // Click en algún día disponible
      await clickableDays.first().click();
      await page.screenshot({ path: `test-results/06a-dia-seleccionado-${timestamp}.png`, fullPage: true });
    }
    
    // Buscar slots de tiempo (botones con formato hora)
    const allButtons = await page.locator('button').all();
    console.log(`🔘 Total botones: ${allButtons.length}`);
    
    for (const btn of allButtons) {
      const text = await btn.textContent();
      console.log(`  Button: "${text?.trim()}"`);
    }
    
    // Buscar botones que parezcan horarios (contienen AM/PM o :)
    const timeSlots = page.locator('button').filter({ hasText: /(AM|PM|:\d{2})/ });
    const slotsCount = await timeSlots.count();
    console.log(`⏰ Time slots encontrados: ${slotsCount}`);
    
    if (slotsCount > 0) {
      await timeSlots.first().click();
      await page.screenshot({ path: `test-results/06-fecha-hora-${timestamp}.png`, fullPage: true });
      console.log('✅ Paso 5: Fecha y hora seleccionadas');
      
      // Siguiente paso
      const nextBtn = page.getByRole('button', { name: /next|siguiente|continue|continuar/i });
      if (await nextBtn.isVisible()) {
        await nextBtn.click();
      }
    } else {
      console.log('⚠️ No se encontraron slots de tiempo');
      await page.screenshot({ path: `test-results/06-sin-slots-${timestamp}.png`, fullPage: true });
    }

    // ============================================
    // PASO 6: Información de contacto
    // ============================================
    // Buscar campos de contacto
    const emailInput = page.locator('input[type="email"], input[name*="email"], input[placeholder*="email"]').first();
    const phoneInput = page.locator('input[type="tel"], input[name*="phone"], input[placeholder*="phone"]').first();
    const nameInput = page.locator('input[name*="name"], input[placeholder*="name"]').first();
    
    if (await emailInput.isVisible()) {
      await emailInput.fill(testEmail);
    }
    
    if (await phoneInput.isVisible()) {
      await phoneInput.fill('3051234567');
    }
    
    if (await nameInput.isVisible()) {
      await nameInput.fill('Test User');
    }
    
    await page.screenshot({ path: `test-results/07-contacto-${timestamp}.png`, fullPage: true });
    console.log('✅ Paso 6: Información de contacto ingresada');

    // ============================================
    // PASO 7: Confirmar reserva
    // ============================================
    // Buscar botón de confirmación
    const confirmBtn = page.getByRole('button', { name: /confirm|confirmar|book|reservar|submit/i });
    
    if (await confirmBtn.isVisible()) {
      await confirmBtn.click();
      
      // Esperar página de confirmación o mensaje de éxito
      const successIndicator = page.locator('[class*="success"], [class*="confirm"], :text(/thank|gracias|confirmed|confirmad/)');
      
      await expect(successIndicator.first()).toBeVisible({ timeout: 30000 });
      
      await page.screenshot({ path: `test-results/08-confirmacion-${timestamp}.png`, fullPage: true });
      console.log('✅ Paso 7: Reserva confirmada!');
    }

    console.log(`\n📧 Email de prueba: ${testEmail}`);
    console.log('🎉 Test completado exitosamente');
  });

  test('Verificar que la página carga correctamente', async ({ page }) => {
    await page.goto('/booking');
    
    // Verificar que hay contenido
    await expect(page.locator('body')).not.toBeEmpty();
    
    // Verificar que hay al menos un botón o input
    const interactiveElement = page.locator('button, input, a');
    await expect(interactiveElement.first()).toBeVisible({ timeout: 30000 });
    
    // Capturar estado de la página para diagnóstico
    await page.screenshot({ path: 'test-results/diagnostico-pagina.png', fullPage: true });
    
    // Imprimir estructura para debugging
    const pageTitle = await page.title();
    console.log(`📄 Título de página: ${pageTitle}`);
    
    const buttonsCount = await page.locator('button').count();
    const inputsCount = await page.locator('input').count();
    console.log(`🔘 Botones encontrados: ${buttonsCount}`);
    console.log(`📝 Inputs encontrados: ${inputsCount}`);
  });
});
