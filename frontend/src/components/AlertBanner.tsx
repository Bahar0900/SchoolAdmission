import React from 'react';
import { AlertCircle, CheckCircle2, X } from 'lucide-react';

interface AlertBannerProps {
  type: 'error' | 'success' | 'info';
  message: string;
  onClose?: () => void;
}

export const AlertBanner: React.FC<AlertBannerProps> = ({ type, message, onClose }) => {
  if (!message) return null;

  const isError = type === 'error';
  const isSuccess = type === 'success';

  return (
    <div
      className={`rounded-lg p-4 mb-4 flex items-start justify-between shadow-sm border transition-all ${
        isError
          ? 'bg-red-50 border-red-200 text-red-800'
          : isSuccess
          ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
          : 'bg-blue-50 border-blue-200 text-blue-800'
      }`}
    >
      <div className="flex items-center gap-3">
        {isError && <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0" />}
        {isSuccess && <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />}
        <p className="text-sm font-medium">{message}</p>
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className="text-gray-400 hover:text-gray-600 p-1 rounded transition-colors"
          title="Close"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
};
