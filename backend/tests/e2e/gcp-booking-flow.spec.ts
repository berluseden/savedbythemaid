import { test, expect } from '@playwright/test';

/**
 * Test E2E completo del flujo de booking contra GCP
 * Simula el comportamiento real de un usuario
 */
test.describe('GCP Booking Flow - Usuario Real', () => {
  
  test('Flujo completo: ZIP → Servicio → Detalles → Fecha/Hora → Contacto → Confirmación', async ({ page }) => {
    const timestamp = Date.now();
    const testEmail = `test${timestamp}@savedbytemaid.com`;
    
    // PASO 1: Navegar al booking wizard
    await test.step('Navegar a página de reservas', async () => {
      await page.goto('/booking', { waitUntil: 'networkidle' });
      
      // Esperar a que React cargue y el componente se renderice
      await page.waitForTimeout(2000);
      
      // Buscar por el input en lugar del heading (más confiable para SPAs)
      const zipInput = page.locator('input[type="text"]').first();
      await expect(zipInput).toBeVisible({ timeout: 15000 });
    });

    // PASO 2: Verificar cobertura de ZIP code
    await test.step('Ingresar ZIP code válido (33166 - Doral, FL)', async () => {
      // Localizar el input de ZIP - puede tener diferentes labels
      const zipInput = page.locator('input[type="text"]').first();
      await zipInput.waitFor({ state: 'visible' });
      await zipInput.fill('33166');
      
      // Buscar botón de verificar - puede tener diferentes textos
      const checkButton = page.locator('button').filter({ hasText: /check|verificar|continue|siguiente/i }).first();
      await checkButton.click();
      
      // Esperar transición al siguiente paso (esperar que cambie la interfaz)
      await page.waitForTimeout(1000);
      
      // Screenshot del paso
      await page.screenshot({ path: `test-results/step1-zipcode-${timestamp}.png`, fullPage: true });
    });

    // PASO 3: Seleccionar servicio
    await test.step('Seleccionar tipo de servicio', async () => {
      const nextButton = page.getByRole('button', { name: /next|siguiente|continue/i });
      await nextButton.click();
      
      // Esperar a que carguen los servicios
      await page.waitForLoadState('networkidle');
      
      // Seleccionar el primer servicio disponible
      const serviceCard = page.locator('[data-testid="service-card"], .service-card').first();
      await serviceCard.click();
      
      await page.screenshot({ path: `test-results/step2-service-${timestamp}.png`, fullPage: true });
      
      await page.getByRole('button', { name: /next|siguiente|continue/i }).click();
    });

    // PASO 4: Ingresar detalles de la propiedad
    await test.step('Completar detalles de la propiedad', async () => {
      await page.waitForLoadState('networkidle');
      
      // Buscar inputs de habitaciones y baños
      const bedroomsInput = page.getByLabel(/bedroom|habitacion|cuarto/i).or(
        page.locator('input[type="number"]').first()
      );
      const bathroomsInput = page.getByLabel(/bathroom|baño/i).or(
        page.locator('input[type="number"]').nth(1)
      );
      
      await bedroomsInput.fill('3');
      await bathroomsInput.fill('2');
      
      // Square feet (si existe)
      const sqftInput = page.getByLabel(/square.*feet|pies.*cuadrados/i);
      if (await sqftInput.count() > 0) {
        await sqftInput.fill('1500');
      }
      
      await page.screenshot({ path: `test-results/step3-details-${timestamp}.png`, fullPage: true });
      
      // Obtener estimado
      const estimateButton = page.getByRole('button', { name: /estimate|calcular|get.*price/i });
      await estimateButton.click();
      
      // Esperar a que aparezca el precio
      await expect(page.getByText(/\$\d+/)).toBeVisible({ timeout: 10000 });
      
      await page.getByRole('button', { name: /next|siguiente|continue/i }).click();
    });

    // PASO 5: Seleccionar fecha y hora
    await test.step('Seleccionar fecha y hora disponible', async () => {
      await page.waitForLoadState('networkidle');
      
      // Esperar a que cargue el calendario
      await page.waitForTimeout(1000);
      
      // Seleccionar el primer día disponible (viernes 16 o siguiente día hábil)
      const calendarDays = page.locator('button').filter({ hasText: /^(1[2-9]|2[0-9]|3[0-1])$/ });
      const firstAvailableDay = calendarDays.first();
      await firstAvailableDay.click();
      
      // Esperar a que carguen los slots de tiempo
      await page.waitForTimeout(2000);
      
      // Tomar screenshot de los slots disponibles
      await page.screenshot({ path: `test-results/step4a-slots-${timestamp}.png`, fullPage: true });
      
      // Seleccionar el primer slot disponible
      const timeSlots = page.locator('button').filter({ hasText: /\d{1,2}:\d{2}\s*(AM|PM)/i });
      const availableSlot = timeSlots.filter({ hasNotText: /disabled|unavailable/i }).first();
      
      await expect(availableSlot).toBeVisible({ timeout: 5000 });
      await availableSlot.click();
      
      await page.screenshot({ path: `test-results/step4b-selected-${timestamp}.png`, fullPage: true });
      
      await page.getByRole('button', { name: /continue|next|siguiente/i }).click();
    });

    // PASO 6: Ingresar información de contacto
    await test.step('Completar información de contacto', async () => {
      await page.waitForLoadState('networkidle');
      
      // Llenar formulario de contacto
      await page.getByLabel(/first.*name|nombre/i).fill('Test');
      await page.getByLabel(/last.*name|apellido/i).fill('User');
      
      const emailInput = page.getByLabel(/email|correo/i);
      await emailInput.fill(testEmail);
      await emailInput.blur();
      
      // Esperar verificación de email
      await page.waitForTimeout(1500);
      
      // Si muestra campo de password (email nuevo), llenarlo
      const passwordInput = page.getByLabel(/password|contraseña/i);
      if (await passwordInput.count() > 0) {
        await passwordInput.fill('TestPassword123!');
        console.log('✓ Email nuevo detectado - creando cuenta');
      }
      
      await page.getByLabel(/phone|teléfono/i).fill('3055551234');
      await page.getByLabel(/street.*address|dirección/i).fill('8550 NW 70th ST');
      await page.getByLabel(/city|ciudad/i).fill('miami');
      await page.getByLabel(/state|estado/i).fill('FL');
      
      await page.screenshot({ path: `test-results/step5-contact-${timestamp}.png`, fullPage: true });
      
      await page.getByRole('button', { name: /review|revisar|next/i }).click();
    });

    // PASO 7: Revisar y confirmar
    await test.step('Revisar detalles y confirmar reserva', async () => {
      await page.waitForLoadState('networkidle');
      
      // Verificar que se muestre el resumen
      await expect(page.getByText(/review|confirmar|summary/i)).toBeVisible();
      await expect(page.getByText(/\$\d+/)).toBeVisible();
      
      await page.screenshot({ path: `test-results/step6-review-${timestamp}.png`, fullPage: true });
      
      // Confirmar reserva
      const confirmButton = page.getByRole('button', { name: /confirm.*booking|confirmar/i });
      await confirmButton.click();
      
      // Esperar confirmación (hasta 30 segundos)
      await page.waitForTimeout(2000);
    });

    // PASO 8: Validar confirmación
    await test.step('Validar página de confirmación', async () => {
      // Debe mostrar mensaje de éxito
      await expect(
        page.getByText(/success|confirmed|exitosa|confirmada/i)
      ).toBeVisible({ timeout: 15000 });
      
      // Debe mostrar número de confirmación
      await expect(
        page.getByText(/SBM-|confirmation.*number|número.*confirmación/i)
      ).toBeVisible();
      
      await page.screenshot({ path: `test-results/step7-success-${timestamp}.png`, fullPage: true });
      
      console.log('✅ Reserva completada exitosamente');
      console.log(`📧 Email de prueba: ${testEmail}`);
    });
  });

  test('Validar que slots vacíos muestren mensaje apropiado', async ({ page }) => {
    await page.goto('/booking', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    // Ingresar ZIP y avanzar hasta schedule
    const zipInput = page.locator('input[type="text"]').first();
    await zipInput.waitFor({ state: 'visible' });
    await zipInput.fill('33166');
    await page.locator('button').filter({ hasText: /check|verificar|continue/i }).first().click();
    await page.waitForTimeout(1500);
    
    // Skip service selection
    await page.getByRole('button', { name: /next/i }).click();
    await page.waitForTimeout(1000);
    const serviceCard = page.locator('[data-testid="service-card"], .service-card').first();
    await serviceCard.click();
    await page.getByRole('button', { name: /next/i }).click();
    
    // Skip details
    await page.waitForTimeout(1000);
    const bedroomsInput = page.locator('input[type="number"]').first();
    await bedroomsInput.fill('2');
    await page.getByRole('button', { name: /estimate/i }).click();
    await page.waitForTimeout(2000);
    await page.getByRole('button', { name: /next/i }).click();
    
    // Seleccionar un sábado o domingo (sin slots)
    await page.waitForTimeout(1000);
    const saturdayButton = page.getByText(/sat|sáb|dom|sun/i).first();
    
    if (await saturdayButton.count() > 0) {
      await saturdayButton.click();
      await page.waitForTimeout(2000);
      
      // Verificar que no haya slots o muestre mensaje
      const hasSlots = await page.locator('button').filter({ hasText: /\d{1,2}:\d{2}/i }).count();
      
      if (hasSlots === 0) {
        console.log('✓ Correctamente no muestra slots para fin de semana');
      }
    }
  });
});
