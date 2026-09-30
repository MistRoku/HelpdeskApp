export const API_BASE = process.env.REACT_APP_API_BASE || 'https://localhost:7049';

let csrfToken: string | null = null;

export function getAccess(): string | null {
  return localStorage.getItem('hd_access');
}

export async function ensureCsrf(): Promise<string | null> {
  if (csrfToken) return csrfToken;
  try {
    const res = await fetch(API_BASE + '/api/antiforgery/token', { credentials: 'include' });
    if (!res.ok) return null;
    const data = await res.json();
    csrfToken = data.token;
    return csrfToken;
  } catch {
    return null;
  }
}

export function authHeaders(): Record<string, string> {
  const h: Record<string, string> = {};
  const a = getAccess();
  if (a) h['Authorization'] = 'Bearer ' + a;
  const u = localStorage.getItem('hd_user');
  if (u) {
    try {
      const p = JSON.parse(u);
      h['X-User'] = p.username;
      h['X-Role'] = p.role;
      h['X-Allow-Write'] = '1';
    } catch { /* ignore corrupt session */ }
  }
  return h;
}

export async function apiGet<T>(path: string, fallback: T): Promise<T> {
  try {
    const res = await fetch(API_BASE + path, { credentials: 'include', headers: authHeaders() });
    if (!res.ok) throw new Error('bad status');
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}

export async function apiWrite(
  path: string,
  method: string,
  body?: unknown
): Promise<{ ok: boolean; data?: unknown; status: number }> {
  try {
    const token = await ensureCsrf();
    const res = await fetch(API_BASE + path, {
      method,
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', ...authHeaders(), ...(token ? { 'X-CSRF-TOKEN': token } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    let data: unknown = null;
    try { data = await res.json(); } catch { /* empty body */ }
    return { ok: res.ok, data, status: res.status };
  } catch {
    return { ok: false, status: 0 };
  }
}
