📗 Documento 02 — PRD (Product Requirements Document) del MVP

Este documento será la "biblia" del desarrollo. Cada vez que dudemos si una funcionalidad entra o no en la V1, la respuesta deberá estar aquí.

Objetivo del documento

Responder una única pregunta:

¿Qué debe hacer exactamente la Versión 1 antes de octubre?

Y, tan importante como eso:

¿Qué NO debe hacer?

Estructura propuesta
02 - PRD - MVP V1

1. Objetivo del MVP

2. Alcance

3. Objetivos de negocio

4. Usuarios del sistema

5. Roles

6. Casos de uso

7. Flujo funcional

8. Módulos del MVP

9. Requisitos funcionales

10. Requisitos no funcionales

11. Integraciones

12. Modelo de datos (alto nivel)

13. Estados del sistema

14. Métricas

15. Criterios de aceptación

16. Fuera de alcance (Out of Scope)

17. Riesgos

18. Plan de validación

19. Cronograma hasta Octubre
Lo que vamos a definir en este documento
Qué sí desarrolla la V1

Ejemplo:

✔ Prospect Factory

✔ Prospect Pool

✔ Campaign Engine

✔ Interest Detector

✔ Human Handoff

✔ Dashboard básico

Qué NO desarrolla

Esto es igual de importante.

Ejemplo:

❌ IA negociando

❌ IA cotizando

❌ IA cerrando ventas

❌ Hunter Mobile

❌ Sales Intelligence

❌ Lead Scoring Predictivo

❌ Automatizaciones complejas

De esta forma evitamos el scope creep (que el proyecto crezca sin control).

También propongo definir los hitos

En lugar de pensar en "terminar el sistema", trabajaremos por entregables.

Hito 1
Core
Proyecto ASP.NET
PostgreSQL
Autenticación
Multiempresa
Configuración
Hito 2
Prospect Factory
Descubrimiento
Normalización
Deduplicación
Prospect Pool
Hito 3
Campaign Engine
Crear campaña
Seleccionar prospectos
Registrar envíos
Hito 4
Interest Detector
Clasificación
Cambio de estado
Crear Lead
Hito 5
CRM
Bandeja
Estados
Seguimiento
Hito 6
Dashboard
Prospectos
Leads
Conversión
Campañas
Una decisión que me gustaría incorporar

Creo que desde el PRD deberíamos dividir todas las funcionalidades en tres categorías.

Must Have (Obligatorio)

Sin esto el MVP no existe.

Ejemplo:

Prospect Pool
Prospect Factory
Campañas
Leads
Usuarios
Should Have (Muy importante)

Aporta mucho valor, pero el MVP puede funcionar sin ello.

Ejemplo:

Dashboard
Filtros avanzados
Historial
Exportaciones
Could Have (Deseable)

Ideas interesantes para agregar si el tiempo lo permite.

Ejemplo:

Plantillas de mensajes.
Etiquetas.
Comentarios internos.
Favoritos.
Vista Kanban.
Importación masiva.
Lo que NO haremos todavía

También propongo crear una sección permanente llamada "Parking Lot".

Allí irán todas las ideas que aparezcan durante el desarrollo.

Por ejemplo:

Hunter Mobile.
OCR.
Audio.
GPS.
IA negociadora.
IA que aprende sola.
Seguimiento automático.
Integración ERP.
Marketplace.
App móvil.

La regla será simple:

Si una idea no ayuda a validar el MVP antes de octubre, va al Parking Lot.

Así no la perdemos, pero tampoco rompe el cronograma.

Mi propuesta para la gestión del proyecto

Creo que conviene trabajar con dos niveles de planificación.

Nivel 1 — Documentación (lo que estamos haciendo ahora)
Visión del Producto.
PRD.
Arquitectura.
Roadmap.
Nivel 2 — Implementación

Una vez terminado el PRD, descomponemos el trabajo en un backlog.

Ejemplo:

Epic: Prospect Factory

 ├── Historia 001
 │    Crear entidad Prospect
 │
 ├── Historia 002
 │    Crear servicio de deduplicación
 │
 ├── Historia 003
 │    Integrar Google Places
 │
 ├── Historia 004
 │    Normalizar direcciones
 │
 └── Historia 005
      Guardar Prospect

Con este enfoque, cada historia podrá estimarse, desarrollarse y probarse de forma independiente.