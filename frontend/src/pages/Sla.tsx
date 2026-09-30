import React, { useEffect, useState } from 'react';
import { apiGet } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { SlaPolicy, Ticket } from '../types';

export function Sla() {
  const [policies, setPolicies] = useState<SlaPolicy[] | null>(null);
  const [tickets, setTickets] = useState<Ticket[] | null>(null);

  useEffect(() => {
    apiGet<SlaPolicy[]>('/api/sla/policies', []).then(setPolicies);
    apiGet<Ticket[]>('/api/tickets', []).then(setTickets);
  }, []);

  return (
    <div>
      <h1>SLA management</h1>
      <p className="muted">Targets use business hours Mon to Fri, 08:00 to 17:00 SAST. A 5 minute sweeper flags breaches, warns when under 30 minutes remain, and escalates to the team lead.</p>
      <div className="panel">
        <h2>Policies</h2>
        {!policies ? <SkeletonList rows={4} /> : (
          <table className="grid">
            <thead><tr><th>Priority</th><th>Response target</th><th>Resolution target</th></tr></thead>
            <tbody>{policies.map((p) => <tr key={p.id}><td><span className="badge">{p.priority}</span></td><td>{p.responseMinutes} min</td><td>{p.resolutionMinutes} min</td></tr>)}</tbody>
          </table>
        )}
      </div>
      <div className="panel">
        <h2>Breached tickets</h2>
        {!tickets ? <SkeletonList rows={3} /> : (
          <table className="grid">
            <thead><tr><th>Number</th><th>Title</th><th>State</th></tr></thead>
            <tbody>{tickets.filter((t) => t.slaBreached).map((t) => (
              <tr key={t.id}><td>{t.ticketNumber}</td><td>{t.title}</td><td><span className="sla-breached">Breached, escalated</span></td></tr>
            ))}</tbody>
          </table>
        )}
      </div>
    </div>
  );
}
