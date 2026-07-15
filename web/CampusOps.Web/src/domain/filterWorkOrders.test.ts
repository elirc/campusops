import { describe, expect, it } from 'vitest'
import { filterWorkOrders } from './filterWorkOrders'
import type { WorkOrder } from '../api/types'
const items: WorkOrder[] = [
  { id: '1', requestId: 'r1', title: 'Repair boiler', priority: 'Emergency', status: 'InProgress', blockedReason: null, completedAt: null },
  { id: '2', requestId: 'r2', title: 'Inspect roof', priority: 'Normal', status: 'Open', blockedReason: null, completedAt: null },
]
describe('filterWorkOrders', () => { it('combines query and status filters', () => expect(filterWorkOrders(items, 'boiler', 'InProgress')).toEqual([items[0]])); })
