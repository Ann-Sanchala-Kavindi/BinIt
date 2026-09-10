import React from 'react';

export interface OverviewCardProps {
  label: string;
  value?: string | number;
  description?: string;
  icon: React.ReactNode;
  iconColorStyle?: string;
  iconBgColor?: string;
}

export const OverviewCard: React.FC<OverviewCardProps> = ({
  label,
  value = '—',
  description,
  icon,
  iconColorStyle,
  iconBgColor,
}) => {
  const resolvedColorStyle =
    iconColorStyle ||
    iconBgColor ||
    'bg-white/90 border-emerald-200/90 text-emerald-800 shadow-2xs';

  return (
    <div className="bg-emerald-50/60 rounded-xl border border-emerald-200/80 shadow-2xs p-4 flex items-start gap-3.5 transition-all hover:bg-emerald-50/80 hover:border-emerald-300">
      <div
        className={`w-11 h-11 rounded-full border flex items-center justify-center shrink-0 mt-0.5 ${resolvedColorStyle}`}
        aria-hidden="true"
      >
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-2xl font-bold text-slate-900 leading-tight tracking-tight">
          {value}
        </p>
        <p className="text-xs font-bold text-slate-800 leading-snug mt-0.5">
          {label}
        </p>
        {description && (
          <p className="text-[11px] text-slate-600 mt-1 leading-normal">{description}</p>
        )}
      </div>
    </div>
  );
};
