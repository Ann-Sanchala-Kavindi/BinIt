import React from 'react';
import { useAuthStore } from '../../../store/authStore';
import { OverviewCard } from '../../officer/components/OverviewCard';
import { QuickActions, type ActionItem } from '../../officer/components/QuickActions';
import { NeedsAttentionSection } from '../../officer/components/NeedsAttentionSection';
import {
  AiWorkflowIcon,
  FleetIcon,
  OperationsIcon,
  AnalyticsIcon,
  AuditIcon,
  TasksIcon,
  ComplaintsIcon,
  LeafIcon,
} from '../../../components/ui/Icons';

const managerActions: ActionItem[] = [
  {
    label: 'Review AI Approvals',
    to: '/manager/ai-approvals',
    icon: <AiWorkflowIcon className="w-4 h-4" />,
    variant: 'amber',
  },
  {
    label: 'View Fleet & Routes',
    to: '/manager/fleet',
    icon: <FleetIcon className="w-4 h-4" />,
    variant: 'emerald',
  },
  {
    label: 'Monitor Operations',
    to: '/manager/operations',
    icon: <OperationsIcon className="w-4 h-4" />,
    variant: 'blue',
  },
  {
    label: 'View Analytics',
    to: '/manager/analytics',
    icon: <AnalyticsIcon className="w-4 h-4" />,
    variant: 'mint',
  },
  {
    label: 'Review Audit Logs',
    to: '/manager/audit',
    icon: <AuditIcon className="w-4 h-4" />,
    variant: 'rose',
  },
];

export const ManagerDashboardPage: React.FC = () => {
  const { user } = useAuthStore();

  const currentFormattedDate = new Intl.DateTimeFormat('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date());

  return (
    <div className="space-y-6">
      {/* Dashboard Top Heading / Breadcrumb */}
      <div className="border-b border-slate-200/80 pb-5">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <div className="flex items-center gap-1.5 text-xs text-slate-400 font-medium mb-1">
              <span>Operations</span>
              <span>/</span>
              <span className="text-slate-600 font-semibold">Municipal Manager Dashboard</span>
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900 flex items-center gap-2">
              <span>Municipal Manager Dashboard</span>
              <LeafIcon className="w-6 h-6 text-emerald-600 shrink-0 inline-block" />
            </h1>
            <p className="mt-1 text-xs sm:text-sm text-slate-500 leading-relaxed">
              Welcome back,{' '}
              <span className="font-semibold text-slate-800">
                {user?.fullName || 'Municipal Manager'}
              </span>
              . Review operational performance, pending decisions and AI-assisted recommendations.
            </p>
          </div>

          {/* Present Date & Environmental Tagline */}
          <div className="text-left sm:text-right shrink-0">
            <p className="text-xs sm:text-sm font-bold text-slate-800 tracking-tight">
              {currentFormattedDate}
            </p>
            <p className="text-[11px] sm:text-xs text-slate-500 font-medium flex items-center sm:justify-end gap-1 mt-0.5">
              <span>Together for a cleaner, greener city</span>
              <span role="img" aria-label="leaf">🍃</span>
            </p>
          </div>
        </div>
      </div>

      {/* Management Overview KPI Cards */}
      <div className="bg-white rounded-xl border border-slate-200/80 shadow-2xs p-5">
        <div className="mb-4">
          <h2 className="text-xs font-bold text-slate-900 tracking-wider uppercase">
            Management Overview
          </h2>
          <p className="text-xs text-slate-500 mt-0.5">
            Real-time status indicators across municipal collection domains
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-3.5">
          <OverviewCard
            label="AI Workflows Awaiting Approval"
            value="—"
            description="Pending executive review"
            icon={<AiWorkflowIcon />}
            variant="amber"
          />
          <OverviewCard
            label="Active Collection Assignments"
            value="—"
            description="In-progress fleet routes"
            icon={<TasksIcon />}
            variant="emerald"
          />
          <OverviewCard
            label="Available Vehicles"
            value="—"
            description="Ready for assignment"
            icon={<FleetIcon />}
            variant="blue"
          />
          <OverviewCard
            label="Open Operational Incidents"
            value="—"
            description="Field exceptions reported"
            icon={<OperationsIcon />}
            variant="mint"
          />
          <OverviewCard
            label="Unresolved Complaints"
            value="—"
            description="Citizen escalations"
            icon={<ComplaintsIcon />}
            variant="rose"
          />
        </div>
      </div>

      {/* Quick Actions Bar */}
      <QuickActions
        title="Quick Actions"
        description="Fast-track executive oversight, approvals, and decision workflows"
        actions={managerActions}
      />

      {/* Decision Queue Section */}
      <NeedsAttentionSection
        title="Needs Attention"
        badgeText="Decision Queue"
        subtitle="Executive approvals, critical incidents, and operational escalations"
        emptyTitle="All Decision Queues Clear"
        emptyDescription="Management decisions and operational issues requiring attention will appear here once the corresponding services are connected."
      />
    </div>
  );
};

export default ManagerDashboardPage;
