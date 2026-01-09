import { test } from '@playwright/test';

/**
 * Test de diagnóstico para ver qué hay realmente en la página
 */
test('Debug: Capturar estructura de la página de booking', async ({ page }) => {
  console.log('🔍 Navegando a /booking...');
  
  // Navegar con espera completa
  await page.goto('/booking', { waitUntil: 'networkidle', timeout: 30000 });
  
  console.log('✅ Página cargada');
  
  // Esperar un poco más para React
  await page.waitForTimeout(3000);
  
  // Capturar screenshot
  await page.screenshot({ 
    path: 'test-results/debug-booking-page.png', 
    fullPage: true 
  });
  
  // Imprimir HTML del body
  const bodyHTML = await page.locator('body').innerHTML();
  console.log('📄 HTML Body (primeros 2000 chars):');
  console.log(bodyHTML.substring(0, 2000));
  
  // Listar todos los inputs
  const inputs = await page.locator('input').all();
  console.log(`\n📝 Total inputs encontrados: ${inputs.length}`);
  for (let i = 0; i < inputs.length; i++) {
    const type = await inputs[i].getAttribute('type');
    const name = await inputs[i].getAttribute('name');
    const placeholder = await inputs[i].getAttribute('placeholder');
    const id = await inputs[i].getAttribute('id');
    console.log(`  Input ${i}: type="${type}", name="${name}", placeholder="${placeholder}", id="${id}"`);
  }
  
  // Listar todos los botones
  const buttons = await page.locator('button').all();
  console.log(`\n🔘 Total botones encontrados: ${buttons.length}`);
  for (let i = 0; i < buttons.length; i++) {
    const text = await buttons[i].textContent();
    const type = await buttons[i].getAttribute('type');
    console.log(`  Button ${i}: text="${text?.trim()}", type="${type}"`);
  }
  
  // Listar headings
  const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();
  console.log(`\n📑 Total headings encontrados: ${headings.length}`);
  for (let i = 0; i < headings.length; i++) {
    const tagName = await headings[i].evaluate(el => el.tagName);
    const text = await headings[i].textContent();
    console.log(`  ${tagName}: "${text?.trim()}"`);
  }
  
  console.log('\n✅ Diagnóstico completado - revisa test-results/debug-booking-page.png');
});
