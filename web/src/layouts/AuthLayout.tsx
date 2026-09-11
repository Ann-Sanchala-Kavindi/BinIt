import React from 'react';
import { Outlet } from 'react-router-dom';

export const AuthLayout: React.FC = () => {
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100/80 px-4 py-8 sm:px-6 lg:px-8">
      <div className="w-full max-w-md bg-white rounded-xl shadow-sm border border-slate-200 p-6 sm:p-8">
        <div className="text-center mb-6">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-emerald-100 text-emerald-700 mb-3">
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              xmlns="http://www.w3.org/2000/svg"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-emerald-700">
            Smart Waste Management
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Clean, efficient municipal waste operations
          </p>
        </div>
        <Outlet />
      </div>
    </div>
  );
};
