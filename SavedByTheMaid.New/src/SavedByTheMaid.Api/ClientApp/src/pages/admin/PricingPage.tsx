import { useState, useEffect } from 'react';
import {
  Plus,
  Edit2,
  Trash2,
  Home,
  Bath,
  Square,
  MapPin,
  Percent,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';

// Types
interface RoomPricing {
  id: string;
  roomType: string;
  pricePerUnit: number;
  minUnits: number;
  maxUnits: number;
}

interface SizePricing {
  id: string;
  minSqFt: number;
  maxSqFt: number;
  multiplier: number;
  label: string;
}

interface AdditionalService {
  id: string;
  name: string;
  price: number;
  duration: number;
  isActive: boolean;
}

interface ZonePricing {
  id: string;
  zipCode: string;
  zoneName: string;
  priceMultiplier: number;
  isActive: boolean;
}

export function AdminPricingPage() {
  const [activeTab, setActiveTab] = useState<'rooms' | 'size' | 'addons' | 'zones'>('rooms');
  const [isLoading, setIsLoading] = useState(true);
  
  // State for each pricing type
  const [roomPricing, setRoomPricing] = useState<RoomPricing[]>([]);
  const [sizePricing, setSizePricing] = useState<SizePricing[]>([]);
  const [additionalServices, setAdditionalServices] = useState<AdditionalService[]>([]);
  const [zonePricing, setZonePricing] = useState<ZonePricing[]>([]);

  useEffect(() => {
    fetchPricingData();
  }, []);

  const fetchPricingData = async () => {
    setIsLoading(true);
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Room pricing
    setRoomPricing([
      { id: '1', roomType: 'Habitaciones', pricePerUnit: 15, minUnits: 1, maxUnits: 10 },
      { id: '2', roomType: 'Baños', pricePerUnit: 20, minUnits: 1, maxUnits: 6 },
      { id: '3', roomType: 'Baños medios', pricePerUnit: 10, minUnits: 0, maxUnits: 4 },
    ]);
    
    // Size pricing
    setSizePricing([
      { id: '1', minSqFt: 0, maxSqFt: 1000, multiplier: 1.0, label: 'Pequeño' },
      { id: '2', minSqFt: 1001, maxSqFt: 2000, multiplier: 1.2, label: 'Mediano' },
      { id: '3', minSqFt: 2001, maxSqFt: 3500, multiplier: 1.5, label: 'Grande' },
      { id: '4', minSqFt: 3501, maxSqFt: 99999, multiplier: 2.0, label: 'Muy Grande' },
    ]);
    
    // Additional services
    setAdditionalServices([
      { id: '1', name: 'Interior de refrigerador', price: 35, duration: 30, isActive: true },
      { id: '2', name: 'Interior de horno', price: 25, duration: 20, isActive: true },
      { id: '3', name: 'Interior de gabinetes', price: 45, duration: 45, isActive: true },
      { id: '4', name: 'Lavado de ropa (1 carga)', price: 20, duration: 60, isActive: true },
      { id: '5', name: 'Lavado de platos', price: 15, duration: 20, isActive: true },
      { id: '6', name: 'Cambio de sábanas', price: 10, duration: 15, isActive: true },
      { id: '7', name: 'Limpieza de ventanas (por ventana)', price: 8, duration: 10, isActive: false },
      { id: '8', name: 'Limpieza de garaje', price: 75, duration: 60, isActive: true },
    ]);
    
    // Zone pricing
    setZonePricing([
      { id: '1', zipCode: '33101', zoneName: 'Downtown Miami', priceMultiplier: 1.0, isActive: true },
      { id: '2', zipCode: '33139', zoneName: 'Miami Beach', priceMultiplier: 1.15, isActive: true },
      { id: '3', zipCode: '33125', zoneName: 'Little Havana', priceMultiplier: 0.95, isActive: true },
      { id: '4', zipCode: '33133', zoneName: 'Coconut Grove', priceMultiplier: 1.2, isActive: true },
      { id: '5', zipCode: '33154', zoneName: 'Bal Harbour', priceMultiplier: 1.3, isActive: true },
    ]);
    
    setIsLoading(false);
  };

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const tabs = [
    { id: 'rooms', label: 'Por Habitación', icon: Home },
    { id: 'size', label: 'Por Tamaño', icon: Square },
    { id: 'addons', label: 'Servicios Adicionales', icon: Plus },
    { id: 'zones', label: 'Por Zona', icon: MapPin },
  ];

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-[#00205B] border-t-transparent rounded-full animate-spin" />
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Configuración de Precios</h1>
          <p className="text-gray-600">Gestiona las tarifas y precios de los servicios</p>
        </div>

        {/* Tabs */}
        <div className="border-b border-gray-200">
          <nav className="flex gap-8">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center gap-2 pb-4 border-b-2 font-medium text-sm transition-colors ${
                  activeTab === tab.id
                    ? 'border-[#00205B] text-[#00205B]'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                <tab.icon className="h-4 w-4" />
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        {/* Content */}
        <div className="bg-white rounded-xl shadow-sm border">
          {/* Room Pricing Tab */}
          {activeTab === 'rooms' && (
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">Precios por Habitación</h2>
                  <p className="text-sm text-gray-600">Precio adicional por cada tipo de habitación</p>
                </div>
              </div>
              
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Tipo</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Precio por Unidad</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Mínimo</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Máximo</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {roomPricing.map((item) => (
                      <tr key={item.id} className="border-b last:border-0">
                        <td className="py-4 px-4">
                          <div className="flex items-center gap-2">
                            {item.roomType === 'Habitaciones' ? <Home className="h-4 w-4 text-gray-400" /> : <Bath className="h-4 w-4 text-gray-400" />}
                            <span className="font-medium text-gray-900">{item.roomType}</span>
                          </div>
                        </td>
                        <td className="py-4 px-4">
                          <span className="text-gray-900 font-medium">{formatCurrency(item.pricePerUnit)}</span>
                        </td>
                        <td className="py-4 px-4 text-gray-600">{item.minUnits}</td>
                        <td className="py-4 px-4 text-gray-600">{item.maxUnits}</td>
                        <td className="py-4 px-4 text-right">
                          <button className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg">
                            <Edit2 className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Size Pricing Tab */}
          {activeTab === 'size' && (
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">Precios por Tamaño</h2>
                  <p className="text-sm text-gray-600">Multiplicador de precio según pies cuadrados</p>
                </div>
              </div>
              
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Categoría</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Rango (sq ft)</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Multiplicador</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Ejemplo (base $100)</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sizePricing.map((item) => (
                      <tr key={item.id} className="border-b last:border-0">
                        <td className="py-4 px-4">
                          <span className="font-medium text-gray-900">{item.label}</span>
                        </td>
                        <td className="py-4 px-4 text-gray-600">
                          {item.minSqFt.toLocaleString()} - {item.maxSqFt > 10000 ? '∞' : item.maxSqFt.toLocaleString()}
                        </td>
                        <td className="py-4 px-4">
                          <span className="inline-flex items-center gap-1 px-2 py-1 bg-[#FFE44D]/10 text-[#001440] rounded font-medium">
                            <Percent className="h-3 w-3" />
                            {(item.multiplier * 100).toFixed(0)}%
                          </span>
                        </td>
                        <td className="py-4 px-4 text-gray-900 font-medium">
                          {formatCurrency(100 * item.multiplier)}
                        </td>
                        <td className="py-4 px-4 text-right">
                          <button className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg">
                            <Edit2 className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Additional Services Tab */}
          {activeTab === 'addons' && (
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">Servicios Adicionales</h2>
                  <p className="text-sm text-gray-600">Extras que los clientes pueden agregar</p>
                </div>
                <button className="inline-flex items-center gap-2 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors">
                  <Plus className="h-4 w-4" />
                  Agregar Servicio
                </button>
              </div>
              
              <div className="grid gap-4">
                {additionalServices.map((service) => (
                  <div
                    key={service.id}
                    className={`flex items-center justify-between p-4 border rounded-lg ${
                      service.isActive ? 'bg-white' : 'bg-gray-50 opacity-60'
                    }`}
                  >
                    <div className="flex items-center gap-4">
                      <div>
                        <h3 className="font-medium text-gray-900">{service.name}</h3>
                        <p className="text-sm text-gray-500">{service.duration} min adicionales</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <span className="text-lg font-semibold text-gray-900">{formatCurrency(service.price)}</span>
                      <button
                        onClick={() => {
                          setAdditionalServices(prev =>
                            prev.map(s => s.id === service.id ? { ...s, isActive: !s.isActive } : s)
                          );
                        }}
                        className={`relative w-12 h-6 rounded-full transition-colors ${
                          service.isActive ? 'bg-green-500' : 'bg-gray-300'
                        }`}
                      >
                        <span
                          className={`absolute top-1 w-4 h-4 bg-white rounded-full transition-transform ${
                            service.isActive ? 'left-7' : 'left-1'
                          }`}
                        />
                      </button>
                      <button className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg">
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg">
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Zone Pricing Tab */}
          {activeTab === 'zones' && (
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">Precios por Zona</h2>
                  <p className="text-sm text-gray-600">Ajustes de precio según la ubicación</p>
                </div>
                <button className="inline-flex items-center gap-2 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors">
                  <Plus className="h-4 w-4" />
                  Agregar Zona
                </button>
              </div>
              
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Código Postal</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Zona</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Multiplicador</th>
                      <th className="text-left py-3 px-4 font-medium text-gray-600">Estado</th>
                      <th className="text-right py-3 px-4 font-medium text-gray-600">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {zonePricing.map((zone) => (
                      <tr key={zone.id} className={`border-b last:border-0 ${!zone.isActive ? 'opacity-50' : ''}`}>
                        <td className="py-4 px-4">
                          <div className="flex items-center gap-2">
                            <MapPin className="h-4 w-4 text-gray-400" />
                            <span className="font-mono font-medium text-gray-900">{zone.zipCode}</span>
                          </div>
                        </td>
                        <td className="py-4 px-4 text-gray-600">{zone.zoneName}</td>
                        <td className="py-4 px-4">
                          <span className={`inline-flex items-center gap-1 px-2 py-1 rounded font-medium ${
                            zone.priceMultiplier > 1 
                              ? 'bg-amber-50 text-amber-700' 
                              : zone.priceMultiplier < 1 
                                ? 'bg-green-50 text-green-700'
                                : 'bg-gray-50 text-gray-700'
                          }`}>
                            {zone.priceMultiplier > 1 ? '+' : ''}{((zone.priceMultiplier - 1) * 100).toFixed(0)}%
                          </span>
                        </td>
                        <td className="py-4 px-4">
                          <span className={`text-sm ${zone.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                            {zone.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        <td className="py-4 px-4 text-right">
                          <button className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg">
                            <Edit2 className="h-4 w-4" />
                          </button>
                          <button className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg">
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>

        {/* Quick Summary */}
        <div className="bg-gradient-to-r from-[#FFE44D]/50 to-blue-600 rounded-xl p-6 text-white">
          <h3 className="text-lg font-semibold mb-2">💡 Cómo se calcula el precio</h3>
          <p className="text-sky-100 text-sm">
            Precio Final = (Precio Base del Servicio + Precio por Habitaciones + Precio por Baños) × Multiplicador de Tamaño × Multiplicador de Zona + Servicios Adicionales
          </p>
          <div className="mt-4 p-4 bg-white/10 rounded-lg">
            <p className="text-sm font-medium">Ejemplo:</p>
            <p className="text-xs text-sky-100 mt-1">
              Limpieza Regular ($85) + 3 habitaciones ($45) + 2 baños ($40) = $170 × 1.2 (mediano) × 1.15 (Miami Beach) + Interior refrigerador ($35) = <strong className="text-white">$269.60</strong>
            </p>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
