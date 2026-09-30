import React, { createContext, useContext, useEffect, useState } from 'react';
import './App.css';
import { BrowserRouter, Routes, Route, Link, useLocation, useParams, useNavigate } from 'react-router-dom';

/* API layer with JWT Bearer and CSRF token support. */
const API_BASE = process.env.REACT_APP_API_BASE || 'https://localhost:7049';
let csrfToken: string | null = null;

function getAccess(): string | null { return localStorage.getItem('hd_access'); }

async function ensureCsrf(): Promise<string | null> {
  if (csrfToken) return csrfToken;
  try {
    const res = await fetch(API_BASE + '/api/antiforgery/token', { credentials: 'include' });
    if (!res.ok) return null;
    const data = await res.json();
    csrfToken = data.token;
    return csrfToken;
  } catch { return null; }
}

function headers(json = true): Record<string, string> {
  const h: Record<string, string> = {};
  if (json) h['Content-Type'] = 'application/json';
  const a = getAccess();
  if (a) h['Authorization'] = 'Bearer ' + a;
  const u = localStorage.getItem('hd_user');
  if (u) { try { const p = JSON.parse(u); h['X-User'] = p.username; h['X-Role'] = p.role; h['X-Allow-Write'] = '1'; } catch { } }
  return h;
}

async function apiGet<T>(path: string, fallback: T): Promise<T> {
  try {
    const res = await fetch(API_BASE + path, { credentials: 'include', headers: headers(false) });
    if (!res.ok) throw new Error('bad status');
    return (await res.json()) as T;
  } catch { return fallback; }
}

