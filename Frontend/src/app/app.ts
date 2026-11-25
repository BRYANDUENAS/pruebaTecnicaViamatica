import { Component } from '@angular/core';
import { LoadingService } from './core/ui.services';

@Component({
  selector: 'app-root',
  standalone: false,
  template: `
    <mat-progress-bar *ngIf="loading.active()" class="global-progress" mode="indeterminate"></mat-progress-bar>
    <router-outlet></router-outlet>
  `,
})
export class App {
  constructor(public loading: LoadingService) {}
}
