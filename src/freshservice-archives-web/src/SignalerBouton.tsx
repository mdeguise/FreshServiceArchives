import { useEffect, useRef, useState } from 'react'
import { createRoot } from 'react-dom/client'

/** Le Guichet's screens. Everything about reports lives there; this application only opens the window. */
const GUICHET = 'https://vm-trm-live.idirectory.itw:8168'
const ORIGINE = new URL(GUICHET).origin

type Erreur = { quand: string; message: string }
const erreurs: Erreur[] = []
function noter(message: string) {
  erreurs.push({ quand: new Date().toISOString(), message: message.slice(0, 500) })
  if (erreurs.length > 10) erreurs.shift()
}
window.addEventListener('error', e => noter(`${e.message} (${e.filename}:${e.lineno})`))
window.addEventListener('unhandledrejection', e => noter(`Promesse rejetée : ${String((e.reason as Error)?.message ?? e.reason)}`))

function titreEcran() {
  const t = Array.from(document.querySelectorAll('main h1, main h2, h1')).find(el => (el as HTMLElement).offsetParent !== null)
  return t?.textContent?.trim().slice(0, 120) || document.title || window.location.pathname
}

function contexte() {
  const script = Array.from(document.scripts).find(s => /\/assets\/index-.*\.js/.test(s.src))
  return {
    version: script ? script.src.split('/').pop() : 'dev',
    url: window.location.pathname + window.location.search + window.location.hash,
    ecran: titreEcran(),
    navigateur: navigator.userAgent,
    fenetre: `${window.innerWidth}x${window.innerHeight}`,
    ecranPhysique: `${window.screen.width}x${window.screen.height} @${window.devicePixelRatio}`,
    langue: navigator.language,
    signaleLe: new Date().toISOString(),
    erreursRecentes: [...erreurs],
  }
}

const voile: React.CSSProperties = { position: 'fixed', inset: 0, zIndex: 2147483000, display: 'grid', placeItems: 'center', padding: '2vh 2vw', boxSizing: 'border-box', background: 'rgba(13, 36, 64, .55)' }
const fenetre: React.CSSProperties = { position: 'relative', width: '100%', height: '100%', maxWidth: 1500, borderRadius: 14, overflow: 'hidden', background: '#fff' }
const repli: React.CSSProperties = { position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12, padding: 24, background: '#fff', color: '#1c1c1a', font: '15px system-ui, sans-serif' }
const bouton: React.CSSProperties = { position: 'fixed', right: 14, bottom: 14, zIndex: 2147482000, padding: '7px 12px', border: 0, borderRadius: 18, background: '#0d2440', color: '#fff', font: '600 13px system-ui, sans-serif', cursor: 'pointer', boxShadow: '0 2px 8px rgba(0, 0, 0, .3)' }

function Fenetre({ url, onClose }: { url: string; onClose: () => void }) {
  const cadre = useRef<HTMLIFrameElement>(null)
  const [message] = useState(() => ({ type: 'signalement:contexte', ecran: titreEcran(), contexte: JSON.stringify(contexte(), null, 2) }))
  const [muet, setMuet] = useState(false)

  useEffect(() => {
    const avant = document.activeElement as HTMLElement | null
    const minuterie = setTimeout(() => setMuet(true), 8000)
    const recevoir = (e: MessageEvent) => {
      if (e.origin !== ORIGINE) return
      if (e.data?.type === 'signalement:pret') {
        clearTimeout(minuterie); setMuet(false)
        ;(e.source as Window | null)?.postMessage(message, ORIGINE)
      }
      if (e.data?.type === 'signalement:fermer') onClose()
    }
    const clavier = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('message', recevoir)
    window.addEventListener('keydown', clavier)
    return () => {
      clearTimeout(minuterie)
      window.removeEventListener('message', recevoir)
      window.removeEventListener('keydown', clavier)
      avant?.focus()
    }
  }, [message, onClose])

  return (
    <div style={voile} onMouseDown={e => { if (e.target === e.currentTarget) onClose() }}>
      <div style={fenetre} role="dialog" aria-modal="true" aria-label="Signalements">
        <iframe ref={cadre} src={url} title="Signalements" style={{ width: '100%', height: '100%', border: 0, display: 'block' }} onLoad={() => cadre.current?.focus()} />
        {muet && (
          <div style={repli} role="alert">
            <p>La fenêtre des signalements ne s'affiche pas ici.</p>
            <a href={url} target="_blank" rel="opener" onClick={onClose}>Ouvrir dans un onglet</a>
            <button type="button" onClick={onClose}>Fermer</button>
          </div>
        )}
      </div>
    </div>
  )
}

function Signaler({ application }: { application: string }) {
  const [ouvert, setOuvert] = useState(false)
  const [fermer] = useState(() => () => setOuvert(false))
  return (
    <>
      <style>{'@media print { .signaler-flottant { display: none !important } }'}</style>
      <button type="button" className="signaler-flottant" style={bouton} title="Signaler un problème ou proposer une amélioration" onClick={() => setOuvert(true)}>⚑ Signaler</button>
      {ouvert && <Fenetre url={`${GUICHET}/signaler.html?app=${encodeURIComponent(application)}`} onClose={fermer} />}
    </>
  )
}

/** « Signaler un problème » in an application that has no reports module of its own: a small button in a corner of every
 *  screen, on its own root so the application's screens stay untouched. Lot S2 of Le Guichet. */
export function monterSignaler(application: string) {
  const hote = document.createElement('div')
  document.body.appendChild(hote)
  createRoot(hote).render(<Signaler application={application} />)
}
