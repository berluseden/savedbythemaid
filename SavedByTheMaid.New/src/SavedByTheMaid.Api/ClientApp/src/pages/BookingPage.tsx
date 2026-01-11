import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useNavigate, Link } from 'react-router-dom';
import { MapPin, Home, Sparkles, Calendar, User, CreditCard, CheckCircle } from 'lucide-react';
import { Button, Input, Card, CardContent, Spinner, Modal } from '@/components/ui';
import api, { bookingApi, type ServiceType, type CleaningPlace, type EstimateResponse, type BookingConfirmation } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { cn, formatCurrency } from '@/lib/utils';

type BookingStep = 'zipcode' | 'service' | 'details' | 'schedule' | 'contact' | 'confirm';

interface BookingData {
  zipCode: string;
  serviceTypeId: number;
  cleaningPlaceId: number;
  numberOfRooms: number;
  numberOfBathrooms: number;
  squareFeet: number;
  additionalServiceIds: number[];
  date: string;
  timeSlot: string;
  employeeId: number;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  specialInstructions: string;
  softReserveId?: number;
  sessionId?: string;
}

const initialBookingData: BookingData = {
  zipCode: '',
  serviceTypeId: 0,
  cleaningPlaceId: 0,
  numberOfRooms: 2,
  numberOfBathrooms: 1,
  squareFeet: 1000,
  additionalServiceIds: [],
  date: '',
  timeSlot: '',
  employeeId: 0,
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  phone: '',
  address: '',
  city: '',
  state: '',
  specialInstructions: '',
};

const steps: { id: BookingStep; title: string; icon: typeof MapPin }[] = [
  { id: 'zipcode', title: 'Location', icon: MapPin },
  { id: 'service', title: 'Service', icon: Sparkles },
  { id: 'details', title: 'Details', icon: Home },
  { id: 'schedule', title: 'Schedule', icon: Calendar },
  { id: 'contact', title: 'Contact', icon: User },
  { id: 'confirm', title: 'Confirm', icon: CreditCard },
];

