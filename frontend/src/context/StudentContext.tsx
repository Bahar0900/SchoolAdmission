import React, { createContext, useContext, useState, useEffect } from 'react';

interface StudentContextType {
  studentId: string;
  setStudentId: (id: string) => void;
  isValidStudentId: boolean;
}

const StudentContext = createContext<StudentContextType | undefined>(undefined);

export const StudentProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [studentId, setStudentIdState] = useState<string>(() => {
    return localStorage.getItem('student_id') || '20152691234567890';
  });

  const isValidStudentId = /^\d{17}$/.test(studentId.trim());

  const setStudentId = (id: string) => {
    setStudentIdState(id);
    localStorage.setItem('student_id', id);
  };

  useEffect(() => {
    if (studentId) {
      localStorage.setItem('student_id', studentId);
    }
  }, [studentId]);

  return (
    <StudentContext.Provider value={{ studentId, setStudentId, isValidStudentId }}>
      {children}
    </StudentContext.Provider>
  );
};

export const useStudent = () => {
  const context = useContext(StudentContext);
  if (!context) {
    throw new Error('useStudent must be used within a StudentProvider');
  }
  return context;
};
