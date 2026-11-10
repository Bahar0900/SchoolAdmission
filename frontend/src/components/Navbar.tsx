import React from 'react';
import { useStudent } from '../context/StudentContext';
import { School, UserCheck, AlertTriangle } from 'lucide-react';

interface NavbarProps {
  activeTab: 'classes' | 'my-applications' | 'admin';
  setActiveTab: (tab: 'classes' | 'my-applications' | 'admin') => void;
  myHoldsCount: number;
}

export const Navbar: React.FC<NavbarProps> = ({ activeTab, setActiveTab, myHoldsCount }) => {
  const { studentId, setStudentId, isValidStudentId } = useStudent();

  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-50 shadow-sm">
      <div className="max-w-6xl mx-auto px-4 py-3 sm:px-6">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
          
          {/* Logo & Brand */}
          <div className="flex items-center gap-3">
            <div className="p-2 bg-indigo-600 text-white rounded-lg shadow-sm">
              <School className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-lg font-bold text-slate-900 leading-tight">School Admission System</h1>
              <p className="text-xs text-slate-500">অনলাইন ভর্তি পোর্টাল (আগে আবেদন করলে আগে পাবেন)</p>
            </div>
          </div>

          {/* Student ID (Birth Registration Number) Input Header */}
          <div className="w-full sm:w-auto flex items-center bg-slate-50 border rounded-lg p-2 gap-2 shadow-inner">
            <div className="text-slate-500 pl-1">
              <UserCheck className="w-5 h-5" />
            </div>
            <div className="flex flex-col">
              <label htmlFor="student-id-input" className="text-[11px] font-semibold text-slate-600">
                শিক্ষার্থীর জন্ম নিবন্ধন নম্বর (১৭ অঙ্ক)
              </label>
              <div className="flex items-center gap-2">
                <input
                  id="student-id-input"
                  type="text"
                  maxLength={17}
                  value={studentId}
                  onChange={(e) => setStudentId(e.target.value.replace(/\D/g, ''))}
                  placeholder="১৭ অঙ্কের নম্বর লিখুন"
                  className="bg-white border border-slate-300 rounded px-2.5 py-1 text-sm font-mono tracking-wider focus:outline-none focus:ring-2 focus:ring-indigo-500 w-48 sm:w-56"
                />
                <span
                  className={`text-xs px-2 py-0.5 rounded font-mono font-medium ${
                    isValidStudentId
                      ? 'bg-emerald-100 text-emerald-700'
                      : 'bg-amber-100 text-amber-700'
                  }`}
                  title={isValidStudentId ? 'সঠিক জন্ম নিবন্ধন নম্বর' : '১৭ অঙ্ক হওয়া আবশ্যক'}
                >
                  {studentId.length}/17
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Tab Navigation */}
        <nav className="flex space-x-1 mt-4 border-t border-slate-100 pt-3">
          <button
            onClick={() => setActiveTab('classes')}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === 'classes'
                ? 'bg-indigo-600 text-white shadow-sm'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            ক্লাস তালিকা ও আবেদন
          </button>
          <button
            onClick={() => setActiveTab('my-applications')}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors relative ${
              activeTab === 'my-applications'
                ? 'bg-indigo-600 text-white shadow-sm'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            আমার আবেদন
            {myHoldsCount > 0 && (
              <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500 text-white">
                {myHoldsCount}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab('admin')}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === 'admin'
                ? 'bg-indigo-600 text-white shadow-sm'
                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
            }`}
          >
            Class তৈরি (School Admin)
          </button>
        </nav>
      </div>

      {!isValidStudentId && (
        <div className="bg-amber-50 border-t border-amber-200 px-4 py-1.5 text-xs text-amber-800 flex items-center justify-center gap-1.5">
          <AlertTriangle className="w-3.5 h-3.5 text-amber-600" />
          <span>আবেদন করতে বা আগের আবেদন দেখতে অবশ্যই ১৭ অঙ্কের বৈধ জন্ম নিবন্ধন নম্বর প্রদান করুন।</span>
        </div>
      )}
    </header>
  );
};
