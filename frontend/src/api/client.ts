import type { SchoolClass, HoldItem, CreateHoldResponse } from '../types';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:8080';

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
