import React, { createContext, useContext, useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Mark } from './ui';
import type { SessionUser } from '../types';

type AuthValue = {
  user: SessionUser | null;
  login: (u: SessionUser) => void;
  logout: () => void;
};

export const AuthCtx = createContext<AuthValue>({ user: null, login: () => { }, logout: () => { } });
export const useAuth = () => useContext(AuthCtx);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null);
  useEffect(() => {
    const raw = localStorage.getItem('hd_user');
    if (raw) { try { setUser(JSON.parse(raw)); } catch { /* ignore */ } }
  }, []);
  const login = (u: SessionUser) => setUser(u);
  const logout = () => {
    localStorage.removeItem('hd_access');
    localStorage.removeItem('hd_refresh');
    localStorage.removeItem('hd_user');
    setUser(null);
  };
  return <AuthCtx.Provider value={{ user, login, logout }}>{children}</AuthCtx.Provider>;
}

function NotificationsLink() {
  const [unread, setUnread] = useState(0);
  useEffect(() => {
    let live = true;
    const poll = async () => {
      try {
        const { apiGet } = await import('../api/client');
        const items = await apiGet<{ isRead: boolean }[]>('/api/notifications', []);
        if (live) setUnread(items.filter((i) => !i.isRead).length);
      } catch { /* offline */ }
    };
    poll();
    const t = setInterval(poll, 15000);
    return () => { live = false; clearInterval(t); };
  }, []);
  return <Link to="/notifications">Notifications ({unread} unread)</Link>;
}

export function Layout({ children }: { children: React.ReactNode }) {
  const loc = useLocation();
  const auth = useAuth();
  const [dark, setDark] = useState(() => localStorage.getItem('hd_dark') === '1');
  useEffect(() => {
    document.body.classList.toggle('dark', dark);
    localStorage.setItem('hd_dark', dark ? '1' : '0');
  }, [dark ]);

  const links: [string, string][] = [
    ['/', 'Dashboard'], ['/tickets', 'Tickets'], ['/workload', 'Workload'],
    ['/knowledge', 'Knowledge Base'], ['/sla', 'SLA'], ['/reports', 'Reports'],
    ['/canned', 'Canned Replies'], ['/notifications', 'Notifications'],
  ];
  if (auth.user?.role === 'Admin') links.push(['/users', 'Users']);
  links.push(['/account', auth.user ? 'Account (' + auth.user.username + ')' : 'Account']);

  return (
    <div className="shell">
      <nav className="sidebar">
        <h2><Mark /> Helpdesk Console</h2>
        <p>Role: {auth.user ? auth.user.role : 'signed out'}. Numbers like TKT-YYYYMMDD-XXXX.</p>
        {links.map(([to, label]) => (
          <Link key={to} className={'navlink' + (loc.pathname === to ? ' active' : '')} to={to}>{label}</Link>
        ))}
        <div className="spacer" />
        <button className="secondary" onClick={() => setDark((d) => !d)}>{dark ? 'Light mode' : 'Dark mode'}</button>
        <div className="spacer" />
        <p>Legal</p>
        <Link className="navlink" to="/tos">Terms of Service</Link>
        <Link className="navlink" to="/privacy">Privacy Policy</Link>
      </nav>
      <main className="main">
        <div className="topbar">
          <div>
            <strong>Helpdesk Ticketing System</strong>
            <div className="small muted">Flow: New to Open to In Progress to Pending to Resolved to Closed. Reopen within 7 days. Auto close after 14 days idle.</div>
          </div>
          <div className="row">
            <span className="small"><NotificationsLink /></span>
            {auth.user ? <button className="secondary" onClick={auth.logout}>Sign out</button> : <Link to="/account">Sign in</Link>}
            <Link to="/tickets"><button>New ticket</button></Link>
          </div>
        </div>
        {children}
        <div className="footer">
          <span>Business hours: Mon to Fri, 08:00 to 17:00 SAST</span>
          <Link to="/tos">Terms</Link>
          <Link to="/privacy">Privacy</Link>
          <span>Reset links expire after 60 minutes and are single use.</span>
        </div>
      </main>
    </div>
  );
}
