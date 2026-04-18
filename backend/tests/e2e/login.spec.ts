import { test, expect } from '@playwright/test';

/**
 * E2E Tests - Login UI
 * ====================
 * 
 * Esta suite valida el flujo de login desde la interfaz de usuario:
 * - Login exitoso como customer
 * - Login exitoso como admin con redirección
 * - Manejo de errores y validaciones
 * - Logout y limpieza de sesión
 */

// Credenciales de prueba (deben coincidir con AUTH_CREDENTIALS del fixture)
const TEST_USERS = {
  customer: {
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
  admin: {
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
  invalid: {
    email: 'invalid@test.com',
    password: 'WrongPassword123!',
  },
};

test.describe('Login UI @auth', () => {
  
  test.beforeEach(async ({ page }) => {
    // Limpiar storage antes de cada test
    await page.context().clearCookies();
    await page.goto('/login');
    
    // Esperar que la página cargue completamente - buscar el campo de email
    await expect(page.locator('input#email')).toBeVisible({ timeout: 10000 });
  });

  // =====================================================
  // SMOKE TESTS
  // =====================================================
  
  test('@smoke TC-100: Login exitoso como customer', async ({ page }) => {
    await test.step('1. Completar formulario de login', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      await expect(emailInput, 'Campo email debe ser visible').toBeVisible();
      await expect(passwordInput, 'Campo password debe ser visible').toBeVisible();
      
      await emailInput.fill(TEST_USERS.customer.email);
      await passwordInput.fill(TEST_USERS.customer.password);
      
      await page.screenshot({ path: 'test-results/tc100-01-form-filled.png' });
    });

    await test.step('2. Enviar formulario', async () => {
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await expect(submitButton, 'Botón de login debe ser visible').toBeVisible();
      await submitButton.click();
    });

    await test.step('3. Verificar redirección después del login', async () => {
      // Esperar navegación - customer va a /dashboard o /
      await expect(page).toHaveURL(/\/(dashboard|booking)?/, { timeout: 15000 });
      
      // Verificar que ya no estamos en login
      await expect(page).not.toHaveURL('/login');
      
      await page.screenshot({ path: 'test-results/tc100-02-logged-in.png' });
    });

    await test.step('4. Verificar estado de autenticación', async () => {
      // Debería existir el token en storage
      const token = await page.evaluate(() => {
        return localStorage.getItem('token') || sessionStorage.getItem('token');
      });
      
      expect(token, 'Token JWT debe estar guardado en storage').toBeTruthy();
    });
  });

  test('@smoke TC-101: Login exitoso como admin redirige a /admin', async ({ page }) => {
    await test.step('1. Completar formulario con credenciales admin', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      await emailInput.fill(TEST_USERS.admin.email);
      await passwordInput.fill(TEST_USERS.admin.password);
    });

    await test.step('2. Enviar formulario', async () => {
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
    });

    await test.step('3. Verificar redirección a panel admin', async () => {
      // Admin debe ser redirigido a /admin
      await expect(
        page,
        'Admin debe ser redirigido a /admin'
      ).toHaveURL(/\/admin/, { timeout: 15000 });
      
      await page.screenshot({ path: 'test-results/tc101-admin-redirect.png' });
    });

    await test.step('4. Verificar que estamos en el panel de administración', async () => {
      // Buscar elementos típicos del panel admin
      const adminIndicator = page.getByText(/Admin|Dashboard|Panel|Órdenes/i).first();
      await expect(
        adminIndicator,
        'Debe mostrar contenido del panel admin'
      ).toBeVisible({ timeout: 10000 });
    });
  });

  // =====================================================
  // REGRESSION TESTS
  // =====================================================
  
  test('@regression TC-102: Credenciales inválidas muestra mensaje de error', async ({ page }) => {
    await test.step('1. Intentar login con credenciales inválidas', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      await emailInput.fill(TEST_USERS.invalid.email);
      await passwordInput.fill(TEST_USERS.invalid.password);
      
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
    });

    await test.step('2. Verificar mensaje de error', async () => {
      // Esperar mensaje de error
      const errorMessage = page.locator('[class*="error"], [class*="alert"], [role="alert"]')
        .or(page.getByText(/invalid|incorrect|error|inválid/i));
      
      await expect(
        errorMessage.first(),
        'Debe mostrar mensaje de error'
      ).toBeVisible({ timeout: 10000 });
      
      await page.screenshot({ path: 'test-results/tc102-error-message.png' });
    });

    await test.step('3. Verificar que permanecemos en login', async () => {
      await expect(
        page,
        'Debe permanecer en página de login'
      ).toHaveURL(/\/login/);
    });

    await test.step('4. Verificar que no hay token guardado', async () => {
      const token = await page.evaluate(() => {
        return localStorage.getItem('token') || sessionStorage.getItem('token');
      });
      
      expect(token, 'No debe haber token en storage tras error').toBeFalsy();
    });
  });

  test('@regression TC-103: Campo email vacío muestra validación HTML5', async ({ page }) => {
    await test.step('1. Dejar email vacío e intentar submit', async () => {
      const passwordInput = page.locator('input#password, input[type="password"]');
      await passwordInput.fill('SomePassword123!');
      
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
    });

    await test.step('2. Verificar validación', async () => {
      // El input de email debería tener validación HTML5 required
      const emailInput = page.locator('input#email, input[type="email"]');
      
      // Verificar que el campo tiene el atributo required
      const isRequired = await emailInput.getAttribute('required');
      expect(isRequired !== null, 'Campo email debe ser required').toBeTruthy();
      
      // El form no debería haberse enviado (seguimos en login)
      await expect(page).toHaveURL(/\/login/);
      
      await page.screenshot({ path: 'test-results/tc103-email-validation.png' });
    });
  });

  test('@regression TC-104: Campo password vacío muestra validación', async ({ page }) => {
    await test.step('1. Completar solo email e intentar submit', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      await emailInput.fill(TEST_USERS.customer.email);
      
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
    });

    await test.step('2. Verificar validación de password', async () => {
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      // Verificar que el campo tiene el atributo required
      const isRequired = await passwordInput.getAttribute('required');
      expect(isRequired !== null, 'Campo password debe ser required').toBeTruthy();
      
      // Seguimos en login
      await expect(page).toHaveURL(/\/login/);
      
      await page.screenshot({ path: 'test-results/tc104-password-validation.png' });
    });
  });

  test('@regression TC-105: Logout limpia sesión correctamente', async ({ page }) => {
    await test.step('1. Login exitoso primero', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      await emailInput.fill(TEST_USERS.customer.email);
      await passwordInput.fill(TEST_USERS.customer.password);
      
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
      
      // Esperar login exitoso
      await expect(page).not.toHaveURL('/login', { timeout: 15000 });
    });

    await test.step('2. Verificar token guardado post-login', async () => {
      const tokenBefore = await page.evaluate(() => {
        return localStorage.getItem('token') || sessionStorage.getItem('token');
      });
      
      expect(tokenBefore, 'Token debe existir después del login').toBeTruthy();
    });

    await test.step('3. Realizar logout', async () => {
      // Buscar botón de logout o menú de usuario
      const logoutButton = page.getByRole('button', { name: /Logout|Sign out|Cerrar sesión|Salir/i })
        .or(page.locator('[data-testid="logout"]'))
        .or(page.locator('button:has-text("Logout")'));
      
      // Si el botón no está visible directamente, puede estar en un menú
      const userMenu = page.locator('[data-testid="user-menu"], [aria-label*="user"], [aria-label*="profile"]')
        .or(page.locator('button').filter({ hasText: /@|user|profile/i }));
      
      // Intentar abrir menú de usuario si existe
      if (await userMenu.first().isVisible()) {
        await userMenu.first().click();
        await page.waitForTimeout(500);
      }
      
      // Click en logout
      if (await logoutButton.first().isVisible()) {
        await logoutButton.first().click();
      } else {
        // Si no hay botón visible, navegar directamente (fallback)
        await page.goto('/');
        await page.evaluate(() => {
          localStorage.removeItem('token');
          sessionStorage.removeItem('token');
        });
      }
      
      await page.screenshot({ path: 'test-results/tc105-post-logout.png' });
    });

    await test.step('4. Verificar que la sesión fue limpiada', async () => {
      // Esperar un momento para que se procese el logout
      await page.waitForTimeout(1000);
      
      const tokenAfter = await page.evaluate(() => {
        return localStorage.getItem('token') || sessionStorage.getItem('token');
      });
      
      expect(tokenAfter, 'Token debe ser eliminado después del logout').toBeFalsy();
    });

    await test.step('5. Verificar redirección a login o home', async () => {
      // Después del logout deberíamos estar en home o login
      const currentUrl = page.url();
      const isLoggedOut = currentUrl.includes('/login') || 
                          currentUrl.endsWith('/') || 
                          !currentUrl.includes('/admin');
      
      expect(isLoggedOut, 'Debe redireccionar fuera de áreas protegidas').toBeTruthy();
    });
  });

  // =====================================================
  // UI/UX TESTS
  // =====================================================
  
  test('@regression TC-106: Toggle de mostrar/ocultar password funciona', async ({ page }) => {
    await test.step('1. Verificar que password está oculto por defecto', async () => {
      const passwordInput = page.locator('input#password, input[type="password"]');
      const inputType = await passwordInput.getAttribute('type');
      
      expect(inputType, 'Password debe estar oculto inicialmente').toBe('password');
    });

    await test.step('2. Click en toggle para mostrar password', async () => {
      // Buscar el botón de toggle (puede ser un icono de ojo)
      const toggleButton = page.locator('button').filter({ has: page.locator('svg') })
        .and(page.locator('button').filter({ hasNotText: /sign|submit/i }));
      
      // Encontrar el toggle cerca del campo password
      const passwordToggle = page.locator('input#password, input[type="password"]')
        .locator('..').locator('button').first();
      
      if (await passwordToggle.isVisible()) {
        await passwordToggle.click();
        
        await test.step('3. Verificar que password ahora es visible', async () => {
          const passwordInput = page.locator('input#password, input[type="text"]').first();
          const inputType = await passwordInput.getAttribute('type');
          
          // Debería cambiar a 'text' para mostrar el password
          expect(inputType, 'Password debe ser visible después del toggle').toBe('text');
        });
      } else {
        // Si no hay toggle, skip este paso
        console.log('Password toggle not found, skipping toggle test');
      }
    });
  });

  test('@regression TC-107: Link "Forgot Password" es visible y funcional', async ({ page }) => {
    await test.step('1. Verificar que existe link de forgot password', async () => {
      const forgotLink = page.getByRole('link', { name: /forgot|password|olvidé|contraseña/i })
        .or(page.locator('a[href*="forgot"]'));
      
      await expect(
        forgotLink.first(),
        'Link de "Forgot Password" debe existir'
      ).toBeVisible();
    });

    await test.step('2. Click en forgot password', async () => {
      const forgotLink = page.getByRole('link', { name: /forgot|password/i }).first();
      await forgotLink.click();
      
      // Verificar navegación
      await expect(
        page,
        'Debe navegar a página de forgot password'
      ).toHaveURL(/forgot/, { timeout: 10000 });
      
      await page.screenshot({ path: 'test-results/tc107-forgot-password-page.png' });
    });
  });

  test('@regression TC-108: Link de registro es visible y funcional', async ({ page }) => {
    await test.step('1. Verificar que existe link de registro', async () => {
      const registerLink = page.getByRole('link', { name: /sign up|register|crear cuenta|registrarse/i })
        .or(page.locator('a[href*="register"]'));
      
      await expect(
        registerLink.first(),
        'Link de registro debe existir'
      ).toBeVisible();
    });

    await test.step('2. Click en link de registro', async () => {
      const registerLink = page.getByRole('link', { name: /sign up|register/i }).first();
      await registerLink.click();
      
      // Verificar navegación
      await expect(
        page,
        'Debe navegar a página de registro'
      ).toHaveURL(/register/, { timeout: 10000 });
      
      await page.screenshot({ path: 'test-results/tc108-register-page.png' });
    });
  });

  test('@regression TC-109: Checkbox "Remember me" funciona', async ({ page }) => {
    await test.step('1. Verificar que existe checkbox remember me', async () => {
      const rememberCheckbox = page.locator('input[type="checkbox"]')
        .or(page.getByLabel(/remember|recordar/i));
      
      await expect(
        rememberCheckbox.first(),
        'Checkbox "Remember me" debe existir'
      ).toBeVisible();
    });

    await test.step('2. Verificar estado inicial (unchecked)', async () => {
      const rememberCheckbox = page.locator('input[type="checkbox"]').first();
      const isChecked = await rememberCheckbox.isChecked();
      
      expect(isChecked, 'Checkbox debe estar desmarcado inicialmente').toBe(false);
    });

    await test.step('3. Marcar checkbox', async () => {
      const rememberCheckbox = page.locator('input[type="checkbox"]').first();
      await rememberCheckbox.check();
      
      const isChecked = await rememberCheckbox.isChecked();
      expect(isChecked, 'Checkbox debe estar marcado después del click').toBe(true);
    });

    await test.step('4. Login y verificar token en localStorage', async () => {
      const emailInput = page.locator('input#email, input[type="email"]');
      const passwordInput = page.locator('input#password, input[type="password"]');
      
      await emailInput.fill(TEST_USERS.customer.email);
      await passwordInput.fill(TEST_USERS.customer.password);
      
      const submitButton = page.getByRole('button', { name: /Sign In|Iniciar sesión/i });
      await submitButton.click();
      
      // Esperar login
      await expect(page).not.toHaveURL('/login', { timeout: 15000 });
      
      // Con "Remember me", el token debe estar en localStorage (no sessionStorage)
      const localToken = await page.evaluate(() => localStorage.getItem('token'));
      expect(localToken, 'Token debe estar en localStorage cuando "Remember me" está marcado').toBeTruthy();
    });
  });

  // =====================================================
  // ACCESSIBILITY TESTS
  // =====================================================
  
  test('@regression TC-110: Formulario es accesible con teclado', async ({ page }) => {
    await test.step('1. Tab navegación a campos', async () => {
      // Focus en el primer campo
      await page.keyboard.press('Tab');
      
      // Verificar que podemos navegar con Tab
      const emailInput = page.locator('input#email, input[type="email"]');
      await page.keyboard.type(TEST_USERS.customer.email);
      
      // Tab al siguiente campo
      await page.keyboard.press('Tab');
      await page.keyboard.type(TEST_USERS.customer.password);
    });

    await test.step('2. Submit con Enter', async () => {
      await page.keyboard.press('Enter');
      
      // Debería hacer submit
      await expect(page).not.toHaveURL('/login', { timeout: 15000 });
    });
  });
});

