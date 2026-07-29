📘 06 — Modelo de Base de Datos Detallado — MVP V1

Producto: Hunter CRM AI
Versión: 2.0
Base de datos: PostgreSQL
ORM: Entity Framework Core
Arquitectura: Modular Monolith
Multi-tenancy: Shared Database + OrganizationId

0. Registro de cambios respecto a la v1.0

La versión anterior de este documento era una copia literal del documento 05 (modelo conceptual), sin llegar a definir tablas físicas. Esta versión sí define la estructura física y, además, reconcilia el modelo con los documentos 17 a 23 (especificación funcional completa, modelo de datos V1, API REST, prospección, scoring y campañas), que ampliaron el dominio después de escrito el documento 05 sin que ese documento se actualizara.

Decisiones de consolidación tomadas en esta versión:

1. **Estados de `prospects` unificados.** Existían tres enumeraciones distintas entre los documentos 05, 17/18 y 23. Se adopta una única lista (sección 7.3), incorporando `NOT_INTERESTED` y `NO_RESPONSE` como estados terminales distintos — el documento 23 remarca explícitamente que "no responder" no equivale a "no interesado", y esa distinción se pierde si no hay un estado propio.
2. **`HUMAN_HANDOFF` deja de ser un estado de `prospects`.** El traspaso a un vendedor ya queda representado por la existencia de un `Lead` en estado `NEW`; mantenerlo también como estado del prospecto duplicaba la misma señal en dos lugares.
3. **`Interaction` (tabla genérica de timeline) se elimina.** Los documentos 17–19 introdujeron entidades específicas (`messages`, `message_responses`, `lead_activities`, `follow_ups`) que ya cubren cada tipo de evento con sus propios campos (clasificación IA, costo, etc.). Mantener además una tabla genérica de interacciones duplicaría el mismo hecho en dos tablas. El timeline de un prospecto o Lead se construye ahora como una consulta (UNION) sobre esas tablas específicas — ver sección 22.
4. **`messages` se separa en `messages` (salientes) y `message_responses` (entrantes).** Cada dirección tiene columnas propias que no aplican a la otra (`cost`/`delivered_at` en salientes; `classification`/`confidence`/`ai_model` en entrantes). Iba mezclado en una sola tabla con `direction` en la v1.0; se adopta el modelo separado de los documentos 18/19 porque evita columnas nulas por diseño y refleja mejor cómo n8n y la IA consumen cada uno.
5. **Se incorporan cinco tablas que no existían en la v1.0** pero son obligatorias según los documentos 13, 15, 18 y 19: `suppressions` (lista de exclusión / opt-out, sin la cual no se puede cumplir el checklist de seguridad del documento 13), `costs` (registro de costos por mensaje/IA/prospección, requerido por el documento 15 para calcular costo por Lead y por venta), `follow_ups`, `sales` e `import_batches` / `import_batch_records` (trazabilidad de importaciones masivas, documento 18).
6. **`prospect_contacts` corrige una inconsistencia interna del documento original:** la sección de índices pedía `UNIQUE(organization_id, channel, value)`, pero la tabla nunca declaraba la columna `organization_id`. Se agrega esa columna (denormalizada desde `prospects`) para que el índice sea implementable.
7. **Campos de segmentación y scoring** (`category`, `business_size`, `recurrence_potential`, `distance_category`, `data_quality`, `commercial_score`, `operational_priority`) se incorporan a `prospects` siguiendo los documentos 21 y 22, que no existían cuando se escribió la v1.0 de este documento.
8. **`campaign_recipients.status`** se amplía para incluir `DELIVERED`, `INTERESTED`, `NOT_INTERESTED`, `STOPPED` y `SKIPPED`, siguiendo el documento 18 y el workflow de n8n del documento 20, que ya asumían estos valores aunque el documento 06 original no los tuviera.

Ningún otro documento (07 a 23) necesita cambiar como consecuencia de esta actualización: los nombres de entidades y estados usados aquí ya son los que la mayoría de esos documentos daba por hecho.

1. Objetivo

Definir la estructura física de la base de datos del MVP V1.

El modelo debe permitir:

