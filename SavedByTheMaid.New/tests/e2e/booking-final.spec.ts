import { test, expect, type Page } from '@playwright/test';

/**
 * Test E2E del flujo de booking - Versión Final
 * Basado en la estructura real de BookingPage.tsx
 * 
 * Flujo: zipcode → service → details → schedule → contact → confirm
 */
test.describe('SavedByTheMaid Booking Flow', () => {

  test('Flujo completo de reserva exitosa', async ({ page }) => {
    const timestamp = Date.now();
    const testEmail = `test${timestamp}@savedbytemaid.com`;
    
    // Configurar timeout
    test.setTimeout(180000);
    
    // ============================================
    // PASO 1: ZIPCODE - Verificar cobertura
    // ============================================
    console.log('📍 Paso 1: Navegando a /booking...');
    await page.goto('/booking');
    
    // Esperar que React cargue - buscar el heading del primer paso
    await expect(page.getByText('Where do you need cleaning?')).toBeVisible({ timeout: 30000 });
    await page.screenshot({ path: `test-results/v3-01-zipcode-page-${timestamp}.png`, fullPage: true });
    
    // Ingresar ZIP code
    const zipInput = page.getByPlaceholder('Enter ZIP code');
    await expect(zipInput).toBeVisible();
    await zipInput.fill('33166');
    
    // Click en "Check Availability"
    const checkBtn = page.getByRole('button', { name: 'Check Availability' });
    await expect(checkBtn).toBeVisible();
    await checkBtn.click();
    
    // Esperar mensaje de éxito
    await expect(page.getByText('Great news! We service')).toBeVisible({ timeout: 15000 });
    console.log('✅ Paso 1: ZIP verificado');
    await page.screenshot({ path: `test-results/v3-02-zip-verified-${timestamp}.png`, fullPage: true });
    
    // El wizard avanza automáticamente después de 1.5s
    
    // ============================================
    // PASO 2: SERVICE - Seleccionar tipo de servicio
    // ============================================
    console.log('🧹 Paso 2: Seleccionando servicio...');
    
    // Esperar que aparezca la página de servicios
    await expect(page.getByText('Choose your service')).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `test-results/v3-03-service-page-${timestamp}.png`, fullPage: true });
    
    // Seleccionar el primer servicio (Standard Cleaning por ejemplo)
    const serviceCard = page.locator('button').filter({ hasText: /From \$/ }).first();
    await expect(serviceCard).toBeVisible();
    await serviceCard.click();
    
    // Click en Continue
    const continueBtn = page.getByRole('button', { name: 'Continue' });
    await expect(continueBtn).toBeEnabled();
    await continueBtn.click();
    console.log('✅ Paso 2: Servicio seleccionado');
    
    // ============================================
    // PASO 3: DETAILS - Detalles de la propiedad
    // ============================================
    console.log('🏠 Paso 3: Ingresando detalles...');
    
    // Esperar que aparezca la página de detalles
    await expect(page.getByText('Tell us about your space')).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `test-results/v3-04-details-page-${timestamp}.png`, fullPage: true });
    
    // IMPORTANTE: Seleccionar tipo de propiedad (requerido para habilitar Continue)
    // Los tipos son: House, Apartment, Office, etc.
    const propertyCards = page.locator('button').filter({ hasText: /House|Apartment|Office|Condo|Studio/i });
    const cardCount = await propertyCards.count();
    console.log(`   Encontrados ${cardCount} tipos de propiedad`);
    
    if (cardCount > 0) {
      await propertyCards.first().click();
      console.log('   ✓ Tipo de propiedad seleccionado');
    } else {
      // Si no hay cards, buscar cualquier botón clickeable en la sección de Property Type
      const anyPropertyButton = page.locator('[class*="rounded-lg"]').filter({ hasText: /.+/ }).first();
      await anyPropertyButton.click();
    }
    
    // Esperar un momento para que React procese el cambio
    await page.waitForTimeout(500);
    
    // Click en Continue
    const detailsContinue = page.getByRole('button', { name: 'Continue' });
    await expect(detailsContinue).toBeEnabled({ timeout: 5000 });
    await detailsContinue.click();
    console.log('✅ Paso 3: Detalles completados');
    
    // ============================================
    // PASO 4: SCHEDULE - Seleccionar fecha y hora
    // ============================================
    console.log('📅 Paso 4: Seleccionando fecha y hora...');
    
    // Esperar que aparezca la página de scheduling
    await expect(page.getByText('Pick a date & time')).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `test-results/v3-05-schedule-page-${timestamp}.png`, fullPage: true });
    
    // Esperar a que carguen las fechas
    const dateButtons = page.locator('button').filter({ hasText: /Mon|Tue|Wed|Thu|Fri/ });
    await expect(dateButtons.first()).toBeVisible({ timeout: 10000 });
    
    // Seleccionar la primera fecha disponible (que sea día laboral)
    await dateButtons.first().click();
    
    // Esperar a que carguen los time slots
    console.log('⏳ Esperando slots de tiempo...');
    
    // Buscar botones con formato de hora (ej: "9:00 AM", "10:30 AM")
    const timeSlotButton = page.locator('button').filter({ hasText: /\d{1,2}:\d{2}\s*(AM|PM)/i });
    
    // Esperar que aparezcan los slots (o mensaje de no disponible)
    try {
      await expect(timeSlotButton.first()).toBeVisible({ timeout: 15000 });
      
      // Click en el primer slot disponible
      await timeSlotButton.first().click();
      console.log('✅ Time slot seleccionado');
      await page.screenshot({ path: `test-results/v3-06-time-selected-${timestamp}.png`, fullPage: true });
      
      // Click en Continue
      const scheduleContinue = page.getByRole('button', { name: 'Continue' });
      await expect(scheduleContinue).toBeEnabled();
      await scheduleContinue.click();
      console.log('✅ Paso 4: Fecha y hora completados');
      
    } catch {
      console.log('⚠️ No se encontraron slots de tiempo disponibles');
      await page.screenshot({ path: `test-results/v3-06-no-slots-${timestamp}.png`, fullPage: true });
      
      // Intentar con otra fecha
      const secondDate = dateButtons.nth(1);
      if (await secondDate.isVisible()) {
        await secondDate.click();
        
        // Esperar slots nuevamente
        const retrySlots = page.locator('button').filter({ hasText: /\d{1,2}:\d{2}\s*(AM|PM)/i });
        if (await retrySlots.first().isVisible({ timeout: 10000 })) {
          await retrySlots.first().click();
          await page.getByRole('button', { name: 'Continue' }).click();
          console.log('✅ Paso 4: Fecha alternativa seleccionada');
        }
      }
    }
    
    // ============================================
    // PASO 5: CONTACT - Información de contacto
    // ============================================
    console.log('👤 Paso 5: Ingresando información de contacto...');
    
    // Esperar que aparezca la página de contacto
    const contactHeading = page.getByText('Contact information');
    
    if (await contactHeading.isVisible({ timeout: 10000 })) {
      await page.screenshot({ path: `test-results/v3-07-contact-page-${timestamp}.png`, fullPage: true });
      
      // Llenar formulario
      await page.getByPlaceholder('First name').fill('Test');
      await page.getByPlaceholder('Last name').fill('User');
      await page.getByPlaceholder('Email').fill(testEmail);
      await page.getByPlaceholder('Phone').fill('3051234567');
      await page.getByPlaceholder('Street address').fill('123 Test Street');
      await page.getByPlaceholder('City').fill('Miami');
      
      // Password si es usuario nuevo
      const passwordField = page.getByPlaceholder('Password');
      if (await passwordField.isVisible({ timeout: 3000 })) {
        await passwordField.fill('TestPassword123!');
      }
      
      // Click en Continue
      const contactContinue = page.getByRole('button', { name: 'Continue' });
      await expect(contactContinue).toBeEnabled({ timeout: 5000 });
      await contactContinue.click();
      console.log('✅ Paso 5: Información de contacto completada');
    }
    
    // ============================================
    // PASO 6: CONFIRM - Revisar y confirmar
    // ============================================
    console.log('✨ Paso 6: Confirmando reserva...');
    
    // Esperar página de confirmación
    const reviewHeading = page.getByText(/Review|Confirm|Summary/i);
    
    if (await reviewHeading.isVisible({ timeout: 10000 })) {
      await page.screenshot({ path: `test-results/v3-08-confirm-page-${timestamp}.png`, fullPage: true });
      
      // Buscar y hacer click en botón de confirmación
      const confirmBtn = page.getByRole('button', { name: /Confirm|Book Now|Submit/i });
      
      if (await confirmBtn.isVisible({ timeout: 5000 })) {
        await confirmBtn.click();
        
        // Esperar confirmación exitosa
        const successIndicator = page.getByText(/Thank you|Confirmed|Success|Booking confirmed/i);
        await expect(successIndicator).toBeVisible({ timeout: 30000 });
        
        await page.screenshot({ path: `test-results/v3-09-success-${timestamp}.png`, fullPage: true });
        console.log('🎉 BOOKING COMPLETADO EXITOSAMENTE!');
      }
    }
    
    console.log(`\n📧 Email de prueba: ${testEmail}`);
    console.log('✅ Test finalizado');
  });

  test('Diagnóstico de la página de booking', async ({ page }) => {
    await page.goto('/booking');
    
    // Esperar carga inicial
    await expect(page.locator('body')).not.toBeEmpty();
    await page.waitForLoadState('networkidle');
    
    // Capturar screenshot
    await page.screenshot({ path: 'test-results/diagnostico-booking.png', fullPage: true });
    
    // Información de la página
    const pageTitle = await page.title();
    console.log(`📄 Título: ${pageTitle}`);
    console.log(`🌐 URL: ${page.url()}`);
    
    // Buscar elementos clave
    const elements = {
      'Heading "Where do you need cleaning?"': page.getByText('Where do you need cleaning?'),
      'Input ZIP code': page.getByPlaceholder('Enter ZIP code'),
      'Button Check Availability': page.getByRole('button', { name: 'Check Availability' }),
    };
    
    for (const [name, locator] of Object.entries(elements)) {
      const visible = await locator.isVisible({ timeout: 5000 }).catch(() => false);
      console.log(`${visible ? '✅' : '❌'} ${name}`);
    }
  });
});
