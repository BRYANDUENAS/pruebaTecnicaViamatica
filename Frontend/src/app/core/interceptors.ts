import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, finalize, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { LoadingService, NotifyService } from './ui.services';

/** Adjunta el JWT y las cabeceras comunes a todas las peticiones a la API. */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!request.url.startsWith('/api')) return next.handle(request);

    let headers = request.headers
      .set('Accept', 'application/json')
      .set('Accept-Language', 'es-EC')
      .set('X-Client', 'viamatica-web');
    const token = this.auth.token();
    if (token) headers = headers.set('Authorization', `Bearer ${token}`);

    return next.handle(request.clone({ headers }));
  }
}

/** Controla el indicador de carga y muestra los errores de todas las peticiones. */
@Injectable()
export class HttpStateInterceptor implements HttpInterceptor {
  constructor(private loading: LoadingService, private notify: NotifyService, private auth: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    this.loading.start();
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        const isLogin = request.url.includes('/auth/login');
        if (error.status === 401 && !isLogin) {
          this.notify.error('La sesión expiró. Inicie sesión nuevamente.');
          this.auth.clear();
        } else {
          this.notify.error(NotifyService.describe(error));
        }
        return throwError(() => error);
      }),
      finalize(() => this.loading.stop()),
    );
  }
}