Gestionar múltiples organizaciones.
Administrar usuarios.
Almacenar y segmentar prospectos.
Evitar duplicados.
Importar prospectos en lote con trazabilidad por fila.
Ejecutar campañas respetando límites de envío.
Registrar mensajes salientes y respuestas entrantes.
Clasificar interés mediante IA.
Crear Leads y transferirlos a vendedores.
Registrar actividades comerciales y seguimientos.
Registrar ventas.
Bloquear contactos que solicitaron no ser contactados (opt-out).
Registrar costos por mensaje, IA y prospección.
Auditar acciones importantes.

2. Convenciones

Identificadores

Se utilizará:

UUID

Ejemplo:

Id UUID PRIMARY KEY

Motivos:

Evitar IDs secuenciales predecibles.
Facilitar generación distribuida.
Preparar futuras integraciones.
Facilitar sincronización entre sistemas.

Fechas

Se utilizará:

TIMESTAMPTZ

Todas las fechas se almacenarán en UTC.

La zona horaria se resolverá a nivel de organización.

Nombres

Las tablas utilizarán snake_case.

Ejemplo:

organizations
prospects
prospect_contacts
campaign_recipients

Las propiedades C# utilizarán PascalCase.

Ejemplo:

OrganizationId
CreatedAt
UpdatedAt

Montos

Los campos monetarios (`amount`, `cost`, `margin`) utilizarán:

NUMERIC(14,2)

junto a una columna `currency` (VARCHAR(3), ISO 4217). Nunca se almacenarán montos en tipos de punto flotante.

3. Tabla organizations

Representa una empresa dentro de Hunter.

organizations

id
name
legal_name
tax_id
email
phone
address
city
province
country
timezone
is_active
created_at
updated_at

Restricciones
PK:
id

UNIQUE parcial:
tax_id WHERE tax_id IS NOT NULL

El tax_id es nullable: Hunter podrá operar con organizaciones fuera de Argentina o sin CUIT cargado en el alta inicial.

4. Tabla users

users

id
organization_id
first_name
last_name
email
phone
password_hash
is_active
created_at
updated_at

Relaciones
users.organization_id
        ↓
organizations.id

Índices
UNIQUE:
organization_id + email

Esto permite que el mismo email pueda existir en diferentes organizaciones.

5. Tabla roles

roles

id
name

Valores:

OWNER
ADMIN
MANAGER
SELLER

6. Tabla user_roles

user_roles

user_id
role_id

PK compuesta:

user_id
role_id

Esto permite que un usuario pueda tener más de un rol.

7. Tabla prospects

Esta será la tabla más importante del sistema.

prospects

id
organization_id

business_name
contact_name

category
business_size
recurrence_potential
distance_category
data_quality

address
city
province
country
postal_code

latitude
longitude

website

commercial_score
operational_priority

status

is_deleted
deleted_at

created_at
updated_at
last_contacted_at

7.1 category

Reemplaza a `business_type` de la v1.0 para alinear con los documentos 17/18/21, que usan `category` de forma consistente.

DISTRIBUTOR
AUTO_PARTS_STORE
WORKSHOP
LUBRICENTRO
TIRE_SHOP
RESELLER
OTHER
UNKNOWN

7.2 business_size / recurrence_potential / distance_category / data_quality

business_size:
UNKNOWN, MICRO, SMALL, MEDIUM, LARGE

recurrence_potential:
UNKNOWN, LOW, MEDIUM, HIGH

distance_category:
UNKNOWN, LOCAL, NEAR, MEDIUM, FAR

data_quality:
A, B, C, D

Ninguno de estos campos debe usarse para descartar automáticamente un prospecto (documentos 21/22): son variables de segmentación y de scoring, no de exclusión.

7.3 status (unificado)

NEW
VALIDATED
READY
CONTACTED
RESPONDED
NOT_INTERESTED
NO_RESPONSE
LEAD
CUSTOMER
SUPPRESSED
INVALID

Notas:

`NOT_INTERESTED` y `NO_RESPONSE` son estados terminales distintos: el primero es una respuesta explícita negativa; el segundo es ausencia de respuesta tras los seguimientos definidos en el documento 23. No deben tratarse como equivalentes en reportes.

