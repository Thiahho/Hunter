📘 19 — Especificación de API REST V1

Producto: DIFRANI | Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Stack: ASP.NET Core 8 + C# + EF Core + PostgreSQL
Objetivo: Definir el contrato entre Backend, Frontend, n8n e integraciones externas.

1. Objetivo

La API será el núcleo central de Hunter.

                    ┌───────────────┐
                    │    FRONTEND   │
                    │ React + TS     │
                    └───────┬───────┘
                            │
                            ▼
                    ┌───────────────┐
                    │   HUNTER API  │
                    │ ASP.NET Core  │
                    └───────┬───────┘
                            │
            ┌───────────────┼───────────────┐
            ▼               ▼               ▼
       PostgreSQL          n8n              IA
                            │
                            ▼
                      Proveedores

La API debe centralizar:

Autenticación
Autorización
Prospectos
Campañas
Mensajería
Respuestas
IA
Leads
Seguimientos
Ventas
Costos
Métricas
Auditoría
2. URL Base

Producción:

https://api.huntercrm.com/api/v1

Desarrollo:

https://localhost:5001/api/v1

La versión debe formar parte de la URL:

/api/v1

Esto permitirá evolucionar posteriormente:

/api/v2

sin romper integraciones existentes.

3. Formato General

Todas las respuestas deberán utilizar JSON.

Éxito
{
  "success": true,
  "data": {},
  "message": null
}
Error
{
  "success": false,
  "data": null,
  "message": "El recurso no existe",
  "errors": []
}
4. Autenticación

La autenticación utilizará:

JWT Bearer Token

Flujo:

Login
  ↓
API
  ↓
JWT
  ↓
Frontend
  ↓
Authorization: Bearer TOKEN

Header:

Authorization: Bearer {token}
5. Endpoint — Login
POST /auth/login
Request
{
  "email": "usuario@difrani.com",
  "password": "********"
}
Response
{
  "success": true,
  "data": {
    "accessToken": "JWT_TOKEN",
    "expiresAt": "2026-07-27T23:59:59Z",
    "user": {
      "id": "uuid",
      "firstName": "Juan",
      "lastName": "Perez",
      "email": "usuario@difrani.com",
      "organizationId": "uuid",
      "roles": [
        "ADMIN"
      ]
    }
  }
}
6. Endpoint — Usuario Actual
GET /auth/me

Devuelve la información del usuario autenticado.

7. Endpoint — Refresh Token

Opcional para V1:

POST /auth/refresh

Permitirá renovar la sesión sin volver a solicitar credenciales.

8. Organizations
Obtener organización actual
GET /organizations/current
Actualizar organización
PUT /organizations/current
Request
{
  "name": "Difrani",
  "phone": "+54...",
  "email": "ventas@difrani.com",
  "address": "..."
}

Solo:

ADMIN

podrá modificar información de la organización.

9. Users
Listar usuarios
GET /users

Parámetros:

?page=1
&pageSize=20
&search=juan
&role=SELLER
&isActive=true
Obtener usuario
GET /users/{id}
Crear usuario
POST /users
Request
{
  "firstName": "Juan",
  "lastName": "Perez",
  "email": "juan@difrani.com",
  "phone": "+54...",
  "role": "SELLER"
}
Actualizar usuario
PUT /users/{id}
Activar / Desactivar
PATCH /users/{id}/status
10. Prospectos
Listar prospectos
GET /prospects

Filtros:

?page=1
&pageSize=50
&search=
&category=
&businessSize=
&city=
&province=
&status=
&source=
Obtener prospecto
GET /prospects/{id}

Debe devolver:

Información del prospecto
Campañas
Mensajes
Respuestas
Leads
Ventas
Crear prospecto
POST /prospects
Request
{
  "name": "Juan",
  "businessName": "Repuestos Oeste",
  "phone": "+549...",
  "whatsapp": "+549...",
  "email": "contacto@empresa.com",
  "address": "Av. ...",
  "city": "Moreno",
  "province": "Buenos Aires",
  "category": "AUTO_PARTS_STORE",
  "businessSize": "SMALL",
  "recurrencePotential": "MEDIUM"
}
Actualizar prospecto
PUT /prospects/{id}
Eliminar prospecto
DELETE /prospects/{id}

En V1 deberá realizarse:

Soft Delete
11. Validación de Prospecto
POST /prospects/{id}/validate

Resultado:

{
  "success": true,
  "data": {
    "isValid": true,
    "issues": []
  }
}
12. Prospectos — Duplicados
GET /prospects/duplicates

Permite detectar posibles duplicados antes de iniciar campañas.

