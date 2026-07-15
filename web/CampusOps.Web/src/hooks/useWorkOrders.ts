import { useCallback, useEffect, useState } from 'react'
import { getWorkOrders } from '../api/client'
import type { WorkOrder } from '../api/types'
export function useWorkOrders() {
  const [items, setItems] = useState<WorkOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const load = useCallback(async (signal?: AbortSignal) => { setLoading(true); setError(null); try { setItems(await getWorkOrders(signal)) } catch (value) { if (!(value instanceof DOMException && value.name === 'AbortError')) setError(value instanceof Error ? value.message : 'Unable to load work orders') } finally { setLoading(false) } }, [])
  useEffect(() => { const controller = new AbortController(); void load(controller.signal); return () => controller.abort() }, [load])
  return { items, loading, error, retry: () => void load() }
}
