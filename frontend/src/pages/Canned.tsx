import React, { useEffect, useState } from 'react';
import { apiGet } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { CannedItem } from '../types';

export function Canned() {
  const [items, setItems] = useState<CannedItem[] | null>(null);
  useEffect(() => { apiGet<CannedItem[]>('/api/canned', []).then(setItems); }, []);

  return (
    <div>
      <h1>Canned replies</h1>
      <p className="muted">Insert with variables for customer name, ticket id, and agent name. In a ticket reply, type // to filter these snippets. Usage is tracked per snippet.</p>
      {!items ? <SkeletonList rows={3} /> : items.map((c) => (
        <div key={c.id} className="panel">
          <h3>{c.title}</h3>
          <p>{c.content}</p>
          <p className="small muted">Used {c.usageCount} times</p>
        </div>
      ))}
    </div>
  );
}
