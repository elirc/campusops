export type WorkOrderStatus = 'Open' | 'Assigned' | 'InProgress' | 'Blocked' | 'Completed' | 'Cancelled'
export type Priority = 'Low' | 'Normal' | 'High' | 'Emergency'
export interface WorkOrder { id: string; requestId: string; title: string; priority: Priority; status: WorkOrderStatus; blockedReason: string | null; completedAt: string | null }
