export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatDateShort(value: string | null | undefined): string {
  if (!value) return '—'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: '2-digit' })
}

export function formatFileSize(bytes: number | null | undefined): string {
  if (bytes == null) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

const STATUS_CLASS: Record<number, string> = {
  2: 'badge-open',
  3: 'badge-pending',
  4: 'badge-resolved',
  5: 'badge-closed',
}

export function statusClass(status: number | null | undefined): string {
  return (status != null && STATUS_CLASS[status]) || 'badge-default'
}

const PRIORITY_CLASS: Record<number, string> = {
  1: 'prio-low',
  2: 'prio-medium',
  3: 'prio-high',
  4: 'prio-urgent',
}

export function priorityClass(priority: number | null | undefined): string {
  return (priority != null && PRIORITY_CLASS[priority]) || 'prio-low'
}
