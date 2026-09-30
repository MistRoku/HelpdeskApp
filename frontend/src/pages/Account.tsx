import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { API_BASE, apiWrite } from '../api/client';
import { useAuth } from '../components/Layout';

export function Account() {
  const auth = useAuth();
  const nav = useNavigate();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [msg, setMsg] = useState('');
  const [resetEmail, setResetEmail] = useState('');

  const doLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg('');
    try {
      const res = await fetch(API_BASE + '/api/auth/login', {
        method: 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      const data = await res.json();
      if (!res.ok) { setMsg('Login failed. Check username and password.'); return; }
      localStorage.setItem('hd_access', data.accessToken);
      localStorage.setItem('hd_refresh', data.refreshToken);
      localStorage.setItem('hd_user', JSON.stringify({ username: data.user.username, displayName: data.user.displayName, role: data.user.role, email: data.user.email }));
      auth.login({ username: data.user.username, displayName: data.user.displayName, role: data.user.role, email: data.user.email });
      nav('/');
    } catch { setMsg('Login failed. API offline.'); }
  };

  const doRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    const r = await apiWrite('/api/auth/register', 'POST', { username, displayName, email, password });
    setMsg(r.ok ? 'Registered. Verification email sent. Now sign in.' : 'Registration failed. Password needs 10 plus chars and unique name.');
  };

  const requestReset = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg('If the address exists, a reset link was sent. Links expire in 60 minutes. Limited to 3 requests per hour.');
    await apiWrite('/api/auth/request-reset', 'POST', { email: resetEmail });
  };

  return (
    <div>
      <h1>Account and security</h1>
      {auth.user ? (
        <div className="panel">
          <p>Signed in as {auth.user.displayName} ({auth.user.username}) with role {auth.user.role}.</p>
          <button className="secondary" onClick={auth.logout}>Sign out</button>
        </div>
      ) : (
        <div className="panel">
          <div className="row">
            <button className={mode === 'login' ? '' : 'secondary'} onClick={() => setMode('login')}>Sign in</button>
            <button className={mode === 'register' ? '' : 'secondary'} onClick={() => setMode('register')}>Register</button>
          </div>
          <div className="spacer" />
          {mode === 'login' ? (
            <form onSubmit={doLogin}>
              <label>Username<input value={username} onChange={(e) => setUsername(e.target.value)} required /></label>
              <div className="spacer" />
              <label>Password<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label>
              <div className="spacer" />
              <button type="submit">Sign in</button>
              <p className="small muted">Demo accounts: admin / Adminpass123, agent1 / Agentpass123, john.doe / Userpass123.</p>
            </form>
          ) : (
            <form onSubmit={doRegister}>
              <label>Username<input value={username} onChange={(e) => setUsername(e.target.value)} required /></label>
              <div className="spacer" />
              <label>Display name<input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required /></label>
              <div className="spacer" />
              <label>Email<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
              <div className="spacer" />
              <label>Password, 10 plus chars<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label>
              <div className="spacer" />
              <button type="submit">Register</button>
            </form>
          )}
          {msg && <p className="small">{msg}</p>}
        </div>
      )}
      <div className="panel">
        <h2>Password reset</h2>
        <p className="small muted">Reset links are single use and expire after 60 minutes. Password changes sign out all other sessions.</p>
        <form onSubmit={requestReset}>
          <label>Email<input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} type="email" required /></label>
          <div className="spacer" />
          <button type="submit">Send reset link</button>
        </form>
      </div>
    </div>
  );
}
