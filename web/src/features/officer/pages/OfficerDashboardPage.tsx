import React from 'react';
import { useAuthStore } from '../../../store/authStore';
import { OverviewCard } from '../components/OverviewCard';
import { NeedsAttentionSection } from '../components/NeedsAttentionSection';
import { QuickActions } from '../components/QuickActions';
import {
  ReportsIcon,
  BinsIcon,
  SchedulesIcon,
  TasksIcon,
  ComplaintsIcon,
  LeafIcon,
} from '../../../components/ui/Icons';

export const OfficerDashboardPage: React.FC = () => {
  const { user } = useAuthStore();

  return (
    <div className="space-y-6">
      {/* Dashboard Top Heading / Breadcrumb */}
      <div className="border-b border-slate-200/80 pb-5">
        <div>
          <div className="flex items-center gap-1.5 text-xs text-slate-400 font-medium mb-1">
            <span>Operations</span>
            <span>/</span>
            <span className="text-slate-600 font-semibold">Waste Officer Dashboard</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900 flex items-center gap-2">
            <span>Waste Officer Dashboard</span>
            <LeafIcon className="w-6 h-6 text-emerald-600 shrink-0 inline-block" />
          </h1>
          <p className="mt-1 text-xs sm:text-sm text-slate-500 leading-relaxed">
            Welcome back,{' '}
            <span className="font-semibold text-slate-800">
              {user?.fullName || 'Waste Officer'}
            </span>
            . Here&apos;s an overview of municipal waste operations requiring your
            attention.
          </p>
        </div>
      </div>

      {/* Operational Overview KPI Cards */}
      <div>
        <div className="mb-4">
          <h2 className="text-xs font-bold text-slate-900 tracking-wider uppercase">
            Operational Overview
          </h2>
          <p className="text-xs text-slate-500 mt-0.5">
            Real-time status indicators across municipal collection domains
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-3.5">
          <OverviewCard
            label="Reports Awaiting Review"
            value="—"
            description="Pending field verification"
            icon={<ReportsIcon />}
          />
          <OverviewCard
            label="Active Bins"
            value="—"
            description="Monitored collection points"
            icon={<BinsIcon />}
          />
          <OverviewCard
            label="Scheduled Collections"
            value="—"
            description="Active route schedules"
            icon={<SchedulesIcon />}
          />
          <OverviewCard
            label="Open Collection Tasks"
            value="—"
            description="Assigned & in-progress"
            icon={<TasksIcon />}
          />
          <OverviewCard
            label="Open Complaints"
            value="—"
            description="Citizen service inquiries"
            icon={<ComplaintsIcon />}
          />
        </div>
      </div>

      {/* Quick Actions Bar */}
      <QuickActions />

      {/* Needs Attention Section */}
      <NeedsAttentionSection />
    </div>
  );
};

export default OfficerDashboardPage;
