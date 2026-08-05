import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import type { AccessLists, AccessTier, AccessUser } from '../types'

const TIERS: { key: AccessTier; title: string; blurb: string }[] = [
  { key: 'full', title: 'Full Access', blurb: 'Sees every group, and can manage access (admin).' },
  { key: 'it', title: 'IT Access', blurb: 'Sees the IT / Sys Admin groups only.' },
  { key: 'rh', title: 'RH Access', blurb: 'Sees the RH / HR groups only.' },
]

export default function AdminPage() {
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null)
  const [lists, setLists] = useState<AccessLists | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setError(null)
    try {
      const info = await api.getAccessInfo()
      setIsAdmin(info.isAdmin)
      if (info.isAdmin) setLists(await api.getAccessLists())
    } catch (e) {
      setError((e as Error).message)
      setIsAdmin(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  if (isAdmin === null) return <div className="state">Loading…</div>

  if (!isAdmin) {
    return (
      <>
        <Link to="/" className="back-link">
          ‹ Back to search
        </Link>
        <div className="card">
          <div className="state">
            You don’t have permission to manage access. This section is for Full Access users only.
          </div>
        </div>
      </>
    )
  }

  return (
    <>
      <Link to="/" className="back-link">
        ‹ Back to search
      </Link>
      <div className="card detail-header">
        <h2>Access Management</h2>
        <span className="muted">
          Add or remove users from each tier. A user sees the combined groups of every tier
          they’re in; a user in no tier sees nothing. Use the Windows sign-in name (e.g.{' '}
          <code>jdoe</code>), not the email.
        </span>
      </div>

      {error && <div className="error">{error}</div>}

      {TIERS.map((tier) => (
        <TierCard
          key={tier.key}
          tier={tier.key}
          title={tier.title}
          blurb={tier.blurb}
          users={lists?.[tier.key] ?? []}
          onChanged={load}
          onError={setError}
        />
      ))}
    </>
  )
}

function TierCard({
  tier,
  title,
  blurb,
  users,
  onChanged,
  onError,
}: {
  tier: AccessTier
  title: string
  blurb: string
  users: AccessUser[]
  onChanged: () => void
  onError: (msg: string) => void
}) {
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [busy, setBusy] = useState(false)

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!userName.trim()) return
    setBusy(true)
    try {
      await api.addAccessUser(tier, {
        userName: userName.trim(),
        email: email.trim() || undefined,
        displayName: displayName.trim() || undefined,
      })
      setUserName('')
      setEmail('')
      setDisplayName('')
      onChanged()
    } catch (err) {
      onError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  async function remove(name: string) {
    if (!confirm(`Remove ${name} from ${title}?`)) return
    setBusy(true)
    try {
      await api.removeAccessUser(tier, name)
      onChanged()
    } catch (err) {
      onError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="section-title">
        {title} <span className="muted" style={{ fontWeight: 400 }}>· {blurb}</span>
      </div>
      <div className="card" style={{ marginBottom: 16 }}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th className="no-sort">User</th>
                <th className="no-sort">Email</th>
                <th className="no-sort">Added</th>
                <th className="no-sort" style={{ width: 90 }}></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.userName}>
                  <td>
                    {u.displayName || u.userName}
                    {u.displayName && <span className="muted"> ({u.userName})</span>}
                  </td>
                  <td className="muted">{u.email || '—'}</td>
                  <td className="muted nowrap">{u.addedBy ? `by ${u.addedBy}` : '—'}</td>
                  <td>
                    <button className="btn secondary" disabled={busy} onClick={() => remove(u.userName)}>
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr>
                  <td colSpan={4} className="muted">
                    No users in this tier.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        <form className="filters" onSubmit={add} style={{ marginBottom: 0 }}>
          <div className="field">
            <label>Sign-in name *</label>
            <input value={userName} onChange={(e) => setUserName(e.target.value)} placeholder="jdoe" />
          </div>
          <div className="field">
            <label>Email</label>
            <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="jdoe@tremblant.ca" />
          </div>
          <div className="field">
            <label>Display name</label>
            <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="Jane Doe" />
          </div>
          <button type="submit" className="btn" disabled={busy || !userName.trim()}>
            Add to {title}
          </button>
        </form>
      </div>
    </>
  )
}
