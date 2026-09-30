import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import { StatusBadge } from './components/ui';

test('renders helpdesk console shell', () => {
  render(<App />);
  expect(screen.getByText(/Helpdesk Console/i)).toBeInTheDocument();
  expect(screen.getByText(/Support overview/i)).toBeInTheDocument();
});

test('nav exposes deep-linkable ticket queue', () => {
  render(<App />);
  const link = screen.getByRole('link', { name: /Tickets/i });
  expect(link.getAttribute('href')).toBe('/tickets');
});

test('status badge renders urgent state', () => {
  render(<StatusBadge value="Urgent" />);
  const badge = screen.getByText('Urgent');
  expect(badge.className).toMatch(/urgent/);
});

test('status badge renders closed state', () => {
  render(<StatusBadge value="Closed" />, { wrapper: ({ children }) => <MemoryRouter>{children}</MemoryRouter> });
  expect(screen.getByText('Closed')).toBeInTheDocument();
});
