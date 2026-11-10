import React from 'react';
import type { SchoolClass } from '../types';
import { useStudent } from '../context/StudentContext';
import { Users, Lock, CheckCircle, Clock, ArrowRight, Loader2 } from 'lucide-react';

interface ClassListProps {
  classes: SchoolClass[];
  loading: boolean;
  onApply: (classId: string) => void;
  applyingClassId: string | null;
}

export const ClassList: React.FC<ClassListProps> = ({ classes, loading, onApply, applyingClassId }) => {
  const { isValidStudentId } = useStudent();

  if (loading && classes.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <Loader2 className="w-8 h-8 animate-spin text-indigo-600 mb-2" />
        <p className="text-sm">ক্লাসের তালিকা লোড হচ্ছে...</p>
      </div>
    );
  }

  if (classes.length === 0) {
    return (
      <div className="text-center py-16 bg-white rounded-xl border border-dashed border-slate-300 p-8">
        <Users className="w-12 h-12 text-slate-400 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-slate-800">এখনও কোনো ক্লাস যোগ করা হয়নি</h3>
        <p className="text-xs text-slate-500 mt-1">
          উপরে "Class তৈরি (School Admin)" ট্যাবে গিয়ে নতুন ক্লাস বা শিফট তৈরি করুন।
        </p>
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-lg font-bold text-slate-900">ভর্তিযোগ্য ক্লাসের তালিকা</h2>
          <p className="text-xs text-slate-500">সিটের তথ্য প্রতি ৩ সেকেন্ড পর পর স্বয়ংক্রিয় হালনাগাদ হচ্ছে</p>
        </div>
        <div className="flex items-center gap-1.5 text-xs text-emerald-600 bg-emerald-50 px-2.5 py-1 rounded-full font-medium border border-emerald-100">
          <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
          <span>Live Seats</span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {classes.map((cls) => {
          const isApplying = applyingClassId === cls.id;
          const isFull = cls.availableSeats <= 0;

          return (
            <div
              key={cls.id}
              className={`bg-white rounded-xl border transition-all p-5 shadow-sm hover:shadow-md flex flex-col justify-between ${
                isFull ? 'border-slate-200 bg-slate-50/50' : 'border-indigo-100'
              }`}
            >
              <div>
                <div className="flex items-start justify-between mb-3">
                  <h3 className="font-bold text-slate-900 text-base">{cls.name}</h3>
                  <span
                    className={`text-xs px-2.5 py-1 rounded-full font-semibold ${
                      isFull
                        ? 'bg-rose-100 text-rose-700'
                        : cls.availableSeats < 10
                        ? 'bg-amber-100 text-amber-700'
                        : 'bg-emerald-100 text-emerald-700'
                    }`}
                  >
                    {isFull ? 'সিট খালি নেই' : `${cls.availableSeats} সিট খালি`}
                  </span>
                </div>

                {/* Seat Breakdown Grid */}
                <div className="grid grid-cols-4 gap-2 py-3 bg-slate-50 rounded-lg border border-slate-100 text-center mb-4">
                  <div className="px-1">
                    <p className="text-[10px] text-slate-500 font-medium flex items-center justify-center gap-1">
                      <Users className="w-3 h-3 text-slate-400" /> মোট
                    </p>
                    <p className="text-sm font-bold text-slate-800 mt-0.5">{cls.totalSeats}</p>
                  </div>
                  <div className="px-1 border-l border-slate-200">
                    <p className="text-[10px] text-amber-600 font-medium flex items-center justify-center gap-1">
                      <Clock className="w-3 h-3" /> Hold
                    </p>
                    <p className="text-sm font-bold text-amber-700 mt-0.5">{cls.heldSeats}</p>
                  </div>
                  <div className="px-1 border-l border-slate-200">
                    <p className="text-[10px] text-emerald-600 font-medium flex items-center justify-center gap-1">
                      <CheckCircle className="w-3 h-3" /> নিশ্চিত
                    </p>
                    <p className="text-sm font-bold text-emerald-700 mt-0.5">{cls.confirmedSeats}</p>
                  </div>
                  <div className="px-1 border-l border-slate-200">
                    <p className="text-[10px] text-indigo-600 font-medium flex items-center justify-center gap-1">
                      <Lock className="w-3 h-3" /> খালি
                    </p>
                    <p className="text-sm font-bold text-indigo-700 mt-0.5">{cls.availableSeats}</p>
                  </div>
                </div>
              </div>

              {/* Action Button */}
              <button
                onClick={() => onApply(cls.id)}
                disabled={isFull || isApplying || !isValidStudentId}
                className={`w-full py-2.5 px-4 rounded-lg font-medium text-sm flex items-center justify-center gap-2 transition-all shadow-sm ${
                  isFull
                    ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                    : !isValidStudentId
                    ? 'bg-indigo-300 text-white cursor-not-allowed'
                    : 'bg-indigo-600 hover:bg-indigo-700 text-white active:scale-[0.99]'
                }`}
                title={!isValidStudentId ? 'উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর দিন' : ''}
              >
                {isApplying ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>আবেদন সম্পন্ন হচ্ছে...</span>
                  </>
                ) : isFull ? (
                  <span>সিট পূর্ণ (আবেদন বন্ধ)</span>
                ) : (
                  <>
                    <span>আবেদন করুন</span>
                    <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
};
