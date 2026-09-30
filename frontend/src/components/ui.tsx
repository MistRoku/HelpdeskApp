import React from 'react';

export function Mark() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" aria-hidden="true">
      <rect x="1" y="1" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" />
      <rect x="6" y="6" width="6" height="6" fill="currentColor" />
    </svg>
  );
}

export function SkeletonList({ rows = 4 }: { rows?: number }) {
  return (
    <div className="loading" aria-label="Loading">
      {Array.from({ length: rows }).map((_, i) => <div key={i} className="skeleton row" />)}
    </div>
  );
}

export function StatusBadge({ value }: { value: string }) {
  const cls =
    value === 'Urgent' ? 'badge urgent'
    : value === 'High' ? 'badge high'
    : value === 'Closed' || value === 'Resolved' ? 'badge ok'
    : 'badge';
  return <span className={cls}>{value}</span>;
}
