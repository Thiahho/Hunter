📘 17 — Especificación Funcional Completa V1

Producto: DIFRANI | Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Horizonte: Hasta octubre de 2026
Objetivo: Convertir la estrategia definida en una especificación funcional concreta para comenzar el desarrollo.

1. Objetivo del sistema

Hunter será una plataforma de prospección comercial automatizada que permitirá:

OBTENER PROSPECTOS
        ↓
VALIDAR DATOS
        ↓
SEGMENTAR
        ↓
CREAR CAMPAÑA
        ↓
CONTACTAR
        ↓
ANALIZAR RESPUESTAS
        ↓
DETECTAR INTERÉS
        ↓
CREAR LEAD
        ↓
DERIVAR A HUMANO
        ↓
CERRAR VENTA
        ↓
REGISTRAR RESULTADO
        ↓
MEDIR RENTABILIDAD

La V1 estará enfocada en generar la mayor cantidad posible de oportunidades comerciales, sin descartar automáticamente prospectos por tamaño, ubicación o recurrencia.

2. Alcance V1
Incluido
Usuarios
Organizaciones
Prospectos
Fuentes
Importación
Normalización
Deduplicación
Segmentación
Campañas
Plantillas
Mensajería
Respuestas
IA
Leads
Asignación
Seguimientos
Ventas
Opt-Out
Suppression List
Costos
Métricas
Dashboard
Notificaciones
Auditoría
3. Fuera de Alcance V1

Queda para versiones posteriores:

Predictive Lead Scoring
CRM avanzado
Facturación
ERP
Gestión de inventario
Cotización automática avanzada
IA autónoma de cierre
IA negociadora
Predicción de compra
Multi-touch attribution
Machine Learning propio
Automatización avanzada de seguimiento
4. Arquitectura Funcional
┌──────────────────────┐
│      FRONTEND        │
│      React + TS      │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│      ASP.NET API     │
│       Business       │
│       Logic          │
└──────────┬───────────┘
           │
      ┌────┴────┐
      ▼         ▼
 PostgreSQL    n8n
      │         │
      │         ▼
      │    External APIs
      │
      ▼
   Metrics
5. Módulo — Usuarios
Funciones
Crear usuario
Editar usuario
Activar usuario
Desactivar usuario
Asignar rol
Cambiar contraseña
Roles
ADMIN
MANAGER
SELLER
6. Módulo — Organizaciones

Cada empresa será una organización.

Ejemplo:

Organization:
Difrani

Posteriormente:

Organization:
Tauro Parts

Cada organización tendrá sus propios:

Prospectos
Campañas
Leads
Ventas
Usuarios
Configuraciones
Métricas
7. Módulo — Prospectos
Datos mínimos
Id
OrganizationId
Name
BusinessName
Phone
WhatsApp
Email
Address
City
Province
Country
Latitude
Longitude
Category
BusinessSize
LocationType
RecurrencePotential
Source
SourceUrl
ExternalId
Status
CreatedAt
UpdatedAt
8. Estados del Prospecto
NEW
VALIDATED
READY
CONTACTED
RESPONDED
LEAD
CUSTOMER
SUPPRESSED
INVALID
9. Módulo — Fuentes

Cada prospecto debe indicar su origen.

Ejemplos:

GOOGLE_PLACES
MANUAL
CSV
EXTERNAL_API
PUBLIC_DIRECTORY
OTHER

Datos:

SourceType
SourceUrl
ExternalId
CollectedAt
10. Módulo — Importación

El sistema permitirá importar:

CSV
Excel
API
Carga manual

La importación deberá ejecutar:

Import
 ↓
Normalize
 ↓
Validate
 ↓
Deduplicate
 ↓
Preview
 ↓
Confirm
 ↓
Save
11. Preview de Importación

Antes de guardar:

Total registros: 1000

Válidos: 850
Duplicados: 100
Inválidos: 30
Suprimidos: 20

El usuario debe confirmar la importación.

12. Normalización

Los teléfonos deberán normalizarse.

Ejemplo:

011 15 1234-5678

convertirse internamente a una representación estándar.

El objetivo es evitar:

+5491112345678
01112345678
1512345678

como tres prospectos distintos.

13. Deduplicación

La deduplicación utilizará:

Phone
WhatsApp
Email
ExternalId

Prioridad recomendada:

ExternalId
    ↓
Phone
    ↓
WhatsApp
    ↓
Email
14. Módulo — Segmentación

Los prospectos podrán segmentarse por:

Categoría
Zona
Ciudad
Provincia
Tamaño
Recurrencia potencial
Fuente
Estado

Ejemplo:

Categoría:
Casa de Repuestos

Zona:
Zona Oeste
15. Módulo — Campañas

Una campaña representa una acción comercial.

Datos:

