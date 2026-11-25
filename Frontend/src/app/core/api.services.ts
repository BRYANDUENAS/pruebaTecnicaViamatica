import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  AttentionType, BulkResult, Cash, CashierOption, Client, Contract, InternetService, MethodPayment,
  OperationIndicators, Payment, Turn, User, UserIndicators, Welcome,
} from './models';

/** Arma los parámetros de consulta omitiendo los valores vacíos. */
function query(values: Record<string, string | number | boolean | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== null && value !== undefined && value !== '') params = params.set(key, value);
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly url = '/api/users';
  constructor(private http: HttpClient) {}

  list(filters: { rolId?: number | null; statusId?: string | null } = {}) {
    return this.http.get<User[]>(this.url, { params: query(filters) });
  }
  create(body: unknown) { return this.http.post<User>(this.url, body); }
  bulk(rows: unknown[]) { return this.http.post<BulkResult>(`${this.url}/bulk`, rows); }
  update(id: number, body: unknown) { return this.http.put<User>(`${this.url}/${id}`, body); }
  changeStatus(id: number, statusId: string) { return this.http.patch<User>(`${this.url}/${id}/status`, { statusId }); }
  approve(id: number) { return this.http.post<User>(`${this.url}/${id}/approve`, {}); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class ClientsApi {
  private readonly url = '/api/clients';
  constructor(private http: HttpClient) {}

  list(filters: { search?: string | null; active?: boolean | null } = {}) {
    return this.http.get<Client[]>(this.url, { params: query(filters) });
  }
  byIdentification(identification: string) {
    return this.http.get<Client>(`${this.url}/by-identification/${identification}`);
  }
  create(body: unknown) { return this.http.post<Client>(this.url, body); }
  bulk(rows: unknown[]) { return this.http.post<BulkResult>(`${this.url}/bulk`, rows); }
  update(id: number, body: unknown) { return this.http.put<Client>(`${this.url}/${id}`, body); }
  changeStatus(id: number, active: boolean) { return this.http.patch<Client>(`${this.url}/${id}/status`, { active }); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class CashesApi {
  private readonly url = '/api/cashes';
  constructor(private http: HttpClient) {}

  list() { return this.http.get<Cash[]>(this.url); }
  create(body: unknown) { return this.http.post<Cash>(this.url, body); }
  update(id: number, body: unknown) { return this.http.put<Cash>(`${this.url}/${id}`, body); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
  cashiers() { return this.http.get<CashierOption[]>(`${this.url}/cashiers`); }
  assign(cashId: number, userId: number) { return this.http.post<Cash>(`${this.url}/${cashId}/users`, { userId }); }
  unassign(cashId: number, userId: number) { return this.http.delete<Cash>(`${this.url}/${cashId}/users/${userId}`); }
  mine() { return this.http.get<Cash | null>(`${this.url}/mine`); }
  open() { return this.http.post<Cash>(`${this.url}/mine/open`, {}); }
  close() { return this.http.post<Cash>(`${this.url}/mine/close`, {}); }
}

@Injectable({ providedIn: 'root' })
export class TurnsApi {
  private readonly url = '/api/turns';
  constructor(private http: HttpClient) {}

  list(filters: { date?: string | null; cashId?: number | null; statusId?: number | null } = {}) {
    return this.http.get<Turn[]>(this.url, { params: query(filters) });
  }
  create(cashId: number, attentionTypeId: string) { return this.http.post<Turn>(this.url, { cashId, attentionTypeId }); }
  move(id: number, cashId: number) { return this.http.put<Turn>(`${this.url}/${id}`, { cashId }); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
  start(id: number) { return this.http.post<Turn>(`${this.url}/${id}/start`, {}); }
  finish(id: number, clientId: number | null) { return this.http.post<Turn>(`${this.url}/${id}/finish`, { clientId }); }
}

@Injectable({ providedIn: 'root' })
export class ContractsApi {
  constructor(private http: HttpClient) {}

  byClient(clientId: number) { return this.http.get<Contract[]>('/api/contracts', { params: query({ clientId }) }); }
  create(clientId: number, serviceId: number, methodPaymentId: number) {
    return this.http.post<Contract>('/api/contracts', { clientId, serviceId, methodPaymentId });
  }
  changeService(id: number, serviceId: number) {
    return this.http.post<Contract>(`/api/contracts/${id}/change-service`, { serviceId });
  }
  changeMethodPayment(id: number, methodPaymentId: number) {
    return this.http.put<Contract>(`/api/contracts/${id}/method-payment`, { methodPaymentId });
  }
  cancel(id: number) { return this.http.post<Contract>(`/api/contracts/${id}/cancel`, {}); }

  payments(clientId: number) { return this.http.get<Payment[]>('/api/payments', { params: query({ clientId }) }); }
  pay(contractId: number, methodPaymentId: number | null) {
    return this.http.post<Payment>('/api/payments', { contractId, methodPaymentId });
  }
}

@Injectable({ providedIn: 'root' })
export class ServicesApi {
  private readonly url = '/api/services';
  constructor(private http: HttpClient) {}

  list() { return this.http.get<InternetService[]>(this.url); }
  create(body: unknown) { return this.http.post<InternetService>(this.url, body); }
  update(id: number, body: unknown) { return this.http.put<InternetService>(`${this.url}/${id}`, body); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class CatalogsApi {
  constructor(private http: HttpClient) {}

  attentionTypes() { return this.http.get<AttentionType[]>('/api/catalogs/attention-types'); }
  methodPayments() { return this.http.get<MethodPayment[]>('/api/catalogs/method-payments'); }
  services() { return this.http.get<InternetService[]>('/api/services'); }
}

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  constructor(private http: HttpClient) {}

  welcome() { return this.http.get<Welcome>('/api/dashboard/welcome'); }
  users() { return this.http.get<UserIndicators>('/api/dashboard/users'); }
  operations(from: string, to: string) {
    return this.http.get<OperationIndicators>('/api/dashboard/operations', { params: query({ from, to }) });
  }
}

export interface AssistantMessage {
  role: 'user' | 'assistant';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class AssistantApi {
  constructor(private http: HttpClient) {}

  info() { return this.http.get<{ enabled: boolean; suggestions: string[] }>('/api/assistant'); }
  chat(message: string, history: AssistantMessage[]) {
    return this.http.post<{ reply: string }>('/api/assistant/chat', { message, history });
  }
}