13. Fuentes
Listar fuentes
GET /prospect-sources
Crear fuente
POST /prospect-sources
14. Importaciones
Crear importación
POST /imports

Se enviará un archivo CSV.

Flujo:

Upload
 ↓
Parse
 ↓
Normalize
 ↓
Validate
 ↓
Deduplicate
 ↓
Preview
Obtener Preview
GET /imports/{id}/preview
Response
{
  "totalRecords": 1000,
  "validRecords": 850,
  "duplicateRecords": 100,
  "invalidRecords": 30,
  "suppressedRecords": 20
}
Confirmar importación
POST /imports/{id}/confirm
Cancelar importación
POST /imports/{id}/cancel
15. Campañas
Listar campañas
GET /campaigns

Filtros:

?page=1
&pageSize=20
&status=RUNNING
Obtener campaña
GET /campaigns/{id}
Crear campaña
POST /campaigns
Request
{
  "name": "Distribuidores Zona Oeste",
  "description": "Campaña comercial para distribuidores",
  "channel": "WHATSAPP",
  "templateId": "uuid",
  "maxMessages": 1000,
  "messagesPerMinute": 5,
  "messagesPerHour": 100,
  "messagesPerDay": 1000
}
16. Agregar Prospectos a Campaña
POST /campaigns/{id}/recipients

Se puede enviar:

{
  "prospectIds": [
    "uuid-1",
    "uuid-2",
    "uuid-3"
  ]
}
17. Agregar Segmento a Campaña

Para campañas masivas:

POST /campaigns/{id}/recipients/from-segment
Request
{
  "category": "AUTO_PARTS_STORE",
  "city": "Moreno",
  "businessSize": "SMALL"
}

La API deberá:

Filtrar
 ↓
Excluir Suppression
 ↓
Excluir duplicados
 ↓
Crear CampaignRecipients
18. Iniciar Campaña
POST /campaigns/{id}/start

Validaciones:

Campaña READY
Template activo
Canal configurado
Prospectos disponibles
Kill Switch desactivado
19. Pausar Campaña
POST /campaigns/{id}/pause
20. Cancelar Campaña
POST /campaigns/{id}/cancel
21. Kill Switch Global
POST /campaigns/kill-switch
Request
{
  "enabled": true,
  "reason": "Detención de emergencia"
}

Cuando:

enabled = true

todas las campañas activas deben detener el envío.

22. Templates
Listar
GET /templates
Obtener
GET /templates/{id}
Crear
POST /templates
Request
{
  "name": "Presentación Distribuidores",
  "channel": "WHATSAPP",
  "content": "Hola {{business_name}}, somos Difrani..."
}
Actualizar
PUT /templates/{id}
Activar / Desactivar
PATCH /templates/{id}/status
23. Mensajes
Listar mensajes
GET /messages

Filtros:

campaignId
prospectId
status
dateFrom
dateTo
Obtener mensaje
GET /messages/{id}
24. Envío de Mensaje

La API no debe ser necesariamente quien envía directamente.

La arquitectura recomendada es:

API
 ↓
Message Queue
 ↓
n8n
 ↓
Provider

Endpoint interno:

POST /internal/messages/queue

Este endpoint será utilizado por:

n8n

o un futuro worker.

25. Actualización de Estado

Proveedor:

SENT
DELIVERED
READ
FAILED

Webhook:

POST /webhooks/messaging/status

Payload:

{
  "externalMessageId": "provider-id",
  "status": "DELIVERED",
  "timestamp": "2026-07-27T15:00:00Z"
}
26. Recepción de Respuesta

Webhook:

POST /webhooks/messaging/inbound

Payload:

{
  "externalMessageId": "provider-id",
  "contact": "+549...",
  "content": "Sí, pasame información",
  "receivedAt": "2026-07-27T15:00:00Z"
}

Flujo:

Webhook
 ↓
Identificar Prospect
 ↓
Guardar Response
 ↓
Enviar a IA
 ↓
Clasificar
 ↓
Procesar resultado
27. Clasificación IA

Endpoint interno:

POST /internal/ai/classify-response
Request
{
  "responseId": "uuid",
  "content": "Sí, pasame información"
}
Response
{
  "classification": "INTERESTED",
  "confidence": 0.97
}
28. Procesamiento de Interés

Cuando:

Classification = INTERESTED

la API debe:

1. Actualizar CampaignRecipient
2. Actualizar Prospect
3. Crear Lead
4. Asignar Lead
5. Registrar actividad
6. Notificar vendedor
29. Endpoint — Procesar Respuesta
POST /internal/responses/{id}/process

Este endpoint puede ser invocado por n8n.

30. Leads
Listar
GET /leads

Filtros:

