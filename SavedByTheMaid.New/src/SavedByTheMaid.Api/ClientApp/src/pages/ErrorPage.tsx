import { AlertTriangle, Home, RefreshCw } from 'lucide-react';
import { Link } from 'react-router-dom';

interface ErrorPageProps {
  code?: number;
  title?: string;
  message?: string;
  onRetry?: () => void;
}

export function ErrorPage({ 
  code = 500, 
  title = 'Oops! Something went wrong',
  message = "We're working to fix this. Please try again in a moment.",
  onRetry
}: ErrorPageProps) {
  const errorMessages: Record<number, { title: string; message: string }> = {
    404: {
      title: 'Page Not Found',
      message: "The page you're looking for doesn't exist or has been moved."
    },
    500: {
      title: 'Server Error',
      message: "We're experiencing technical difficulties. Please try again later."
    },
    503: {
      title: 'Service Unavailable',
      message: "We're temporarily offline for maintenance. We'll be back shortly!"
    }
  };

  const { title: defaultTitle, message: defaultMessage } = errorMessages[code] || errorMessages[500];

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100 flex items-center justify-center px-4">
      <div className="max-w-md w-full text-center">
        {/* Error Icon */}
        <div className="mb-8">
          <div className="w-24 h-24 mx-auto bg-red-100 rounded-full flex items-center justify-center mb-6">
            <AlertTriangle className="w-12 h-12 text-red-500" />
          </div>
          <p className="text-8xl font-bold text-gray-200 mb-4">{code}</p>
        </div>

        {/* Error Message */}
        <h1 className="text-2xl font-bold text-gray-900 mb-3">
          {title || defaultTitle}
        </h1>
        <p className="text-gray-600 mb-8">
          {message || defaultMessage}
        </p>

        {/* Actions */}
        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          {onRetry && (
            <button
              onClick={onRetry}
              className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-[#00205B] text-white font-medium rounded-lg hover:bg-[#001440] transition-colors"
            >
              <RefreshCw className="w-4 h-4" />
              Try Again
            </button>
          )}
          <Link
            to="/"
            className="inline-flex items-center justify-center gap-2 px-6 py-3 border border-gray-300 text-gray-700 font-medium rounded-lg hover:bg-gray-50 transition-colors"
          >
            <Home className="w-4 h-4" />
            Go Home
          </Link>
        </div>

        {/* Support Info */}
        <p className="mt-8 text-sm text-gray-500">
          Need help?{' '}
          <Link to="/contact" className="text-[#00205B] hover:text-[#00205B]">
            Contact Support
          </Link>
        </p>
      </div>
    </div>
  );
}

export function NotFoundPage() {
  return <ErrorPage code={404} />;
}

export function ServerErrorPage() {
  return (
    <ErrorPage 
      code={500} 
      onRetry={() => window.location.reload()}
    />
  );
}
