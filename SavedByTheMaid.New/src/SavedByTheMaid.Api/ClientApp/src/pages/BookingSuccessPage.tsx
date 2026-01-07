import { Link, useLocation } from 'react-router-dom';
import { CheckCircle, Calendar, MapPin, ArrowRight, Clock, DollarSign, Copy, Check } from 'lucide-react';
import { Button, Card } from '@/components/ui';
import { useState } from 'react';

interface BookingConfirmation {
  orderId: number;
  meetId: number;
  confirmationNumber: string;
  scheduledStart: string;
  scheduledEnd: string;
  total: number;
  orderStatus: string;
  message: string;
}

interface BookingData {
  address: string;
  city: string;
  state: string;
  zipCode: string;
  date: string;
  timeSlot: string;
}

interface EstimateResponse {
  estimatedMinutes: number;
  total: number;
}

export default function BookingSuccessPage() {
  const location = useLocation();
  const { confirmation, bookingData, estimate } = (location.state || {}) as {
    confirmation?: BookingConfirmation;
    bookingData?: BookingData;
    estimate?: EstimateResponse;
  };

  const [copied, setCopied] = useState(false);

  const copyConfirmation = () => {
    if (confirmation?.confirmationNumber) {
      navigator.clipboard.writeText(confirmation.confirmationNumber);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'Your scheduled date';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const formatTime = (dateStr?: string) => {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  const formatCurrency = (amount?: number) => {
    if (amount === undefined) return '';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="mx-auto max-w-lg px-4">
        <Card className="p-8 text-center">
          {/* Success Icon */}
          <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-green-100">
            <CheckCircle className="h-10 w-10 text-green-600" />
          </div>

          <h1 className="text-2xl font-bold text-gray-900 mb-2">Booking Confirmed!</h1>
          <p className="text-gray-600 mb-6">
            {confirmation?.message || "Your cleaning has been scheduled. We've sent a confirmation email with all the details."}
          </p>

          {/* Confirmation Number */}
          {confirmation?.confirmationNumber && (
            <div className="bg-sky-50 rounded-xl p-4 mb-6">
              <p className="text-sm text-sky-600 mb-1">Confirmation Number</p>
              <div className="flex items-center justify-center gap-2">
                <span className="text-xl font-bold text-sky-700 font-mono">
                  {confirmation.confirmationNumber}
                </span>
                <button
                  onClick={copyConfirmation}
                  className="p-1.5 text-sky-600 hover:bg-sky-100 rounded transition-colors"
                  title="Copy to clipboard"
                >
                  {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </button>
              </div>
            </div>
          )}

          {/* Booking Details */}
          <div className="bg-gray-50 rounded-xl p-6 mb-8 text-left">
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <Calendar className="h-5 w-5 text-gray-400" />
                <div>
                  <p className="text-sm text-gray-500">Date & Time</p>
                  <p className="font-medium text-gray-900">
                    {confirmation?.scheduledStart ? (
                      new Date(confirmation.scheduledStart).toLocaleString('en-US', {
                        weekday: 'long',
                        month: 'long',
                        day: 'numeric',
                        year: 'numeric',
                        hour: 'numeric',
                        minute: '2-digit',
                        hour12: true,
                      })
                    ) : (
                      <>{formatDate(bookingData?.date)} at {formatTime(bookingData?.timeSlot)}</>
                    )}
                  </p>
                </div>
              </div>

              {bookingData?.address && (
                <div className="flex items-center gap-3">
                  <MapPin className="h-5 w-5 text-gray-400" />
                  <div>
                    <p className="text-sm text-gray-500">Location</p>
                    <p className="font-medium text-gray-900">
                      {bookingData.address}, {bookingData.city}, {bookingData.state} {bookingData.zipCode}
                    </p>
                  </div>
                </div>
              )}

              {estimate?.estimatedMinutes && (
                <div className="flex items-center gap-3">
                  <Clock className="h-5 w-5 text-gray-400" />
                  <div>
                    <p className="text-sm text-gray-500">Duration</p>
                    <p className="font-medium text-gray-900">
                      ~{Math.round(estimate.estimatedMinutes / 60)} hours
                    </p>
                  </div>
                </div>
              )}

              {(confirmation?.total || estimate?.total) && (
                <div className="flex items-center gap-3">
                  <DollarSign className="h-5 w-5 text-gray-400" />
                  <div>
                    <p className="text-sm text-gray-500">Total</p>
                    <p className="font-medium text-gray-900">
                      {formatCurrency(confirmation?.total || estimate?.total)}
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* What's Next */}
          <div className="text-left mb-8">
            <h3 className="font-semibold text-gray-900 mb-3">What's Next?</h3>
            <ul className="space-y-2 text-sm text-gray-600">
              <li className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                <span>Check your email for confirmation details</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                <span>We'll send a reminder 24 hours before your appointment</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                <span>Your cleaner will arrive on time and ready to work</span>
              </li>
            </ul>
          </div>

          {/* Actions */}
          <div className="space-y-3">
            <Link to="/dashboard" className="block">
              <Button className="w-full">
                Go to Dashboard
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </Link>
            <Link to="/booking" className="block">
              <Button variant="outline" className="w-full">
                Book Another Cleaning
              </Button>
            </Link>
          </div>
        </Card>
      </div>
    </div>
  );
}
