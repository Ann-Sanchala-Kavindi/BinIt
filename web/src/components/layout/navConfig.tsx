import React from 'react';
import {
  DashboardIcon,
  ReportsIcon,
  BinsIcon,
  SchedulesIcon,
  TasksIcon,
  ComplaintsIcon,
  AiWorkflowIcon,
  FleetIcon,
  OperationsIcon,
  AnalyticsIcon,
  AuditIcon,
  UsersIcon,
} from '../ui/Icons';

export interface NavItem {
  label: string;
  to: string;
  icon: React.ReactNode;
}

export const defaultOfficerNavItems: NavItem[] = [
  { label: 'Dashboard', to: '/officer/dashboard', icon: <DashboardIcon /> },
  { label: 'Waste Reports', to: '/officer/waste-reports', icon: <ReportsIcon /> },
  { label: 'Bin Management', to: '/officer/bins', icon: <BinsIcon /> },
  { label: 'Collection Needs', to: '/officer/schedules', icon: <SchedulesIcon /> },
  { label: 'Collection Tasks', to: '/officer/tasks', icon: <TasksIcon /> },
  { label: 'Complaints', to: '/officer/complaints', icon: <ComplaintsIcon /> },
];

export const managerNavItems: NavItem[] = [
  { label: 'Dashboard', to: '/manager/dashboard', icon: <DashboardIcon /> },
  { label: 'User Management', to: '/manager/users', icon: <UsersIcon /> },
  { label: 'AI Approvals', to: '/manager/ai-approvals', icon: <AiWorkflowIcon /> },
  { label: 'Fleet & Routes', to: '/manager/fleet', icon: <FleetIcon /> },
  { label: 'Waste Reports', to: '/manager/reports', icon: <ReportsIcon /> },
  { label: 'Operations', to: '/manager/operations', icon: <OperationsIcon /> },
  { label: 'Analytics', to: '/manager/analytics', icon: <AnalyticsIcon /> },
  { label: 'Audit Logs', to: '/manager/audit', icon: <AuditIcon /> },
];
