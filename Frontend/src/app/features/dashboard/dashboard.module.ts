import { Component, NgModule, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DashboardApi } from '../../core/api.services';
import { Count, OperationIndicators, UserIndicators } from '../../core/models';
import { SharedModule } from '../../shared/shared.module';

function today(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
}

/** Indicadores del administrador: usuarios, cajas, cajeros y gestores. */
@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  users: UserIndicators | null = null;
  operations: OperationIndicators | null = null;

  from = today();
  to = today();

  readonly cashierColumns = ['name', 'cash', 'attended'];
  readonly gestorColumns = ['name', 'assigned', 'attended', 'usersCreated'];

  constructor(private api: DashboardApi) {}

  ngOnInit(): void {
    this.api.users().subscribe((users) => (this.users = users));
    this.loadOperations();
  }

  get validRange(): boolean {
    return !!this.from && !!this.to && this.from <= this.to;
  }

  loadOperations(): void {
    if (!this.validRange) return;
    this.api.operations(this.from, this.to).subscribe((operations) => (this.operations = operations));
  }

  setToday(): void {
    this.from = this.to = today();
    this.loadOperations();
  }

  /** Ancho de una barra como porcentaje del valor más alto de su grupo. */
  width(value: number, max: number): string {
    return max > 0 ? `${(value / max) * 100}%` : '0%';
  }

  maxOf(counts: Count[]): number {
    return Math.max(0, ...counts.map((count) => count.total));
  }

  get maxTurns(): number {
    return Math.max(0, ...(this.operations?.cashes.map((cash) => cash.totalTurns) ?? []));
  }

  get totals() {
    const cashes = this.operations?.cashes ?? [];
    return {
      turns: cashes.reduce((sum, cash) => sum + cash.totalTurns, 0),
      attended: cashes.reduce((sum, cash) => sum + cash.attended, 0),
      pending: cashes.reduce((sum, cash) => sum + cash.pending, 0),
    };
  }

  /** Turnos que no están ni atendidos ni pendientes (cancelados). */
  other(cash: { totalTurns: number; attended: number; pending: number }): number {
    return cash.totalTurns - cash.attended - cash.pending;
  }
}

@NgModule({
  declarations: [DashboardComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: DashboardComponent }])],
})
export class DashboardModule {}
