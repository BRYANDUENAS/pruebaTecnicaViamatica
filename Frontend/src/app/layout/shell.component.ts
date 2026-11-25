import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, signal } from '@angular/core';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: false,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  /** En pantallas angostas el menú se superpone y empieza cerrado. */
  readonly compact = signal(false);
  readonly opened = signal(true);

  constructor(public auth: AuthService, breakpoints: BreakpointObserver) {
    breakpoints.observe('(max-width: 900px)').subscribe((state) => {
      this.compact.set(state.matches);
      this.opened.set(!state.matches);
    });
  }

  get initials(): string {
    const user = this.auth.user();
    return user ? (user.firstName[0] + user.lastName[0]).toUpperCase() : '';
  }

  closeIfCompact(): void {
    if (this.compact()) this.opened.set(false);
  }
}
