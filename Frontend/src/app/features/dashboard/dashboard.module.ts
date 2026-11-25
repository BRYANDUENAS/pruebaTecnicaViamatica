import { Component, NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  template: `
    <div class="page">
      <div class="page-header"><h1>Dashboard</h1></div>
      <div class="empty">Pantalla en construcción.</div>
    </div>
  `,
})
export class DashboardComponent {}

@NgModule({
  declarations: [DashboardComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: DashboardComponent }])],
})
export class DashboardModule {}
