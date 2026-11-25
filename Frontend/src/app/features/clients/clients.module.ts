import { Component, NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-clients',
  standalone: false,
  template: `
    <div class="page">
      <div class="page-header"><h1>Clientes</h1></div>
      <div class="empty">Pantalla en construcción.</div>
    </div>
  `,
})
export class ClientsComponent {}

@NgModule({
  declarations: [ClientsComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: ClientsComponent }])],
})
export class ClientsModule {}
