import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api'
import type { ConversationItem, TicketDetail } from '../types'
import { formatDate, formatFileSize, priorityClass, statusClass } from '../lib/format'
import SafeHtml from '../components/SafeHtml'

export default function TicketDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [ticket, setTicket] = useState<TicketDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const ticketId = Number(id)
    if (!Number.isFinite(ticketId)) {
      setError('Invalid ticket id.')
      setLoading(false)
      return
    }
    let cancelled = false
    setLoading(true)
    setError(null)
    api
      .getTicket(ticketId)
      .then((t) => !cancelled && setTicket(t))
      .catch((e: Error) => !cancelled && setError(e.message))
      .finally(() => !cancelled && setLoading(false))
    return () => {
      cancelled = true
    }
  }, [id])

  if (loading) return <div className="state">Loading ticket…</div>
  if (error) return <div className="error">Could not load ticket: {error}</div>
  if (!ticket) return <div className="error">Ticket not found.</div>

  const ticketAttachments = ticket.attachments.filter((a) => a.conversationID == null)

  return (
    <>
      <Link to="/" className="back-link">
        ‹ Back to search
      </Link>

      <div className="card detail-header">
        <h2>
          {ticket.subject || '(no subject)'}{' '}
          <span className="ticket-num">#{ticket.ticketID}</span>
        </h2>
        <span className={`badge ${statusClass(ticket.status)}`}>{ticket.statusText ?? '—'}</span>{' '}
        <span className={`prio ${priorityClass(ticket.priority)}`}>
          {ticket.priorityText ?? '—'} priority
        </span>
      </div>

      <div className="card">
        <div className="meta-grid">
          <Meta label="Requester" value={personLine(ticket.requester)} />
          <Meta label="Agent" value={personLine(ticket.responder)} />
          <Meta label="Group" value={ticket.group?.name ?? '—'} />
          <Meta label="Source" value={ticket.sourceText ?? '—'} />
          <Meta label="Category" value={categoryLine(ticket)} />
          <Meta label="Created" value={formatDate(ticket.createdAt)} />
          <Meta label="Updated" value={formatDate(ticket.updatedAt)} />
          <Meta label="Due by" value={formatDate(ticket.dueBy)} />
        </div>
      </div>

      <div className="section-title">Description</div>
      <div className="card">
        {ticket.descriptionHTML ? (
          <SafeHtml className="desc-body" html={ticket.descriptionHTML} />
        ) : (
          <div className="desc-body muted">{ticket.descriptionText || 'No description.'}</div>
        )}
      </div>

      {ticketAttachments.length > 0 && (
        <>
          <div className="section-title">Attachments</div>
          <div className="card">
            <AttachmentList items={ticketAttachments} />
          </div>
        </>
      )}

      <div className="section-title">
        Conversation{ticket.conversations.length !== 1 ? 's' : ''} ({ticket.conversations.length})
      </div>
      <div className="card">
        {ticket.conversations.length === 0 && (
          <div className="state">No conversations archived for this ticket.</div>
        )}
        {ticket.conversations.map((c) => (
          <Conversation key={c.conversationID} conv={c} ticket={ticket} />
        ))}
      </div>
    </>
  )
}

function Meta({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="meta-item">
      <div className="k">{label}</div>
      <div className="v">{value}</div>
    </div>
  )
}

function personLine(p: TicketDetail['requester']) {
  if (!p) return '—'
  const name = p.fullName || p.email || `#${p.id}`
  return (
    <span>
      {name}
      {p.email && p.fullName && (
        <div className="muted" style={{ fontSize: 12 }}>
          {p.email}
        </div>
      )}
    </span>
  )
}

function categoryLine(t: TicketDetail) {
  const parts = [t.category, t.subCategory, t.itemCategory].filter(Boolean)
  return parts.length ? parts.join(' › ') : '—'
}

function Conversation({ conv, ticket }: { conv: ConversationItem; ticket: TicketDetail }) {
  const attachments = ticket.attachments.filter((a) => a.conversationID === conv.conversationID)
  return (
    <div className="conversation">
      <div className="conv-head">
        <span>{conv.fromEmail || 'Unknown sender'}</span>
        {conv.privateNote ? (
          <span className="tag tag-private">Private note</span>
        ) : conv.incoming ? (
          <span className="tag tag-incoming">Incoming</span>
        ) : (
          <span className="tag tag-outgoing">Outgoing</span>
        )}
        <span className="spacer" style={{ flex: 1 }} />
        <span>{formatDate(conv.createdAt)}</span>
      </div>
      {conv.bodyHTML ? (
        <SafeHtml className="conv-body" html={conv.bodyHTML} />
      ) : (
        <div className="conv-body muted">{conv.bodyText || '(empty)'}</div>
      )}
      {attachments.length > 0 && <AttachmentList items={attachments} />}
    </div>
  )
}

function AttachmentList({ items }: { items: TicketDetail['attachments'] }) {
  return (
    <ul className="attach-list">
      {items.map((a) => (
        <li key={a.attachmentID}>
          📎{' '}
          {a.canDownload ? (
            <a href={api.attachmentUrl(a.attachmentID)} target="_blank" rel="noopener noreferrer">
              {a.fileName || 'attachment'}
            </a>
          ) : (
            <span title="File was not archived to disk">{a.fileName || 'attachment'}</span>
          )}
          {a.fileSize != null && <span className="size">{formatFileSize(a.fileSize)}</span>}
          {!a.canDownload && <span className="size">(not available)</span>}
        </li>
      ))}
    </ul>
  )
}
