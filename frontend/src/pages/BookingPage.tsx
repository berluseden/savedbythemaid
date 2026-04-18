import { useNavigate } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui';
import { ReservationTimer } from '@/components/ui/ReservationTimer';
import { PriceSummaryBar } from '@/components/booking/PriceSummaryBar';
import {
  useBookingWizard,
  useReservationTimer,
  usePricingEstimate,
  clearWizardState,
  ZipCodeStep,
  ServiceStep,
  DetailsStep,
  ScheduleStep,
  ContactStep,
  ConfirmStep,
  StepProgress,
  ExpiryModal,
} from './booking';

export default function BookingPage() {
  const navigate = useNavigate();
  const { currentStep, data, updateData, goNext, goBack, goToStep } = useBookingWizard();
  const { estimate, setEstimate } = usePricingEstimate();
  const {
    showExpireModal,
    isRenewing,
    renewalError,
    handleExpire,
    handleResetAfterExpiry,
    handleTryRenew,
  } = useReservationTimer(data, estimate, updateData, goToStep);

  const renderStep = () => {
    switch (currentStep) {
      case 'zipcode':
        return (
          <ZipCodeStep
            value={data.zipCode}
            onChange={(zipCode) => updateData({ zipCode })}
            onNext={goNext}
          />
        );
      case 'service':
        return (
          <ServiceStep
            selectedId={data.serviceTypeId}
            onChange={(serviceTypeId) => updateData({ serviceTypeId })}
            onNext={goNext}
            onBack={goBack}
          />
        );
      case 'details':
        return (
          <DetailsStep
            data={data}
            onChange={updateData}
            onEstimate={setEstimate}
            onNext={goNext}
            onBack={goBack}
          />
        );
      case 'schedule':
        return (
          <ScheduleStep
            data={data}
            estimate={estimate}
            onChange={updateData}
            onNext={goNext}
            onBack={goBack}
          />
        );
      case 'contact':
        return (
          <ContactStep
            data={data}
            onChange={updateData}
            onNext={goNext}
            onBack={goBack}
          />
        );
      case 'confirm':
        return (
          <ConfirmStep
            data={data}
            estimate={estimate}
            onBack={goBack}
            onSuccess={(confirmation) => {
              clearWizardState();
              navigate('/booking/success', { state: { confirmation, bookingData: data, estimate } });
            }}
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <ExpiryModal
        isOpen={showExpireModal}
        isRenewing={isRenewing}
        renewalError={renewalError}
        onTryRenew={handleTryRenew}
        onResetAfterExpiry={handleResetAfterExpiry}
      />

      <div className="mx-auto max-w-4xl px-4">
        <StepProgress currentStep={currentStep} />

        {data.expiresAt && data.softReserveId && !showExpireModal && (
          <ReservationTimer
            expiresAt={data.expiresAt}
            onExpire={handleExpire}
            onSelectNewTime={handleResetAfterExpiry}
          />
        )}

        <Card className="overflow-hidden">
          <CardContent className="p-6 sm:p-8">
            {renderStep()}
          </CardContent>
        </Card>

        <PriceSummaryBar data={data} estimate={estimate} currentStep={currentStep} />
      </div>
    </div>
  );
}
