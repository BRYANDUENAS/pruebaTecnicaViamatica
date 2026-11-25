import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { AuthLayoutComponent } from './auth-layout.component';
import { LoginComponent } from './login.component';
import { RecoverComponent } from './recover.component';

const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: '', component: LoginComponent },
      { path: 'recuperar', component: RecoverComponent },
    ],
  },
];

@NgModule({
  declarations: [AuthLayoutComponent, LoginComponent, RecoverComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class AuthModule {}
