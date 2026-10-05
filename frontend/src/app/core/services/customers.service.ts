import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Customer, CustomerContact, UpsertCustomerRequest } from '../models/lookups';
import { PagedQuery, PagedResult } from '../models/paged-result';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly api = inject(ApiService);

  list(query?: PagedQuery): Observable<PagedResult<Customer>> {
    return this.api.get<PagedResult<Customer>>('/customers', query);
  }

  get(id: string): Observable<Customer> {
    return this.api.get<Customer>(`/customers/${id}`);
  }

  create(body: UpsertCustomerRequest): Observable<Customer> {
    return this.api.post<Customer>('/customers', body);
  }

  update(id: string, body: UpsertCustomerRequest): Observable<Customer> {
    return this.api.put<Customer>(`/customers/${id}`, body);
  }

  contacts(id: string): Observable<CustomerContact[]> {
    return this.api.get<CustomerContact[]>(`/customers/${id}/contacts`);
  }

  addContact(id: string, body: Partial<CustomerContact>): Observable<CustomerContact> {
    return this.api.post<CustomerContact>(`/customers/${id}/contacts`, body);
  }
}
