import type { WorkOrder, WorkOrderStatus } from '../api/types'
export function filterWorkOrders(items: WorkOrder[], query: string, status: WorkOrderStatus | 'All') {
  const text = query.trim().toLowerCase()
  return items.filter(item => (status === 'All' || item.status === status) && (!text || item.title.toLowerCase().includes(text)))
}