status
priority
assignedUserId
campaignId
dateFrom
dateTo
Obtener Lead
GET /leads/{id}

Debe devolver:

Prospecto
Campaña
Mensaje inicial
Respuesta
Clasificación IA
Historial
Vendedor
Actividades
Seguimientos
31. Asignar Lead
POST /leads/{id}/assign
Request
{
  "userId": "uuid"
}
32. Actualizar Lead
PUT /leads/{id}
33. Marcar Lead como Ganado
POST /leads/{id}/won
34. Marcar Lead como Perdido
POST /leads/{id}/lost
Request
{
  "lostReason": "PRICE",
  "notes": "El cliente consiguió mejor precio."
}
35. Actividades
Crear actividad
POST /leads/{id}/activities
Request
{
  "type": "WHATSAPP",
  "description": "Se envió cotización solicitada."
}
Listar actividades
GET /leads/{id}/activities
36. Seguimientos
Crear
POST /leads/{id}/followups
Request
{
  "scheduledAt": "2026-07-29T15:00:00Z",
  "notes": "Consultar si recibió la cotización."
}
Completar
POST /followups/{id}/complete
37. Ventas
Crear venta
POST /sales
Request
{
  "leadId": "uuid",
  "amount": 150000,
  "currency": "ARS",
  "margin": 30000,
  "productCategory": "SUSPENSION"
}
Listar ventas
GET /sales

Filtros:

sellerId
campaignId
dateFrom
dateTo
38. Suppression List
Agregar contacto
POST /suppressions
Request
{
  "contact": "+549...",
  "contactType": "WHATSAPP",
  "reason": "USER_REQUESTED"
}
Verificar contacto
GET /suppressions/check?contact=+549...
Response
{
  "suppressed": true
}
39. Webhook de Opt-Out

Cuando la IA detecta:

STOP

se ejecutará:

Response
 ↓
Classification STOP
 ↓
POST /suppressions
 ↓
Bloquear contacto
40. Costos
Registrar costo
POST /costs
Request
{
  "type": "MESSAGING",
  "provider": "PROVIDER_NAME",
  "referenceId": "message-id",
  "amount": 10,
  "currency": "ARS"
}
41. Métricas
Dashboard
GET /metrics/dashboard
Response
{
  "prospects": 10000,
  "messagesSent": 5000,
  "responses": 800,
  "interested": 200,
  "leads": 200,
  "sales": 50,
  "revenue": 10000000,
  "cost": 100000
}
42. Métricas de Campaña
GET /metrics/campaigns/{id}

Debe calcular:

Total Prospectos
Contactados
Entregados
Leídos
Respondidos
Interesados
Leads
Ventas
Ingresos
Costos
43. Métricas de Conversión

La API deberá calcular:

Response Rate
Interest Rate
Lead Conversion Rate
Sales Conversion Rate

Ejemplo:

1000 prospectos
    ↓
800 mensajes entregados
    ↓
100 respuestas
    ↓
30 interesados
    ↓
30 leads
    ↓
8 ventas
44. Métricas de Costos
Costo total
Costo por prospecto
Costo por mensaje
Costo por respuesta
Costo por Lead
Costo por venta
45. Auditoría
GET /audit-logs

Filtros:

userId
action
entityType
dateFrom
dateTo

Solo:

ADMIN
MANAGER
46. Paginación

Todas las colecciones utilizarán:

?page=1
&pageSize=50

Response:

{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "totalItems": 1000,
  "totalPages": 20
}
47. Ordenamiento

Formato:

?sortBy=createdAt
&sortDirection=desc

Valores:

asc
desc

La API debe validar los campos permitidos.

No aceptar directamente nombres de columnas enviados sin validación.

48. Búsqueda

Ejemplo:

GET /prospects?search=repuestos

La búsqueda podrá consultar:

Name
BusinessName
Phone
Email
City
49. Autorización
ADMIN
Todo
MANAGER
Campañas
Prospectos
Leads
Ventas
Métricas
Usuarios limitados
SELLER
Leads asignados
Actividades
Seguimientos
Ventas
50. Multi-Tenancy

Todas las solicitudes autenticadas tendrán:

OrganizationId

derivado del JWT.

Nunca debe aceptarse:

{
  "organizationId": "uuid"
}

desde el frontend para decidir el tenant.

El backend debe obtenerlo del usuario autenticado.

51. Excepción Multi-Tenant

Los endpoints internos utilizados por:

n8n
Workers
Webhooks

deberán autenticarse mediante:

API Key

o:

Service Account

Nunca deberán quedar públicos sin autenticación.

52. Integración API + n8n

