import { useCallback, useEffect, useState } from 'react';
import { apiGet } from '../api/client';
import type { Ticket } from '../types';

export type TicketFilters = {
  q: string; status: string; priority: string; category: string; view: string;
};

/**
 * Ticket queue with server-side search and filter. Saved view persists
 * to localStorage so agents keep their queue between visits.
 */
export function useTickets() {
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  const [filters, setFilters] = useState<TicketFilters>(() => ({
    q: '',
    status: 'All',
    priority: 'All',
    category: 'All',
    view: localStorage.getItem('hd_view') || 'All',
  }));

  const load = useCallback(async () => {
    setTickets(null);
    const params = new URLSearchParams();
    if (filters.q) params.set('q', filters.q);
    if (filters.status !== 'All') params.set('status', filters.status);
    if (filters.priority !== 'All') params.set('priority', filters.priority);
    if (filters.category !== 'All') params.set('category', filters.category);
    const raw = localStorage.getItem('hd_user');
    if (filters.view === 'Mine' && raw) {
      try { params.set('assignee', JSON.parse(raw).username); } catch { /* ignore */ }
    }
    const list = await apiGet<Ticket[]>('/api/tickets?' + params.toString(), []);
    setTickets(list);
  }, [filters]);

  useEffect(() => { load(); }, [load]);

  const update = (patch: Partial<TicketFilters>) => {
    setFilters((f) => {
      const next = { ...f, ...patch };
      if (patch.view) localStorage.setItem('hd_view', patch.view);
      return next;
    });
  };

  return { tickets, filters, update, reload: load };
}
