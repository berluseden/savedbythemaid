import { cn } from '@/lib/utils';

const sizeMap = {
  sm: 'h-8 w-8',
  md: 'h-12 w-12',
  lg: 'h-16 w-16',
} as const;

export interface LogoProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

function Logo({ size = 'md', className }: LogoProps) {
  return (
    <svg
      viewBox="0 0 100 100"
      fill="none"
      className={cn(sizeMap[size], className)}
      aria-hidden="true"
    >
      <circle cx="58" cy="65" r="24" fill="#2196f3" />
      <ellipse cx="50" cy="55" rx="7" ry="10" fill="white" opacity="0.35" />
      <circle cx="72" cy="32" r="16" fill="#2196f3" />
      <ellipse cx="66" cy="26" rx="5" ry="7" fill="white" opacity="0.35" />
      <circle cx="38" cy="28" r="12" fill="#2196f3" />
      <ellipse cx="34" cy="24" rx="4" ry="5" fill="white" opacity="0.35" />
      <g transform="translate(18, 50)">
        <path
          d="M0 -6 L0 6 M-6 0 L6 0"
          stroke="#F7C52D"
          strokeWidth="2.5"
          strokeLinecap="round"
        />
      </g>
    </svg>
  );
}
Logo.displayName = 'Logo';

export { Logo };
