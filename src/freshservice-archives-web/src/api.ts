import type {
  CodeLookup,
  CurrentUser,
  GroupLookup,
  PagedResult,
  TicketDetail,
  TicketListItem,
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
}