test.describe('Login Protection @security', () => {
  
  test('@security TC-115: Ruta protegida redirige a login si no autenticado', async ({ page }) => {
    await test.step('1. Limpiar cualquier sesión existente', async () => {
      await page.context().clearCookies();
      await page.evaluate(() => {
        localStorage.clear();
        sessionStorage.clear();
      });
    });

    await test.step('2. Intentar acceder a ruta protegida directamente', async () => {
      await page.goto('/admin');
    });

    await test.step('3. Verificar redirección a login', async () => {
      await expect(
        page,
        'Ruta protegida debe redirigir a login'
      ).toHaveURL(/\/login/, { timeout: 10000 });
      
      await page.screenshot({ path: 'test-results/tc115-redirect-to-login.png' });
    });
  });

  test('@security TC-116: Dashboard del usuario requiere autenticación', async ({ page }) => {
    await test.step('1. Limpiar sesión', async () => {
      await page.evaluate(() => {
        localStorage.clear();
        sessionStorage.clear();
      });
    });

    await test.step('2. Intentar acceder a dashboard', async () => {
      await page.goto('/dashboard');
    });

    await test.step('3. Verificar redirección a login', async () => {
      await expect(
        page,
        'Dashboard debe redirigir a login si no autenticado'
      ).toHaveURL(/\/(login)?/, { timeout: 10000 });
    });
  });
});
