import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import { StudentProvider } from './context/StudentContext.tsx';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <StudentProvider>
      <App />
    </StudentProvider>
  </React.StrictMode>,
);
