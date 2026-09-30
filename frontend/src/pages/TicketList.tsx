import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { apiWrite } from '../api/client';
import { SkeletonList, StatusBadge } from '../components/ui';
import { useTickets } from '../hooks/useTickets';
import { useSignalR } from '../hooks/useSignalR';
import { useAuth } from '../components/Layout';

const STATUSES = ['All', 'New', 'Open', 'InProgress', 'Pending', 'Resolved', 'Closed'];
const PRIORITIES = ['All', 'Low', 'Medium', 'High', 'Urgent'];
const CATEGORIES = ['All', 'Network', 'Software', 'Hardware', 'Access', 'Billing', 'Other', 'General'];

export function TicketList() {
  const auth = useAuth();
  const { tickets, filters, update, reload } = useTickets();
  const [title, setTitle] = useState('');
  const [desc, setDesc] = useState('');
  const [prio, setPrio] = useState('Medium');
  const [cat, setCat] = useState('General');
  const [suggest, setSuggest] = useState<{ category: string; priority: string; confidence: number; articles: { id: number; title: string }[] } | null>(null);

  useSignalR({ onEvent: reload });

  const preview = async () => {
    if (title.trim().length < 5) { setSuggest(null); return; }
    const r = await apiWrite('/api/ai/suggest-for-ticket', 'POST', { subject: title, description: desc || title });
    if (r.ok) setSuggest(r.data as typeof suggest);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !desc.trim()) return;
    await apiWrite('/api/tickets', 'POST', {
      title, description: desc, status: 'New', priority: prio, category: cat,
      channel: 'Web', userName: auth.user?.username || 'web-user',
    });
    setTitle(''); setDesc(''); setSuggest(null); reload();
  };

  return (
    <div>
      <h1>Tickets</h1>
      <div className="panel">
        <h2>Create ticket</h2>
        <form onSubmit={submit}>
          <label>Subject<input value={title} onChange={(e) => setTitle(e.target.value)} onBlur={preview} maxLength={200} required /></label>
          <div className="spacer" />
          <label>Description<textarea value={desc} onChange={(e) => setDesc(e.target.value)} rows={3} required /></label>
          <div className="spacer" />
          <div className="row">
            <label>Priority<select value={prio} onChange={(e) => setPrio(e.target.value)}>{['Low', 'Medium', 'High', 'Urgent'].map((p) => <option key={p} value={p}>{p}</option>)}</select></label>
            <label>Category<select value={cat} onChange={(e) => setCat(e.target.value)}>{['Network', 'Software', 'Hardware', 'Access', 'Billing', 'Other', 'General'].map((p) => <option key={p} value={p}>{p}</option>)}</select></label>
            <button type="button" className="secondary" onClick={preview}>Suggest articles</button>
            <button type="submit">Submit ticket</button>
          </div>
        </form>
        {suggest && (
          <div className="panel plain">
            <p className="small">Triage guess: {suggest.category} / {suggest.priority} ({Math.round(suggest.confidence * 100)} percent). Read these first:</p>
            <ol>{suggest.articles.map((a) => <li key={a.id}><Link to="/knowledge">{a.title}</Link></li>)}</ol>
          </div>
        )}
      </div>
      <div className="panel">
        <h2>Search and filter</h2>
        <div className="row">
          <label>Search<input placeholder="Number, subject, text" value={filters.q} onChange={(e) => update({ q: e.target.value })} /></label>
          <label>Status<select value={filters.status} onChange={(e) => update({ status: e.target.value })}>{STATUSES.map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Priority<select value={filters.priority} onChange={(e) => update({ priority: e.target.value })}>{PRIORITIES.map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Category<select value={filters.category} onChange={(e) => update({ category: e.target.value })}>{CATEGORIES.map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Saved view<select value={filters.view} onChange={(e) => update({ view: e.target.value })}>{['All', 'Mine', 'Unassigned'].map((f) => <option key={f} value={f}>{f === 'Mine' ? 'My Open Tickets' : f}</option>)}</select></label>
          <button onClick={reload}>Apply</button>
        </div>
      </div>
      {!tickets ? <SkeletonList rows={5} /> : (
        <table className="grid">
          <thead><tr><th>Number</th><th>Title</th><th>Status</th><th>Priority</th><th>Assignee</th></tr></thead>
          <tbody>
            {tickets.map((t) => (
              <tr key={t.id}>
                <td className="small">{t.ticketNumber || t.id}</td>
                <td><Link to={'/tickets/' + t.id}>{t.title}</Link><div className="small muted">{t.category} | {t.channel} | {t.userName}</div></td>
                <td><StatusBadge value={t.status} /></td>
                <td><StatusBadge value={t.priority} /></td>
                <td className="small">{t.assignedTo || 'unassigned'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
