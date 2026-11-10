export interface SchoolClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface HoldItem {
  holdId: string;
  classId: string;
  className: string;
  status: 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';
  expiresAt: string;
}

export interface CreateHoldResponse {
  holdId: string;
  classId: string;
  studentId: string;
  status: string;
  expiresAt: string;
}
