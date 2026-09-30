import { render, screen } from '@testing-library/react';
import { StatusBadge } from './ui';

test.each([
  ['New', 'badge'],
  ['Urgent', 'urgent'],
  ['High', 'high'],
  ['Closed', 'ok'],
])('badge for %s carries class %s', (value, cls) => {
  render(<StatusBadge value={value} />);
  expect(screen.getByText(value).className).toMatch(cls);
});
