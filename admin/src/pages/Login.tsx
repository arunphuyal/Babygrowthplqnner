import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, auth, User } from '../api'

export default function Login() {
  const nav = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    try {
      const r = await api<{ token: string; user: User }>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
      if (r.user.role !== 'Admin') { setError('This account is not an administrator.'); return }
      auth.set(r.token)
      nav('/')
    } catch (err) { setError((err as Error).message) }
  }

  return (
    <div className="login">
      <form className="panel" onSubmit={submit}>
        <h2>PottyTrain Admin</h2>
        <label>Email<input type="email" value={email} onChange={e => setEmail(e.target.value)} required autoFocus /></label>
        <label>Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} required /></label>
        {error && <p className="err">{error}</p>}
        <button className="btn">Sign in</button>
      </form>
    </div>
  )
}