Id
OrganizationId
Name
Description
Segment
Channel
Template
Status
StartDate
EndDate
CreatedBy
16. Estados de Campaña
DRAFT
READY
RUNNING
PAUSED
COMPLETED
CANCELLED
17. Flujo de Campaña
DRAFT
 ↓
READY
 ↓
RUNNING
 ↓
PAUSED
 ↓
RUNNING
 ↓
COMPLETED

También:

RUNNING
 ↓
CANCELLED
18. Creación de Campaña

El usuario seleccionará:

Nombre
Segmento
Zona
Fuente
Canal
Plantilla
Cantidad máxima
Velocidad
Fecha de inicio

El sistema mostrará:

Prospectos disponibles
Prospectos válidos
Prospectos suprimidos
Duplicados
19. Módulo — Plantillas

Una plantilla representa un mensaje reutilizable.

Datos:

Id
OrganizationId
Name
Content
Channel
Variables
Active

Variables:

{{business_name}}
{{city}}
{{category}}
{{sender_name}}
20. Reglas de Plantillas

Las plantillas deben:

Ser editables
Tener versión
Ser activables/desactivables
Estar asociadas a campañas
21. Mensajería

Cada mensaje debe registrar:

Id
CampaignId
ProspectId
Channel
Content
Provider
ExternalMessageId
Status
SentAt
DeliveredAt
FailedAt
Cost
22. Estados del Mensaje
PENDING
SENT
DELIVERED
READ
FAILED
CANCELLED
23. Canal V1

La V1 debe diseñarse con una capa de abstracción:

MessagingProvider

Esto permitirá cambiar de proveedor posteriormente.

Ejemplo:

WhatsAppProvider
EmailProvider

El núcleo del sistema no debe depender directamente de un único proveedor.

24. Módulo — Respuestas

Cada respuesta debe registrar:

Id
ProspectId
CampaignId
MessageId
Content
ReceivedAt
Classification
Confidence
25. Clasificación IA

La IA clasificará respuestas en:

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP
26. Reglas de Clasificación
INTERESTED

Ejemplos:

Sí, pasame info.
Dale, mandame.
Me interesa.
Quiero saber más.
NOT_INTERESTED
No me interesa.
Gracias, pero no.
QUESTION
¿Qué productos tienen?
¿Trabajan con distribuidores?
¿Hacen envíos?
UNCLEAR
Después veo.
Ok.
Bueno.
STOP
No me contacten más.
Borrame.
No quiero recibir mensajes.
27. Human Handoff

Cuando la IA detecta interés:

INTERESTED
     ↓
Create Lead
     ↓
Assign Seller
     ↓
Notify
     ↓
Human Takes Over

El bot no debe continuar intentando cerrar la venta en V1.

28. Módulo — Leads

Datos:

Id
OrganizationId
ProspectId
CampaignId
AssignedUserId
Status
Priority
CreatedAt
FirstResponseAt
LastActivityAt
29. Estados de Lead
NEW
IN_PROGRESS
WON
LOST
30. Prioridad
HIGH
MEDIUM
LOW

Inicialmente podrá asignarse según reglas simples.

Ejemplo:

INTERESTED + pregunta concreta
→ HIGH
31. Asignación

V1:

Manual

o:

Round Robin

El sistema deberá guardar:

AssignedUserId
AssignedAt
32. Notificación de Lead

Cuando se cree un Lead:

🚨 NUEVO LEAD

Empresa:
Distribuidora X

Contacto:
+54...

Campaña:
Distribuidores Zona Oeste

Mensaje:
"Sí, pasame información."

Acciones:

Abrir WhatsApp
Ver Lead
Asignar
33. Módulo — Seguimientos

Cada Lead podrá tener actividades.

Tipos:

CONTACT
CALL
WHATSAPP
QUOTE
NOTE
FOLLOW_UP

Datos:

LeadId
UserId
Type
Description
Date
34. Próximo Seguimiento

Cada Lead puede tener:

NextFollowUpAt

Ejemplo:

Lead:
Distribuidora X

Próximo seguimiento:
28/07/2026
35. Módulo — Ventas

Una venta debe registrar:

Id
LeadId
CampaignId
SellerId
Amount
Currency
Date

Opcional:

Margin
ProductCategory
36. Venta Ganada
Lead
 ↓
WON
 ↓
Register Sale

El sistema debe asociar automáticamente:

Campaign
Lead
Prospect
Seller
37. Venta Perdida

Al marcar:

LOST

se debe solicitar:

LostReason

Opciones:

PRICE
NO_STOCK
EXISTING_SUPPLIER
NO_RESPONSE
NOT_INTERESTED
LOGISTICS
COMMERCIAL_CONDITIONS
OTHER
38. Módulo — Suppression List

Datos:

Id
OrganizationId
Contact
ContactType
Reason
Source
CreatedAt

Ejemplo:

