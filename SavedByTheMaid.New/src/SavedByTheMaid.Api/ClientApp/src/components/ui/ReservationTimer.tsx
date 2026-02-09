import { useEffect, useState } from 'react';
import { Clock } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ReservationTimerProps {
  expiresAt: string | Date;
  onExpire: () => void;
  className?: string;
}

export function ReservationTimer({ expiresAt, onExpire, className }: ReservationTimerProps) {
  const [timeLeft, setTimeLeft] = useState<number>(0);
  const [isWarning, setIsWarning] = useState(false);

  useEffect(() => {
    const calculateTimeLeft = () => {
      const expiration = new Date(expiresAt).getTime();
      const now = new Date().getTime();
      const difference = expiration - now;
      
      return difference; // Return milliseconds
    };

    const updateTimer = () => {
      const difference = calculateTimeLeft();

      if (difference <= 0) {
        setTimeLeft(0);
        // Only trigger expire callback once when it actually transitions to 0
        // We handle this check in the interval
      } else {
        setTimeLeft(Math.floor(difference / 1000));
        setIsWarning(difference < 120000);
      }
      return difference;
    };

    // Initial check
    const initialDiff = updateTimer();
    if (initialDiff <= 0) {
        onExpire(); // Expired immediately
        return;
    }

    const timer = setInterval(() => {
      const diff = updateTimer();
      
      if (diff <= 0) {
        clearInterval(timer);
        onExpire();
      }
    }, 1000);

    return () => clearInterval(timer);
  }, [expiresAt, onExpire]);

  if (timeLeft <= 0) return null;

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  return (
    <div className={cn(
      "fixed top-4 right-4 z-40 flex items-center gap-2 rounded-full px-4 py-2 text-sm font-bold shadow-lg transition-colors animate-in fade-in slide-in-from-top-4",
      isWarning 
        ? "bg-red-500 text-white animate-pulse" 
        : "bg-[#00205B] text-white",
      className
    )}>
      <Clock className="h-4 w-4" />
      <span>
        {minutes.toString().padStart(2, '0')}:{seconds.toString().padStart(2, '0')}
      </span>
      <span className="font-normal opacity-90 hidden sm:inline">
        remaining
      </span>
    </div>
  );
}
