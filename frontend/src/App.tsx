import React, { useEffect } from 'react';
import './App.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider, Layout } from './components/Layout';
import { ensureCsrf } from './api/client';
import { Dashboard } from './pages/Dashboard';
import { TicketList } from './pages/TicketList';
import { TicketDetail } from './pages/TicketDetail';
import { Workload } from './pages/Workload';
import { Knowledge } from './pages/Knowledge';
import { Sla } from './pages/Sla';
import { Reports } from './pages/Reports';
import { Canned } from './pages/Canned';
import { NotificationsPage } from './pages/Notifications';
import { UsersPage } from './pages/Users';
import { Account } from './pages/Account';
import { Privacy, Tos } from './pages/Legal';

export default function App() {
  useEffect(() => { ensureCsrf(); }, []);
  return (
    <AuthProvider>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/tickets" element={<TicketList />} />
            <Route path="/tickets/:id" element={<TicketDetail />} />
            <Route path="/workload" element={<Workload />} />
            <Route path="/knowledge" element={<Knowledge />} />
            <Route path="/sla" element={<Sla />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/canned" element={<Canned />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/users" element={<UsersPage />} />
            <Route path="/account" element={<Account />} />
            <Route path="/tos" element={<Tos />} />
            <Route path="/privacy" element={<Privacy />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </AuthProvider>
  );
}
