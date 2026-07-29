import DOMPurify from 'dompurify'
import { useMemo } from 'react'

// Archived ticket/email bodies are untrusted HTML. Sanitize before rendering to
// avoid stored XSS, and force any surviving links to open safely in a new tab.
DOMPurify.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName === 'A' && node.hasAttribute('href')) {
    node.setAttribute('target', '_blank')
    node.setAttribute('rel', 'noopener noreferrer')
  }
})

export default function SafeHtml({ html, className }: { html: string | null; className?: string }) {
  const clean = useMemo(
    () => DOMPurify.sanitize(html ?? '', { USE_PROFILES: { html: true } }),
    [html],
  )
  return <div className={className} dangerouslySetInnerHTML={{ __html: clean }} />
}
