import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * Page Object para el Booking Wizard
 */
export class BookingPage extends BasePage {
  // Locators - Step 1: ZIP Code
  readonly zipInput: Locator;
  readonly checkCoverageBtn: Locator;
  readonly coverageMessage: Locator;

  // Locators - Step 2: Service Selection
  readonly serviceCards: Locator;

  // Locators - Step 3: Details
  readonly bedroomsInput: Locator;
  readonly bathroomsInput: Locator;
  readonly squareFeetSlider: Locator;
  readonly getEstimateBtn: Locator;

  // Locators - Step 4: Schedule
  readonly calendarDays: Locator;
  readonly timeSlots: Locator;
  readonly reserveSlotBtn: Locator;

  // Locators - Step 5: Contact
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly phoneInput: Locator;
  readonly addressInput: Locator;

  // Locators - Step 6: Confirm
  readonly confirmButton: Locator;
  readonly orderSummary: Locator;

  // Common
  readonly nextButton: Locator;
  readonly backButton: Locator;
  readonly priceEstimate: Locator;

  constructor(page: Page) {
    super(page);
    
    // Step 1
    this.zipInput = page.getByPlaceholder(/zip code|código postal/i);
    this.checkCoverageBtn = page.getByRole('button', { name: /check coverage|verificar cobertura/i });
    this.coverageMessage = page.locator('[data-testid="coverage-message"]');

    // Step 2
    this.serviceCards = page.locator('[data-testid="service-card"], .service-card');

    // Step 3
    this.bedroomsInput = page.getByLabel(/bedrooms?|habitaciones/i);
    this.bathroomsInput = page.getByLabel(/bathrooms?|baños/i);
    this.squareFeetSlider = page.getByLabel(/square feet|pies cuadrados/i);
    this.getEstimateBtn = page.getByRole('button', { name: /get estimate|obtener estimado/i });

    // Step 4
    this.calendarDays = page.locator('.calendar-day, [data-testid="calendar-day"]');
    this.timeSlots = page.locator('[data-testid="time-slot"], .time-slot button');
    this.reserveSlotBtn = page.getByRole('button', { name: /reserve slot|reservar/i });

    // Step 5
    this.firstNameInput = page.getByLabel(/first name|nombre/i);
    this.lastNameInput = page.getByLabel(/last name|apellido/i);
    this.emailInput = page.getByLabel(/email|correo/i);
    this.phoneInput = page.getByLabel(/phone|teléfono/i);
    this.addressInput = page.getByLabel(/address|dirección/i);

    // Step 6
    this.confirmButton = page.getByRole('button', { name: /confirm booking|confirmar reserva/i });
    this.orderSummary = page.locator('[data-testid="order-summary"]');

    // Common
    this.nextButton = page.getByRole('button', { name: /next|continue|siguiente|continuar/i });
    this.backButton = page.getByRole('button', { name: /back|atrás/i });
    this.priceEstimate = page.locator('[data-testid="price-estimate"], .price-estimate');
  }

  /**
   * Navega al wizard de reservas
   */
  async gotoBooking() {
    await this.goto('/booking');
    await expect(this.page).toHaveTitle(/booking|reserva/i);
  }

  /**
   * STEP 1: Verifica cobertura por ZIP code
   */
  async checkCoverage(zipCode: string): Promise<boolean> {
    await this.zipInput.waitFor({ state: 'visible' });
    await this.zipInput.fill(zipCode);
    await this.checkCoverageBtn.click();
    
    // Esperar respuesta del API
    const response = await this.waitForAPIResponse('/api/booking/coverage');
    const body = await response.json();
    
    if (body.isCovered) {
      await expect(this.coverageMessage).toContainText(/excelente|excellent|we serve/i);
      await this.nextButton.click();
      return true;
    } else {
      await expect(this.coverageMessage).toContainText(/lo sentimos|sorry|not serve/i);
      return false;
    }
  }

  /**
   * STEP 2: Selecciona un tipo de servicio
   */
  async selectService(serviceName: string) {
    await this.page.getByText(serviceName, { exact: true }).click();
    await expect(this.page.getByText(serviceName)).toHaveClass(/selected|active/);
    await this.nextButton.click();
    await this.waitForLoading();
  }

  /**
   * STEP 3: Llena detalles de la propiedad y obtiene estimado
   */
  async fillPropertyDetails(details: {
    bedrooms: number;
    bathrooms: number;
    squareFeet: number;
  }): Promise<{ total: number; estimatedMinutes: number }> {
    // Llenar inputs
    await this.bedroomsInput.fill(details.bedrooms.toString());
    await this.bathroomsInput.fill(details.bathrooms.toString());
    
    // Slider de square feet
    await this.squareFeetSlider.fill(details.squareFeet.toString());
    
    // Obtener estimado
    await this.getEstimateBtn.click();
    
    const response = await this.waitForAPIResponse('/api/booking/estimate');
    const estimate = await response.json();
    
    // Verificar que el estimado se muestra en la UI
    await expect(this.priceEstimate).toBeVisible();
    const priceText = await this.priceEstimate.textContent();
    expect(priceText).toContain(estimate.total);
    
    await this.nextButton.click();
    
    return {
      total: parseFloat(estimate.total),
      estimatedMinutes: estimate.estimatedMinutes
    };
  }

