import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { API_BASE, getAccess } from '../api/client';

/**
 * Live ticket updates with reconnect backoff. Joins the ticket group for
 * detail pages and the agent group for queue pages. Falls back to polling
 * when the hub is unreachable so pages still work offline.
 */
export function useSignalR(opts: { ticketId?: number; onEvent?: () => void }) {
  const cb = useRef(opts.onEvent);
  cb.current = opts.onEvent;

  useEffect(() => {
    let conn: signalR.HubConnection | null = null;
    let cancelled = false;
    (async () => {
      try {
        conn = new signalR.HubConnectionBuilder()
          .withUrl(API_BASE + '/hubs/tickets', {
            accessTokenFactory: () => getAccess() || '',
          })
          .withAutomaticReconnect([0, 2000, 10000, 30000])
          .build();
        conn.on('ticketUpdated', () => cb.current?.());
        conn.on('replyAdded', () => cb.current?.());
        conn.on('ticketCreated', () => cb.current?.());
        conn.on('ticketAssigned', () => cb.current?.());
        await conn.start();
        if (cancelled) { await conn.stop(); return; }
        if (opts.ticketId) await conn.invoke('JoinTicket', opts.ticketId);
        await conn.invoke('JoinAgents').catch(() => { /* non-agent role */ });
      } catch {
        conn = null; // polling fallback owned by the page
      }
    })();
    return () => {
      cancelled = true;
      if (conn) conn.stop().catch(() => { /* already closed */ });
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opts.ticketId]);
}