Contact:
+5491112345678

Reason:
USER_REQUESTED
39. Regla de Supresión

Antes de enviar:

Prospect
 ↓
Suppression Check
 ↓
¿Bloqueado?

Si:

YES

No enviar.

40. Opt-Out Automático

Flujo:

Respuesta
 ↓
IA
 ↓
STOP
 ↓
Suppression List
 ↓
Bloquear

Debe ocurrir automáticamente.

41. Módulo — Costos

Cada operación debe poder registrar costos.

Tipos:

MESSAGING
AI
PROSPECTING
INFRASTRUCTURE
OTHER

Datos:

Type
Provider
Amount
Currency
Date
CampaignId
42. Costos de Mensajería

Debe poder registrarse:

Costo por mensaje
Costo por conversación
Costo total

La implementación exacta dependerá del proveedor y de su modelo de cobro vigente.

43. Módulo — Métricas

Métricas mínimas:

Prospectos
Mensajes
Entregados
Respuestas
Interesados
Leads
Cotizaciones
Ventas
Ingresos
Costos
44. Dashboard Principal

Debe mostrar:

Prospectos
Mensajes enviados
Respuestas
Leads
Ventas
Ingresos
Costo
Costo por Lead
Costo por Venta
45. Dashboard de Campaña
Prospectos
Contactados
Entregados
Respuestas
Interesados
Leads
Cotizaciones
Ventas
Ingresos
Costos
Conversión
46. Dashboard de Leads
Nuevos
Sin atender
En proceso
Ganados
Perdidos

Prioridad:

🔴 Sin atender
🟡 En proceso
🟢 Ganados
47. Dashboard de Costos
Mensajería
IA
Prospección
Infraestructura
Total

Y:

Costo / Prospecto
Costo / Mensaje
Costo / Lead
Costo / Venta
48. Módulo — Auditoría

Registrar:

LOGIN
CAMPAIGN_CREATED
CAMPAIGN_STARTED
CAMPAIGN_PAUSED
CAMPAIGN_CANCELLED
MESSAGE_SENT
LEAD_CREATED
LEAD_ASSIGNED
LEAD_UPDATED
SALE_CREATED
OPT_OUT
49. Kill Switch

El sistema deberá incluir:

STOP ALL CAMPAIGNS

Al activarse:

Todas las campañas RUNNING
        ↓
PAUSED
50. Rate Limiting

Cada campaña tendrá:

MaxMessages
MessagesPerMinute
MessagesPerHour
MessagesPerDay

La V1 deberá permitir modificar estos valores.

51. Cola de Mensajes

Los mensajes no deben enviarse directamente desde el frontend.

Flujo:

Campaign
 ↓
Recipients
 ↓
Queue
 ↓
Worker / n8n
 ↓
Provider
52. Arquitectura n8n

n8n será responsable de automatización.

Ejemplo:

Schedule
 ↓
Obtain Campaign
 ↓
Obtain Recipients
 ↓
Check Suppression
 ↓
Send Message
 ↓
Wait
 ↓
Next

Para respuestas:

Webhook
 ↓
Receive Message
 ↓
API
 ↓
IA Classification
 ↓
Create Lead
 ↓
Notify Human
53. API

Endpoints conceptuales:

/auth
/users
/organizations
/prospects
/sources
/imports
/campaigns
/templates
/messages
/responses
/leads
/followups
/sales
/suppressions
/costs
/metrics
/audit
54. API — Prospectos
GET    /api/prospects
GET    /api/prospects/{id}
POST   /api/prospects
PUT    /api/prospects/{id}
DELETE /api/prospects/{id}
55. API — Campañas
GET    /api/campaigns
GET    /api/campaigns/{id}
POST   /api/campaigns
PUT    /api/campaigns/{id}
POST   /api/campaigns/{id}/start
POST   /api/campaigns/{id}/pause
POST   /api/campaigns/{id}/cancel
56. API — Leads
GET    /api/leads
GET    /api/leads/{id}
PUT    /api/leads/{id}
POST   /api/leads/{id}/assign
POST   /api/leads/{id}/followup
POST   /api/leads/{id}/won
POST   /api/leads/{id}/lost
57. API — Ventas
POST /api/sales
GET  /api/sales
GET  /api/sales/{id}
58. API — Suppression
GET  /api/suppressions
POST /api/suppressions
DELETE /api/suppressions/{id}

La eliminación de una supresión deberá estar restringida.

59. API — Métricas
GET /api/metrics/dashboard
GET /api/metrics/campaigns/{id}
GET /api/metrics/leads
GET /api/metrics/costs
GET /api/metrics/sales
60. Frontend

Módulos principales:

Dashboard
Prospectos
Importaciones
Campañas
Plantillas
Leads
Seguimientos
Ventas
Métricas
Configuración
61. Dashboard

Pantalla inicial:

