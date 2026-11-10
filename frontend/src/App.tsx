import React, { useState, useEffect, useCallback } from 'react';
import { api } from './api/client';
import type { SchoolClass, HoldItem } from './types';
import { useStudent } from './context/StudentContext';
import { Navbar } from './components/Navbar';
import { ClassList } from './components/ClassList';
import { AdminClassForm } from './components/AdminClassForm';
import { MyApplications } from './components/MyApplications';
import { AlertBanner } from './components/AlertBanner';

export const AppContent: React.FC = () => {
  const { studentId, isValidStudentId } = useStudent();
  const [activeTab, setActiveTab] = useState<'classes' | 'my-applications' | 'admin'>('classes');

  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [myHolds, setMyHolds] = useState<HoldItem[]>([]);
  const [loadingClasses, setLoadingClasses] = useState(false);
  const [loadingHolds, setLoadingHolds] = useState(false);
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);

  const [alert, setAlert] = useState<{ type: 'error' | 'success' | 'info'; message: string } | null>(null);

  // Fetch classes
  const loadClasses = useCallback(async (showLoading = false) => {
    try {
      if (showLoading) setLoadingClasses(true);
      const data = await api.getClasses();
      setClasses(data);
    } catch (err: any) {
      // If server is unreachable
      if (showLoading) {
        setAlert({
          type: 'error',
          message: 'সার্ভারের সাথে সংযোগ স্থাপন করা যাচ্ছে না। দয়া করে ব্যাকএন্ড চালু আছে কি না পরীক্ষা করুন।'
        });
      }
    } finally {
      if (showLoading) setLoadingClasses(false);
    }
  }, []);

  // Fetch student holds
  const loadMyHolds = useCallback(async (showLoading = false) => {
    if (!isValidStudentId) {
      setMyHolds([]);
      return;
    }
    try {
      if (showLoading) setLoadingHolds(true);
      const data = await api.getMyHolds(studentId);
      setMyHolds(data);
    } catch {
      // ignore silent background error
    } finally {
      if (showLoading) setLoadingHolds(false);
    }
  }, [studentId, isValidStudentId]);

  // Initial load
  useEffect(() => {
    loadClasses(true);
  }, [loadClasses]);

  useEffect(() => {
    loadMyHolds(true);
  }, [loadMyHolds]);

  // Rule 10: Auto-polling seat counts every 3 seconds without page reload
  useEffect(() => {
    const interval = setInterval(() => {
      loadClasses(false);
      loadMyHolds(false);
    }, 3000);

    return () => clearInterval(interval);
  }, [loadClasses, loadMyHolds]);

  // Handle Apply for seat
  const handleApply = async (classId: string) => {
    if (!isValidStudentId) {
      setAlert({
        type: 'error',
        message: 'আবেদন করার পূর্বে উপরে আপনার ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর সঠিকভাবে দিন।'
      });
      return;
    }

    try {
      setApplyingClassId(classId);
      await api.createHold(classId, studentId);
      setAlert({
        type: 'success',
        message: 'আসন সফলভাবে সাময়িকভাবে সংরক্ষণ (Hold) করা হয়েছে! নির্দিষ্ট মেয়াদের মধ্যে ভর্তি ফি প্রদান করে আসন নিশ্চিত করুন।'
      });
      await loadClasses(false);
      await loadMyHolds(false);
      setActiveTab('my-applications');
    } catch (err: any) {
      setAlert({
        type: 'error',
        message: err.message || 'আবেদন সম্পন্ন করা সম্ভব হয়নি।'
      });
    } finally {
      setApplyingClassId(null);
    }
  };

  // Handle Confirm hold (Pay Admission Fee)
  const handleConfirm = async (holdId: string) => {
    try {
      await api.confirmHold(holdId, studentId);
      setAlert({
        type: 'success',
        message: 'ভর্তি ফি সফলভাবে জমা নেওয়া হয়েছে এবং ভর্তি নিশ্চিত করা হয়েছে!'
      });
      await loadClasses(false);
      await loadMyHolds(false);
    } catch (err: any) {
      setAlert({
        type: 'error',
        message: err.message || 'ভর্তি ফি নিশ্চিত করা যায়নি।'
      });
      throw err;
    }
  };

  // Handle Cancel hold
  const handleCancel = async (holdId: string) => {
    try {
      await api.cancelHold(holdId, studentId);
      setAlert({
        type: 'info',
        message: 'আবেদনটি সফলভাবে বাতিল করা হয়েছে এবং আসনটি অন্য শিক্ষার্থীদের জন্য উন্মুক্ত করা হয়েছে।'
      });
      await loadClasses(false);
      await loadMyHolds(false);
    } catch (err: any) {
      setAlert({
        type: 'error',
        message: err.message || 'আবেদন বাতিল করা সম্ভব হয়নি।'
      });
      throw err;
    }
  };

  // Rule 9: When timer expires, update status without reload
  const handleExpired = useCallback((holdId: string) => {
    setMyHolds((prev) =>
      prev.map((h) => (h.holdId === holdId ? { ...h, status: 'EXPIRED' } : h))
    );
    loadClasses(false);
  }, [loadClasses]);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 flex flex-col">
      <Navbar
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        myHoldsCount={myHolds.filter((h) => h.status === 'HELD').length}
      />

      <main className="flex-1 max-w-6xl w-full mx-auto px-4 py-6 sm:px-6">
        {/* User-friendly Alert Notifications */}
        {alert && (
          <AlertBanner
            type={alert.type}
            message={alert.message}
            onClose={() => setAlert(null)}
          />
        )}

        {/* Tab 1: Class List & Apply */}
        {activeTab === 'classes' && (
          <ClassList
            classes={classes}
            loading={loadingClasses}
            onApply={handleApply}
            applyingClassId={applyingClassId}
          />
        )}

        {/* Tab 2: My Applications */}
        {activeTab === 'my-applications' && (
          <MyApplications
            holds={myHolds}
            loading={loadingHolds}
            onConfirm={handleConfirm}
            onCancel={handleCancel}
            onExpired={handleExpired}
            onGoToClasses={() => setActiveTab('classes')}
          />
        )}

        {/* Tab 3: Admin Class Creation */}
        {activeTab === 'admin' && (
          <AdminClassForm
            onClassCreated={() => {
              loadClasses(false);
              setActiveTab('classes');
            }}
            onError={(msg) => setAlert({ type: 'error', message: msg })}
            onSuccess={(msg) => setAlert({ type: 'success', message: msg })}
          />
        )}
      </main>

      <footer className="bg-white border-t border-slate-200 py-4 text-center text-xs text-slate-400">
        School Admission System &bull; .NET 8 + PostgreSQL + React 18 + Docker
      </footer>
    </div>
  );
};

export default function App() {
  return <AppContent />;
}
