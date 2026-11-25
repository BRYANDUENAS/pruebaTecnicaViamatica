import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiError } from './models';

/** Cuenta las peticiones en curso para mostrar la barra de progreso global. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private pending = 0;
  readonly active = signal(false);

  start(): void {
    this.pending++;
    this.active.set(true);
  }

  stop(): void {
    this.pending = Math.max(0, this.pending - 1);
    if (this.pending === 0) this.active.set(false);
  }
}

/** Mensajes de éxito y error con MatSnackBar. */
@Injectable({ providedIn: 'root' })
export class NotifyService {
  constructor(private snackBar: MatSnackBar) {}

  success(message: string): void {
    this.snackBar.open(message, 'Cerrar', { duration: 3500, panelClass: 'snack-success' });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Cerrar', { duration: 7000, panelClass: 'snack-error' });
  }

  /** Convierte la respuesta de error de la API en un texto legible. */
  static describe(error: HttpErrorResponse): string {
    if (error.status === 0) return 'No se pudo conectar con el servidor.';
    const body = error.error as ApiError | null;
    const details = body?.errors ? Object.values(body.errors).flat() : [];
    if (details.length) return details.join(' ');
    if (body?.message) return body.message;
    if (error.status === 403) return 'No tiene permisos para realizar esta acción.';
    return 'Ocurrió un error inesperado. Intente nuevamente.';
  }
}