  /**
   * STEP 4: Selecciona fecha y hora (crea SoftReserve)
   */
  async selectTimeSlot(dayOffset: number, timeString: string): Promise<{
    softReserveId: number;
    sessionId: string;
  }> {
    // Seleccionar día (offset desde hoy)
    await this.calendarDays.nth(dayOffset).click();
    await this.waitForLoading();
    
    // Seleccionar hora
    const timeSlot = this.page.getByRole('button', { name: new RegExp(timeString, 'i') });
    await timeSlot.waitFor({ state: 'visible' });
    await timeSlot.click();
    
    // Reservar slot (crea SoftReserve en backend)
    const softReservePromise = this.waitForAPIResponse('/api/booking/soft-reserve');
    await this.reserveSlotBtn.click();
    
    const response = await softReservePromise;
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Verificar mensaje de confirmación
    await expect(
      this.page.getByText(/reserved for 15 minutes|reservado por 15 minutos/i)
    ).toBeVisible();
    
    await this.nextButton.click();
    
    return {
      softReserveId: data.softReserveId,
      sessionId: data.sessionId
    };
  }

  /**
   * STEP 5: Llena información de contacto
   */
  async fillContactInfo(contact: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    address: string;
    city?: string;
    state?: string;
  }) {
    await this.firstNameInput.fill(contact.firstName);
    await this.lastNameInput.fill(contact.lastName);
    await this.emailInput.fill(contact.email);
    await this.phoneInput.fill(contact.phone);
    await this.addressInput.fill(contact.address);
    
    if (contact.city) {
      await this.fillInput(/city|ciudad/i, contact.city);
    }
    
    if (contact.state) {
      await this.fillInput(/state|estado/i, contact.state);
    }
    
    await this.nextButton.click();
  }

  /**
   * STEP 6: Confirma la reserva
   */
  async confirmBooking(): Promise<{
    serviceOrderId: number;
    orderNumber: string;
    total: number;
  }> {
    // Verificar resumen de orden
    await expect(this.orderSummary).toBeVisible();
    
    // Confirmar
    const confirmPromise = this.waitForAPIResponse('/api/booking/confirm');
    await this.confirmButton.click();
    
    const response = await confirmPromise;
    expect(response.status()).toBe(200);
    
    const confirmation = await response.json();
    
    // Verificar página de éxito
    await expect(
      this.page.getByText(/booking confirmed|reserva confirmada|success/i)
    ).toBeVisible();
    
    // Debe mostrar número de orden
    await expect(
      this.page.getByText(/#\d+/)
    ).toBeVisible();
    
    return {
      serviceOrderId: confirmation.serviceOrderId,
      orderNumber: confirmation.orderNumber || `#${confirmation.serviceOrderId}`,
      total: confirmation.total
    };
  }

  /**
   * Helper: Flujo completo de reserva (Happy Path)
   */
  async completeBookingFlow(data: {
    zipCode: string;
    serviceName: string;
    bedrooms: number;
    bathrooms: number;
    squareFeet: number;
    dayOffset: number;
    timeSlot: string;
    contact: {
      firstName: string;
      lastName: string;
      email: string;
      phone: string;
      address: string;
    };
  }) {
    await this.gotoBooking();
    
    const hasCoverage = await this.checkCoverage(data.zipCode);
    expect(hasCoverage).toBe(true);
    
    await this.selectService(data.serviceName);
    
    const estimate = await this.fillPropertyDetails({
      bedrooms: data.bedrooms,
      bathrooms: data.bathrooms,
      squareFeet: data.squareFeet
    });
    
    const reserve = await this.selectTimeSlot(data.dayOffset, data.timeSlot);
    
    await this.fillContactInfo(data.contact);
    
    const confirmation = await this.confirmBooking();
    
    return {
      estimate,
      reserve,
      confirmation
    };
  }

  /**
   * Helper: Cancela SoftReserve haciendo Back
   */
  async cancelSoftReserve() {
    await this.backButton.click();
    
    // Debería regresar al paso anterior y el slot debe estar disponible
    await expect(this.timeSlots.first()).toBeEnabled();
  }

  /**
   * Helper: Obtiene el precio total mostrado
   */
  async getDisplayedTotal(): Promise<number> {
    const text = await this.priceEstimate.textContent();
    const match = text?.match(/\$?([\d,]+\.?\d*)/);
    return match ? parseFloat(match[1].replace(',', '')) : 0;
  }
}