`LEAD` indica que existe un registro en `leads` asociado; no hay un estado `INTERESTED` ni `HUMAN_HANDOFF` propios en `prospects` — esa señal vive en `leads.status` y en `message_responses.classification`.

`SUPPRESSED` se aplica automáticamente cuando el contacto aparece en `suppressions` (sección 18), incluso si el prospecto reingresa por una fuente externa.

7.4 commercial_score / operational_priority

commercial_score: INTEGER (0-100), calculado según el documento 22 (tipo de cliente, recurrencia, contactabilidad, calidad del dato, tamaño, distancia).

operational_priority: PRIORITY_A, PRIORITY_B, PRIORITY_C, PRIORITY_D — deriva de `commercial_score` pero se almacena aparte porque puede recalcularse con reglas distintas por campaña (documento 22, sección 39).

8. Índices de prospects

INDEX:
organization_id

INDEX:
organization_id + status

INDEX:
organization_id + city

INDEX:
organization_id + category

INDEX:
organization_id + operational_priority

INDEX:
organization_id + commercial_score

Estos índices permiten consultas como:

Prospectos de Buenos Aires.
Prospectos listos para contactar.
Prospectos de tipo distribuidora.
Prospectos de prioridad A pendientes de campaña.

9. Tabla prospect_contacts

prospect_contacts

id
organization_id
prospect_id

channel
value

is_primary
is_verified
is_active

created_at
updated_at

`organization_id` se agrega en esta versión (denormalizado desde `prospects`) para poder implementar el índice único de deduplicación de la sección 10 sin necesidad de un JOIN.

Ejemplo:

Prospect
    │
    ├── WHATSAPP
    │   +5491112345678
    │
    ├── PHONE
    │   +541112345678
    │
    └── EMAIL
        ventas@empresa.com

10. Índices de deduplicación

La prioridad será:

organization_id
+
channel
+
value

Índice único:

UNIQUE (
    organization_id,
    channel,
    value
)

Esto evita que una organización tenga dos prospectos con el mismo contacto.

Esta regla debe revisarse cuando un mismo teléfono pueda pertenecer legítimamente a una empresa y a una sucursal (ver documento 06 sección 32 sobre sucursales, fuera de alcance en V1).

11. Tabla prospect_sources

prospect_sources

id
organization_id
prospect_id

source_type
external_id
source_url
collected_at

created_at

Se agrega `organization_id` (para filtrado multi-tenant directo) y `collected_at` (requerido por el documento 13, sección 20, para trazabilidad de origen y base legal del dato).

Tipos (`source_type`):

GOOGLE_PLACES
OPEN_STREET_MAP
DIRECTORY
PUBLIC_WEBSITE
CSV_IMPORT
MANUAL
EXTERNAL_API
OTHER

Índice:

UNIQUE (
    source_type,
    external_id
)

Cuando el proveedor entrega un identificador externo único.

12. Tabla import_batches

Representa una importación masiva (CSV/Excel/API). No existía en la v1.0; requerido por el documento 18 para poder mostrar el preview de importación (sección 11 del documento 17) antes de confirmar.

import_batches

id
organization_id
file_name
source_id
total_records
valid_records
duplicate_records
invalid_records
suppressed_records
status
created_by
created_at
completed_at

Estados (`status`):

PROCESSING
PREVIEW
CONFIRMED
COMPLETED
FAILED
CANCELLED

13. Tabla import_batch_records

Permite auditar cada fila importada individualmente.

import_batch_records

id
import_batch_id
row_number
raw_data
normalized_data
status
error_message
prospect_id
created_at

`raw_data` y `normalized_data` se almacenan como JSONB.

Estados (`status`):

VALID
DUPLICATE
INVALID
SUPPRESSED
IMPORTED

14. Tabla tags

tags

id
organization_id
name
color
created_at

Restricción:

UNIQUE (
    organization_id,
    name
)

15. Tabla prospect_tags

Relación muchos a muchos.

prospect_tags

prospect_id
tag_id

