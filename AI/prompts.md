# Registro de prompts

Prompts enviados al agente (Claude Code), en orden cronológico y resumidos. Se omiten los datos personales.

## 1. Análisis y plan

> Revisemos el enunciado de la prueba y armemos un plan: qué hay que hacer, con qué tecnologías y con qué arquitectura. Quiero definirlo primero conmigo y luego pasar al desarrollo. Indícame también si se me escapa algún punto de la entrega.

**Resultado:** propuesta de stack y de orden de trabajo, y lista de los puntos obligatorios.

## 2. Decisiones de stack y base del backend

> Vamos con PostgreSQL, .NET 8 y Angular Material. Empecemos con el orden de trabajo: estructura del repositorio y primer commit, y luego seguimos avanzando.

**Resultado:** estructura `Backend/`, `Frontend/` y `AI/`; solución Onion con el modelo de datos, cifrado de datos sensibles, JWT con claims cifradas, funciones de PostgreSQL, login y CRUD de usuarios.

## 3. Convenciones del proyecto

> Definamos skills útiles para el proyecto, incluyendo una de diseño, y dejemos las instalaciones documentadas en el README general. En los prompts no debe aparecer información sensible.

**Resultado:** skills `backend-conventions` y `frontend-conventions`, y sección de requisitos en el README principal.

## 4. CRUD de clientes

> Con la base del backend, la seguridad, el login y los usuarios listos, vamos con el CRUD de clientes, sus validaciones y la carga masiva, manteniendo la misma arquitectura y convenciones. Validaciones:
>
> - La identificación debe tener mínimo 10 dígitos y máximo 13, solo números.
> - La dirección debe tener al menos 20 caracteres y máximo 100.
> - La referencia de la dirección debe tener al menos 20 caracteres y máximo 100.
> - El teléfono debe tener al menos 10 dígitos, solo números, y empezar con 09.

**Resultado:** servicio, validadores y controlador de clientes, con búsqueda por identificación, cambio de estado, carga masiva y eliminación lógica.

Reglas definidas en la revisión de la fase:

> - La identificación no se puede editar, ya que funciona prácticamente como la llave del cliente.
> - Una identificación eliminada no se puede volver a registrar: la eliminación es lógica, el cliente sigue existiendo y solo deja de ser visible.
> - Al eliminar un cliente hay que validar que no tenga un contrato vigente; si lo tiene, se muestra un mensaje indicando que primero debe cancelarlo.
> - El teléfono queda con un máximo de 13 dígitos, por el código de país y la estructura de la base de datos.

## 5. Cajas, asignación de cajeros y turnos

> Vamos con la fase de cajas: asignación de cajeros y turnos, con estas reglas:
>
> - Antes de atender un turno, el cajero debe abrir la caja; con eso se valida que los 2 cajeros asignados no puedan tener abierta la misma caja al mismo tiempo.
> - Solo el administrador crea y edita las cajas.
> - El gestor únicamente asigna los cajeros a las cajas y los turnos a esas cajas.
> - Un cajero solo puede atender una caja.
> - El contrato tendrá una duración de 1 año por defecto, ya que el enunciado no establece un tiempo.

**Resultado:** mantenimiento de cajas, asignación de cajeros (máximo 2 por caja), apertura y cierre de caja con control de concurrencia, y CRUD de turnos con la descripción generada por la función `fn_create_turn`.

Reglas definidas en la revisión de la fase:

> - Una caja no se puede cerrar si tiene un turno en atención; primero se debe finalizar el turno.
> - Si la caja no tiene cajeros asignados no se pueden crear turnos, ya que no hay quien los atienda.
> - Al cerrar sesión, la caja queda libre para que el otro cajero asignado pueda usarla.
> - La aprobación del administrador aplica a los usuarios creados por el gestor; las asignaciones de cajeros y turnos quedan activas de inmediato.
