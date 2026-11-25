---
name: backend-conventions
description: Convenciones del backend .NET 8 de este proyecto (arquitectura Onion, validaciones, seguridad, logs). Usar antes de crear o modificar entidades, servicios, controladores o migraciones.
---

# Convenciones del backend

## Capas (Onion)

- `Domain`: entidades y constantes. Sin dependencias de otros proyectos ni de paquetes de infraestructura.
- `Application`: servicios de caso de uso, DTOs (records), validadores e interfaces (`IAppDbContext`, `IFieldCipher`, `ICurrentUser`). No referencia a `Infrastructure`.
- `Infrastructure`: EF Core, PostgreSQL, cifrado y JWT. Implementa las interfaces de `Application`.
- `Api`: controladores delgados que solo delegan en un servicio. Sin lógica de negocio.

## Reglas

- Toda regla de formato de entrada va en un validador de FluentValidation, con el mensaje en español.
- Las reglas de negocio lanzan `DomainException` con el código HTTP adecuado; el middleware la convierte en respuesta.
- Nunca se borra físicamente: las entidades implementan `ISoftDelete` y tienen filtro global de consulta.
- Datos sensibles de usuario (correo, identificación) pasan por `IFieldCipher`; la contraseña solo por `IPasswordHasher`.
- Las fechas se guardan en UTC. El día de negocio se calcula en la zona `America/Guayaquil`.
- Los códigos de catálogo se referencian por las constantes de `Domain/Common/Constants.cs`, no con literales.
- Autorización por rol con `[Authorize(Roles = ...)]` en el controlador, y alcance por usuario dentro del servicio.

## Logs (Serilog)

- `Information` en cada creación, actualización, eliminación e inicio o cierre de sesión, con el actor.
- `Warning` en validaciones fallidas, reglas de negocio e inicios de sesión rechazados.
- `Error` solo en excepciones no controladas.
- Nunca se registran contraseñas, tokens, correos ni identificaciones.

## Al terminar un cambio

- Compilar la solución sin advertencias nuevas.
- Probar el endpoint contra la base de desarrollo, incluyendo un caso inválido y uno sin permisos.
- Si cambia el modelo, generar la migración; si cambia una función, actualizar `Database/functions.sql`.
