import React from 'react';
import { AlertTriangle } from 'lucide-react';
import { Button, Modal } from '@/components/ui';
import { cn } from '@/lib/utils';

interface ExpiryModalProps {
  isOpen: boolean;
  isRenewing: boolean;
  renewalError: string | null;
  onTryRenew: () => void;
  onResetAfterExpiry: () => void;
}

export const ExpiryModal = React.memo(function ExpiryModal({
  isOpen,
  isRenewing,
  renewalError,
  onTryRenew,
  onResetAfterExpiry,
}: ExpiryModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={() => {}} // Force user to click action button
      title="Reservation Expired"
      showCloseButton={false}
    >
      <div className="text-center py-4">
        <div className={cn(
          "mx-auto flex h-12 w-12 items-center justify-center rounded-full mb-4",
          renewalError ? "bg-red-100" : "bg-yellow-100"
        )}>
          <AlertTriangle className={cn(
            "h-6 w-6",
            renewalError ? "text-red-600" : "text-yellow-600"
          )} />
        </div>

        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {renewalError ? "Slot No Longer Available" : "Reservation Hold Expired"}
        </h3>

        <p className="text-gray-600 mb-6">
          {renewalError
            ? "Someone else has booked this time slot while you were away. Please choose another time."
            : "Your 15-minute reservation hold has expired. Would you like to check if this time is still available?"}
        </p>

        {renewalError ? (
          <Button onClick={onResetAfterExpiry} className="w-full" aria-label="Select new time">
            Select New Time
          </Button>
        ) : (
          <div className="flex flex-col gap-3">
            <Button onClick={onTryRenew} loading={isRenewing} className="w-full" aria-label="Check availability and renew">
              Check Availability & Renew
            </Button>
            <Button variant="outline" onClick={onResetAfterExpiry} className="w-full" aria-label="Select different time">
              Select Different Time
            </Button>
          </div>
        )}
      </div>
    </Modal>
  );
});
