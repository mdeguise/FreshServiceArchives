import { useEffect, useState } from 'react'
import { Link, Outlet } from 'react-router-dom'
import { api } from '../api'

export default function Layout() {
  const [user, setUser] = useState<string>('')

  useEffect(() => {
    api
      .getMe()
      .then((me) => setUser(me.displayName || me.userName))
      .catch(() => setUser(''))
  }, [])

  return (
    <>
      <header className="app-header">
        <Link to="/" style={{ color: '#fff' }}>
          <h1>Freshservice Ticket Archive</h1>
        </Link>
        {user && <span className="user">Signed in as {user}</span>}
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </>
  )
}
