import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards';
import { ROLES } from './core/models';
import { ShellComponent } from './layout/shell.component';

// Cada ruta carga su módulo de forma diferida (lazy loading).
const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      {
        path: 'inicio',
        loadChildren: () => import('./features/home/home.module').then((m) => m.HomeModule),
      },
      {
        path: 'dashboard',
        canActivate: [roleGuard],
        data: { roles: [ROLES.admin] },
        loadChildren: () => import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule),
      },
      {
        path: 'usuarios',
        canActivate: [roleGuard],
        data: { roles: [ROLES.admin, ROLES.gestor] },
        loadChildren: () => import('./features/users/users.module').then((m) => m.UsersModule),
      },
      {
        path: 'mantenimiento',
        canActivate: [roleGuard],
        data: { roles: [ROLES.admin] },
        loadChildren: () => import('./features/maintenance/maintenance.module').then((m) => m.MaintenanceModule),
      },
      {
        path: 'turnos',
        canActivate: [roleGuard],
        data: { roles: [ROLES.gestor] },
        loadChildren: () => import('./features/turns/turns.module').then((m) => m.TurnsModule),
      },
      {
        path: 'caja',
        canActivate: [roleGuard],
        data: { roles: [ROLES.cajero] },
        loadChildren: () => import('./features/cashier/cashier.module').then((m) => m.CashierModule),
      },
      {
        path: 'clientes',
        canActivate: [roleGuard],
        data: { roles: [ROLES.cajero] },
        loadChildren: () => import('./features/clients/clients.module').then((m) => m.ClientsModule),
      },
    ],
  },
  { path: '**', redirectTo: 'inicio' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
