import React, { useState, useEffect } from 'react';
import type { HoldItem } from '../types';
import { Clock, CreditCard, XCircle, CheckCircle2, AlertOctagon, Loader2 } from 'lucide-react';

interface ApplicationCardProps {
  hold: HoldItem;
  onConfirm: (holdId: string) => Promise<void>;
  onCancel: (holdId: string) => Promise<void>;
  onExpired: (holdId: string) => void;
}

export const ApplicationCard: React.FC<ApplicationCardProps> = ({
  hold,
  onConfirm,
  onCancel,
  onExpired,
}) => {
  const [timeLeft, setTimeLeft] = useState<number>(() => {
    const diff = new Date(hold.expiresAt).getTime() - new Date().getTime();
    return Math.max(0, Math.floor(diff / 1000));
  });

  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  // Live countdown timer
  useEffect(() => {
    if (hold.status !== 'HELD') return;

    const interval = setInterval(() => {
      const diff = new Date(hold.expiresAt).getTime() - new Date().getTime();
      const seconds = Math.max(0, Math.floor(diff / 1000));
      setTimeLeft(seconds);

      if (seconds <= 0) {
        clearInterval(interval);
        onExpired(hold.holdId);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [hold.expiresAt, hold.status, hold.holdId, onExpired]);

  const formatTimer = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const isHeld = hold.status === 'HELD' && timeLeft > 0;
  const isExpired = hold.status === 'EXPIRED' || (hold.status === 'HELD' && timeLeft <= 0);
  const isConfirmed = hold.status === 'CONFIRMED';
  const isCancelled = hold.status === 'CANCELLED';

  const handleConfirmClick = async () => {
    try {
      setConfirming(true);
      await onConfirm(hold.holdId);
    } finally {
      setConfirming(false);
    }
  };

  const handleCancelClick = async () => {
    if (!window.confirm('আপনি কি নিশ্চিত যে আবেদনটি বাতিল করতে চান? আসনটি তাৎক্ষণিক উন্মুক্ত হয়ে যাবে।')) {
      return;
    }
    try {
      setCancelling(true);
      await onCancel(hold.holdId);
    } finally {
      setCancelling(false);
    }
  };

  return (
    <div
      className={`bg-white rounded-xl border p-5 shadow-sm transition-all ${
        isConfirmed
          ? 'border-emerald-300 ring-1 ring-emerald-200'
          : isExpired
          ? 'border-rose-200 bg-rose-50/20'
          : isCancelled
          ? 'border-slate-200 bg-slate-50/50'
          : 'border-indigo-300 ring-2 ring-indigo-50'
      }`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <span className="text-[11px] font-mono text-slate-400 block mb-0.5">ID: {hold.holdId}</span>
          <h3 className="text-base font-bold text-slate-900">{hold.className}</h3>
        </div>

        {/* Status Badge */}
        <div>
          {isConfirmed && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              ভর্তি নিশ্চিত (CONFIRMED)
            </span>
          )}
          {isHeld && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-indigo-100 text-indigo-800 animate-pulse">
              <Clock className="w-4 h-4 text-indigo-600" />
              আসন সংরক্ষিত (HELD)
            </span>
          )}
          {isExpired && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-rose-100 text-rose-800">
              <AlertOctagon className="w-4 h-4 text-rose-600" />
              মেয়াদোত্তীর্ণ (EXPIRED)
            </span>
          )}
          {isCancelled && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-slate-200 text-slate-700">
              <XCircle className="w-4 h-4 text-slate-500" />
              বাতিলকৃত (CANCELLED)
            </span>
          )}
        </div>
      </div>

      {/* Countdown Timer Block (Rule 9: No reload when expired) */}
      {isHeld && (
        <div className="bg-indigo-50 border border-indigo-100 rounded-lg p-3.5 mb-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Clock className="w-5 h-5 text-indigo-600 animate-spin" style={{ animationDuration: '6s' }} />
            <div>
              <p className="text-xs font-semibold text-indigo-900">ফি প্রদানের সময় বাকি:</p>
              <p className="text-[11px] text-indigo-600">মেয়াদ শেষ হলে সিটটি স্বয়ংক্রিয়ভাবে ছেড়ে দেওয়া হবে</p>
            </div>
          </div>
          <span className="text-xl font-mono font-bold text-indigo-700 bg-white px-3 py-1 rounded border border-indigo-200 shadow-inner">
            {formatTimer(timeLeft)}
          </span>
        </div>
      )}

      {isExpired && (
        <div className="bg-rose-50 border border-rose-100 rounded-lg p-3 mb-4 text-xs text-rose-700 flex items-center gap-2">
          <AlertOctagon className="w-4 h-4 text-rose-500 flex-shrink-0" />
          <span>আবেদনের মেয়াদ শেষ হয়ে গেছে এবং সিটটি অন্য শিক্ষার্থীদের জন্য উন্মুক্ত করা হয়েছে।</span>
        </div>
      )}

      {isConfirmed && (
        <div className="bg-emerald-50 border border-emerald-100 rounded-lg p-3 mb-4 text-xs text-emerald-800 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />
          <span>ভর্তি ফি সফলভাবে গৃহীত হয়েছে। আপনার সন্তান এই ক্লাসে ভর্তির জন্য চূড়ান্তভাবে নির্বাচিত।</span>
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex items-center gap-3 pt-2 border-t border-slate-100">
        <button
          onClick={handleConfirmClick}
          disabled={!isHeld || confirming || cancelling}
          className={`flex-1 py-2.5 px-4 rounded-lg font-medium text-sm flex items-center justify-center gap-2 shadow-sm transition-all ${
            isHeld && !confirming
              ? 'bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer active:scale-[0.99]'
              : 'bg-slate-200 text-slate-400 cursor-not-allowed'
          }`}
          title={!isHeld ? 'মেয়াদ শেষ হওয়ায় ফি দেওয়া সম্ভব নয়' : 'ফি প্রদান সম্পন্ন করুন'}
        >
          {confirming ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              <span>কনফার্ম হচ্ছে...</span>
            </>
          ) : (
            <>
              <CreditCard className="w-4 h-4" />
              <span>ভর্তি ফি দিন</span>
            </>
          )}
        </button>

        {isHeld && (
          <button
            onClick={handleCancelClick}
            disabled={cancelling || confirming}
            className="py-2.5 px-4 rounded-lg font-medium text-sm text-slate-600 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 hover:border-rose-200 transition-colors flex items-center gap-1.5"
          >
            {cancelling ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <XCircle className="w-4 h-4" />
            )}
            <span>বাতিল</span>
          </button>
        )}
      </div>
    </div>
  );
};
