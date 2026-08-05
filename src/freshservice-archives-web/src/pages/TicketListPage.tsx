import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import type { CodeLookup, GroupLookup, PagedResult, TicketListItem } from '../types'
import { formatDate, priorityClass, statusClass } from '../lib/format'

const PAGE_SIZE = 25

export default function TicketListPage() {
  const [params, setParams] = useSearchParams()
  const navigate = useNavigate()

  const [groups, setGroups] = useState<GroupLookup[]>([])
  const [statuses, setStatuses] = useState<CodeLookup[]>([])
  const [priorities, setPriorities] = useState<CodeLookup[]>([])

  const [data, setData] = useState<PagedResult<TicketListItem> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Local form state, seeded from the URL so links and refreshes are stable.
  const [search, setSearch] = useState(params.get('search') ?? '')
  const [requester, setRequester] = useState(params.get('requester') ?? '')
  const [description, setDescription] = useState(params.get('description') ?? '')

  const page = Number(params.get('page') ?? '1') || 1
  const sortBy = params.get('sortBy') ?? 'CreatedAt'
  const sortDesc = params.get('sortDesc') !== 'false'

  useEffect(() => {
    api.getGroups().then(setGroups).catch(() => {})
    api.getStatuses().then(setStatuses).catch(() => {})
    api.getPriorities().then(setPriorities).catch(() => {})
  }, [])

  const queryParams = useMemo(
    () => ({
      search: params.get('search') ?? undefined,
      requester: params.get('requester') ?? undefined,
      description: params.get('description') ?? undefined,
      status: params.get('status') ? Number(params.get('status')) : undefined,
      priority: params.get('priority') ? Number(params.get('priority')) : undefined,
      groupId: params.get('groupId') ? Number(params.get('groupId')) : undefined,
      createdFrom: params.get('createdFrom') ?? undefined,
      createdTo: params.get('createdTo') ?? undefined,
      page,
      pageSize: PAGE_SIZE,
      sortBy,
      sortDesc,
    }),
    [params, page, sortBy, sortDesc],
  )

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    api
      .searchTickets(queryParams)
      .then((res) => {
        if (!cancelled) setData(res)
      })
      .catch((e: Error) => {
        if (!cancelled) setError(e.message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [queryParams])

  function patch(next: Record<string, string | undefined>, resetPage = true) {
    const merged = new URLSearchParams(params)
    for (const [k, v] of Object.entries(next)) {
      if (v === undefined || v === '') merged.delete(k)
      else merged.set(k, v)
    }
    if (resetPage) merged.set('page', '1')
    setParams(merged)
  }

  function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    patch({
      search: search.trim() || undefined,
      requester: requester.trim() || undefined,
      description: description.trim() || undefined,
    })
  }

  function onSort(column: string) {
    if (sortBy === column) {
      patch({ sortBy: column, sortDesc: String(!sortDesc) }, false)
    } else {
      patch({ sortBy: column, sortDesc: 'true' }, false)
    }
  }

  function sortIndicator(column: string) {
    if (sortBy !== column) return ''
    return sortDesc ? ' ▼' : ' ▲'
  }

  function clearFilters() {
    setSearch('')
    setRequester('')
    setDescription('')
    setParams(new URLSearchParams())
  }

  const totalPages = data?.totalPages ?? 0

  return (
    <>
      <form className="card filters" onSubmit={onSubmit}>
        <div className="field grow">
          <label htmlFor="search">Search</label>
          <input
            id="search"
            type="text"
            placeholder="Ticket # or subject…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="field grow">
          <label htmlFor="requester">Requester</label>
          <input
            id="requester"
            type="text"
            placeholder="Requester name or email…"
            value={requester}
            onChange={(e) => setRequester(e.target.value)}
          />
        </div>

        <div className="field grow">
          <label htmlFor="description">Word in description</label>
          <input
            id="description"
            type="text"
            placeholder="Keyword in ticket description…"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="status">Status</label>
          <select
            id="status"
            value={params.get('status') ?? ''}
            onChange={(e) => patch({ status: e.target.value || undefined })}
          >
            <option value="">All</option>
            {statuses.map((s) => (
              <option key={s.value} value={s.value}>
                {s.text}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label htmlFor="priority">Priority</label>
          <select
            id="priority"
            value={params.get('priority') ?? ''}
            onChange={(e) => patch({ priority: e.target.value || undefined })}
          >
            <option value="">All</option>
            {priorities.map((p) => (
              <option key={p.value} value={p.value}>
                {p.text}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label htmlFor="group">Group</label>
          <select
            id="group"
            value={params.get('groupId') ?? ''}
            onChange={(e) => patch({ groupId: e.target.value || undefined })}
          >
            <option value="">All</option>
            {groups.map((g) => (
              <option key={g.groupID} value={g.groupID}>
                {g.name ?? `Group ${g.groupID}`}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label>Created</label>
          <div className="date-range">
            <input
              id="from"
              type="date"
              aria-label="Created from"
              value={params.get('createdFrom') ?? ''}
              onChange={(e) => patch({ createdFrom: e.target.value || undefined })}
            />
            <span className="date-sep">to</span>
            <input
              id="to"
              type="date"
              aria-label="Created to"
              value={params.get('createdTo') ?? ''}
              onChange={(e) => patch({ createdTo: e.target.value || undefined })}
            />
          </div>
        </div>

        <button type="submit" className="btn">
          Search
        </button>
        <button type="button" className="btn secondary" onClick={clearFilters}>
          Clear
        </button>
      </form>

      {error && <div className="error">Could not load tickets: {error}</div>}

      <div className="card">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th onClick={() => onSort('TicketID')}>#{sortIndicator('TicketID')}</th>
                <th onClick={() => onSort('Subject')}>Subject{sortIndicator('Subject')}</th>
                <th onClick={() => onSort('Status')}>Status{sortIndicator('Status')}</th>
                <th onClick={() => onSort('Priority')}>Priority{sortIndicator('Priority')}</th>
                <th className="no-sort">Group</th>
                <th className="no-sort">Requester</th>
                <th onClick={() => onSort('CreatedAt')} className="nowrap">
                  Created{sortIndicator('CreatedAt')}
                </th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((t) => (
                <tr
                  key={t.ticketID}
                  onClick={() => navigate(`/tickets/${t.ticketID}`)}
                  style={{ cursor: 'pointer' }}
                >
                  <td className="ticket-id nowrap">{t.ticketID}</td>
                  <td className="subject-cell">
                    {t.subject || <span className="muted">(no subject)</span>}
                    {t.hasAttachments && (
                      <span className="attach-flag" title="Has attachments">
                        {' '}
                        📎
                      </span>
                    )}
                  </td>
                  <td>
                    <span className={`badge ${statusClass(t.status)}`}>{t.statusText ?? '—'}</span>
                  </td>
                  <td>
                    <span className={`prio ${priorityClass(t.priority)}`}>{t.priorityText ?? '—'}</span>
                  </td>
                  <td>{t.groupName || <span className="muted">—</span>}</td>
                  <td>
                    {t.requesterName || <span className="muted">—</span>}
                    {t.requesterEmail && (
                      <div className="muted" style={{ fontSize: 12 }}>
                        {t.requesterEmail}
                      </div>
                    )}
                  </td>
                  <td className="nowrap muted">{formatDate(t.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {loading && <div className="state">Loading tickets…</div>}
        {!loading && data && data.items.length === 0 && (
          <div className="state">No tickets match your filters.</div>
        )}

        {data && data.totalCount > 0 && (
          <div className="pagination">
            <span>
              {(data.page - 1) * data.pageSize + 1}–
              {Math.min(data.page * data.pageSize, data.totalCount)} of{' '}
              {data.totalCount.toLocaleString()} tickets
            </span>
            <span className="spacer" />
            <button
              className="btn secondary"
              disabled={page <= 1}
              onClick={() => patch({ page: String(page - 1) }, false)}
            >
              ‹ Prev
            </button>
            <span>
              Page {data.page} of {totalPages}
            </span>
            <button
              className="btn secondary"
              disabled={page >= totalPages}
              onClick={() => patch({ page: String(page + 1) }, false)}
            >
              Next ›
            </button>
          </div>
        )}
      </div>
    </>
  )
}