PK:

prospect_id
tag_id

16. Tabla message_templates

message_templates

id
organization_id
name
content
channel
version
is_active
created_by
created_at
updated_at

Se agrega `version` (documento 18): cada edición relevante de una plantilla activa crea una nueva fila con la misma `organization_id + name` e incrementa `version`, en lugar de sobrescribir el contenido histórico usado por campañas ya enviadas.

Variables soportadas en `content`:

{{business_name}}
{{city}}
{{category}}
{{sender_name}}

La plantilla no debe contener lógica comercial; la resolución de variables ocurre en el sistema (n8n al momento de render, documento 20 sección 9).

17. Tabla campaigns

campaigns

id
organization_id
name
description
status
channel
message_template_id
max_messages
messages_per_minute
messages_per_hour
messages_per_day
start_date
end_date
created_by
created_at
updated_at

Se agregan los límites de envío (`max_messages`, `messages_per_minute/hour/day`) requeridos por los documentos 18 y 19 para el control de velocidad y el Kill Switch por campaña.

Estados (`status`):

DRAFT
READY
RUNNING
PAUSED
COMPLETED
CANCELLED

18. Tabla campaign_recipients

Entidad fundamental: representa la participación de un prospecto en una campaña.

campaign_recipients

id
campaign_id
prospect_id
status
attempts
last_attempt_at
first_message_id
last_message_id
created_at
updated_at

Restricción:

UNIQUE (
    campaign_id,
    prospect_id
)

Esto evita agregar dos veces el mismo prospecto a una misma campaña.

18.1 status (unificado)

PENDING
QUEUED
SENT
DELIVERED
RESPONDED
INTERESTED
NOT_INTERESTED
STOPPED
SKIPPED
FAILED
COMPLETED

Esta lista amplía la de la v1.0 (que solo tenía `PENDING, SENT, DELIVERED, RESPONDED, FAILED, SKIPPED`) para alinear con el workflow de n8n del documento 20 (que ya distingue `QUEUED` de `PENDING`) y con la clasificación de IA del documento 18 (`INTERESTED`/`NOT_INTERESTED`/`STOPPED` a nivel de destinatario, no solo de prospecto).

19. Tabla messages (salientes)

Representa cada mensaje enviado por el sistema.

messages

id
organization_id
prospect_id
campaign_id
campaign_recipient_id
template_id
channel
provider
content
external_message_id
status
sent_at
delivered_at
read_at
failed_at
cost
currency
created_at

Estados (`status`):

PENDING
SENT
DELIVERED
READ
FAILED
CANCELLED

20. Índices de messages

INDEX:
organization_id + prospect_id

INDEX:
campaign_id

INDEX:
UNIQUE external_message_id (cuando no sea null)

INDEX:
created_at

Esto permite reconstruir conversaciones, calcular costos por campaña y garantizar idempotencia frente a webhooks duplicados de confirmación de entrega.

21. Tabla message_responses (entrantes)

Representa cada respuesta recibida de un prospecto. Se separa de `messages` porque necesita columnas propias de clasificación por IA que no tienen sentido en un mensaje saliente.

message_responses

id
organization_id
prospect_id
campaign_id
message_id
content
received_at
classification
confidence
ai_model
ai_prompt_version
processed_at

`message_id` referencia el mensaje saliente al que responde, cuando puede determinarse.

`ai_model` y `ai_prompt_version` se agregan siguiendo el documento 18 (sección 27) para poder auditar qué modelo y qué versión de prompt generó cada clasificación.

21.1 classification

INTERESTED
NOT_INTERESTED
QUESTION
UNCLEAR
STOP

21.2 Regla de confianza

Se fija un único umbral para todo el sistema, resolviendo la discrepancia entre el documento 08/10/20 (`>= 0.80`) y el documento 13 (`>= 0.85`):

Confidence >= 0.80
    ↓
Clasificación se aplica automáticamente

Confidence < 0.80
    ↓
classification = UNCLEAR
    ↓
Revisión humana

22. Índices de message_responses

INDEX:
organization_id + prospect_id

INDEX:
campaign_id

INDEX:
classification

