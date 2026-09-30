import React, { useEffect, useState } from 'react';
import { apiGet, apiWrite } from '../api/client';
import { SkeletonList } from '../components/ui';

type Row = { username: string; displayName: string; role: string; email: string };
type Audit = { id: number; ticketId?: number; actor: string; action: string; createdAt: string };

export function UsersPage() {
  const [users, setUsers] = useState<Row[] | null>(null);
  const [audit, setAudit] = useState<Audit[]>([]);
  const load = () => {
    apiGet<Row[]>('/api/users', []).then(setUsers);
    apiGet<Audit[]>('/api/audit', []).then(setAudit);
  };
  useEffect(load, []);

  const setRole = async (u: string, role: string) => {
    await apiWrite('/api/users/' + u + '/role', 'PUT', { role });
    load();
  };

  return (
    <div>
      <h1>Users (Admin)</h1>
      <p className="muted">Manage roles. User sees own tickets, Agent sees queue, Admin sees all plus settings. Identity is server derived, never trusted from the client.</p>
      {!users ? <SkeletonList rows={4} /> : (
        <table className="grid">
          <thead><tr><th>Username</th><th>Name</th><th>Role</th><th>Change</th></tr></thead>
          <tbody>{users.map((u) => (
            <tr key={u.username}>
              <td>{u.username}</td><td>{u.displayName}</td>
              <td><span className="badge">{u.role}</span></td>
              <td><div className="row">{['User', 'Agent', 'Admin'].map((r) => <button key={r} className="secondary" onClick={() => setRole(u.username, r)}>{r}</button>)}</div></td>
            </tr>
          ))}</tbody>
        </table>
      )}
      <div className="panel">
        <h2>Admin audit log</h2>
        {audit.length === 0 ? <p className="small muted">No admin events recorded yet.</p> : (
          <ol>{audit.slice(0, 30).map((a) => <li key={a.id}>{a.actor}: {a.action} {a.ticketId ? '(ticket ' + a.ticketId + ')' : ''}</li>)}</ol>
        )}
      </div>
    </div>
  );
}
