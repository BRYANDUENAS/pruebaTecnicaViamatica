import { Component, NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-users',
  standalone: false,
  template: `
    <div class="page">
      <div class="page-header"><h1>Usuarios</h1></div>
      <div class="empty">Pantalla en construcción.</div>
    </div>
  `,
})
export class UsersComponent {}

@NgModule({
  declarations: [UsersComponent],
  imports: [SharedModule, RouterModule.forChild([{ path: '', component: UsersComponent }])],
})
export class UsersModule {}
