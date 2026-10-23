import { FormEvent, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { API, SITE } from '../site'
import { useSeo } from '../seo'

async function post(path: string, body: object): Promise<{ ok: boolean; error?: string }> {
  try {
    const r = await fetch(`${API}/api/auth/${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    if (r.ok) return { ok: true }
    const j = await r.json().catch(() => ({}))
    return { ok: false, error: j.error ?? j.errors?.NewPassword?.[0] ?? 'Something went wrong. Please try again.' }
  } catch { return { ok: false, error: 'Could not reach the server. Please try again.' } }
}

/** Landing page for the link in the "confirm your email" message. */
export function VerifyEmail() {
  useSeo({ title: `Confirm your email – ${SITE.name}`, path: '/verify-email', noindex: true })
  const token = useSearchParams()[0].get('token')
  const [state, setState] = useState<'working' | 'done' | 'error'>(token ? 'working' : 'error')
  const [error, setError] = useState(token ? '' : 'This link is missing its code.')
  useEffect(() => {
    if (!token) return
    post('verify-email', { token }).then(r => { setState(r.ok ? 'done' : 'error'); setError(r.error ?? '') })
  }, [token])
  return (
    <div className="page">
      <h1>Confirm your email</h1>
      {state === 'working' && <p>Confirming…</p>}
      {state === 'done' && <p className="ok">✅ Your email is confirmed. You can close this page and return to the app.</p>}
      {state === 'error' && <><p className="err">{error}</p><p>Open the app and choose "Resend confirmation email" to get a new link.</p></>}
      <Link to="/">Back to {SITE.name}</Link>
    </div>
  )
}

/** Landing page for the link in the "reset your password" message. */
export function ResetPassword() {
  useSeo({ title: `Reset your password – ${SITE.name}`, path: '/reset-password', noindex: true })
  const token = useSearchParams()[0].get('token')
  const [password, setPassword] = useState('')
  const [again, setAgain] = useState('')
  const [state, setState] = useState<'idle' | 'busy' | 'done'>('idle')
  const [error, setError] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (password.length < 8) return setError('Please use at least 8 characters.')
    if (password !== again) return setError('The two passwords do not match.')
    setState('busy')
    const r = await post('reset-password', { token, newPassword: password })
    if (r.ok) setState('done'); else { setState('idle'); setError(r.error ?? '') }
  }

  if (!token) return <div className="page"><h1>Reset your password</h1><p className="err">This link is missing its code. Request a new one from the app.</p></div>
  return (
    <div className="page">
      <h1>Choose a new password</h1>
      {state === 'done' ? <p className="ok">✅ Your password has been changed. You've been signed out everywhere, so sign in again in the app.</p> : (
        <form className="reset" onSubmit={submit}>
          <label>New password<input type="password" autoComplete="new-password" minLength={8} required value={password} onChange={e => setPassword(e.target.value)} /></label>
          <label>Repeat it<input type="password" autoComplete="new-password" minLength={8} required value={again} onChange={e => setAgain(e.target.value)} /></label>
          {error && <p className="err">{error}</p>}
          <button className="btn" disabled={state === 'busy'}>{state === 'busy' ? 'Saving…' : 'Change password'}</button>
        </form>
      )}
    </div>
  )
}
