export const ROLES = {
  admin: 'Administrador',
  gestor: 'Gestor',
  cajero: 'Cajero',
} as const;

export interface User {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  identification: string;
  rolId: number;
  rolName: string;
  statusId: string;
  statusDescription: string;
  creationDate: string;
  userCreate: number | null;
  sessionActive: boolean;
  lastLogin: string | null;
}

export interface MenuItem {
  menuItemId: number;
  label: string;
  route: string;
  icon: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
  menu: MenuItem[];
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]> | null;
}

export interface BulkRow {
  row: number;
  key: string;
  success: boolean;
  error: string | null;
}

export interface BulkResult {
  total: number;
  created: number;
  failed: number;
  rows: BulkRow[];
}

export interface Client {
  clientId: number;
  name: string;
  lastName: string;
  identification: string;
  email: string;
  phoneNumber: string;
  address: string;
  referenceAddress: string;
  active: boolean;
  creationDate: string;
}

export interface CashUser {
  userId: number;
  username: string;
  fullName: string;
}

export interface Cash {
  cashId: number;
  cashDescription: string;
  active: boolean;
  currentUserId: number | null;
  currentUsername: string | null;
  openedAt: string | null;
  users: CashUser[];
}

export interface CashierOption {
  userId: number;
  username: string;
  fullName: string;
  cashId: number | null;
  cashDescription: string | null;
}

export interface Turn {
  turnId: number;
  description: string;
  date: string;
  cashId: number;
  cashDescription: string;
  attentionTypeId: string;
  attentionTypeDescription: string;
  gestorUsername: string;
  statusId: number;
  statusDescription: string;
  clientId: number | null;
  clientName: string | null;
  cashierUsername: string | null;
  attendedAt: string | null;
}

export const TURN_STATUS = { pending: 1, inProgress: 2, attended: 3, cancelled: 4 } as const;

export interface AttentionType {
  attentionTypeId: string;
  description: string;
  prefix: string;
}

export interface InternetService {
  serviceId: number;
  serviceName: string;
  serviceDescription: string;
  speedMbps: number;
  price: number;
  devices: string[];
}

export interface MethodPayment {
  methodPaymentId: number;
  description: string;
}

export interface Contract {
  contractId: number;
  startDate: string;
  endDate: string;
  clientId: number;
  clientName: string;
  serviceId: number;
  serviceName: string;
  speedMbps: number;
  price: number;
  statusId: string;
  statusDescription: string;
  methodPaymentId: number;
  methodPaymentDescription: string;
  previousContractId: number | null;
}

export const OPEN_CONTRACT_STATUSES = ['VIG', 'REN'];

export interface Payment {
  paymentId: number;
  paymentDate: string;
  amount: number;
  contractId: number;
  serviceName: string;
  methodPaymentDescription: string;
}

export interface Welcome {
  user: User;
  attendedToday: number;
  assignedToday: number;
  pendingUsers: User[];
}

export interface Count {
  key: string;
  label: string;
  total: number;
}

export interface UserIndicators {
  total: number;
  sessionActive: number;
  sessionInactive: number;
  blocked: number;
  pendingApproval: number;
  byStatus: Count[];
  byRole: Count[];
}

export interface OperationIndicators {
  from: string;
  to: string;
  cashes: { cashId: number; cashDescription: string; totalTurns: number; attended: number; pending: number }[];
  cashiers: { userId: number; username: string; fullName: string; cashDescription: string | null; attended: number }[];
  gestores: { userId: number; username: string; fullName: string; assigned: number; attended: number; usersCreated: number }[];
}