23. Timeline (vista, no tabla física)

La v1.0 de este documento definía una tabla genérica `interactions` para reconstruir la línea de tiempo de un prospecto. Esta versión la elimina como tabla y la reemplaza por una consulta (vista o UNION en la capa de Application) sobre las tablas específicas, evitando duplicar el mismo evento en dos lugares:

messages            (mensajes enviados)
    UNION ALL
message_responses   (respuestas recibidas, con clasificación)
    UNION ALL
lead_activities      (acciones humanas sobre un Lead)
    UNION ALL
follow_ups           (seguimientos programados/completados)
    UNION ALL
audit_logs           (cambios de estado relevantes, cuando corresponda)

ORDER BY created_at

Cada fuente ya tiene su propio timestamp y su propio tipo; la vista solo necesita proyectar un `event_type`, `occurred_at` y una referencia al `prospect_id` / `lead_id`.

24. Tabla leads

Representa una oportunidad comercial que requiere intervención humana.

leads

id
organization_id
prospect_id
campaign_id
assigned_to_user_id
status
priority
lost_reason
created_at
updated_at
first_response_at
last_activity_at
closed_at

24.1 status

NEW
IN_PROGRESS
WON
LOST

24.2 priority

HIGH
MEDIUM
LOW

24.3 lost_reason (solo si status = LOST)

PRICE
NO_STOCK
EXISTING_SUPPLIER
NO_RESPONSE
NOT_INTERESTED
LOGISTICS
COMMERCIAL_CONDITIONS
OTHER

25. Índices de leads

INDEX:
organization_id + status

INDEX:
organization_id + assigned_to_user_id

INDEX:
organization_id + created_at

26. Tabla lead_activities

Registra las acciones realizadas por el vendedor sobre un Lead.

lead_activities

id
lead_id
user_id
type
description
created_at

Tipos (`type`):

CONTACT
CALL
WHATSAPP
QUOTE
NOTE
FOLLOW_UP

27. Tabla follow_ups

Representa una tarea de seguimiento futura sobre un Lead. No existía en la v1.0; requerida por los documentos 14, 17 y 18.

follow_ups

id
lead_id
user_id
scheduled_at
status
notes
completed_at
created_at

Estados (`status`):

PENDING
COMPLETED
CANCELLED

28. Tabla sales

Representa una venta cerrada. No existía en la v1.0; requerida por los documentos 14, 15, 17, 18 y 23 para poder calcular costo por venta, ticket promedio y ROI.

sales

id
organization_id
lead_id
campaign_id
prospect_id
seller_id
amount
currency
margin
product_category
status
date
created_at

Estados (`status`):

WON
CANCELLED

Se agrega `status` propio (además de `leads.status = WON`) para poder representar una venta que fue ganada y luego cancelada sin perder el registro histórico del Lead.

29. Tabla suppressions

Lista global de exclusión / opt-out. No existía en la v1.0 pese a ser un requisito no negociable del documento 13 ("ningún contacto que pida no ser contactado debe volver a serlo") y estar completamente especificada en los documentos 17, 18 y 19.

suppressions

id
organization_id
contact
contact_type
reason
source
created_at

Restricción:

UNIQUE (
    organization_id,
    contact
)

29.1 contact_type

PHONE
WHATSAPP
EMAIL

29.2 reason

USER_REQUESTED
INVALID_NUMBER
BLOCKED
MANUAL
OTHER

29.3 Regla de uso

Antes de cualquier envío (creación de `campaign_recipients` y antes de cada intento de `messages`), debe verificarse:

prospect_contacts.value
    ↓
EXISTS IN suppressions (organization_id, contact)?
    │
   SI → no crear/enviar, campaign_recipients.status = STOPPED
   NO → continuar

30. Tabla costs

Registra los costos generados por el sistema (mensajería, IA, prospección, infraestructura). No existía en la v1.0; es un requisito explícito del documento 12 (sección 29, "Punto Crítico": Hunter debe incorporar Cost Tracking desde el día 1) y del documento 15.

costs

id
organization_id
campaign_id
type
provider
reference_id
amount
currency
date
created_at

