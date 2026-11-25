# Backend

API REST en .NET 8 para la gestión de caja, turnos, clientes y contratos.

## Arquitectura (Onion)

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `Viamatica.Domain` | Entidades, constantes y excepción de dominio | Nada |
| `Viamatica.Application` | Casos de uso, DTOs, validadores e interfaces | Domain |
| `Viamatica.Infrastructure` | EF Core, PostgreSQL, cifrado, JWT, correo y modelo de IA | Application |
| `Viamatica.Api` | Controladores, middleware, Swagger y configuración | Application, Infrastructure |

Los controladores solo delegan en un servicio de `Application`. Las reglas de formato están en validadores de FluentValidation y las de negocio lanzan `DomainException`, que el middleware convierte en una respuesta JSON uniforme:

```json
{ "message": "Los datos enviados no son válidos.", "errors": { "PhoneNumber": ["El teléfono debe empezar con 09..."] } }
```

## Ejecutar

Con Docker, desde la raíz del repositorio: `docker compose up --build`. Swagger queda en http://localhost:5080/swagger.

Sin Docker:

```bash
# Base de datos de desarrollo en el puerto 5544
docker run -d --name viamatica-dev-db -e POSTGRES_USER=viamatica -e POSTGRES_PASSWORD=viamatica_dev -e POSTGRES_DB=viamatica -p 5544:5432 postgres:16-alpine

cd Backend/src/Viamatica.Api
dotnet run --urls http://localhost:5080
```

Al iniciar se aplican las migraciones, se crea el administrador (`admin001` / `Admin1234`) y, si `Seed:DemoData` está activo, se cargan los datos de demostración.

## Seguridad

| Dato | Protección |
|---|---|
| Contraseña | Hash BCrypt |
| Correo e identificación del usuario | AES-256, cifrado determinista para poder buscar por el valor |
| Claims del JWT | Cada valor cifrado con AES; se descifran al validar el token |

- El login bloquea al usuario al tercer intento fallido.
- Los permisos se aplican por rol en los controladores y por alcance en los servicios (por ejemplo, el gestor solo ve los usuarios que creó).
- Las claves de cifrado y de firma se configuran en la sección `Security`; las del repositorio son solo para desarrollo.

## Endpoints

| Ruta | Rol | Contenido |
|---|---|---|
| `/api/auth` | Público y autenticado | Login, cierre de sesión, menú por rol y recuperación de contraseña |
| `/api/users` | Administrador, Gestor | CRUD, aprobación, estados y carga masiva |
| `/api/clients` | Cajero | CRUD, búsqueda por identificación, estado y carga masiva |
| `/api/cashes` | Según la acción | Mantenimiento (administrador), asignación de cajeros (gestor), apertura y cierre (cajero) |
| `/api/turns` | Según la acción | Asignación (gestor) y atención (cajero) |
| `/api/contracts` | Cajero | Contratación, cambio de servicio, forma de pago y cancelación |
| `/api/payments` | Cajero | Registro, corrección y anulación de pagos |
| `/api/services` | Lectura: todos. Escritura: administrador | Planes de internet y sus equipos |
| `/api/catalogs` | Autenticado | Catálogos de solo lectura |
| `/api/dashboard` | Según el indicador | Bienvenida e indicadores |
| `/api/assistant` | Autenticado | Asistente con IA (opcional) |

El detalle de cada endpoint está en Swagger y en la [colección de Postman](Postman/NetCaja.postman_collection.json).

## Reglas de validación

| Entidad | Reglas |
|---|---|
| Usuario | Nombre de 8 a 20 caracteres, letras y al menos un número, sin caracteres especiales y sin duplicados. Contraseña de 8 a 30 caracteres con un número y una mayúscula |
| Cliente | Identificación de 10 a 13 dígitos. Teléfono de 10 a 13 dígitos que empieza con 09. Dirección y referencia de 20 a 100 caracteres |
| Turno | Descripción de 2 letras mayúsculas y 4 números según el tipo de atención (`AC0001`, `PS0001`), generada en la base de datos |

## Funciones de base de datos

El script está en [Database/functions.sql](Database/functions.sql). La migración lo lee como recurso incrustado, así que el archivo y la base siempre coinciden.

| Función | Uso |
|---|---|
| `fn_create_turn` | Crea el turno y su atención pendiente. Genera la descripción con el prefijo del tipo de atención y un secuencial diario, con un bloqueo que evita números repetidos |
| `fn_cash_indicators` | Indicadores por caja en un rango de fechas, para el dashboard |

## Logs (Serilog)

Se escriben en consola y en `logs/viamatica-<fecha>.log`.

| Nivel | Qué registra |
|---|---|
| Information | Creaciones, actualizaciones, eliminaciones, inicios y cierres de sesión, con el usuario que los hizo |
| Warning | Validaciones fallidas, reglas de negocio e inicios de sesión rechazados |
| Error | Excepciones no controladas y fallos de servicios externos |

No se registran contraseñas, tokens, correos ni identificaciones.

## Migraciones

```bash
cd Backend
dotnet ef migrations add <Nombre> -p src/Viamatica.Infrastructure -s src/Viamatica.Api -o Persistence/Migrations
```

## Configuración

| Sección | Para qué |
|---|---|
| `ConnectionStrings:Default` | Conexión a PostgreSQL |
| `Security` | Claves de cifrado y de firma del JWT, y duración del token |
| `Seed` | Administrador inicial y datos de demostración |
| `Email` | SMTP para la recuperación por correo (opcional) |
| `Assistant` | Clave y modelo del asistente con IA (opcional) |

Cualquier valor se puede sobrescribir con variables de entorno, por ejemplo `Email__Password`.
