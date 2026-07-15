import { useMemo, useState } from 'react'
import type { WorkOrderStatus } from './api/types'
import { filterWorkOrders } from './domain/filterWorkOrders'
import { useWorkOrders } from './hooks/useWorkOrders'

const statuses: Array<WorkOrderStatus | 'All'> = ['All', 'Open', 'Assigned', 'InProgress', 'Blocked', 'Completed', 'Cancelled']
export function App() {
  const { items, loading, error, retry } = useWorkOrders()
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState<WorkOrderStatus | 'All'>('All')
  const visible = useMemo(() => filterWorkOrders(items, query, status), [items, query, status])
  const urgent = items.filter(item => item.priority === 'Emergency' || item.priority === 'High').length
  return <div className="shell">
    <header><div><p className="eyebrow">CampusOps / North Campus</p><h1>Work control</h1><p className="subtitle">A live view of the jobs keeping campus running.</p></div><div className="operator"><span>AM</span><p><strong>Alex Morgan</strong><small>Dispatcher</small></p></div></header>
    <section className="metrics" aria-label="Work order summary"><article><small>Total work</small><strong>{items.length}</strong></article><article><small>In progress</small><strong>{items.filter(x => x.status === 'InProgress').length}</strong></article><article className="urgent"><small>High attention</small><strong>{urgent}</strong></article></section>
    <main><div className="toolbar"><div><h2>Work orders</h2><p>{visible.length} matching jobs</p></div><label><span className="sr-only">Search work orders</span><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search title…" /></label><select aria-label="Filter by status" value={status} onChange={event => setStatus(event.target.value as WorkOrderStatus | 'All')}>{statuses.map(value => <option key={value}>{value}</option>)}</select></div>
      {loading && <p className="state">Loading work orders…</p>}{error && <div className="state error"><p>{error}</p><button onClick={retry}>Try again</button></div>}{!loading && !error && visible.length === 0 && <p className="state">No work orders match these filters.</p>}
      {!loading && !error && visible.length > 0 && <div className="table-wrap"><table><thead><tr><th>Work</th><th>Priority</th><th>Status</th><th>Completion</th></tr></thead><tbody>{visible.map(item => <tr key={item.id}><td><strong>{item.title}</strong><small>#{item.id.slice(0, 8)}</small></td><td><span className={`priority ${item.priority.toLowerCase()}`}>{item.priority}</span></td><td>{item.status}</td><td>{item.completedAt ? new Date(item.completedAt).toLocaleDateString() : '—'}</td></tr>)}</tbody></table></div>}
    </main>
  </div>
}
