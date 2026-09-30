import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { API_BASE, apiGet } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { DashboardStats } from '../types';

const FALLBACK: DashboardStats = {
  openTickets: 0, inProgress: 0, resolvedToday: 0, avgResponseMinutes: 0, avgResolutionHours: 0,
  byStatus: {}, byAgent: [],
  feedback: { csatAverage: 0, csatCount: 0, npsAverage: 0, npsScore: 0, npsCount: 0 },
  activity: [],
};

export function Dashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  useEffect(() => { apiGet<DashboardStats>('/api/dashboard/stats', FALLBACK).then(setStats); }, []);

  return (
    <div>
      <h1>Support overview</h1>
      <p className="muted">Live counts, response pace, agent load, and recent activity.</p>
      <h2>Platform modules</h2>
      <p className="small muted">Scroll sideways to review each module. This strip replaces a card grid.</p>
      <div className="strip">
        <div className="strip-item"><h3>Ticket queue</h3><p>Full text search, filters by status, priority, category, agent, date. Saved views included.</p><Link to="/tickets">Open tickets</Link></div>
        <div className="strip-item"><h3>Workload</h3><p>Unassigned queue, per-agent load, one-click round-robin assign.</p><Link to="/workload">Open workload</Link></div>
        <div className="strip-item"><h3>Replies and notes</h3><p>Public replies for customers plus internal notes for agents on each ticket.</p><Link to="/tickets">Open a ticket</Link></div>
        <div className="strip-item"><h3>Files</h3><p>Attachments up to 10 MB. Images, PDF, text, office docs. Secure download.</p><Link to="/tickets">Attach files</Link></div>
        <div className="strip-item"><h3>Knowledge Base</h3><p>Published guides that reduce repeat tickets. Search, categories, feedback counts.</p><Link to="/knowledge">Open articles</Link></div>
        <div className="strip-item"><h3>SLA tracking</h3><p>Response and resolution targets per priority, breach warnings, escalation.</p><Link to="/sla">Open SLA</Link></div>
        <div className="strip-item"><h3>CSAT and NPS</h3><p>Post closure surveys with trends per agent and per category.</p><Link to="/reports">Open reports</Link></div>
      </div>
      {!stats ? <SkeletonList rows={5} /> : (
        <div>
          <div className="panel">
            <h2>Queue summary</h2>
            <table className="grid">
              <thead><tr><th>Metric</th><th>Value</th><th>Note</th></tr></thead>
              <tbody>
                <tr><td>Open tickets</td><td>{stats.openTickets}</td><td>New plus Open</td></tr>
                <tr><td>In Progress</td><td>{stats.inProgress}</td><td>Actively worked</td></tr>
                <tr><td>Resolved today</td><td>{stats.resolvedToday}</td><td>By UTC date</td></tr>
                <tr><td>Avg response</td><td>{stats.avgResponseMinutes} min</td><td>Creation to first reply</td></tr>
                <tr><td>Avg resolution</td><td>{stats.avgResolutionHours} hrs</td><td>Creation to resolved</td></tr>
              </tbody>
            </table>
            <div className="spacer" />
            <button className="secondary" onClick={() => window.open(API_BASE + '/api/tickets/export', '_blank')}>Export tickets CSV (Admin)</button>
          </div>
          <div className="panel">
            <h2>Tickets by status</h2>
            <table className="grid">
              <thead><tr><th>Status</th><th>Count</th></tr></thead>
              <tbody>{Object.entries(stats.byStatus).map(([k, v]) => <tr key={k}><td><span className="badge">{k}</span></td><td>{v}</td></tr>)}</tbody>
            </table>
          </div>
          <div className="panel">
            <h2>Agent load</h2>
            <table className="grid">
              <thead><tr><th>Agent</th><th>Assigned</th><th>Resolved</th></tr></thead>
              <tbody>{stats.byAgent.map((a) => <tr key={a.agent}><td>{a.agent}</td><td>{a.assigned}</td><td>{a.resolved}</td></tr>)}</tbody>
            </table>
          </div>
          <div className="panel plain">
            <h2>Activity feed</h2>
            {stats.activity.length === 0
              ? <p className="small muted">No recent events yet. Create or update a ticket to seed the feed.</p>
              : <ol>{stats.activity.map((a) => <li key={a.id}>{a.actor}: {a.action} {a.ticketId ? '(ticket ' + a.ticketId + ')' : ''}</li>)}</ol>}
          </div>
        </div>
      )}
    </div>
  );
}
