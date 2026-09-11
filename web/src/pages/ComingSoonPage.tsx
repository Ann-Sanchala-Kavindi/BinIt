import React from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../components/ui/Card';
import { useAuthStore } from '../store/authStore';
import { getDefaultRouteForRole } from '../routes/routeUtils';

export interface ComingSoonPageProps {
  title: string;
  description: string;
  moduleName?: string;
  backTo?: string;
  backText?: string;
}

export const ComingSoonPage: React.FC<ComingSoonPageProps> = ({
  title,
  description,
  moduleName,
  backTo,
  backText = 'Back to Dashboard',
}) => {
  const { user } = useAuthStore();
  const resolvedBackTo = backTo || getDefaultRouteForRole(user?.role);
  return (
    <div className="py-8 max-w-2xl mx-auto">
      <Card className="text-center p-8 sm:p-12">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-emerald-50 text-emerald-700 border border-emerald-200 mb-6">
          <svg
            className="w-8 h-8"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.75}
              d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"
            />
          </svg>
        </div>

        {moduleName && (
          <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 uppercase tracking-wider mb-3">
            {moduleName}
          </span>
        )}

        <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight mb-3">
          {title}
        </h1>

        <p className="text-sm sm:text-base text-slate-500 max-w-md mx-auto leading-relaxed mb-8">
          {description}
        </p>

        <div className="inline-flex items-center gap-4">
          <Link
            to={resolvedBackTo}
            className="inline-flex items-center justify-center font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 bg-emerald-600 text-white hover:bg-emerald-700 active:bg-emerald-800 shadow-sm focus:ring-emerald-500 px-5 py-2.5 text-sm rounded-lg"
          >
            &larr; {backText}
          </Link>
        </div>
      </Card>
    </div>
  );
};

export default ComingSoonPage;
