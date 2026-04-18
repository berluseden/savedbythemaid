import { useEffect, useRef, useState } from 'react';
import { Clock, AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ReservationTimerProps {
  expiresAt: string | Date;
  onExpire: () => void;
  onSelectNewTime?: () => void;
  className?: string;
}

function useCountdown(expiresAt: string | Date, onExpire: () => void) {
  const [secondsRemaining, setSecondsRemaining] = useState<number>(() => {
    const diff = new Date(expiresAt).getTime() - Date.now();
    return Math.max(0, Math.floor(diff / 1000));
  });
  const [isExpired, setIsExpired] = useState(false);

  // Keep latest onExpire in a ref so the interval effect doesn't recreate
  // every render when the parent passes an inline arrow function.
  const onExpireRef = useRef(onExpire);
  useEffect(() => {
    onExpireRef.current = onExpire;
  }, [onExpire]);

  useEffect(() => {
    const calculate = () => {
      const diff = new Date(expiresAt).getTime() - Date.now();
      return Math.max(0, Math.floor(diff / 1000));
    };

    const initial = calculate();
    setSecondsRemaining(initial);

    if (initial <= 0) {
      setIsExpired(true);
      onExpireRef.current();
      return;
    }

    setIsExpired(false);

    const timer = setInterval(() => {
      const remaining = calculate();
      setSecondsRemaining(remaining);

      if (remaining <= 0) {
        clearInterval(timer);
        setIsExpired(true);
        onExpireRef.current();
      }
    }, 1000);

    return () => clearInterval(timer);
  }, [expiresAt]);

  return { secondsRemaining, isExpired };
}

type TimerUrgency = 'normal' | 'warning' | 'critical';

function getUrgency(seconds: number): TimerUrgency {
  if (seconds <= 60) return 'critical';
  if (seconds <= 180) return 'warning';
  return 'normal';
}

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
}

const urgencyStyles: Record<TimerUrgency, { bar: string; icon: string; text: string }> = {
  normal: {
    bar: 'bg-gray-100 border-gray-200',
    icon: 'text-gray-500',
    text: 'text-gray-700',
  },
  warning: {
    bar: 'bg-amber-50 border-amber-200',
    icon: 'text-amber-600',
    text: 'text-amber-800',
  },
  critical: {
    bar: 'bg-red-50 border-red-200',
    icon: 'text-red-600',
    text: 'text-red-800',
  },
};

function TimerDisplay({ secondsRemaining }: { secondsRemaining: number }) {
  const urgency = getUrgency(secondsRemaining);
  const styles = urgencyStyles[urgency];

  return (
    <div
      className={cn(
        'flex items-center justify-center gap-2 rounded-lg border px-4 py-2.5 text-sm transition-colors duration-300',
        styles.bar,
        urgency === 'critical' && 'animate-pulse',
      )}
      role="timer"
      aria-live="polite"
      aria-label={`Reservation expires in ${formatTime(secondsRemaining)}`}
    >
      <Clock className={cn('h-4 w-4 shrink-0', styles.icon)} aria-hidden="true" />
      <span className={cn('font-medium', styles.text)}>
        Your time slot is reserved for{' '}
        <span className="tabular-nums font-semibold">{formatTime(secondsRemaining)}</span>
      </span>
    </div>
  );
}

function ExpiredDisplay({ onSelectNewTime }: { onSelectNewTime?: () => void }) {
  return (
    <div
      className="flex items-center justify-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-sm"
      role="alert"
    >
      <AlertTriangle className="h-4 w-4 shrink-0 text-red-600" aria-hidden="true" />
      <span className="font-medium text-red-800">Your reservation has expired</span>
      {onSelectNewTime && (
        <button
          type="button"
          onClick={onSelectNewTime}
          className="ml-2 rounded-md bg-red-600 px-3 py-1 text-xs font-medium text-white transition-colors hover:bg-red-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500 focus-visible:ring-offset-2"
        >
          Select new time
        </button>
      )}
    </div>
  );
}

export function ReservationTimer({
  expiresAt,
  onExpire,
  onSelectNewTime,
  className,
}: ReservationTimerProps) {
  const { secondsRemaining, isExpired } = useCountdown(expiresAt, onExpire);

  if (!isExpired && secondsRemaining <= 0) return null;

  return (
    <div className={cn('mb-3', className)}>
      {isExpired ? (
        <ExpiredDisplay onSelectNewTime={onSelectNewTime} />
      ) : (
        <TimerDisplay secondsRemaining={secondsRemaining} />
      )}
    </div>
  );
}
