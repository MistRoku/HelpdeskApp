export type Ticket = {
  id: number; ticketNumber: string; title: string; description: string;
  status: string; priority: string; category: string; channel: string;
  userName: string; assignedTo?: string;
  createdAt: string; updatedAt?: string; lastActivityAt: string;
  firstResponseAt?: string; resolvedAt?: string; closedAt?: string;
  responseDueAt?: string; resolutionDueAt?: string;
  slaBreached?: boolean; aiCategory?: string; aiPriority?: string; aiConfidence?: number;
};

export type Reply = {
  id: number; ticketId: number; author: string; body: string;
  isInternal: boolean; createdAt: string;
};

export type Article = {
  id: number; title: string; content: string; category: string; tags: string;
  status: string; author: string; viewCount: number;
  helpfulCount: number; unhelpfulCount: number;
};

export type SlaPolicy = {
  id: number; name: string; priority: string;
  responseMinutes: number; resolutionMinutes: number;
};

export type DashboardStats = {
  openTickets: number; inProgress: number; resolvedToday: number;
  avgResponseMinutes: number; avgResolutionHours: number;
  byStatus: Record<string, number>;
  byAgent: { agent: string; assigned: number; resolved: number }[];
  feedback: { csatAverage: number; csatCount: number; npsAverage: number; npsScore: number; npsCount: number };
  activity: { id: number; ticketId?: number; actor: string; action: string; createdAt: string }[];
};

export type SessionUser = {
  username: string; displayName: string; role: string; email: string;
};

export type NotificationItem = {
  id: number; title: string; body: string; isRead: boolean; ticketId?: number;
};

export type AttachmentMeta = { id: number; fileName: string; sizeBytes: number };

export type CannedItem = { id: number; title: string; content: string; usageCount: number };
