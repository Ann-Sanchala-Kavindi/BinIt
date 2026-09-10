import React from 'react';
import { NavLink } from 'react-router-dom';
import { CloseIcon, LeafIcon } from '../ui/Icons';
import { defaultOfficerNavItems, type NavItem } from './navConfig';

export type { NavItem };

interface DashboardSidebarProps {
  onCloseMobile?: () => void;
  items?: NavItem[];
}

export const DashboardSidebar: React.FC<DashboardSidebarProps> = ({
  onCloseMobile,
  items = defaultOfficerNavItems,
}) => {

  return (
    <aside className="flex flex-col h-full bg-[#064e3b] text-white border-r border-[#043d2e] w-64 select-none relative overflow-hidden">
      {/* Brand Header */}
      <div className="h-16 flex items-center justify-between px-6 border-b border-white/10 shrink-0">
        <div className="flex items-center gap-2.5">
          <div
            className="w-8 h-8 rounded-lg bg-emerald-500 text-white flex items-center justify-center font-bold text-sm shadow-xs"
            aria-hidden="true"
          >
            SW
          </div>
          <div>
            <span className="font-bold text-white tracking-tight block text-base leading-tight">
              SmartWaste
            </span>
            <span className="text-xs text-emerald-300/90 font-medium">
              Waste Operations
            </span>
          </div>
        </div>
        {onCloseMobile && (
          <button
            type="button"
            onClick={onCloseMobile}
            className="md:hidden text-emerald-200 hover:text-white p-1.5 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-400"
            aria-label="Close navigation menu"
          >
            <CloseIcon className="w-5 h-5" />
          </button>
        )}
      </div>

      {/* Navigation Links */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto" aria-label="Main navigation">
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/officer/dashboard' || item.to === '/manager/dashboard'}
            onClick={onCloseMobile}
            className={({ isActive }) =>
              `group relative flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all ${
                isActive
                  ? 'bg-emerald-900/60 text-white font-semibold shadow-xs'
                  : 'text-emerald-100/75 hover:bg-white/5 hover:text-white'
              }`
            }
          >
            {({ isActive }) => (
              <>
                {isActive && (
                  <span
                    className="absolute left-0 top-1.5 bottom-1.5 w-1 bg-emerald-400 rounded-r-full"
                    aria-hidden="true"
                  />
                )}
                <span
                  className={`shrink-0 transition-colors ${
                    isActive ? 'text-white' : 'text-emerald-200/70 group-hover:text-white'
                  }`}
                >
                  {item.icon}
                </span>
                <span>{item.label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Environmental Motto Banner */}
      <div className="px-5 py-3.5 border-t border-white/5 bg-white/[0.03] flex items-center gap-3 shrink-0">
        <div
          className="w-8 h-8 rounded-lg bg-emerald-800/60 text-emerald-300 flex items-center justify-center shrink-0 shadow-2xs"
          aria-hidden="true"
        >
          <LeafIcon className="w-4 h-4 text-emerald-300" />
        </div>
        <div className="text-xs leading-tight">
          <p className="font-semibold text-white">Cleaner Communities</p>
          <p className="text-[11px] text-emerald-300/80">Brighter Tomorrows</p>
        </div>
      </div>
    </aside>
  );
};
