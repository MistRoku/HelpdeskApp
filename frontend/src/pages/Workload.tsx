import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiGet, apiWrite } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { Ticket } from '../types';

type Workload = {
  unassigned: Ticket[];
  perAgent: { agent: string; open: number; urgent: number }[];
};

export function Workload() {
  const [data, setData] = useState<Workload | null>(null);
  const load = () => apiGet<Workload>('/api/workload', { unassigned: [], perAgent: [] }).then(setData);
  useEffect(load, []);

  const autoAssign = async (id: number) => {
    await apiWrite('/api/workload/auto-assign/' + id, 'POST', {});
    load();
  };

  return (
    <div>
      <h1>Agent workload</h1>
      <p className="muted">Unassigned queue plus per-agent load. Auto assign uses round-robin to the lightest agent.</p>
      {!data ? <SkeletonList rows={4} /> : (
        <div>
          <div className="panel">
            <h2>Per-agent load</h2>
            <table className="grid">
              <thead><tr><th>Agent</th><th>Open</th><th>Urgent</th></tr></thead>
              <tbody>{data.perAgent.map((a) => <tr key={a.agent}><td>{a.agent}</td><td>{a.open}</td><td>{a.urgent}</td></tr>)}</tbody>
            </table>
          </div>
          <div className="panel">
            <h2>Unassigned queue</h2>
            {data.unassigned.length === 0 ? <p className="small muted">Queue is clear.</p> : (
              <table className="grid">
                <thead><tr><th>Number</th><th>Title</th><th>Action</th></tr></thead>
                <tbody>{data.unassigned.map((t) => (
                  <tr key={t.id}>
                    <td className="small">{t.ticketNumber}</td>
                    <td><Link to={'/tickets/' + t.id}>{t.title}</Link></td>
                    <td><button className="secondary" onClick={() => autoAssign(t.id)}>Auto assign</button></td>
                  </tr>
                ))}</tbody>
              </table>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
