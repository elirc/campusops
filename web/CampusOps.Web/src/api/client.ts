import type { WorkOrder } from './types'

const apiBase = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'
export async function getWorkOrders(signal?: AbortSignal): Promise<WorkOrder[]> {
  const response = await fetch(`${apiBase}/api/v1/work-orders`, { signal, headers: { 'X-Demo-Role': 'Dispatcher', 'X-Demo-Campus': 'North' } })
  if (!response.ok) throw new Error(`CampusOps API returned ${response.status}`)
  return response.json() as Promise<WorkOrder[]>
}