export default function BookingPage() {
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState<BookingStep>('zipcode');
  const [bookingData, setBookingData] = useState<BookingData>(initialBookingData);
  const [estimate, setEstimate] = useState<EstimateResponse | null>(null);

  const currentStepIndex = steps.findIndex((s) => s.id === currentStep);

  const updateBookingData = (data: Partial<BookingData>) => {
    setBookingData((prev) => ({ ...prev, ...data }));
  };

  const goToNextStep = () => {
    const nextIndex = currentStepIndex + 1;
    if (nextIndex < steps.length) {
      setCurrentStep(steps[nextIndex].id);
    }
  };

  const goToPreviousStep = () => {
    const prevIndex = currentStepIndex - 1;
    if (prevIndex >= 0) {
      setCurrentStep(steps[prevIndex].id);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="mx-auto max-w-4xl px-4">
        {/* Progress Steps */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            {steps.map((step, index) => (
              <div key={step.id} className="flex items-center">
                <div
                  className={cn(
                    'flex h-10 w-10 items-center justify-center rounded-full border-2 transition-colors',
                    index < currentStepIndex
                      ? 'border-sky-500 bg-sky-500 text-white'
                      : index === currentStepIndex
                      ? 'border-sky-500 bg-white text-sky-500'
                      : 'border-gray-300 bg-white text-gray-400'
                  )}
                >
                  {index < currentStepIndex ? (
                    <CheckCircle className="h-5 w-5" />
                  ) : (
                    <step.icon className="h-5 w-5" />
                  )}
                </div>
                {index < steps.length - 1 && (
                  <div
                    className={cn(
                      'hidden sm:block h-0.5 w-12 lg:w-24',
                      index < currentStepIndex ? 'bg-sky-500' : 'bg-gray-300'
                    )}
                  />
                )}
              </div>
            ))}
          </div>
          <div className="mt-4 flex justify-between">
            {steps.map((step, index) => (
              <span
                key={step.id}
                className={cn(
                  'text-xs font-medium',
                  index <= currentStepIndex ? 'text-sky-600' : 'text-gray-400'
                )}
              >
                {step.title}
              </span>
            ))}
          </div>
        </div>

        {/* Step Content */}
        <Card className="overflow-hidden">
          <CardContent className="p-6 sm:p-8">
            {currentStep === 'zipcode' && (
              <ZipCodeStep
                value={bookingData.zipCode}
                onChange={(zipCode) => updateBookingData({ zipCode })}
                onNext={goToNextStep}
              />
            )}
            {currentStep === 'service' && (
              <ServiceStep
                selectedId={bookingData.serviceTypeId}
                onChange={(serviceTypeId) => updateBookingData({ serviceTypeId })}
                onNext={goToNextStep}
                onBack={goToPreviousStep}
              />
            )}
            {currentStep === 'details' && (
              <DetailsStep
                data={bookingData}
                onChange={updateBookingData}
                onEstimate={setEstimate}
                onNext={goToNextStep}
                onBack={goToPreviousStep}
              />
            )}
            {currentStep === 'schedule' && (
              <ScheduleStep
                data={bookingData}
                estimate={estimate}
                onChange={updateBookingData}
                onNext={goToNextStep}
                onBack={goToPreviousStep}
              />
            )}
            {currentStep === 'contact' && (
              <ContactStep
                data={bookingData}
                onChange={updateBookingData}
                onNext={goToNextStep}
                onBack={goToPreviousStep}
              />
            )}
            {currentStep === 'confirm' && (
              <ConfirmStep
                data={bookingData}
                estimate={estimate}
                onBack={goToPreviousStep}
                onSuccess={(confirmation) => navigate('/booking/success', { state: { confirmation, bookingData, estimate } })}
              />
            )}
          </CardContent>
        </Card>

        {/* Price Summary (sticky on desktop) */}
        {estimate && currentStep !== 'zipcode' && currentStep !== 'service' && (
          <Card className="mt-6 p-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600">Estimated Total</span>
              <span className="text-2xl font-bold text-gray-900">{formatCurrency(estimate.total)}</span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              ~{Math.round(estimate.estimatedMinutes / 60)} hours estimated
            </p>
          </Card>
        )}
      </div>
    </div>
  );
}

// ==========================================
// Step Components
// ==========================================

function ZipCodeStep({
  value,
  onChange,
  onNext,
}: {
  value: string;
  onChange: (value: string) => void;
  onNext: () => void;
}) {
  const [zipCode, setZipCode] = useState(value);
  const [error, setError] = useState('');
  const [coverageInfo, setCoverageInfo] = useState<{ city?: string; state?: string } | null>(null);

  const checkCoverage = useMutation({
    mutationFn: (zip: string) => bookingApi.checkCoverage(zip),
    onSuccess: (response) => {
      if (response.data.isCovered) {
        setCoverageInfo({ city: response.data.city, state: response.data.state });
        onChange(zipCode);
        // Auto-advance after showing success message
        setTimeout(() => onNext(), 1500);
      } else {
        setError(response.data.message || 'Sorry, we do not service this area yet.');
      }
    },
    onError: () => {
      setError('Unable to check coverage. Please try again.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!/^\d{5}$/.test(zipCode)) {
      setError('Please enter a valid 5-digit ZIP code');
      return;
    }
    setError('');
    setCoverageInfo(null);
    checkCoverage.mutate(zipCode);
  };

  return (
    <div className="text-center max-w-md mx-auto">
      <MapPin className="mx-auto h-16 w-16 text-sky-500 mb-6" />
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Where do you need cleaning?</h2>
      <p className="text-gray-600 mb-8">Enter your ZIP code to check if we service your area.</p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          type="text"
          placeholder="Enter ZIP code"
          value={zipCode}
          onChange={(e) => setZipCode(e.target.value.replace(/\D/g, '').slice(0, 5))}
          className="text-center text-2xl tracking-widest"
          maxLength={5}
          error={error}
          disabled={!!coverageInfo}
        />
        
        {coverageInfo && (
          <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
            <p className="text-green-800 font-medium">
              ✓ Great news! We service {coverageInfo.city || 'your area'}
              {coverageInfo.state ? `, ${coverageInfo.state}` : ''}.
            </p>
            <p className="text-green-600 text-sm mt-1">Continuing to next step...</p>
          </div>
        )}
        
        {!coverageInfo && (
          <Button type="submit" className="w-full" loading={checkCoverage.isPending}>
            Check Availability
          </Button>
        )}
      </form>
    </div>
  );
}

function ServiceStep({
  selectedId,
  onChange,
  onNext,
  onBack,
}: {
  selectedId: number;
  onChange: (id: number) => void;
  onNext: () => void;
  onBack: () => void;
}) {
  const { data: serviceTypes, isLoading } = useQuery({
    queryKey: ['serviceTypes'],
    queryFn: () => bookingApi.getServiceTypes(),
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Choose your service</h2>
      <p className="text-gray-600 mb-8">Select the type of cleaning you need.</p>

      <div className="grid gap-4 sm:grid-cols-2">
        {serviceTypes?.data.map((service: ServiceType) => (
          <button
            key={service.id}
            onClick={() => onChange(service.id)}
            className={cn(
              'p-6 rounded-xl border-2 text-left transition-all',
              selectedId === service.id
                ? 'border-sky-500 bg-sky-50 ring-2 ring-sky-500/20'
                : 'border-gray-200 hover:border-gray-300'
            )}
          >
            <Sparkles className={cn('h-8 w-8 mb-3', selectedId === service.id ? 'text-sky-500' : 'text-gray-400')} />
            <h3 className="font-semibold text-gray-900">{service.name}</h3>
            <p className="text-sm text-gray-600 mt-1">{service.description}</p>
            <p className="text-lg font-bold text-sky-600 mt-3">From {formatCurrency(service.price)}</p>
          </button>
        ))}
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack}>Back</Button>
        <Button onClick={onNext} disabled={!selectedId}>Continue</Button>
      </div>
    </div>
  );
}

function DetailsStep({
  data,
  onChange,
  onEstimate,
  onNext,
  onBack,
}: {
  data: BookingData;
  onChange: (data: Partial<BookingData>) => void;
  onEstimate: (estimate: EstimateResponse) => void;
  onNext: () => void;
  onBack: () => void;
}) {
  const { data: cleaningPlaces, isLoading: loadingPlaces } = useQuery({
    queryKey: ['cleaningPlaces'],
    queryFn: () => bookingApi.getCleaningPlaces(),
  });

  const { data: serviceTypes } = useQuery({
    queryKey: ['serviceTypes'],
    queryFn: () => bookingApi.getServiceTypes(),
  });

  const { data: additionalServices } = useQuery({
    queryKey: ['additionalServices'],
    queryFn: () => bookingApi.getAdditionalServices(),
  });

  // Calcular precio en tiempo real
  const selectedService = serviceTypes?.data.find((s: ServiceType) => s.id === data.serviceTypeId);
  
  const calculatePrice = (): { basePrice: number; bedroomExtra: number; bathroomExtra: number; extrasTotal: number; total: number } => {
    if (!selectedService) return { basePrice: 0, bedroomExtra: 0, bathroomExtra: 0, extrasTotal: 0, total: 0 };
    
    const basePrice = selectedService.price;
    const bedroomExtra = Math.max(0, data.numberOfRooms - 1) * selectedService.pricePerBedroom;
    const bathroomExtra = Math.max(0, data.numberOfBathrooms - 1) * selectedService.pricePerBathroom;
    
    const extrasTotal = data.additionalServiceIds.reduce((sum, id) => {
      const extra = additionalServices?.data.find((s: { id: number; price: number }) => s.id === id);
      return sum + (extra?.price || 0);
    }, 0);

    const subtotal = basePrice + bedroomExtra + bathroomExtra + extrasTotal;
    
    return {
      basePrice,
      bedroomExtra,
      bathroomExtra,
      extrasTotal,
      total: subtotal,
    };
  };

  const priceCalc = calculatePrice();

  const handleContinue = () => {
    // Crear estimate local
    const estimatedMinutes = selectedService 
      ? selectedService.estimatedMinutes + 
        (data.numberOfRooms - 1) * selectedService.minutesPerBedroom + 
        (data.numberOfBathrooms - 1) * selectedService.minutesPerBathroom
      : 120;

    onEstimate({
      basePrice: priceCalc.basePrice,
      additionalServicesPrice: priceCalc.extrasTotal,
      subtotal: priceCalc.total,
      tax: 0,
      total: priceCalc.total,
      estimatedMinutes,
      breakdown: [],
    });
    onNext();
  };

  const toggleExtra = (id: number) => {
    const current = data.additionalServiceIds;
    if (current.includes(id)) {
      onChange({ additionalServiceIds: current.filter(x => x !== id) });
    } else {
      onChange({ additionalServiceIds: [...current, id] });
    }
  };

  if (loadingPlaces) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Tell us about your space</h2>
      <p className="text-gray-600 mb-8">Help us give you an accurate estimate.</p>

      <div className="space-y-6">
        {/* Property Type */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-3">Property Type</label>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {cleaningPlaces?.data.map((place: CleaningPlace) => (
              <button
                key={place.id}
                onClick={() => onChange({ cleaningPlaceId: place.id })}
                className={cn(
                  'p-4 rounded-lg border-2 text-center transition-all',
                  data.cleaningPlaceId === place.id
                    ? 'border-sky-500 bg-sky-50'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <Home className={cn('h-6 w-6 mx-auto mb-2', data.cleaningPlaceId === place.id ? 'text-sky-500' : 'text-gray-400')} />
                <span className="text-sm font-medium">{place.name}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Bedrooms & Bathrooms */}
        <div className="grid grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Bedrooms</label>
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={() => onChange({ numberOfRooms: Math.max(1, data.numberOfRooms - 1) })}
              >
                -
              </Button>
              <span className="text-xl font-semibold w-8 text-center">{data.numberOfRooms}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => onChange({ numberOfRooms: Math.min(10, data.numberOfRooms + 1) })}
              >
                +
              </Button>
            </div>
            {data.numberOfRooms > 1 && selectedService && (
              <p className="text-xs text-gray-500 mt-1">
                +{formatCurrency(selectedService.pricePerBedroom)} each extra
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Bathrooms</label>
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={() => onChange({ numberOfBathrooms: Math.max(1, data.numberOfBathrooms - 1) })}
              >
                -
              </Button>
              <span className="text-xl font-semibold w-8 text-center">{data.numberOfBathrooms}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => onChange({ numberOfBathrooms: Math.min(10, data.numberOfBathrooms + 1) })}
              >
                +
              </Button>
            </div>
            {data.numberOfBathrooms > 1 && selectedService && (
              <p className="text-xs text-gray-500 mt-1">
                +{formatCurrency(selectedService.pricePerBathroom)} each extra
              </p>
            )}
          </div>
        </div>

        {/* Additional Services */}
        {additionalServices?.data && additionalServices.data.length > 0 && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-3">
              Additional Services <span className="text-gray-400 font-normal">(Optional)</span>
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {additionalServices.data.map((extra: { id: number; title: string; description?: string; price: number }) => (
                <button
                  key={extra.id}
                  onClick={() => toggleExtra(extra.id)}
                  className={cn(
                    'p-4 rounded-lg border-2 text-left transition-all',
                    data.additionalServiceIds.includes(extra.id)
                      ? 'border-sky-500 bg-sky-50'
                      : 'border-gray-200 hover:border-gray-300'
                  )}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{extra.title}</p>
                      {extra.description && (
                        <p className="text-sm text-gray-500 mt-1">{extra.description}</p>
                      )}
                    </div>
                    <span className="text-sky-600 font-semibold">+{formatCurrency(extra.price)}</span>
                  </div>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Price Summary */}
        <div className="bg-gray-50 rounded-xl p-6 border border-gray-200">
          <h3 className="font-semibold text-gray-900 mb-4">Your Estimate</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">{selectedService?.name || 'Service'} (base)</span>
              <span className="font-medium">{formatCurrency(priceCalc.basePrice)}</span>
            </div>
            {priceCalc.bedroomExtra > 0 && (
              <div className="flex justify-between">
                <span className="text-gray-600">{data.numberOfRooms - 1} extra bedroom(s)</span>
                <span className="font-medium">+{formatCurrency(priceCalc.bedroomExtra)}</span>
              </div>
            )}
            {priceCalc.bathroomExtra > 0 && (
              <div className="flex justify-between">
                <span className="text-gray-600">{data.numberOfBathrooms - 1} extra bathroom(s)</span>
                <span className="font-medium">+{formatCurrency(priceCalc.bathroomExtra)}</span>
              </div>
            )}
            {priceCalc.extrasTotal > 0 && (
              <div className="flex justify-between">
                <span className="text-gray-600">Additional services</span>
                <span className="font-medium">+{formatCurrency(priceCalc.extrasTotal)}</span>
              </div>
            )}
            <div className="border-t pt-2 mt-2 flex justify-between text-lg">
              <span className="font-semibold text-gray-900">Total</span>
              <span className="font-bold text-sky-600">{formatCurrency(priceCalc.total)}</span>
            </div>
          </div>
        </div>
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack}>Back</Button>
        <Button onClick={handleContinue} disabled={!data.cleaningPlaceId}>
          Continue
        </Button>
      </div>
    </div>
  );
}

function ScheduleStep({
  data,
  estimate,
  onChange,
  onNext,
  onBack,
}: {
  data: BookingData;
  estimate: EstimateResponse | null;
  onChange: (data: Partial<BookingData>) => void;
  onNext: () => void;
  onBack: () => void;
}) {
  const [selectedDate, setSelectedDate] = useState(data.date);

  // Generate next 14 days
  const dates = Array.from({ length: 14 }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() + i + 1);
    return date.toISOString().split('T')[0];
  });

  const { data: availability, isLoading } = useQuery({
    queryKey: ['availability', selectedDate, data.zipCode, estimate?.estimatedMinutes],
    queryFn: () =>
      bookingApi.getAvailability(data.zipCode, selectedDate, estimate?.estimatedMinutes || 120),
    enabled: !!selectedDate && !!estimate,
  });

  const createReservation = useMutation({
    mutationFn: () =>
      bookingApi.createSoftReserve({
        date: new Date(data.date),
        startTime: data.timeSlot,
        estimatedMinutes: estimate?.estimatedMinutes || 120,
        zipCode: data.zipCode,
        employeeId: data.employeeId,
      }),
    onSuccess: (response) => {
      onChange({ 
        softReserveId: response.data.softReserveId,
        sessionId: response.data.sessionId 
      });
      onNext();
    },
  });

  const handleDateSelect = (date: string) => {
    setSelectedDate(date);
    onChange({ date, timeSlot: '' });
  };

  const handleTimeSelect = (timeSlot: string) => {
    onChange({ timeSlot });
  };

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Pick a date & time</h2>
      <p className="text-gray-600 mb-8">Choose when you'd like us to come.</p>

      {/* Date Selection */}
      <div className="mb-8">
        <label className="block text-sm font-medium text-gray-700 mb-3">Select Date</label>
        <div className="flex gap-2 overflow-x-auto pb-2">
          {dates.map((date) => {
            const d = new Date(date);
            const isSelected = selectedDate === date;
            return (
              <button
                key={date}
                onClick={() => handleDateSelect(date)}
                className={cn(
                  'flex-shrink-0 w-16 p-3 rounded-lg border-2 text-center transition-all',
                  isSelected
                    ? 'border-sky-500 bg-sky-50'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <p className="text-xs text-gray-500">{d.toLocaleDateString('en-US', { weekday: 'short' })}</p>
                <p className="text-lg font-bold">{d.getDate()}</p>
                <p className="text-xs text-gray-500">{d.toLocaleDateString('en-US', { month: 'short' })}</p>
              </button>
            );
          })}
        </div>
      </div>

      {/* Time Selection */}
      {selectedDate && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-3">Select Time</label>
          {isLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : (
            <div className="grid grid-cols-3 sm:grid-cols-4 gap-3">
              {availability?.data.slots?.map((slot) => {
                const isAvailable = slot.availableEmployeeIds.length > 0;
                const isSelected = data.timeSlot === slot.startTime;
                
                return (
                  <button
                    key={slot.startTime}
                    onClick={() => {
                      if (isAvailable) {
                        handleTimeSelect(slot.startTime);
                        // Seleccionar primer empleado disponible
                        onChange({ employeeId: slot.availableEmployeeIds[0] });
                      }
                    }}
                    disabled={!isAvailable}
                    className={cn(
                      'p-3 rounded-lg border-2 text-sm font-medium transition-all',
                      !isAvailable && 'opacity-50 cursor-not-allowed bg-gray-100',
                      isSelected
                        ? 'border-sky-500 bg-sky-50 text-sky-700'
                        : 'border-gray-200 hover:border-gray-300'
                    )}
                  >
                    {slot.formattedTime}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack}>Back</Button>
        <Button
          onClick={() => createReservation.mutate()}
          loading={createReservation.isPending}
          disabled={!data.date || !data.timeSlot}
        >
          Continue
        </Button>
      </div>
    </div>
  );
}

function ContactStep({
  data,
  onChange,
  onNext,
  onBack,
}: {
  data: BookingData;
  onChange: (data: Partial<BookingData>) => void;
  onNext: () => void;
  onBack: () => void;
}) {
  const { login } = useAuth();
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [checkingEmail, setCheckingEmail] = useState(false);
  const [loginPassword, setLoginPassword] = useState('');
  const [loginError, setLoginError] = useState('');
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  const checkEmailAndProceed = async () => {
    // Validar campos básicos primero
    const newErrors: Record<string, string> = {};
    if (!data.firstName) newErrors.firstName = 'First name is required';
    if (!data.lastName) newErrors.lastName = 'Last name is required';
    if (!data.email || !/\S+@\S+\.\S+/.test(data.email)) newErrors.email = 'Valid email is required';
    if (!data.phone || data.phone.length < 10) newErrors.phone = 'Valid phone is required';
    if (!data.address) newErrors.address = 'Address is required';
    if (!data.city) newErrors.city = 'City is required';
    if (!data.state) newErrors.state = 'State is required';
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }
    setErrors({});

    // Si ya tiene password, continuar
    if (data.password) {
      onNext();
      return;
    }

    // Verificar si email existe
    setCheckingEmail(true);
    try {
      const response = await api.get<{ email: string; exists: boolean }>(`/auth/check-email?email=${encodeURIComponent(data.email)}`);
      
      if (response.data.exists) {
        // Email existe - mostrar modal con opciones
        setShowLoginModal(true);
      } else {
        // Email nuevo, mostrar modal para crear password
        setShowPasswordModal(true);
      }
    } catch (err) {
      console.error('Error checking email:', err);
      // Si falla la verificación, continuar sin password (backend lo manejará)
      onNext();
    } finally {
      setCheckingEmail(false);
    }
  };

  const handleCreatePassword = () => {
    if (password.length < 8) {
      setPasswordError('Password must be at least 8 characters');
      return;
    }
    if (password !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }
    
    // Guardar password y continuar
    onChange({ password });
    setShowPasswordModal(false);
    onNext();
  };

  const handleLogin = async () => {
    if (!loginPassword) {
      setLoginError('Please enter your password');
      return;
    }
    
    setIsLoggingIn(true);
    setLoginError('');
    
    try {
      await login(data.email, loginPassword);
      setShowLoginModal(false);
      setLoginPassword('');
      // Usuario autenticado, continuar al siguiente paso
      onNext();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Invalid password. Please try again.';
      setLoginError(errorMessage);
    } finally {
      setIsLoggingIn(false);
    }
  };

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Contact Information</h2>
      <p className="text-gray-600 mb-8">Tell us where to send the cleaning crew.</p>

      {/* Modal para crear contraseña */}
      <Modal 
        isOpen={showPasswordModal} 
        onClose={() => setShowPasswordModal(false)}
        title="Create Your Account"
        showCloseButton={false}
      >
        <div className="space-y-4">
          <p className="text-gray-600 text-sm">
            Create a password to track your bookings, manage appointments, and get exclusive offers.
          </p>
          
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => { setPassword(e.target.value); setPasswordError(''); }}
            placeholder="Minimum 8 characters"
            error={passwordError && password.length < 8 ? passwordError : undefined}
          />
          
          <Input
            label="Confirm Password"
            type="password"
            value={confirmPassword}
            onChange={(e) => { setConfirmPassword(e.target.value); setPasswordError(''); }}
            placeholder="Re-enter your password"
            error={passwordError && password !== confirmPassword ? passwordError : undefined}
          />

          {passwordError && (
            <p className="text-sm text-red-500">{passwordError}</p>
          )}
          
          <div className="flex gap-3 pt-2">
            <Button variant="outline" onClick={() => setShowPasswordModal(false)} className="flex-1">
              Cancel
            </Button>
            <Button onClick={handleCreatePassword} className="flex-1">
              Create Account & Continue
            </Button>
          </div>
        </div>
      </Modal>
{/* Modal cuando el email ya está registrado */}
      <Modal 
        isOpen={showLoginModal} 
        onClose={() => { setShowLoginModal(false); setLoginPassword(''); setLoginError(''); }}
        title="Welcome Back!"
        showCloseButton={true}
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            The email <strong>{data.email}</strong> is already registered. Please enter your password to continue.
          </p>
          
          <Input
            label="Password"
            type="password"
            value={loginPassword}
            onChange={(e) => { setLoginPassword(e.target.value); setLoginError(''); }}
            placeholder="Enter your password"
            error={loginError}
            onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
          />
          
          <a href="/forgot-password" className="text-sm text-sky-600 hover:text-sky-700">
            Forgot your password?
          </a>
          
          {loginError && (
            <p className="text-sm text-red-500">{loginError}</p>
          )}
          
          <div className="flex flex-col gap-3 pt-2">
            <Button 
              onClick={handleLogin}
              loading={isLoggingIn}
              className="w-full"
            >
              Login & Continue
            </Button>
            <Button 
              variant="outline" 
              onClick={() => {
                setShowLoginModal(false);
                setLoginPassword('');
                setLoginError('');
                // Continuar sin password - backend asociará el booking a la cuenta existente
                onNext();
              }} 
              className="w-full"
              disabled={isLoggingIn}
            >
              Continue as Guest
            </Button>
          </div>
          
          <div className="text-center">
            <Link to="/forgot-password" className="text-sm text-sky-600 hover:text-sky-700">
              Forgot your password?
            </Link>
          </div>
          
          <p className="text-xs text-gray-500 text-center">
            If you continue as guest, a confirmation email will be sent to {data.email}
          </p>
        </div>
      </Modal>

      
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="First Name"
            value={data.firstName}
            onChange={(e) => onChange({ firstName: e.target.value })}
            error={errors.firstName}
          />
          <Input
            label="Last Name"
            value={data.lastName}
            onChange={(e) => onChange({ lastName: e.target.value })}
            error={errors.lastName}
          />
        </div>
        <div>
          <Input
            label="Email"
            type="email"
            value={data.email}
            onChange={(e) => {
              onChange({ email: e.target.value, password: undefined });
              setErrors(prev => ({ ...prev, email: '' }));
            }}
            error={errors.email}
          />
          {checkingEmail && <p className="text-sm text-gray-500 mt-1">Checking email...</p>}
          {data.password && (
            <p className="text-sm text-green-600 mt-1">✓ Account will be created with this email</p>
          )}
        </div>

        <Input
          label="Phone"
          type="tel"
          value={data.phone}
          onChange={(e) => onChange({ phone: e.target.value.replace(/\D/g, '').slice(0, 10) })}
          error={errors.phone}
        />
        <Input
          label="Street Address"
          value={data.address}
          onChange={(e) => onChange({ address: e.target.value })}
          error={errors.address}
        />
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="City"
            value={data.city}
            onChange={(e) => onChange({ city: e.target.value })}
            error={errors.city}
          />
          <Input
            label="State"
            value={data.state}
            onChange={(e) => onChange({ state: e.target.value.toUpperCase().slice(0, 2) })}
            error={errors.state}
            maxLength={2}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Special Instructions (optional)</label>
          <textarea
            value={data.specialInstructions}
            onChange={(e) => onChange({ specialInstructions: e.target.value })}
            rows={3}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            placeholder="Gate code, parking instructions, pet info, etc."
          />
        </div>
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack}>Back</Button>
        <Button onClick={checkEmailAndProceed} loading={checkingEmail}>
          Review Booking
        </Button>
      </div>
    </div>
  );
}

function ConfirmStep({
  data,
  estimate,
  onBack,
  onSuccess,
}: {
  data: BookingData;
  estimate: EstimateResponse | null;
  onBack: () => void;
  onSuccess: (confirmation: BookingConfirmation) => void;
}) {
  const [error, setError] = useState<string | null>(null);

  const confirmBooking = useMutation({
    mutationFn: () =>
      bookingApi.confirmBooking({
        softReserveId: data.softReserveId || 0,
        sessionId: data.sessionId || '',
        zipCode: data.zipCode,
        address: data.address,
        city: data.city,
        state: data.state,
        serviceTypeId: data.serviceTypeId,
        cleaningPlaceId: data.cleaningPlaceId,
        bedrooms: data.numberOfRooms,
        bathrooms: data.numberOfBathrooms,
        squareFootage: data.squareFeet,
        additionalServiceIds: data.additionalServiceIds,
        subtotal: estimate?.subtotal || 0,
        tax: estimate?.tax || 0,
        discount: 0,
        total: estimate?.total || 0,
        contactName: `${data.firstName} ${data.lastName}`,
        contactPhone: data.phone,
        contactEmail: data.email,
        password: data.password || undefined,
        specialInstructions: data.specialInstructions,
      }),
    onSuccess: (response) => {
      setError(null);
      // Si se creó usuario, guardar tokens automáticamente
      if (response.data.authToken) {
        localStorage.setItem('token', response.data.authToken.accessToken);
        localStorage.setItem('refreshToken', response.data.authToken.refreshToken);
      }
      onSuccess(response.data);
    },
    onError: (err: Error & { response?: { data?: { message?: string } } }) => {
      const message = err.response?.data?.message 
        || err.message 
        || 'Something went wrong. Please try again.';
      setError(message);
    },
  });

  const scheduledDate = new Date(data.date);

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Review & Confirm</h2>
      <p className="text-gray-600 mb-8">Please review your booking details before confirming.</p>

      <div className="space-y-6">
        {/* Schedule Summary */}
        <div className="bg-sky-50 rounded-xl p-6">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-sky-100">
              <Calendar className="h-7 w-7 text-sky-600" />
            </div>
            <div>
              <p className="text-lg font-semibold text-gray-900">
                {scheduledDate.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}
              </p>
              <p className="text-sky-600 font-medium">
                {new Date(`2000-01-01T${data.timeSlot}`).toLocaleTimeString('en-US', {
                  hour: 'numeric',
                  minute: '2-digit',
                  hour12: true,
                })}
              </p>
            </div>
          </div>
        </div>

        {/* Address */}
        <div className="border rounded-xl p-4">
          <h3 className="font-semibold text-gray-900 mb-2">Service Address</h3>
          <p className="text-gray-600">
            {data.address}<br />
            {data.city}, {data.state} {data.zipCode}
          </p>
        </div>

        {/* Price Breakdown */}
        {estimate && (
          <div className="border rounded-xl p-4">
            <h3 className="font-semibold text-gray-900 mb-4">Price Breakdown</h3>
            <div className="space-y-2">
              {estimate.breakdown.map((item, index) => (
                <div key={index} className="flex justify-between text-sm">
                  <span className="text-gray-600">{item.description}</span>
                  <span className="font-medium">{formatCurrency(item.amount)}</span>
                </div>
              ))}
              <div className="border-t pt-2 mt-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Subtotal</span>
                  <span className="font-medium">{formatCurrency(estimate.subtotal)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Tax</span>
                  <span className="font-medium">{formatCurrency(estimate.tax)}</span>
                </div>
              </div>
              <div className="border-t pt-2 mt-2 flex justify-between">
                <span className="font-semibold text-gray-900">Total</span>
                <span className="text-xl font-bold text-sky-600">{formatCurrency(estimate.total)}</span>
              </div>
            </div>
          </div>
        )}

        {/* Payment notice */}
        <div className="flex items-center gap-3 text-sm text-gray-600 bg-gray-50 rounded-lg p-4">
          <CreditCard className="h-5 w-5 text-gray-400" />
          <span>Payment will be collected after service completion</span>
        </div>

        {/* Error message */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
            <svg className="h-5 w-5 text-red-500 mt-0.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <div>
              <p className="text-sm font-medium text-red-800">Booking failed</p>
              <p className="text-sm text-red-700">{error}</p>
            </div>
          </div>
        )}
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack}>Back</Button>
        <Button onClick={() => { setError(null); confirmBooking.mutate(); }} loading={confirmBooking.isPending}>
          Confirm Booking
        </Button>
      </div>
    </div>
  );
}
