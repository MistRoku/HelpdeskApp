import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { API_BASE, apiGet, apiWrite, ensureCsrf, getAccess } from '../api/client';
import { SkeletonList } from '../components/ui';
import { useSignalR } from '../hooks/useSignalR';
import { useAuth } from '../components/Layout';
import type { AttachmentMeta, CannedItem, Reply, Ticket } from '../types';

export function TicketDetail() {
  const { id } = useParams();
  const auth = useAuth();
  const [ticket, setTicket] = useState<Ticket | null | undefined>(undefined);
  const [replies, setReplies] = useState<Reply[] | null>(null);
  const [files, setFiles] = useState<AttachmentMeta[]>([]);
  const [canned, setCanned] = useState<CannedItem[]>([]);
  const [body, setBody] = useState('');
  const [internal, setInternal] = useState(false);
  const [status, setStatus] = useState('');
  const [assignee, setAssignee] = useState('');
  const [msg, setMsg] = useState('');

  const load = () => {
    apiGet<Ticket | null>('/api/tickets/' + id, null).then((t) => { setTicket(t); if (t) setStatus(t.status); });
    apiGet<Reply[]>('/api/tickets/' + id + '/replies', []).then(setReplies);
    apiGet<AttachmentMeta[]>('/api/tickets/' + id + '/attachments', []).then(setFiles);
  };
  useEffect(load, [id]);
  useEffect(() => { apiGet<CannedItem[]>('/api/canned', []).then(setCanned); }, []);
  useSignalR({ ticketId: Number(id), onEvent: load });

  // Canned shortcut: typing // shows matching snippets, click inserts.
  const cannedMatches = body.startsWith('//')
    ? canned.filter((c) => c.title.toLowerCase().includes(body.slice(2).toLowerCase())).slice(0, 3)
    : [];

  const sendReply = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!body.trim()) return;
    const r = await apiWrite('/api/tickets/' + id + '/replies', 'POST', { body, isInternal: internal });
    setMsg(r.ok ? 'Reply saved.' : 'Reply failed. Sign in as agent for internal notes.');
    setBody(''); load();
  };
  const changeStatus = async () => {
    const r = await apiWrite('/api/tickets/' + id + '/status', 'PUT', { status });
    setMsg(r.ok ? 'Status updated to ' + status + '.' : 'Status change refused. Check allowed flow and role.');
    load();
  };
  const assign = async () => {
    const r = await apiWrite('/api/tickets/' + id + '/assign', 'PUT', { assignee });
    setMsg(r.ok ? 'Assigned to ' + assignee + '.' : 'Assign needs Agent role.');
    load();
  };
  const reopen = async () => {
    const r = await apiWrite('/api/tickets/' + id + '/reopen', 'POST', {});
    setMsg(r.ok ? 'Ticket reopened.' : 'Reopen allowed within 7 days of closure only.');
    load();
  };
  const upload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    if (f.size > 10 * 1024 * 1024) { setMsg('File exceeds 10 MB limit.'); return; }
    const fd = new FormData();
    fd.append('file', f);
    try {
      const token = await ensureCsrf();
      const res = await fetch(API_BASE + '/api/tickets/' + id + '/attachments', {
        method: 'POST', credentials: 'include',
        headers: { ...(getAccess() ? { Authorization: 'Bearer ' + getAccess() } : {}), ...(token ? { 'X-CSRF-TOKEN': token } : {}) },
        body: fd,
      });
      setMsg(res.ok ? 'File uploaded.' : 'Upload failed. Check type and size.');
    } catch { setMsg('Upload failed.'); }
    load();
  };

  if (ticket === undefined) return <div><h1>Ticket</h1><SkeletonList rows={4} /></div>;
  if (ticket === null) return <div><h1>Ticket not found</h1><p>No ticket matches this id.</p></div>;
  const isStaff = auth.user?.role === 'Agent' || auth.user?.role === 'Admin';

  return (
    <div>
      <h1>{ticket.title}</h1>
      <p className="muted">{ticket.ticketNumber} | {ticket.category} | Channel: {ticket.channel} | Filed by {ticket.userName} | Updated: {ticket.lastActivityAt}</p>
      <div className="panel">
        <span className="badge">{ticket.status}</span>
        <span className="badge">{ticket.priority}</span>
        {ticket.slaBreached ? <span className="badge urgent">SLA breached</span> : <span className="badge ok">SLA on track</span>}
        <div className="spacer" />
        <p>{ticket.description}</p>
        {ticket.aiCategory && <p className="small">AI suggestion: {ticket.aiCategory} / {ticket.aiPriority} ({Math.round((ticket.aiConfidence || 0) * 100)} percent confidence).</p>}
        <p className="small muted">Assignee: {ticket.assignedTo || 'unassigned'} | Response due: {ticket.responseDueAt || 'per policy'} | Resolution due: {ticket.resolutionDueAt || 'per policy'}</p>
      </div>
      <div className="panel">
        <h2>Conversation history</h2>
        {!replies ? <SkeletonList rows={3} /> : replies.length === 0 ? <p className="small muted">No replies yet.</p> : replies.map((r) => (
          <div key={r.id} className="panel plain">
            <div className="small muted">{r.author} | {r.createdAt} {r.isInternal ? '| Internal note (agents only)' : '| Public reply'}</div>
            <p>{r.body}</p>
          </div>
        ))}
        <form onSubmit={sendReply}>
          <label>Reply<textarea value={body} onChange={(e) => setBody(e.target.value)} rows={3} required /></label>
          {cannedMatches.length > 0 && (
            <div className="panel plain">
              <p className="small">Canned matches for {body}:</p>
              {cannedMatches.map((c) => <div key={c.id} className="row"><button type="button" className="secondary" onClick={() => setBody(c.content)}>{c.title}</button></div>)}
            </div>
          )}
          {isStaff && <label className="small"><input type="checkbox" checked={internal} onChange={(e) => setInternal(e.target.checked)} /> Internal note (hidden from customer)</label>}
          <div className="spacer" />
          <button type="submit">Add reply</button>
          <p className="small muted">Tip: type // to filter canned replies.</p>
        </form>
      </div>
      <div className="panel">
        <h2>Status and assignment</h2>
        <p className="small muted">Allowed flow: New to Open to In Progress to Pending to Resolved to Closed. Customers may close own tickets and reopen within 7 days.</p>
        <div className="row">
          <label>Status<select value={status} onChange={(e) => setStatus(e.target.value)}>{['New', 'Open', 'InProgress', 'Pending', 'Resolved', 'Closed'].map((s) => <option key={s} value={s}>{s}</option>)}</select></label>
          <button className="secondary" onClick={changeStatus}>Update status</button>
          <button className="secondary" onClick={reopen}>Reopen</button>
        </div>
        <div className="spacer" />
        <div className="row">
          <label>Assign to<input value={assignee} onChange={(e) => setAssignee(e.target.value)} placeholder="agent username" /></label>
          <button className="secondary" onClick={assign}>Assign (Agent)</button>
        </div>
      </div>
      <div className="panel">
        <h2>Attachments (max 10 MB)</h2>
        <p className="small muted">Allowed: images, PDF, text, office documents.</p>
        <ul>{files.map((f) => <li key={f.id}><a href={API_BASE + '/api/tickets/attachments/' + f.id + '/download'}>{f.fileName}</a> ({Math.round(f.sizeBytes / 1024)} KB)</li>)}</ul>
        <input type="file" onChange={upload} />
      </div>
      {msg && <p className="small">{msg}</p>}
      <Link to="/tickets">Back to list</Link>
    </div>
  );
}
