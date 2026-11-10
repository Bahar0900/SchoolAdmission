import React, { useState } from 'react';
import { api } from '../api/client';
import { PlusCircle, Loader2 } from 'lucide-react';

interface AdminClassFormProps {
  onClassCreated: () => void;
  onError: (msg: string) => void;
  onSuccess: (msg: string) => void;
}

export const AdminClassForm: React.FC<AdminClassFormProps> = ({ onClassCreated, onError, onSuccess }) => {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState<number | ''>('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      onError('ক্লাসের নাম দেওয়া আবশ্যক।');
      return;
    }
    if (!totalSeats || totalSeats <= 0) {
      onError('আসন সংখ্যা অবশ্যই কমপক্ষে ১ হতে হবে।');
      return;
    }

    try {
      setLoading(true);
      await api.createClass(name.trim(), Number(totalSeats));
      onSuccess(`"${name.trim()}" সফলভাবে ${totalSeats}টি আসন সহ তৈরি হয়েছে!`);
      setName('');
      setTotalSeats('');
      onClassCreated();
    } catch (err: any) {
      onError(err.message || 'ক্লাস তৈরি করতে সমস্যা হয়েছে।');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-6 mb-8 max-w-xl mx-auto">
      <div className="flex items-center gap-2 mb-4 text-slate-800">
        <PlusCircle className="w-5 h-5 text-indigo-600" />
        <h2 className="text-lg font-bold">নতুন Class বা Shift যোগ করুন (School Admin)</h2>
      </div>

      <p className="text-xs text-slate-500 mb-5 leading-relaxed">
        প্রতিটি শিফট আলাদা একটি ক্লাস হিসেবে গণ্য হবে (যেমন: "Class 6 – Morning Shift", "Class 6 – Day Shift")।
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            Class ও Shift-এর নাম
          </label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="উদাঃ Class 6 - Morning Shift"
            className="w-full border border-slate-300 rounded-lg px-3.5 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            required
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">
            মোট আসন সংখ্যা (Total Seats)
          </label>
          <input
            type="number"
            min="1"
            max="10000"
            value={totalSeats}
            onChange={(e) => setTotalSeats(e.target.value ? parseInt(e.target.value, 10) : '')}
            placeholder="উদাঃ 120"
            className="w-full border border-slate-300 rounded-lg px-3.5 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            required
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2.5 px-4 rounded-lg transition-colors flex items-center justify-center gap-2 shadow-sm disabled:opacity-50"
        >
          {loading ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              <span>তৈরি হচ্ছে...</span>
            </>
          ) : (
            <span>Class তৈরি করুন</span>
          )}
        </button>
      </form>
    </div>
  );
};