Flujo recomendado:

                ASP.NET API
                    │
                    ▼
              Campaign Ready
                    │
                    ▼
                   n8n
                    │
                    ▼
              Get Recipients
                    │
                    ▼
             Check Suppression
                    │
                    ▼
             Queue Message
                    │
                    ▼
                Provider
                    │
                    ▼
                Webhook
                    │
                    ▼
                ASP.NET API
                    │
                    ▼
             Process Response
                    │
                    ▼
                    IA
                    │
                    ▼
                  Lead
53. Endpoints Internos

Estos endpoints no deben estar disponibles públicamente.

/internal/messages/queue
/internal/responses/{id}/process
/internal/ai/classify-response

Autenticación:

Service API Key
54. Webhooks

Endpoints públicos controlados:

POST /webhooks/messaging/inbound
POST /webhooks/messaging/status

Deben implementar:

Firma
Validación
Idempotencia
Logs
55. Idempotencia

Los Webhooks pueden llegar varias veces.

Por lo tanto:

ExternalMessageId

deberá ser único cuando corresponda.

Si llega dos veces:

Webhook
    ↓
¿Ya existe?
    │
    ├── Sí → Ignorar
    │
    └── No → Procesar

Esto evita:

Leads duplicados
Respuestas duplicadas
Ventas duplicadas
56. Manejo de Errores

Códigos principales:

400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
429 Too Many Requests
500 Internal Server Error
57. Error de Validación
{
  "success": false,
  "message": "Error de validación",
  "errors": [
    {
      "field": "phone",
      "message": "El teléfono es obligatorio."
    }
  ]
}
58. Error 409

Usar cuando exista conflicto.

Ejemplo:

Prospecto duplicado

Response:

{
  "success": false,
  "message": "El prospecto ya existe."
}
59. Rate Limit API

La API deberá limitar:

Login
Webhooks
Endpoints internos
Importaciones

Ejemplo:

100 requests/minuto

Los valores exactos se definirán durante implementación.

60. Logs

Registrar:

Request
Response
StatusCode
ExecutionTime
UserId
OrganizationId
CorrelationId

No registrar:

Passwords
JWT completos
API Keys
Información sensible innecesaria
61. Correlation ID

Cada request debe tener:

X-Correlation-Id

Esto permitirá seguir una operación completa:

Campaign
 ↓
n8n
 ↓
Message
 ↓
Webhook
 ↓
IA
 ↓
Lead
62. Arquitectura de Capas

Backend:

Hunter.Api
    ↓
Hunter.Application
    ↓
Hunter.Domain
    ↓
Hunter.Infrastructure
API

Controllers.

Application

Casos de uso.

Domain

Entidades y reglas.

Infrastructure

EF Core:

PostgreSQL
n8n
Messaging Providers
IA Providers
63. Ejemplo de Flujo de Creación de Lead
POST /webhooks/messaging/inbound
        │
        ▼
Webhook Controller
        │
        ▼
InboundMessageService
        │
        ▼
Find Prospect
        │
        ▼
Create MessageResponse
        │
        ▼
AI Classification
        │
        ▼
INTERESTED
        │
        ▼
LeadService
        │
        ├── Create Lead
        ├── Assign Seller
        ├── Create Activity
        └── Notify
64. Criterio de Aceptación API V1

La API estará lista cuando pueda:

✓ Autenticar usuarios
✓ Aislar organizaciones
✓ Crear prospectos
✓ Importar prospectos
✓ Detectar duplicados
✓ Crear campañas
✓ Agregar destinatarios
✓ Iniciar campañas
✓ Pausar campañas
✓ Detener campañas globalmente
✓ Gestionar plantillas
✓ Registrar mensajes
✓ Recibir Webhooks
✓ Procesar respuestas
✓ Clasificar con IA
✓ Crear Leads
✓ Asignar Leads
✓ Registrar actividades
✓ Crear seguimientos
✓ Registrar ventas
✓ Registrar costos
✓ Bloquear Opt-Out
✓ Generar métricas
✓ Registrar auditoría
65. Orden de Desarrollo

La API deberá desarrollarse en este orden:

FASE 1
Auth
Organization
Users
Roles
FASE 2
Prospects
Sources
Imports
FASE 3
Campaigns
Recipients
Templates
FASE 4
Messages
Webhooks
FASE 5
AI
Responses
Lead Creation
FASE 6
Leads
Activities
FollowUps
FASE 7
Sales
Costs
Metrics
Audit
66. Resultado

Con esta especificación, el backend tendrá un contrato claro para integrar:

React
      │
      ▼
ASP.NET Core API
      │
      ├── PostgreSQL
      │
      ├── n8n
      │
      ├── IA
      │
      └── Proveedor de Mensajería