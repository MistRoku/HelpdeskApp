import React, { useEffect, useState } from 'react';
import { apiGet, apiWrite } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { NotificationItem } from '../types';

export function NotificationsPage() {
  const [items, setItems] = useState<NotificationItem[] | null>(null);
  const load = () => apiGet<NotificationItem[]>('/api/notifications', []).then(setItems);
  useEffect(load, []);

  return (
    <div>
      <h1>Notifications</h1>
      <p className="muted">Assignment alerts, reply alerts, and status changes. Live updates arrive while you work.</p>
      {!items ? <SkeletonList rows={4} /> : items.length === 0 ? <p>No notifications. New tickets and replies will appear here.</p> : (
        <table className="grid">
          <thead><tr><th>Title</th><th>Detail</th><th>State</th></tr></thead>
          <tbody>{items.map((n) => (
            <tr key={n.id}>
              <td>{n.title}</td>
              <td className="small">{n.body}</td>
              <td>{n.isRead ? 'Read' : <button className="secondary" onClick={() => apiWrite('/api/notifications/' + n.id + '/read', 'POST', {}).then(load)}>Mark read</button>}</td>
            </tr>
          ))}</tbody>
        </table>
      )}
    </div>
  );
}
