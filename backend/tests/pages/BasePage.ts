import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Base Page Object - Funcionalidad común a todas las páginas
 */
export class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Navega a una ruta y espera a que la red esté idle
   */
  async goto(path: string) {
    await this.page.goto(path);
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Llena un input por su label
   */
  async fillInput(label: string | RegExp, value: string) {
    const input = this.page.getByLabel(label);
    await input.waitFor({ state: 'visible' });
    await input.fill(value);
  }

  /**
   * Click en un botón por su texto
   */
  async clickButton(text: string | RegExp) {
    const button = this.page.getByRole('button', { name: text });
    await button.waitFor({ state: 'visible' });
    await button.click();
  }

  /**
   * Espera a que aparezca un mensaje toast/notificación
   */
  async waitForToast(message: string | RegExp, timeout = 5000) {
    await this.page.getByText(message).waitFor({ 
      state: 'visible',
      timeout 
    });
  }

  /**
   * Espera a que un loader/spinner desaparezca
   */
  async waitForLoading() {
    const loader = this.page.locator('[data-testid="spinner"], .spinner, .loading');
    await loader.waitFor({ state: 'hidden', timeout: 30000 }).catch(() => {
      // Si no hay loader, continuar
    });
  }

  /**
   * Toma screenshot con nombre descriptivo
   */
  async takeScreenshot(name: string) {
    await this.page.screenshot({ 
      path: `test-results/screenshots/${name}-${Date.now()}.png`,
      fullPage: true 
    });
  }

  /**
   * Verifica que no haya errores de consola críticos
   */
  async checkConsoleErrors() {
    const errors: string[] = [];
    
    this.page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    this.page.on('pageerror', error => {
      errors.push(error.message);
    });

    return errors;
  }

  /**
   * Espera respuesta de API
   */
  async waitForAPIResponse(urlPattern: string | RegExp, status = 200) {
    return await this.page.waitForResponse(
      resp => {
        const matchesUrl = typeof urlPattern === 'string' 
          ? resp.url().includes(urlPattern)
          : urlPattern.test(resp.url());
        return matchesUrl && resp.status() === status;
      },
      { timeout: 30000 }
    );
  }

  /**
   * Intercepta y modifica request (útil para testing de seguridad)
   */
  async interceptRequest(urlPattern: string | RegExp, modify: (route: any) => Promise<void>) {
    await this.page.route(urlPattern, modify);
  }
}
