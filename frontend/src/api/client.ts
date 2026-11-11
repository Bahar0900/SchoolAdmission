import type { SchoolClass, HoldItem, CreateHoldResponse } from '../types';

const getApiBaseUrl = (): string => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // On Windows with Docker Desktop / WSL2, resolving 'localhost' routes to IPv6 [::1] which
    // can cause network hangs with wslrelay. 127.0.0.1 guarantees direct IPv4 Docker routing.
    if (!host || host === 'localhost' || host === '127.0.0.1') {
      return 'http://127.0.0.1:8080';
    }
    return `http://${host}:8080`;
  }
  const envUrl = import.meta.env.VITE_API_BASE_URL as string;
  return envUrl || 'http://127.0.0.1:8080';
};

const API_BASE_URL = getApiBaseUrl();

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let errorMsg = `Server error (${res.status})`;
    try {
      const errorData = await res.json();
      errorMsg = errorData.message || errorData.detail || JSON.stringify(errorData);
    } catch {
      // fallback to status text
      errorMsg = res.statusText || errorMsg;
    }
    throw new Error(errorMsg);
  }
  if (res.status === 204) {
    return {} as T;
  }
  return res.json();
}

export const api = {
  async getClasses(): Promise<SchoolClass[]> {
    const res = await fetch(`${API_BASE_URL}/classes`);
    return handleResponse<SchoolClass[]>(res);
  },

  async createClass(name: string, totalSeats: number): Promise<{ id: string; name: string; totalSeats: number }> {
    const res = await fetch(`${API_BASE_URL}/classes`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, totalSeats }),
    });
    return handleResponse(res);
  },

  async createHold(classId: string, studentId: string): Promise<CreateHoldResponse> {
    const res = await fetch(`${API_BASE_URL}/classes/${classId}/holds`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Student-Id': studentId,
      },
    });
    return handleResponse<CreateHoldResponse>(res);
  },

  async getHold(holdId: string): Promise<{ holdId: string; classId: string; status: string; expiresAt: string }> {
    const res = await fetch(`${API_BASE_URL}/holds/${holdId}`);
    return handleResponse(res);
  },

  async confirmHold(holdId: string, studentId: string): Promise<{ holdId: string; status: string }> {
    const res = await fetch(`${API_BASE_URL}/holds/${holdId}/confirm`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Student-Id': studentId,
      },
    });
    return handleResponse(res);
  },

  async cancelHold(holdId: string, studentId: string): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/holds/${holdId}`, {
      method: 'DELETE',
      headers: {
        'X-Student-Id': studentId,
      },
    });
    return handleResponse<void>(res);
  },

  async getMyHolds(studentId: string): Promise<HoldItem[]> {
    const res = await fetch(`${API_BASE_URL}/me/holds`, {
      headers: {
        'X-Student-Id': studentId,
      },
    });
    return handleResponse<HoldItem[]>(res);
  },
};
