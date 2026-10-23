import { useEffect, useState } from 'react'
import { api, Stats } from '../api'

export default function Dashboard() {
  const [s, setS] = useState<Stats>()
  const [err, setErr] = useState('')
  useEffect(() => { api<Stats>('/admin/stats').then(setS).catch(e => setErr(e.message)) }, [])
  if (err) return <p className="err">{err}</p>
  if (!s) return <p>Loading…</p>
  const max = Math.max(1, ...s.signupsLast30Days.map(d => d.count))
  const tiles: [string, string | number][] = [
    ['Total users', s.totalUsers], ['Active (7d)', s.activeLast7Days], ['Paying users', s.payingUsers],
    ['Est. MRR', `$${s.estimatedMrr.toFixed(2)}`], ['Children', s.totalChildren], ['Active children (7d)', s.activeChildren7d], ['Shared children', s.sharedChildren], ['Logs (7d)', s.logsLast7Days], ['Waitlist', s.waitlist],
  ]
  return (
    <>
      <h2>Dashboard</h2>
      <div className="tiles">{tiles.map(([l, v]) => <div className="tile" key={l}><small>{l}</small><strong>{v}</strong></div>)}</div>
      <div className="panel">
        <h3>Sign-ups, last 30 days</h3>
        {s.signupsLast30Days.length === 0 ? <p className="muted">No sign-ups yet.</p> : (
          <div className="bars">{s.signupsLast30Days.map(d => (
            <div key={d.date} className="bar" title={`${d.date.slice(0, 10)}: ${d.count}`} style={{ height: `${(d.count / max) * 100}%` }} />
          ))}</div>
        )}
      </div>
      <div className="panel">
        <h3>What parents log, last 30 days</h3>
        {s.logsByType30d.length === 0 ? <p className="muted">No logs yet.</p> : (
          <table><tbody>{[...s.logsByType30d].sort((a, b) => b.count - a.count).map(t => <tr key={t.type}><td>{t.type}</td><td>{t.count}</td></tr>)}</tbody></table>
        )}
        <small className="muted">Aggregate counts only. Individual children's logs are deliberately not viewable here.</small>
      </div>
    </>
  )
}