30.1 type

MESSAGING
AI
PROSPECTING
INFRASTRUCTURE
OTHER

31. Tabla organization_settings

Configuraciones específicas de cada empresa.

organization_settings

id
organization_id
key
value
created_at
updated_at

Restricción:

UNIQUE (
    organization_id,
    key
)

Para configuraciones estructuradas se recomienda almacenar `value` como JSONB en lugar de forzar todo a texto plano.

32. Tabla audit_logs

Registra acciones importantes.

audit_logs

id
organization_id
user_id
entity_type
entity_id
action
old_value
new_value
ip_address
user_agent
created_at

`old_value` y `new_value` se almacenan como JSONB. Se agregan `ip_address` y `user_agent`, requeridos por el checklist de seguridad del documento 13.

33. Enums

Se mantiene la recomendación de la v1.0: los estados se manejan como enums de aplicación en C# y se almacenan como `VARCHAR` en PostgreSQL (no como enum nativo), para no requerir una migración de esquema cada vez que se agregue un valor.

Ejemplo:

public enum ProspectStatus
{
    New,
    Validated,
    Ready,
    Contacted,
    Responded,
    NotInterested,
    NoResponse,
    Lead,
    Customer,
    Suppressed,
    Invalid
}

34. Diagrama ER simplificado

┌─────────────────┐
│ organizations   │
└────────┬────────┘
         │
         ├───────────────────────────┐
         │                           │
         ▼                           ▼
┌──────────────┐             ┌─────────────┐
│ prospects    │             │ users       │
└──────┬───────┘             └─────────────┘
       │
       ├───────────────┬───────────────┐
       │               │               │
       ▼               ▼               ▼
┌──────────────┐ ┌───────────────┐ ┌──────────────┐
│ contacts     │ │ sources       │ │ suppressions │
└──────────────┘ └───────────────┘ └──────────────┘
       │
       ▼
┌──────────────┐
│ campaigns    │
└──────┬───────┘
       │
       ▼
┌─────────────────────┐
│ campaign_recipients │
└──────────┬──────────┘
           │
           ├─────────────────┐
           ▼                 ▼
      ┌──────────┐   ┌──────────────────┐
      │ messages │   │ message_responses│
      └────┬─────┘   └─────────┬────────┘
           │                   │
           └─────────┬─────────┘
                     ▼
                 ┌────────┐
                 │ leads  │
                 └───┬────┘
                     │
          ┌──────────┼──────────┐
          ▼          ▼          ▼
┌─────────────┐ ┌──────────┐ ┌───────┐
│lead_activities│ │follow_ups│ │ sales │
└─────────────┘ └──────────┘ └───────┘

Costos (`costs`) y auditoría (`audit_logs`) se relacionan transversalmente con `organizations` y, opcionalmente, con `campaigns`; no se representan en el diagrama para no sobrecargarlo.

35. Flujo de persistencia

El flujo recomendado será:

Google Places / Fuente
        │
        ▼
       n8n
        │
        ▼
POST /api/imports  (o /api/prospects)
        │
        ▼
ProspectService
        │
        ├── Validar
        ├── Normalizar
        ├── Deduplicar (prospect_contacts UNIQUE)
        ├── Segmentar y puntuar (commercial_score / operational_priority)
        │
        ▼
PostgreSQL

Para campañas:

Campaign
    │
    ▼
CampaignRecipient
    │
    ▼
Message
    │
    ▼
External Provider

Para respuestas:

External Provider
        │
        ▼
Webhook
        │
        ▼
n8n
        │
        ▼
ASP.NET API
        │
        ├── Guardar MessageResponse
        ├── Clasificar con IA
        ├── Actualizar CampaignRecipient
        │
        ▼
¿INTERESTED?
        │
   ┌────┴────┐
   SI         NO
   │           │
   ▼           ▼
 Lead     NOT_INTERESTED / UNCLEAR

36. Índices críticos del MVP

Los índices prioritarios serán:

prospects
├── organization_id
├── organization_id + status
├── organization_id + city
├── organization_id + category
└── organization_id + operational_priority

