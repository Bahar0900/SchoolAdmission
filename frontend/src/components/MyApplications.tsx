import React from 'react';
import type { HoldItem } from '../types';
import { ApplicationCard } from './ApplicationCard';
import { ClipboardList, Loader2 } from 'lucide-react';

interface MyApplicationsProps {
  holds: HoldItem[];
  loading: boolean;
  onConfirm: (holdId: string) => Promise<void>;
  onCancel: (holdId: string) => Promise<void>;
  onExpired: (holdId: string) => void;
  onGoToClasses: () => void;
}

export const MyApplications: React.FC<MyApplicationsProps> = ({
  holds,
  loading,
  onConfirm,
  onCancel,
  onExpired,
  onGoToClasses,
}) => {
  if (loading && holds.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <Loader2 className="w-8 h-8 animate-spin text-indigo-600 mb-2" />
        <p className="text-sm">আপনার আবেদনসমূহ লোড হচ্ছে...</p>
      </div>
    );
  }

  if (holds.length === 0) {
    return (
      <div className="text-center py-16 bg-white rounded-xl border border-dashed border-slate-300 p-8 max-w-lg mx-auto">
        <ClipboardList className="w-12 h-12 text-slate-400 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-slate-800">আপনার কোনো আবেদন নেই</h3>
        <p className="text-xs text-slate-500 mt-1 mb-4">
          নির্দিষ্ট ক্লাসে সিট সংরক্ষণ করার জন্য ক্লাস তালিকা থেকে আবেদন করুন।
        </p>
        <button
          onClick={onGoToClasses}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold px-4 py-2 rounded-lg transition-colors"
        >
          ক্লাস তালিকা দেখুন
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-2">
        <div>
          <h2 className="text-lg font-bold text-slate-900">আমার সকল আবেদন</h2>
          <p className="text-xs text-slate-500">আপনার জন্ম নিবন্ধন নম্বরের অধীনে থাকা সকল আবেদনের অবস্থা</p>
        </div>
        <span className="text-xs font-semibold bg-slate-100 text-slate-700 px-3 py-1 rounded-full border border-slate-200">
          মোট আবেদন: {holds.length}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {holds.map((hold) => (
          <ApplicationCard
            key={hold.holdId}
            hold={hold}
            onConfirm={onConfirm}
            onCancel={onCancel}
            onExpired={onExpired}
          />
        ))}
      </div>
    </div>
  );
};
