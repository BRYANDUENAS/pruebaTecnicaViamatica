# Backend

API REST en .NET 8 para la gestión de caja, turnos, clientes y contratos.

> Estado: en desarrollo.

## Arquitectura prevista (Onion)

| Proyecto | Responsabilidad |
|---|---|
| `Domain` | Entidades y reglas del dominio, sin dependencias externas |
| `Application` | Casos de uso, DTOs, validaciones e interfaces |
| `Infrastructure` | EF Core, PostgreSQL, seguridad y servicios externos |
| `Api` | Controladores, middleware, Swagger y configuración |

Las instrucciones de ejecución, la colección de Postman y los scripts de las funciones de base de datos se documentan aquí al completarse.
