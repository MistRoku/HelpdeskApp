import React, { useEffect, useState } from 'react';
import { apiGet, apiWrite } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { Ticket } from '../types';

type Stats = { csatAverage: number; csatCount: number; npsAverage: number; npsScore: number; npsCount: number };

export function Reports() {
  const [stats, setStats] = useState<Stats | null>(null);
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [ticketId, setTicketId] = useState('1');
  const [score, setScore] = useState('5');
  const [msg, setMsg] = useState('');

  useEffect(() => {
    apiGet('/api/feedback/stats', { csatAverage: 0, csatCount: 0, npsAverage: 0, npsScore: 0, npsCount: 0 }).then(setStats);
    apiGet<Ticket[]>('/api/tickets', []).then(setTickets);
  }, []);

  const byCategory = tickets.reduce<Record<string, { open: number; closed: number }>>((acc, t) => {
    acc[t.category] = acc[t.category] || { open: 0, closed: 0 };
    if (t.status === 'Closed' || t.status === 'Resolved') acc[t.category].closed++;
    else acc[t.category].open++;
    return acc;
  }, {});

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const r = await apiWrite('/api/feedback/submit', 'POST', { ticketId: Number(ticketId), type: 'CSAT', score: Number(score) });
    setMsg(r.ok ? 'Thanks. Survey recorded.' : 'Survey failed.');
  };

  return (
    <div>
      <h1>Reports</h1>
      <p className="muted">CSAT uses a 1 to 5 scale after closure. NPS uses a 0 to 10 scale monthly.</p>
      <div className="panel">
        {!stats ? <SkeletonList rows={3} /> : (
          <table className="grid">
            <thead><tr><th>Metric</th><th>Score</th><th>Responses</th></tr></thead>
            <tbody>
              <tr><td>CSAT average</td><td>{stats.csatAverage} / 5</td><td>{stats.csatCount}</td></tr>
              <tr><td>NPS average</td><td>{stats.npsAverage} / 10</td><td>{stats.npsCount}</td></tr>
              <tr><td>NPS score</td><td>{stats.npsScore}</td><td>Promoters minus detractors</td></tr>
            </tbody>
          </table>
        )}
      </div>
      <div className="panel">
        <h2>Volume by category</h2>
        <table className="grid">
          <thead><tr><th>Category</th><th>Open</th><th>Closed</th></tr></thead>
          <tbody>{Object.entries(byCategory).map(([k, v]) => <tr key={k}><td>{k}</td><td>{v.open}</td><td>{v.closed}</td></tr>)}</tbody>
        </table>
      </div>
      <div className="panel plain">
        <h2>Submit CSAT survey</h2>
        <form onSubmit={submit}>
          <div className="row">
            <label>Ticket<input value={ticketId} onChange={(e) => setTicketId(e.target.value)} /></label>
            <label>Score 1 to 5<input value={score} onChange={(e) => setScore(e.target.value)} /></label>
            <button type="submit">Send</button>
          </div>
        </form>
        {msg && <p className="small">{msg}</p>}
      </div>
    </div>
  );
}
