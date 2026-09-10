import React from 'react';
import { Link } from 'react-router-dom';
import {
  ReportsIcon,
  BinsIcon,
  SchedulesIcon,
  TasksIcon,
  ComplaintsIcon,
} from '../../../components/ui/Icons';

export interface ActionItem {
  label: string;
  to: string;
  icon: React.ReactNode;
}

export interface QuickActionsProps {
  title?: string;
  description?: string;
  actions?: ActionItem[];
}

const defaultActions: ActionItem[] = [
  {
    label: 'Review Waste Reports',
    to: '/officer/waste-reports',
    icon: <ReportsIcon className="w-4 h-4" />,
  },
  {
    label: 'Manage Bins',
    to: '/officer/bins',
    icon: <BinsIcon className="w-4 h-4" />,
  },
  {
    label: 'View Collection Schedules',
    to: '/officer/schedules',
    icon: <SchedulesIcon className="w-4 h-4" />,
  },
  {
    label: 'View Collection Tasks',
    to: '/officer/tasks',
    icon: <TasksIcon className="w-4 h-4" />,
  },
  {
    label: 'Review Complaints',
    to: '/officer/complaints',
    icon: <ComplaintsIcon className="w-4 h-4" />,
  },
];

export const QuickActions: React.FC<QuickActionsProps> = ({
  title = 'Quick Actions',
  description = 'Fast-track operational dispatch and review workflows',
  actions = defaultActions,
}) => {
  return (
    <div className="bg-white rounded-xl border border-slate-200/80 shadow-2xs p-5">
      <div className="mb-4">
        <h2 className="text-xs font-bold text-slate-900 tracking-wider uppercase">
          {title}
        </h2>
        {description && (
          <p className="text-xs text-slate-500 mt-0.5">
            {description}
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3.5">
        {actions.map((act) => (
          <Link
            key={act.to}
            to={act.to}
            className="group flex items-center justify-between p-3 rounded-xl border border-teal-200/70 bg-teal-50/40 hover:bg-teal-50/90 hover:border-teal-300 hover:shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-teal-500"
          >
            <div className="flex items-center gap-2.5 min-w-0">
              <div
                className="w-9 h-9 rounded-full bg-white border border-teal-200/80 text-teal-800 flex items-center justify-center shrink-0 group-hover:bg-teal-100 group-hover:text-teal-900 transition-colors shadow-2xs"
                aria-hidden="true"
              >
                {act.icon}
              </div>
              <span className="text-xs font-semibold text-slate-800 group-hover:text-teal-950 transition-colors leading-snug truncate">
                {act.label}
              </span>
            </div>
            <span
              className="text-teal-500 group-hover:text-teal-700 group-hover:translate-x-0.5 transition-transform text-xs font-bold shrink-0 ml-2"
              aria-hidden="true"
            >
              &rarr;
            </span>
          </Link>
        ))}
      </div>
    </div>
  );
};
