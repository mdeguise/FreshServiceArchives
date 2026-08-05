import { useEffect, useState } from 'react'
import { Link, Outlet } from 'react-router-dom'
import { api } from '../api'

export default function Layout() {
  const [user, setUser] = useState<string>('')
  const [isAdmin, setIsAdmin] = useState(false)

  useEffect(() => {
    api
      .getMe()
      .then((me) => setUser(me.displayName || me.userName))
      .catch(() => setUser(''))
    api
      .getAccessInfo()
      .then((a) => setIsAdmin(a.isAdmin))
      .catch(() => setIsAdmin(false))
  }, [])

  return (
    <>
      <header className="app-header">
        <Link to="/" style={{ color: '#fff' }}>
          <h1>Freshservice Ticket Archive</h1>
        </Link>
        <div className="header-right">
          {isAdmin && (
            <Link to="/admin" className="header-link">
              Access Management
            </Link>
          )}
          {user && <span className="user">Signed in as {user}</span>}
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </>
  )
}
