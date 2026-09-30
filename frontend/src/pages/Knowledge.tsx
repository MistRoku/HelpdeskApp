import React, { useEffect, useState } from 'react';
import { apiGet, apiWrite } from '../api/client';
import { SkeletonList } from '../components/ui';
import type { Article } from '../types';

const CATS = ['All', 'Access', 'Network', 'Hardware', 'Billing', 'Software', 'General'];

export function Knowledge() {
  const [articles, setArticles] = useState<Article[] | null>(null);
  const [q, setQ] = useState('');
  const [cat, setCat] = useState('All');

  useEffect(() => {
    setArticles(null);
    const t = setTimeout(() => {
      apiGet<Article[]>('/api/knowledgebase/articles', []).then((a) => {
        setArticles(a.filter((x) =>
          (cat === 'All' || x.category === cat) &&
          (!q || (x.title + ' ' + x.content + ' ' + x.tags).toLowerCase().includes(q.toLowerCase()))));
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
            {CATS.map((c) => <button key={c} className="category-btn secondary" onClick={() => setCat(c)}>{c}</button>)}
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
