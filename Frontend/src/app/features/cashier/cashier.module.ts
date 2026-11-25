import { Component, NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-cashier',
  standalone: false,
  template: `
    <div class="page">
      <div class="page-header"><h1>Procesos de caja</h1></div>
      <div class="empty">Pantalla en construcción.</div>
    </div>
  `,
})
export class CashierComponent {}

@NgModule({
  declarations: [CashierComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: CashierComponent }])],
})
export class CashierModule {}