prospect_contacts
└── UNIQUE organization_id + channel + value

campaign_recipients
└── UNIQUE campaign_id + prospect_id

messages
├── organization_id + prospect_id
└── UNIQUE external_message_id (parcial, WHERE NOT NULL)

message_responses
├── organization_id + prospect_id
└── classification

leads
├── organization_id + status
└── organization_id + assigned_to_user_id

suppressions
└── UNIQUE organization_id + contact

No se deben crear índices indiscriminadamente. Cada índice tiene un costo de almacenamiento y de escritura.

37. Estrategia Multi-Tenant

Todas las consultas tenant-aware deberán filtrar automáticamente por:

OrganizationId

Ejemplo conceptual:

modelBuilder.Entity<Prospect>()
    .HasQueryFilter(x =>
        x.OrganizationId == _currentOrganization.Id);

El mismo filtro global debe aplicarse a `prospect_contacts`, `campaign_recipients` (vía join con `campaigns`/`prospects`), `messages`, `message_responses`, `leads`, `suppressions` y `costs`.

El `OrganizationId` nunca deberá confiar únicamente en un valor enviado por el frontend. Debe derivarse del contexto autenticado (documento 19, sección 50).

38. Estrategia de eliminación

No se recomienda eliminar físicamente:

Prospects.
Leads.
Campaigns.
Messages / MessageResponses.
Sales.
AuditLogs.

Para las entidades de negocio (`Prospect`, `Campaign`, `Lead`, `MessageTemplate`) se utilizará:

IsDeleted
DeletedAt

Las entidades históricas (`Message`, `MessageResponse`, `Sale`, `AuditLog`) no deberían eliminarse nunca; son esencialmente inmutables una vez creadas.

39. Decisión sobre Prospect y Customer

Para el MVP no se crea una entidad `Customer` independiente. El resultado comercial se maneja mediante `leads.status = WON` y el registro correspondiente en `sales`.

En una versión posterior:

Prospect
    ↓
Lead
    ↓
Customer

La entidad `Customer` podrá incorporar historial de compras, frecuencia, ticket promedio, última compra, recompra y condiciones comerciales (documento 15, sección 34-35). No forma parte de la V1.

40. Decisión sobre sucursales

Para el MVP, una empresa se trata como un único prospecto. En una versión posterior:

Organization
    │
    └── Business
            │
            ├── Branch
            ├── Branch
            └── Branch

No se implementa inicialmente. Esto es relevante para el índice único de `prospect_contacts` (sección 10): un mismo teléfono compartido entre casa central y sucursal generará, por ahora, un conflicto que deberá resolverse manualmente.

41. Decisión sobre contactos

El modelo permite múltiples contactos por prospecto (`prospect_contacts`).

En V1 solo es necesario:

channel
value
is_primary

Posteriormente se podrán agregar:

contact_name
contact_role
department

42. Estado final del modelo V1

CORE
├── organizations
├── users
├── roles
└── user_roles

PROSPECTING
├── prospects
├── prospect_contacts
├── prospect_sources
├── import_batches
├── import_batch_records
├── tags
└── prospect_tags

CAMPAIGNS
├── message_templates
├── campaigns
└── campaign_recipients

MESSAGING
├── messages
└── message_responses

CRM
├── leads
├── lead_activities
└── follow_ups

SALES
└── sales

COMPLIANCE
└── suppressions

FINANCE
└── costs

SYSTEM
├── organization_settings
└── audit_logs

43. Próximo paso

Con este modelo consolidado, el orden de implementación recomendado (documento 18, sección 52, sin cambios) sigue siendo:

FASE 1 → organizations, users, roles, user_roles
FASE 2 → prospects, prospect_sources, import_batches, import_batch_records
FASE 3 → campaigns, campaign_recipients, message_templates
FASE 4 → messages, message_responses
FASE 5 → leads, lead_activities, follow_ups
FASE 6 → sales
FASE 7 → suppressions, costs, audit_logs

La primera migración de EF Core debería contener únicamente las tablas de FASE 1 y FASE 2, siguiendo la misma lógica incremental que el resto del backlog técnico (documento 07).
