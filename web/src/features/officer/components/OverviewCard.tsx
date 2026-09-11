import React from 'react';

export type OverviewVariant = 'amber' | 'emerald' | 'blue' | 'mint' | 'rose' | 'slate';

export interface OverviewCardProps {
  label: string;
  value?: string | number;
  description?: string;
  icon: React.ReactNode;
  iconColorStyle?: string;
  iconBgColor?: string;
  variant?: OverviewVariant;
}

const variantStyles: Record<
  OverviewVariant,
  { card: string; iconContainer: string; iconText: string }
> = {
  amber: {
    card: 'bg-[#FEF9F1] border-[#FDE68A] hover:border-amber-300 hover:bg-[#FEF6E9]',
    iconContainer: 'bg-[#FEF3C7] border-amber-200/80',
    iconText: 'text-[#D97706]',
  },
  emerald: {
    card: 'bg-[#ECFDF5] border-[#A7F3D0] hover:border-emerald-300 hover:bg-[#E6FAF0]',
    iconContainer: 'bg-[#D1FAE5] border-emerald-200/80',
    iconText: 'text-[#059669]',
  },
  blue: {
    card: 'bg-[#EDF6FE] border-[#BAE6FD] hover:border-blue-300 hover:bg-[#E4F0FD]',
    iconContainer: 'bg-[#DBEAFE] border-blue-200/80',
    iconText: 'text-[#2563EB]',
  },
  mint: {
    card: 'bg-[#F1FDF7] border-[#A7F3D0] hover:border-emerald-300 hover:bg-[#E6F9F0]',
    iconContainer: 'bg-[#D1FAE5] border-emerald-200/80',
    iconText: 'text-[#10B981]',
  },
  rose: {
    card: 'bg-[#FEF2F5] border-[#FECDD3] hover:border-rose-300 hover:bg-[#FDE8ED]',
    iconContainer: 'bg-[#FFE4E6] border-rose-200/80',
    iconText: 'text-[#E11D48]',
  },
  slate: {
    card: 'bg-white border-slate-200 hover:border-slate-300',
    iconContainer: 'bg-slate-100 border-slate-200',
    iconText: 'text-slate-700',
  },
};

export const OverviewCard: React.FC<OverviewCardProps> = ({
  label,
  value = '—',
  description,
  icon,
  iconColorStyle,
  iconBgColor,
  variant = 'emerald',
}) => {
  const currentVariant = variantStyles[variant] || variantStyles.emerald;
  const resolvedCardStyle = currentVariant.card;
  const resolvedIconContainerStyle =
    iconColorStyle ||
    iconBgColor ||
    `${currentVariant.iconContainer} ${currentVariant.iconText}`;

  return (
    <div
      className={`rounded-xl border shadow-2xs p-4 flex items-start gap-3.5 transition-all ${resolvedCardStyle}`}
    >
      <div
        className={`w-11 h-11 rounded-full border flex items-center justify-center shrink-0 mt-0.5 shadow-2xs ${resolvedIconContainerStyle}`}
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
          <p className="text-[11px] text-slate-500 mt-1 leading-normal">
            {description}
          </p>
        )}
      </div>
    </div>
  );
};
