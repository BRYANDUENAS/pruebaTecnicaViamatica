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
