import React from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../../components/ui/Button';
import { useAuthStore } from '../../../store/authStore';
import {
  BIN_CONDITION_LABELS,
  BIN_STATUS_LABELS,
  type BinAdministrativeStatus,
  type BinCondition,
  type WasteBinSummaryDto,
} from '../types/bins';

interface BinsTableProps {
  bins: WasteBinSummaryDto[];
  isLoading: boolean;
  isFiltered: boolean;
  onClearFilters: () => void;
}

const statusClasses: Record<BinAdministrativeStatus, string> = {
  Active: 'bg-emerald-50 text-emerald-800 border-emerald-200',
  OutOfService: 'bg-amber-50 text-amber-800 border-amber-200',
  Retired: 'bg-slate-100 text-slate-700 border-slate-200',
};

const conditionClasses: Record<BinCondition, string> = {
  Good: 'text-emerald-700',
  Damaged: 'text-amber-700',
  Blocked: 'text-red-700',
  Missing: 'text-red-700',
};

const formatObservationDate = (value: string | null) => {
  if (!value) return 'No observation recorded';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }).format(date);
};

export const BinsTable: React.FC<BinsTableProps> = ({ bins, isLoading, isFiltered, onClearFilters }) => {
  const { user } = useAuthStore();
  const actionLabel = user?.role === 'MunicipalManager' ? 'View Details' : 'Manage Bin';
  if (isLoading) {
    return <div className="p-6 space-y-3 animate-pulse" data-testid="bins-table-loading">{Array.from({ length: 6 }).map((_, index) => <div key={index} className="h-14 rounded bg-slate-100" />)}</div>;
  }

  if (bins.length === 0) {
    return (
      <div className="py-16 px-4 text-center" data-testid={isFiltered ? 'bins-table-filtered-empty' : 'bins-table-empty'}>
        <div className="w-12 h-12 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-600 mx-auto flex items-center justify-center mb-3" aria-hidden="true">⌫</div>
        <p className="text-base font-bold text-slate-800">{isFiltered ? 'No bins match the current filters.' : 'No bins have been registered.'}</p>
        <p className="mt-1 text-xs text-slate-500">{isFiltered ? 'Adjust or clear the filters to review the bin registry.' : 'Registered municipal roadside bins will appear here.'}</p>
        {isFiltered && <Button variant="secondary" size="sm" onClick={onClearFilters} className="mt-4">Clear filters</Button>}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[1080px] text-left text-xs border-collapse">
        <thead><tr className="bg-slate-50/80 border-b border-slate-200/80 text-slate-500 uppercase tracking-wider font-semibold"><th className="py-3.5 px-4">Bin</th><th className="py-3.5 px-4">Location</th><th className="py-3.5 px-4">Status</th><th className="py-3.5 px-4">Accepted waste</th><th className="py-3.5 px-4">Latest observation</th><th className="py-3.5 px-4">Task</th><th className="py-3.5 px-4 text-right">Actions</th></tr></thead>
        <tbody className="divide-y divide-slate-100">
          {bins.map((bin) => {
            const location = bin.addressText?.trim() || `${bin.latitude.toFixed(5)}, ${bin.longitude.toFixed(5)}`;
            const hasObservation = bin.latestFillLevelPercent !== null || bin.latestCondition !== null || bin.latestObservationAt !== null;
            return <tr key={bin.id} className="hover:bg-slate-50/70 transition-colors" data-testid={`bin-row-${bin.id}`}>
              <td className="py-3.5 px-4"><Link to={`/officer/bins/${bin.id}`} className="font-mono text-[11px] font-bold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 px-1.5 py-0.5 rounded border border-emerald-200/60 transition-colors" aria-label={`View details for bin ${bin.binCode}`} data-testid={`bin-details-${bin.id}`}>{bin.binCode}</Link><p className="mt-1 text-slate-500">{bin.capacityLiters.toLocaleString()} L</p></td>
              <td className="py-3.5 px-4 max-w-64"><p className="text-slate-700 leading-relaxed" title={location}>{location}</p><p className="mt-1 text-slate-400 font-mono">{bin.latitude.toFixed(5)}, {bin.longitude.toFixed(5)}</p></td>
              <td className="py-3.5 px-4"><span className={`inline-flex px-2 py-0.5 rounded border text-[11px] font-semibold ${statusClasses[bin.administrativeStatus]}`}>{BIN_STATUS_LABELS[bin.administrativeStatus]}</span></td>
              <td className="py-3.5 px-4"><div className="flex flex-wrap gap-1">{bin.acceptedWasteTypes.map((type) => <span key={type} className="px-2 py-0.5 rounded bg-slate-100 border border-slate-200 text-slate-700">{type}</span>)}</div></td>
              <td className="py-3.5 px-4"><p className="text-slate-700">{hasObservation ? <><span className="font-semibold">{bin.latestFillLevelPercent === null ? 'Fill unknown' : `${bin.latestFillLevelPercent}%`}</span>{bin.latestCondition && <span className={`ml-1.5 ${conditionClasses[bin.latestCondition]}`}>· {BIN_CONDITION_LABELS[bin.latestCondition]}</span>}</> : 'No observation'}</p><p className="mt-1 text-slate-400">Latest: {formatObservationDate(bin.latestObservationAt)}</p></td>
              <td className="py-3.5 px-4"><span className={bin.hasActiveTask ? 'text-amber-700 font-semibold' : 'text-slate-500'}>{bin.hasActiveTask ? 'Active task' : 'No active task'}</span></td>
              <td className="py-3.5 px-4 text-right"><Link to={`/officer/bins/${bin.id}`} className="inline-flex items-center justify-center rounded-md bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-emerald-700" data-testid={`manage-bin-${bin.id}`}>{actionLabel}</Link></td>
            </tr>;
          })}
        </tbody>
      </table>
    </div>
  );
};