┌─────────────────────────────┐
│ Prospectos       10.000     │
│ Leads                200    │
│ Ventas                50    │
│ Ingresos       $10.000.000  │
│ Costo             $100.000  │
└─────────────────────────────┘
62. Pantalla Prospectos

Tabla:

Empresa
Contacto
Categoría
Zona
Fuente
Estado
Último contacto

Filtros:

Categoría
Zona
Estado
Fuente
Campaña
63. Pantalla Campañas

Mostrar:

Nombre
Segmento
Estado
Prospectos
Enviados
Respuestas
Leads
Ventas

Acciones:

Crear
Editar
Iniciar
Pausar
Cancelar
Ver métricas
64. Pantalla Lead

Debe mostrar:

Empresa
Contacto
Campaña
Mensaje original
Clasificación IA
Confidence
Vendedor
Estado
Historial
Seguimiento

Acción principal:

ABRIR WHATSAPP
65. Pantalla Venta

Formulario mínimo:

Monto
Moneda
Fecha
Margen opcional
Categoría producto
66. Pantalla Configuración

Configuraciones:

Empresa
Usuarios
Proveedores
Canales
Mensajería
IA
Rate Limits
Costos
67. Flujo Completo V1
                    PROSPECTO
                        │
                        ▼
                    IMPORTACIÓN
                        │
                        ▼
                    VALIDACIÓN
                        │
                        ▼
                   DEDUPLICACIÓN
                        │
                        ▼
                    SEGMENTACIÓN
                        │
                        ▼
                     CAMPAÑA
                        │
                        ▼
                      COLA
                        │
                        ▼
                    MENSAJERÍA
                        │
                        ▼
                     RESPUESTA
                        │
                        ▼
                    CLASIFICACIÓN
                        │
             ┌──────────┴──────────┐
             ▼                     ▼
         NO INTERÉS              INTERÉS
             │                     │
             ▼                     ▼
           CERRAR                 LEAD
                                   │
                                   ▼
                                HUMANO
                                   │
                                   ▼
                               COTIZACIÓN
                                   │
                          ┌────────┴────────┐
                          ▼                 ▼
                        VENTA            PÉRDIDA
                          │                 │
                          ▼                 ▼
                         WON               LOST
68. Requisitos Funcionales Prioritarios
P0 — Obligatorios
Autenticación
Multi-tenancy
Prospectos
Importación
Deduplicación
Campañas
Mensajería
Respuestas
IA
Leads
Human Handoff
Opt-Out
Suppression List
Métricas básicas
Kill Switch
P1 — Importantes
Seguimientos
Ventas
Costos
Dashboard avanzado
Auditoría
P2 — Posteriores
A/B Testing
Scoring
Automatización avanzada
Predicción
69. Criterio de Aceptación V1

La V1 estará funcional cuando pueda ejecutar exitosamente:

1. Importar 100 prospectos.
2. Validarlos.
3. Eliminar duplicados.
4. Crear campaña.
5. Seleccionar plantilla.
6. Crear cola.
7. Enviar mensajes.
8. Recibir respuestas.
9. Clasificar respuestas.
10. Detectar INTERESTED.
11. Crear Lead.
12. Notificar vendedor.
13. Registrar actividad humana.
14. Registrar venta.
15. Registrar costo.
16. Mostrar métricas.
17. Ejecutar STOP.
18. Respetar Suppression List.
70. Arquitectura de Desarrollo Recomendada

Para mantener el desarrollo ordenado:

Hunter
│
├── hunter-api
│   ├── Auth
│   ├── Organizations
│   ├── Users
│   ├── Prospects
│   ├── Campaigns
│   ├── Messages
│   ├── Leads
│   ├── Sales
│   ├── Costs
│   └── Metrics
│
├── hunter-web
│
├── hunter-workflows
│   └── n8n
│
└── hunter-infrastructure
    ├── Docker
    ├── PostgreSQL
    ├── Nginx
    └── Backup
71. Stack Sugerido
Backend:
ASP.NET Core 8
C#

ORM:
Entity Framework Core

Database:
PostgreSQL

Frontend:
React
TypeScript
Vite

Automation:
n8n

Authentication:
JWT

Deployment:
Docker
Nginx

AI:
LLM API

Monitoring:
Logs
Audit
Metrics
72. Principio Arquitectónico

La lógica crítica debe permanecer en:

ASP.NET API

n8n debe encargarse de:

Automatización
Orquestación
Integraciones
Triggers

No debe convertirse en el núcleo de negocio.

La arquitectura recomendada:

                 FRONTEND
                     │
                     ▼
               ASP.NET API
                     │
          ┌──────────┴──────────┐
          ▼                     ▼
      PostgreSQL               n8n
                                │
                    ┌───────────┴───────────┐
                    ▼                       ▼
               Messaging                  IA