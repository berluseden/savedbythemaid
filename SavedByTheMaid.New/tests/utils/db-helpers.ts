import mysql from 'mysql2/promise';

/**
 * Helpers para interactuar con la base de datos en tests
 */
export class DbHelpers {
  private connection: mysql.Connection | null = null;

  async connect() {
    if (this.connection) return;

    this.connection = await mysql.createConnection({
      host: process.env.DB_HOST || 'localhost',
      port: parseInt(process.env.DB_PORT || '3306'),
      user: process.env.DB_USER || 'root',
      password: process.env.DB_PASSWORD || 'Root@123456',
      database: process.env.DB_NAME || 'SavedByTheMaidNew'
    });
  }

  /**
   * Limpia datos de prueba por email
   */
  async cleanupTestData(email: string) {
    await this.connect();
    
    try {
      // Eliminar órdenes de prueba
      await this.connection!.execute(
        'DELETE FROM ServiceOrders WHERE ContactEmail = ?',
        [email]
      );

      // Eliminar soft reserves de prueba
      await this.connection!.execute(
        'DELETE FROM SoftReserves WHERE SessionId LIKE ?',
        ['test-%']
      );
    } catch (error) {
      console.error('Error cleaning up test data:', error);
    }
  }

  /**
   * Obtiene orden por email
   */
  async getOrderByEmail(email: string): Promise<any> {
    await this.connect();
    
    const [rows] = await this.connection!.execute(
      'SELECT * FROM ServiceOrders WHERE ContactEmail = ? ORDER BY CreatedAt DESC LIMIT 1',
      [email]
    );
    
    return (rows as any[])[0] || null;
  }

  /**
   * Verifica si existe un SoftReserve activo
   */
  async hasSoftReserve(employeeId: number, scheduledStart: Date): Promise<boolean> {
    await this.connect();
    
    const [rows] = await this.connection!.execute(
      `SELECT COUNT(*) as count 
       FROM SoftReserves 
       WHERE EmployeeId = ? 
         AND ScheduledStart = ? 
         AND Status = 'Active'`,
      [employeeId, scheduledStart]
    );
    
    return (rows as any[])[0].count > 0;
  }

  /**
   * Marca un SoftReserve como expirado (para testing)
   */
  async expireSoftReserve(softReserveId: number) {
    await this.connect();
    
    await this.connection!.execute(
      `UPDATE SoftReserves 
       SET Status = 'Expired', ExpiresAt = DATE_SUB(NOW(), INTERVAL 1 HOUR)
       WHERE Id = ?`,
      [softReserveId]
    );
  }

  /**
   * Obtiene empleadas disponibles en una zona
   */
  async getAvailableEmployees(serviceAreaId: number): Promise<any[]> {
    await this.connect();
    
    const [rows] = await this.connection!.execute(
      `SELECT e.* 
       FROM Employees e
       INNER JOIN EmployeeServiceAreas esa ON e.Id = esa.EmployeeId
       WHERE esa.ServiceAreaId = ? AND e.IsActive = 1`,
      [serviceAreaId]
    );
    
    return rows as any[];
  }

  /**
   * Crea datos de prueba (seed)
   */
  async seedTestData() {
    await this.connect();
    
    // Verificar si ya existen datos
    const [serviceTypes] = await this.connection!.execute(
      'SELECT COUNT(*) as count FROM ServiceTypes'
    );
    
    if ((serviceTypes as any[])[0].count > 0) {
      console.log('Test data already exists, skipping seed');
      return;
    }

    // Insertar ServiceTypes de prueba
    await this.connection!.execute(`
      INSERT INTO ServiceTypes (Name, Description, Price, EstimatedMinutes, IsActive, CreatedAt)
      VALUES 
        ('Standard Clean', 'Regular cleaning service', 100.00, 120, 1, NOW()),
        ('Deep Clean', 'Thorough deep cleaning', 150.00, 180, 1, NOW()),
        ('Move In/Out', 'Moving cleaning service', 200.00, 240, 1, NOW())
    `);

    console.log('Test data seeded successfully');
  }

  /**
   * Cierra la conexión
   */
  async close() {
    if (this.connection) {
      await this.connection.end();
      this.connection = null;
    }
  }
}

// Singleton instance
export const db = new DbHelpers();
