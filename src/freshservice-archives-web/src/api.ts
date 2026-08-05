import type {
  AccessLists,
  AccessTier,
  CodeLookup,
  CurrentUser,
  DirectoryUser,
  GroupLookup,
  PagedResult,
  TicketDetail,
  TicketListItem,
  UserAccessInfo,
} from './types'

export interface TicketSearchParams {
  search?: string
  requester?: string
  description?: string
  status?: number
  priority?: number
  groupId?: number
  createdFrom?: string
  createdTo?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDesc?: boolean
}

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { credentials: 'include' })
  if (!res.ok) {
    const body = await res.text().catch(() => '')
    throw new Error(`Request failed (${res.status}): ${body || res.statusText}`)
  }
  return (await res.json()) as T
}

async function send(url: string, method: 'POST' | 'DELETE', bodyObj?: unknown): Promise<void> {
  const res = await fetch(url, {
    method,
    credentials: 'include',
    headers: bodyObj ? { 'Content-Type': 'application/json' } : undefined,
    body: bodyObj ? JSON.stringify(bodyObj) : undefined,
  })
  if (!res.ok) {
    const body = await res.text().catch(() => '')
    throw new Error(`Request failed (${res.status}): ${body || res.statusText}`)
  }
}

function buildQuery(params: Record<string, unknown>): string {
  const qs = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    qs.set(key, String(value))
  }
  const s = qs.toString()
  return s ? `?${s}` : ''
}

export const api = {
  searchTickets(params: TicketSearchParams): Promise<PagedResult<TicketListItem>> {
    return getJson(`/api/tickets${buildQuery(params as Record<string, unknown>)}`)
  },
  getTicket(id: number): Promise<TicketDetail> {
    return getJson(`/api/tickets/${id}`)
  },
  attachmentUrl(attachmentId: number): string {
    return `/api/tickets/attachments/${attachmentId}`
  },
  getGroups(): Promise<GroupLookup[]> {
    return getJson('/api/lookups/groups')
  },
  getStatuses(): Promise<CodeLookup[]> {
    return getJson('/api/lookups/statuses')
  },
  getPriorities(): Promise<CodeLookup[]> {
    return getJson('/api/lookups/priorities')
  },
  getMe(): Promise<CurrentUser> {
    return getJson('/api/me')
  },
  getAccessInfo(): Promise<UserAccessInfo> {
    return getJson('/api/access/me')
  },
  getAccessLists(): Promise<AccessLists> {
    return getJson('/api/access/lists')
  },
  addAccessUser(
    tier: AccessTier,
    user: { userName: string; email?: string; displayName?: string },
  ): Promise<void> {
    return send(`/api/access/${tier}`, 'POST', user)
  },
  removeAccessUser(tier: AccessTier, userName: string): Promise<void> {
    return send(`/api/access/${tier}/${encodeURIComponent(userName)}`, 'DELETE')
  },
  searchDirectory(q: string): Promise<DirectoryUser[]> {
    return getJson(`/api/access/directory/search?q=${encodeURIComponent(q)}`)
  },
}
