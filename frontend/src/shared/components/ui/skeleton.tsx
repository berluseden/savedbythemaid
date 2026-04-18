import { cn } from '@/lib/utils';

/**
 * Base Skeleton block.
 *
 * Usage: size it like the real content it replaces to prevent CLS.
 * Screen readers announce loading via a parent `role="status"`; the
 * skeleton itself is `aria-hidden` so it is not read as decorative text.
 */
export function Skeleton({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      aria-hidden="true"
      className={cn('animate-pulse rounded-md bg-gray-200', className)}
      {...props}
    />
  );
}

/** Pre-built skeleton row for table listings (8 columns). */
export function TableRowSkeleton({ columns = 8 }: { columns?: number }) {
  return (
    <tr className="border-b last:border-0">
      {Array.from({ length: columns }).map((_, i) => (
        <td key={i} className="px-6 py-4">
          <Skeleton className="h-4 w-full max-w-[120px]" />
        </td>
      ))}
    </tr>
  );
}