async function apiWrite(path: string, method: string, body?: unknown): Promise<{ ok: boolean; data?: unknown; status: number }> {
  try {
    const token = await ensureCsrf();
    const res = await fetch(API_BASE + path, {
      method, credentials: 'include',
      headers: { ...headers(), ...(token ? { 'X-CSRF-TOKEN': token } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    let data: unknown = null;
    try { data = await res.json(); } catch { }
    return { ok: res.ok, data, status: res.status };
  } catch { return { ok: false, status: 0 }; }
}

/* Types */
type Ticket = {
  id: number; ticketNumber: string; title: string; description: string; status: string; priority: string;
  category: string; channel: string; userName: string; assignedTo?: string;
  createdAt: string; updatedAt?: string; lastActivityAt: string; firstResponseAt?: string; resolvedAt?: string; closedAt?: string;
  responseDueAt?: string; resolutionDueAt?: string;
  slaBreached?: boolean; aiCategory?: string; aiPriority?: string; aiConfidence?: number;
};
type Reply = { id: number; ticketId: number; author: string; body: string; isInternal: boolean; createdAt: string };
type Article = { id: number; title: string; content: string; category: string; tags: string; status: string; author: string; viewCount: number; helpfulCount: number; unhelpfulCount: number };
type SlaPolicy = { id: number; name: string; priority: string; responseMinutes: number; resolutionMinutes: number };
type Stats = {
  openTickets: number; inProgress: number; resolvedToday: number; avgResponseMinutes: number; avgResolutionHours: number;
  byStatus: Record<string, number>; byAgent: { agent: string; assigned: number; resolved: number }[];
  feedback: { csatAverage: number; csatCount: number; npsAverage: number; npsScore: number; npsCount: number };
  activity: { id: number; ticketId?: number; actor: string; action: string; createdAt: string }[];
};
type SessionUser = { username: string; displayName: string; role: string; email: string };

const SEED_TICKETS: Ticket[] = [
  { id: 1, ticketNumber: 'TKT-20260101-1001', title: 'Login fails after password change', description: 'User cannot sign in with new password.', status: 'Open', priority: 'High', category: 'Access', channel: 'Web', userName: 'john.doe', createdAt: new Date().toISOString(), lastActivityAt: new Date().toISOString(), aiCategory: 'Access', aiPriority: 'High', aiConfidence: 0.88 },
  { id: 2, ticketNumber: 'TKT-20260102-1002', title: 'Warehouse printer offline', description: 'HP printer shows offline since morning shift.', status: 'InProgress', priority: 'Medium', category: 'Hardware', channel: 'Email', userName: 'jane.smith', assignedTo: 'agent1', createdAt: new Date().toISOString(), lastActivityAt: new Date().toISOString(), slaBreached: true },
];
const SEED_ARTICLES: Article[] = [
  { id: 1, title: 'How to reset your password', content: 'Open the sign in page, select Forgot password, then follow the link within 60 minutes. Links expire after one use.', category: 'Access', tags: 'password, login', status: 'Published', author: 'support', viewCount: 412, helpfulCount: 96, unhelpfulCount: 4 },
  { id: 2, title: 'Network troubleshooting checklist', content: 'Check cable or WiFi, restart router, test VPN, record exact error text before opening a ticket.', category: 'Network', tags: 'wifi, vpn', status: 'Published', author: 'support', viewCount: 287, helpfulCount: 61, unhelpfulCount: 9 },
];
const SEED_SLA: SlaPolicy[] = [
  { id: 1, name: 'Urgent', priority: 'Urgent', responseMinutes: 15, resolutionMinutes: 240 },
  { id: 2, name: 'High', priority: 'High', responseMinutes: 60, resolutionMinutes: 1440 },
  { id: 3, name: 'Medium', priority: 'Medium', responseMinutes: 240, resolutionMinutes: 4320 },
  { id: 4, name: 'Low', priority: 'Low', responseMinutes: 1440, resolutionMinutes: 10080 },
];

/* Auth context */
const AuthCtx = createContext<{ user: SessionUser | null; login: (u: SessionUser, access: string, refresh: string) => void; logout: () => void }>({ user: null, login: () => { }, logout: () => { } });
function useAuth() { return useContext(AuthCtx); }

function Mark() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" aria-hidden="true">
      <rect x="1" y="1" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" />
      <rect x="6" y="6" width="6" height="6" fill="currentColor" />
    </svg>
  );
}

function SkeletonList({ rows = 4 }: { rows?: number }) {
  return (
    <div className="loading" aria-label="Loading">
      {Array.from({ length: rows }).map((_, i) => <div key={i} className="skeleton row" />)}
    </div>
  );
}

function NotificationsBell() {
  const [items, setItems] = useState<{ id: number; title: string; body: string; isRead: boolean; ticketId?: number }[]>([]);
  useEffect(() => {
    let live = true;
    const poll = () => apiGet('/api/notifications', []).then((d) => { if (live) setItems(d as typeof items); });
    poll();
    const t = setInterval(poll, 15000);
    return () => { live = false; clearInterval(t); };
  }, []);
  const unread = items.filter((i) => !i.isRead).length;
  return (
    <span className="small">
      <Link to="/notifications">Notifications ({unread} unread)</Link>
    </span>
  );
}

function Layout({ children }: { children: React.ReactNode }) {
  const loc = useLocation();
  const auth = useAuth();
  const links: [string, string][] = [
    ['/', 'Dashboard'], ['/tickets', 'Tickets'], ['/knowledge', 'Knowledge Base'],
    ['/sla', 'SLA'], ['/reports', 'Reports'], ['/canned', 'Canned Replies'],
    ['/notifications', 'Notifications'],
  ];
  if (auth.user?.role === 'Admin') links.push(['/users', 'Users']);
  links.push(['/account', auth.user ? 'Account (' + auth.user.username + ')' : 'Account']);
  return (
    <div className="shell">
      <nav className="sidebar">
        <h2><Mark /> Helpdesk Console</h2>
        <p>Role: {auth.user ? auth.user.role : 'signed out'}. Tickets use numbers like TKT-YYYYMMDD-XXXX.</p>
        {links.map(([to, label]) => <Link key={to} className={'navlink' + (loc.pathname === to ? ' active' : '')} to={to}>{label}</Link>)}
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
            <NotificationsBell />
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

function Dashboard() {
  const [stats, setStats] = useState<Stats | null>(null);
  useEffect(() => {
    apiGet<Stats | null>('/api/dashboard/stats', null).then((s) => {
      if (s) setStats(s);
      else setStats({
        openTickets: 2, inProgress: 1, resolvedToday: 0, avgResponseMinutes: 42, avgResolutionHours: 5.2,
        byStatus: { New: 0, Open: 1, InProgress: 1 }, byAgent: [{ agent: 'agent1', assigned: 1, resolved: 0 }],
        feedback: { csatAverage: 4.5, csatCount: 12, npsAverage: 8.4, npsScore: 42, npsCount: 9 }, activity: [],
      });
    });
  }, []);
  const exportCsv = () => { window.open(API_BASE + '/api/tickets/export', '_blank'); };
  return (
    <div>
      <h1>Support overview</h1>
      <p className="muted">Live counts, response pace, agent load, and recent activity.</p>
      <h2>Platform modules</h2>
      <p className="small muted">Scroll sideways to review each module. This strip replaces a card grid.</p>
      <div className="strip">
        <div className="strip-item"><h3>Ticket queue</h3><p>Full text search, filters by status, priority, category, agent, date. Saved views included.</p><Link to="/tickets">Open tickets</Link></div>
        <div className="strip-item"><h3>Replies and notes</h3><p>Public replies for customers plus internal notes for agents on each ticket.</p><Link to="/tickets">Open a ticket</Link></div>
        <div className="strip-item"><h3>Files</h3><p>Attachments up to 10 MB. Images, PDF, text, office docs. Secure download.</p><Link to="/tickets">Attach files</Link></div>
        <div className="strip-item"><h3>Knowledge Base</h3><p>Published guides that reduce repeat tickets. Search, categories, feedback counts.</p><Link to="/knowledge">Open articles</Link></div>
        <div className="strip-item"><h3>SLA tracking</h3><p>Response and resolution targets per priority, with breach list and escalation path.</p><Link to="/sla">Open SLA</Link></div>
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
            <button className="secondary" onClick={exportCsv}>Export tickets CSV (Admin)</button>
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
            {stats.activity.length === 0 ? <p className="small muted">No recent events in demo data.</p> : (
              <ol>{stats.activity.map((a) => <li key={a.id}>{a.actor}: {a.action} {a.ticketId ? '(ticket ' + a.ticketId + ')' : ''}</li>)}</ol>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function TicketList() {
  const auth = useAuth();
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  const [q, setQ] = useState('');
  const [status, setStatus] = useState('All');
  const [priority, setPriority] = useState('All');
  const [category, setCategory] = useState('All');
  const [view, setView] = useState('All');
  const [title, setTitle] = useState('');
  const [desc, setDesc] = useState('');
  const [prio, setPrio] = useState('Medium');
  const [cat, setCat] = useState('General');

  const load = () => {
    setTickets(null);
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (status !== 'All') params.set('status', status);
    if (priority !== 'All') params.set('priority', priority);
    if (category !== 'All') params.set('category', category);
    if (view === 'Mine' && auth.user) params.set('assignee', auth.user.username);
    if (view === 'Unassigned') params.set('assignee', '');
    apiGet<Ticket[]>('/api/tickets?' + params.toString(), SEED_TICKETS).then((t) => {
      let list = t;
      if (view === 'Mine' && auth.user) list = list.filter((x) => x.assignedTo === auth.user!.username);
      setTickets(list);
    });
  };
  useEffect(load, []);
  useEffect(() => {
    const saved = localStorage.getItem('hd_view');
    if (saved) setView(saved);
  }, []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !desc.trim()) return;
    await apiWrite('/api/tickets', 'POST', { title, description: desc, status: 'New', priority: prio, category: cat, channel: 'Web', userName: auth.user?.username || 'web-user' });
    setTitle(''); setDesc(''); load();
  };

  return (
    <div>
      <h1>Tickets</h1>
      <div className="panel">
        <h2>Create ticket</h2>
        <form onSubmit={submit}>
          <label>Subject<input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required /></label>
          <div className="spacer" />
          <label>Description<textarea value={desc} onChange={(e) => setDesc(e.target.value)} rows={3} required /></label>
          <div className="spacer" />
          <div className="row">
            <label>Priority<select value={prio} onChange={(e) => setPrio(e.target.value)}>{['Low', 'Medium', 'High', 'Urgent'].map((p) => <option key={p} value={p}>{p}</option>)}</select></label>
            <label>Category<select value={cat} onChange={(e) => setCat(e.target.value)}>{['Network', 'Software', 'Hardware', 'Access', 'Billing', 'Other', 'General'].map((p) => <option key={p} value={p}>{p}</option>)}</select></label>
            <button type="submit">Submit ticket</button>
          </div>
        </form>
      </div>
      <div className="panel">
        <h2>Search and filter</h2>
        <div className="row">
          <label>Search<input placeholder="Number, subject, text" value={q} onChange={(e) => setQ(e.target.value)} /></label>
          <label>Status<select value={status} onChange={(e) => setStatus(e.target.value)}>{['All', 'New', 'Open', 'InProgress', 'Pending', 'Resolved', 'Closed'].map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Priority<select value={priority} onChange={(e) => setPriority(e.target.value)}>{['All', 'Low', 'Medium', 'High', 'Urgent'].map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Category<select value={category} onChange={(e) => setCategory(e.target.value)}>{['All', 'Network', 'Software', 'Hardware', 'Access', 'Billing', 'Other', 'General'].map((f) => <option key={f} value={f}>{f}</option>)}</select></label>
          <label>Saved view<select value={view} onChange={(e) => { setView(e.target.value); localStorage.setItem('hd_view', e.target.value); }}>{['All', 'Mine', 'Unassigned'].map((f) => <option key={f} value={f}>{f === 'Mine' ? 'My Open Tickets' : f}</option>)}</select></label>
          <button onClick={load}>Apply</button>
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
                <td><span className="badge">{t.status}</span></td>
                <td><span className={'badge' + (t.priority === 'Urgent' ? ' urgent' : t.priority === 'High' ? ' high' : '')}>{t.priority}</span></td>
                <td className="small">{t.assignedTo || 'unassigned'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function TicketDetail() {
  const { id } = useParams();
  const auth = useAuth();
  const [ticket, setTicket] = useState<Ticket | null | undefined>(undefined);
  const [replies, setReplies] = useState<Reply[] | null>(null);
  const [files, setFiles] = useState<{ id: number; fileName: string; sizeBytes: number }[]>([]);
  const [body, setBody] = useState('');
  const [internal, setInternal] = useState(false);
  const [status, setStatus] = useState('');
  const [assignee, setAssignee] = useState('');
  const [msg, setMsg] = useState('');

  const load = () => {
    apiGet<Ticket | null>('/api/tickets/' + id, SEED_TICKETS.find((t) => String(t.id) === String(id)) || null).then((t) => { setTicket(t); if (t) setStatus(t.status); });
    apiGet<Reply[]>('/api/tickets/' + id + '/replies', []).then(setReplies);
    apiGet('/api/tickets/' + id + '/attachments', []).then((d) => setFiles(d as typeof files));
  };
  useEffect(load, [id]);

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
          {isStaff && <label className="small"><input type="checkbox" checked={internal} onChange={(e) => setInternal(e.target.checked)} /> Internal note (hidden from customer)</label>}
          <div className="spacer" />
          <button type="submit">Add reply</button>
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

function Knowledge() {
  const [articles, setArticles] = useState<Article[] | null>(null);
  const [q, setQ] = useState('');
  const [cat, setCat] = useState('All');
  useEffect(() => {
    setArticles(null);
    const t = setTimeout(() => {
      apiGet<Article[]>('/api/knowledgebase/articles', SEED_ARTICLES).then((a) => {
        setArticles(a.filter((x) => (cat === 'All' || x.category === cat) && (!q || (x.title + ' ' + x.content + ' ' + x.tags).toLowerCase().includes(q.toLowerCase()))));
      });
    }, 300);
    return () => clearTimeout(t);
  }, [q, cat]);
  return (
    <div>
      <h1>Knowledge Base</h1>
      <p className="muted">Search guides first. Each view and vote improves ranking.</p>
      <div className="kb-layout">
        <div className="kb-side">
          <div className="panel">
            <h3>Categories</h3>
            {['All', 'Access', 'Network', 'Hardware', 'Billing', 'Software', 'General'].map((c) => <button key={c} className="category-btn secondary" onClick={() => setCat(c)}>{c}</button>)}
          </div>
        </div>
        <div className="kb-main">
          <input placeholder="Search articles by keyword" value={q} onChange={(e) => setQ(e.target.value)} />
          <div className="spacer" />
          {!articles ? <SkeletonList rows={4} /> : articles.map((a) => (
            <div key={a.id} className="panel">
              <h3>{a.title}</h3>
              <p className="small muted">{a.category} | {a.viewCount} views | Helpful: {a.helpfulCount} | Not helpful: {a.unhelpfulCount}</p>
              <p>{a.content}</p>
              <div className="row">
                <button className="secondary" onClick={() => apiWrite('/api/knowledgebase/articles/' + a.id + '/feedback', 'POST', { helpful: true })}>Helpful</button>
                <button className="secondary" onClick={() => apiWrite('/api/knowledgebase/articles/' + a.id + '/feedback', 'POST', { helpful: false })}>Not helpful</button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function Sla() {
  const [policies, setPolicies] = useState<SlaPolicy[] | null>(null);
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  useEffect(() => {
    apiGet<SlaPolicy[]>('/api/sla/policies', SEED_SLA).then(setPolicies);
    apiGet<Ticket[]>('/api/tickets', SEED_TICKETS).then(setTickets);
  }, []);
  return (
    <div>
      <h1>SLA management</h1>
      <p className="muted">Targets use business hours Mon to Fri, 08:00 to 17:00 SAST. Breaches escalate to the team lead.</p>
      <div className="panel">
        <h2>Policies</h2>
        {!policies ? <SkeletonList rows={4} /> : (
          <table className="grid"><thead><tr><th>Priority</th><th>Response target</th><th>Resolution target</th></tr></thead>
          <tbody>{policies.map((p) => <tr key={p.id}><td><span className="badge">{p.priority}</span></td><td>{p.responseMinutes} min</td><td>{p.resolutionMinutes} min</td></tr>)}</tbody></table>
        )}
      </div>
      <div className="panel">
        <h2>Breached tickets</h2>
        {!tickets ? <SkeletonList rows={3} /> : (
          <table className="grid"><thead><tr><th>Number</th><th>Title</th><th>State</th></tr></thead>
          <tbody>{tickets.filter((t) => t.slaBreached).map((t) => <tr key={t.id}><td>{t.ticketNumber}</td><td>{t.title}</td><td><span className="sla-breached">Breached, escalated</span></td></tr>)}</tbody></table>
        )}
      </div>
    </div>
  );
}

function Reports() {
  const [stats, setStats] = useState<{ csatAverage: number; csatCount: number; npsAverage: number; npsScore: number; npsCount: number } | null>(null);
  useEffect(() => { apiGet('/api/feedback/stats', { csatAverage: 4.5, csatCount: 12, npsAverage: 8.4, npsScore: 42, npsCount: 9 }).then(setStats); }, []);
  return (
    <div>
      <h1>Reports</h1>
      <p className="muted">CSAT uses a 1 to 5 scale after closure. NPS uses a 0 to 10 scale monthly. Close a ticket to trigger a survey.</p>
      <div className="panel">
        {!stats ? <SkeletonList rows={3} /> : (
          <table className="grid"><thead><tr><th>Metric</th><th>Score</th><th>Responses</th></tr></thead>
          <tbody>
            <tr><td>CSAT average</td><td>{stats.csatAverage} / 5</td><td>{stats.csatCount}</td></tr>
            <tr><td>NPS average</td><td>{stats.npsAverage} / 10</td><td>{stats.npsCount}</td></tr>
            <tr><td>NPS score</td><td>{stats.npsScore}</td><td>Promoters minus detractors</td></tr>
          </tbody></table>
        )}
      </div>
      <SurveyBox />
    </div>
  );
}

function SurveyBox() {
  const [ticketId, setTicketId] = useState('1');
  const [score, setScore] = useState('5');
  const [msg, setMsg] = useState('');
  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const r = await apiWrite('/api/feedback/submit', 'POST', { ticketId: Number(ticketId), type: 'CSAT', score: Number(score) });
    setMsg(r.ok ? 'Thanks. Survey recorded.' : 'Survey failed.');
  };
  return (
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
  );
}

function Canned() {
  const [items, setItems] = useState<{ id: number; title: string; content: string; usageCount: number }[]>([]);
  useEffect(() => { apiGet('/api/canned', [
    { id: 1, title: 'Password reset guide', content: 'Hello, here are the steps to reset access. Reply if the link has expired.', usageCount: 34 },
    { id: 2, title: 'Request received', content: 'Thanks for contacting support. Your ticket is assigned and we will respond within target.', usageCount: 58 },
  ]).then((d) => setItems(d as typeof items)); }, []);
  return (
    <div>
      <h1>Canned replies</h1>
      <p className="muted">Insert with variables for customer name, ticket id, and agent name. Usage is tracked per snippet.</p>
      {items.length === 0 ? <SkeletonList rows={3} /> : items.map((c) => (
        <div key={c.id} className="panel">
          <h3>{c.title}</h3>
          <p>{c.content}</p>
          <p className="small muted">Used {c.usageCount} times</p>
        </div>
      ))}
    </div>
  );
}

function NotificationsPage() {
  const [items, setItems] = useState<{ id: number; title: string; body: string; isRead: boolean; ticketId?: number }[] | null>(null);
  const load = () => apiGet('/api/notifications', []).then((d) => setItems(d as typeof items));
  useEffect(load, []);
  return (
    <div>
      <h1>Notifications</h1>
      <p className="muted">Assignment alerts, reply alerts, and status changes. Live updates arrive while you work.</p>
      {!items ? <SkeletonList rows={4} /> : items.length === 0 ? <p>No notifications. New tickets and replies will appear here.</p> : (
        <table className="grid"><thead><tr><th>Title</th><th>Detail</th><th>State</th></tr></thead>
        <tbody>{items.map((n) => (
          <tr key={n.id}><td>{n.title}</td><td className="small">{n.body}</td>
          <td>{n.isRead ? 'Read' : <button className="secondary" onClick={() => apiWrite('/api/notifications/' + n.id + '/read', 'POST', {}).then(load)}>Mark read</button>}</td></tr>
        ))}</tbody></table>
      )}
    </div>
  );
}

function UsersPage() {
  const [users, setUsers] = useState<{ username: string; displayName: string; role: string; email: string }[] | null>(null);
  const load = () => apiGet('/api/users', []).then((d) => setUsers(d as typeof users));
  useEffect(load, []);
  const setRole = async (u: string, role: string) => { await apiWrite('/api/users/' + u + '/role', 'PUT', { role }); load(); };
  return (
    <div>
      <h1>Users (Admin)</h1>
      <p className="muted">Manage roles. User sees own tickets, Agent sees queue, Admin sees all plus settings.</p>
      {!users ? <SkeletonList rows={4} /> : (
        <table className="grid"><thead><tr><th>Username</th><th>Name</th><th>Role</th><th>Change</th></tr></thead>
        <tbody>{users.map((u) => (
          <tr key={u.username}><td>{u.username}</td><td>{u.displayName}</td><td><span className="badge">{u.role}</span></td>
          <td><div className="row">{['User', 'Agent', 'Admin'].map((r) => <button key={r} className="secondary" onClick={() => setRole(u.username, r)}>{r}</button>)}</div></td></tr>
        ))}</tbody></table>
      )}
    </div>
  );
}

function Account() {
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
      const res = await fetch(API_BASE + '/api/auth/login', { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username, password }) });
      const data = await res.json();
      if (!res.ok) { setMsg('Login failed. Check username and password.'); return; }
      localStorage.setItem('hd_access', data.accessToken);
      localStorage.setItem('hd_refresh', data.refreshToken);
      localStorage.setItem('hd_user', JSON.stringify({ username: data.user.username, displayName: data.user.displayName, role: data.user.role, email: data.user.email }));
      auth.login({ username: data.user.username, displayName: data.user.displayName, role: data.user.role, email: data.user.email }, data.accessToken, data.refreshToken);
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

function Tos() {
  return (
    <div className="legal">
      <h1>Terms of Service</h1>
      <p>Last updated: 30 September 2026. These terms govern use of the Helpdesk Ticketing System.</p>
      <ol>
        <li>Service scope: the system provides ticket intake, knowledge articles, SLA tracking, and satisfaction surveys for support teams and their customers.</li>
        <li>Acceptable use: you agree not to submit unlawful content, not to attempt unauthorized access, and not to submit instruction override prompts aimed at the AI triage feature. Such prompts are blocked and logged.</li>
        <li>Accounts: you are responsible for credentials. Password changes revoke all other sessions. Reset links are single use and expire after 60 minutes.</li>
        <li>Fair use: automated classification is capped per user per day. Excess use returns a limit notice.</li>
        <li>Content: knowledge articles and replies you submit must be accurate to your knowledge. Do not include payment card data or passwords in tickets.</li>
        <li>Availability: targets follow stated SLA policies during business hours Mon to Fri, 08:00 to 17:00 SAST, excluding public holidays.</li>
        <li>Limitation: the service is provided as is. To the extent permitted by law, liability is limited to fees paid in the prior 3 months.</li>
        <li>Contact: support team via the Tickets page for questions about these terms.</li>
      </ol>
    </div>
  );
}

function Privacy() {
  return (
    <div className="legal">
      <h1>Privacy Policy</h1>
      <p>Last updated: 30 September 2026. This policy explains what we collect and why.</p>
      <ol>
        <li>Data collected: account identifiers, ticket content, replies, attachments, article feedback, survey scores, and security logs such as reset attempts and rate limit events.</li>
        <li>Use: to operate the helpdesk, enforce SLAs, improve articles, measure satisfaction, and protect against abuse.</li>
        <li>Sanitization: submitted text is sanitized before storage to remove scripts and active content.</li>
        <li>Cookies: strictly necessary session and CSRF cookies only. They use HttpOnly, Secure, and SameSite Strict flags. No advertising cookies.</li>
        <li>Retention: tickets and feedback are kept while the account is active and per legal duties, then deleted or anonymized.</li>
        <li>Sharing: data is not sold. Processors such as hosting and email delivery act only on instruction.</li>
        <li>Rights: request access, correction, or deletion via the support team. Reset links expire and cannot be reused.</li>
        <li>Security: HSTS, locked down CORS, rate limits, JWT short expiry with refresh rotation, and session revocation on password change are enabled.</li>
      </ol>
    </div>
  );
}

export default function App() {
  const [user, setUser] = useState<SessionUser | null>(null);
  useEffect(() => {
    ensureCsrf();
    const raw = localStorage.getItem('hd_user');
    if (raw) { try { setUser(JSON.parse(raw)); } catch { } }
  }, []);
  const login = (u: SessionUser) => setUser(u);
  const logout = () => { localStorage.removeItem('hd_access'); localStorage.removeItem('hd_refresh'); localStorage.removeItem('hd_user'); setUser(null); };
  return (
    <AuthCtx.Provider value={{ user, login, logout }}>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/tickets" element={<TicketList />} />
            <Route path="/tickets/:id" element={<TicketDetail />} />
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
    </AuthCtx.Provider>
  );
}
