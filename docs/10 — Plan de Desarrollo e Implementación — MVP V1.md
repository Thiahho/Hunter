📘 10 — Plan de Desarrollo e Implementación — MVP V1

Producto: Hunter CRM AI
Versión: MVP V1
Objetivo: Tener una primera versión funcional y operativa antes de octubre de 2026, aprovechando la etapa inicial de costos reducidos y validando el modelo comercial antes de construir la V2.

1. Objetivo del desarrollo

El objetivo de la V1 no es construir un CRM completo.

El objetivo es validar este proceso:

CONSEGUIR PROSPECTOS
        ↓
CONTACTARLOS
        ↓
RECIBIR RESPUESTAS
        ↓
DETECTAR INTERÉS
        ↓
CREAR LEAD
        ↓
PASAR A HUMANO
        ↓
CERRAR VENTA

La métrica principal será:

¿Cuántas ventas reales genera el sistema?

No:

Cantidad de workflows.
Cantidad de funcionalidades.
Cantidad de prospectos almacenados.
2. Principio de desarrollo

La V1 se desarrollará bajo la estrategia:

Código propio
+
n8n
+
IA
+
Proveedores externos

Cada tecnología tendrá una responsabilidad concreta.

ASP.NET Core
→ Cerebro de negocio

PostgreSQL
→ Memoria

n8n
→ Orquestador

IA
→ Interpretación

Proveedor de mensajería
→ Comunicación

Humano
→ Venta
3. Qué desarrollar con código propio

Debe desarrollarse con ASP.NET Core:

Core
Organizaciones.
Usuarios.
Roles.
Autenticación.
Multi-tenancy.
Prospectos
Prospect.
Contact.
Sources.
Tags.
Deduplicación.
Campañas
Campaign.
Recipients.
Templates.
Estados.
CRM
Leads.
Activities.
Interactions.
Reglas
Estados.
Validaciones.
Idempotencia.
Autorización.
Métricas.
4. Qué resolver con n8n

n8n será utilizado para:

Consultar APIs externas.
Ejecutar tareas programadas.
Ejecutar campañas.
Procesar lotes.
Conectar proveedores.
Recibir webhooks.
Ejecutar IA.
Notificar vendedores.

No debe contener lógica comercial crítica.

5. Qué resolver con IA

La IA tendrá inicialmente una función muy específica:

Analizar respuesta
       ↓
Detectar intención
       ↓
Determinar interés

Ejemplo:

"Sí, pasame información"

Resultado:

{
  "intent": "INTERESTED",
  "confidence": 0.96
}

No se utilizará IA inicialmente para:

Cotizar.
Negociar.
Definir precios.
Responder preguntas complejas.
Cerrar ventas.
6. Sprint 0 — Setup
Objetivo

Preparar el entorno.

Tareas
[ ] Crear repositorio Git
[ ] Crear solución .NET
[ ] Configurar Clean Architecture
[ ] Configurar PostgreSQL
[ ] Configurar EF Core
[ ] Crear Docker Compose
[ ] Configurar Swagger
[ ] Configurar Serilog
[ ] Configurar variables de entorno
[ ] Configurar GitHub Actions
Resultado
API
+
PostgreSQL
+
Docker

funcionando localmente.

7. Sprint 1 — Core + Auth + Multi-Tenancy
Objetivo

Construir la base del sistema.

Tareas
[ ] Organization
[ ] User
[ ] Role
[ ] UserRole
[ ] JWT
[ ] Refresh Token
[ ] Login
[ ] Register
[ ] Authorization
[ ] Organization Context
[ ] Global Query Filters
Resultado

Un usuario puede:

Registrarse
    ↓
Crear organización
    ↓
Iniciar sesión
    ↓
Acceder únicamente a sus datos
8. Sprint 2 — Prospect Pool
Objetivo

Crear la base de datos comercial.

Tareas
[ ] Prospect
[ ] ProspectContact
[ ] ProspectSource
[ ] Tags
[ ] ProspectTags
[ ] CRUD
[ ] Búsqueda
[ ] Filtros
[ ] Paginación
[ ] Deduplicación
Resultado

El usuario puede administrar:

Prospectos
    ↓
Contactos
    ↓
Fuentes
    ↓
Etiquetas
9. Sprint 3 — Prospect Factory
Objetivo

Incorporar prospectos desde fuentes externas.

Primera fuente

Se recomienda comenzar con una sola fuente.

Por ejemplo:

Google Places

Flujo:

n8n
 ↓
Buscar empresas
 ↓
Normalizar
 ↓
POST /prospects/import
 ↓
API
 ↓
Deduplicación
 ↓
PostgreSQL
Tareas
[ ] Crear endpoint de importación
[ ] Crear DTO de importación
[ ] Crear servicio de normalización
[ ] Crear deduplicación
[ ] Crear registro de fuente
[ ] Crear workflow n8n
Resultado

Hunter comienza a generar su propia base de prospectos.

10. Sprint 4 — Campaign Engine
Objetivo

Poder seleccionar prospectos y preparar campañas.

Tareas
[ ] Campaign
[ ] CampaignRecipient
[ ] MessageTemplate
[ ] Crear campaña
[ ] Seleccionar prospectos
[ ] Preparar destinatarios
[ ] Estados
[ ] Pausar
[ ] Reanudar
[ ] Cancelar
Resultado
Prospect Pool
      ↓
Seleccionar
      ↓
Campaign
      ↓
Recipients
11. Sprint 5 — Messaging
Objetivo

Enviar el primer mensaje real.

Tareas
[ ] Message
[ ] Outbound Message
[ ] Inbound Message
[ ] ExternalMessageId
[ ] Idempotencia
[ ] Estado de envío
[ ] Webhook
Flujo
Campaign
    ↓
n8n
    ↓
Proveedor
    ↓
Prospecto
12. Sprint 6 — IA + Interest Detection
Objetivo

Detectar oportunidades comerciales.

Tareas
[ ] Clasificación
[ ] Intent
[ ] Confidence
[ ] INTERESTED
[ ] NOT_INTERESTED
[ ] QUESTION
[ ] UNCLEAR
[ ] STOP
Flujo
Respuesta
    ↓
n8n
    ↓
IA
    ↓
Clasificación
    ↓
ASP.NET
13. Sprint 7 — Leads + Human Handoff
Objetivo

Transferir la oportunidad a una persona.

Tareas
[ ] Crear Lead
[ ] Lead Status
[ ] Asignar vendedor
[ ] Lead Activities
[ ] Timeline
[ ] Notificación

Flujo:

INTERESTED
    ↓
Lead NEW
    ↓
Asignación
    ↓
Vendedor
    ↓
WhatsApp
    ↓
Cotización
    ↓
Venta
14. Sprint 8 — Automatización n8n
Objetivo

Conectar todos los componentes.

Workflows:

01 Prospect Discovery
02 Prospect Import
03 Campaign Preparation
04 Campaign Sending
05 Incoming Message
06 Interest Detection
07 Lead Creation
08 Human Handoff
09 Metrics
15. Sprint 9 — Testing
Tests críticos
Multi-Tenancy
Usuario A
NO puede acceder
a Organización B.
Deduplicación
Mismo teléfono
+
Misma organización
=
No duplicar.
Mensajes
Mismo ExternalMessageId
=
No duplicar.
IA
INTERESTED
+
Confidence >= 0.80
=
Lead.
Human Handoff
Lead creado
=
Vendedor notificado.
16. Sprint 10 — Deploy + Piloto
Objetivo

Poner el MVP en funcionamiento real.

Infraestructura:

VPS
│
├── ASP.NET API
├── PostgreSQL
├── n8n
└── Frontend

Configurar:

HTTPS
Backups
Logs
Variables de entorno
Monitoring
17. Fase de Piloto

No se recomienda comenzar directamente con:

1000 mensajes/día

Primero:

Día 1
10 prospectos

Día 2
20 prospectos

Día 3
50 prospectos

Día 4
100 prospectos

Analizar:

Respuestas.
Interés.
Errores.
Duplicados.
Calidad de prospectos.
Conversión.

Luego aumentar progresivamente.

18. Métricas del Piloto

El dashboard debe mostrar:

PROSPECTOS
──────────────
Encontrados
Válidos
Duplicados

CONTACTO
──────────────
Enviados
Entregados
Fallidos

RESPUESTA
──────────────
Respuestas
Tasa de respuesta

INTERÉS
──────────────
Interesados
Tasa de interés

VENTAS
──────────────
Leads
Cotizaciones
Ventas
Conversión

La métrica más importante:

VENTAS GENERADAS
19. Fórmula Comercial

El sistema debe permitir medir:

Prospectos
      ↓
Contactados
      ↓
Respuestas
      ↓
Interesados
      ↓
Leads
      ↓
Cotizaciones
      ↓
Ventas

Por ejemplo:

1000 prospectos
       ↓
800 contactados
       ↓
100 respuestas
       ↓
30 interesados
       ↓
30 Leads
       ↓
20 cotizaciones
       ↓
8 ventas

El sistema debe permitir saber exactamente dónde se pierde cada oportunidad.

20. Plan de Desarrollo Técnico

La secuencia recomendada es:

SEMANA 1
Core
Auth
Multi-Tenancy

SEMANA 2
Prospects
Contacts
Deduplication

SEMANA 3
Prospect Factory
n8n

SEMANA 4
Campaign Engine

SEMANA 5
Messaging

SEMANA 6
IA
Interest Detection

SEMANA 7
Leads
Human Handoff

SEMANA 8
Testing
Deploy

Esto es una estimación de desarrollo, no una fecha fija.

21. Definición de MVP Completado

El MVP estará terminado cuando un usuario pueda:

1.
Encontrar un prospecto

2.
Guardarlo

3.
Crear una campaña

4.
Enviar un mensaje

5.
Recibir respuesta

6.
Analizar respuesta con IA

7.
Detectar interés

8.
Crear Lead

9.
Asignar Lead

10.
Notificar humano

11.
Registrar venta

El flujo debe ejecutarse de forma repetible.

22. Arquitectura Final V1
                         ┌─────────────┐
                         │   FUENTES   │
                         └──────┬──────┘
                                │
                                ▼
                           ┌─────────┐
                           │   n8n   │
                           └────┬────┘
                                │
                                ▼
                     ┌────────────────────┐
                     │    ASP.NET API     │
                     │                    │
                     │  Business Logic    │
                     └─────────┬──────────┘
                               │
                               ▼
                       ┌───────────────┐
                       │  PostgreSQL   │
                       └───────────────┘

                                │
                                │
                    ┌───────────▼───────────┐
                    │       CAMPAIGN        │
                    └───────────┬───────────┘
                                │
                                ▼
                             n8n
                                │
                                ▼
                         PROVEEDOR MSG
                                │
                                ▼
                           PROSPECTO
                                │
                                ▼
                           RESPUESTA
                                │
                                ▼
                             n8n
                                │
                                ▼
                              IA
                                │
                                ▼
                         INTERESTED
                                │
                                ▼
                             LEAD
                                │
                                ▼
                            HUMANO
                                │
                                ▼
                             VENTA
23. V1 vs V2

La V1 debe permanecer deliberadamente simple.

Funcionalidad	V1	V2
Prospect Pool	✅	✅
Importación automática	✅	✅
Campañas	✅	✅
Mensajería	✅	✅
IA para interés	✅	IA avanzada
Human Handoff	✅	✅
CRM básico	✅	CRM avanzado
Dashboard	Básico	Avanzado
Scoring	Básico	Predictivo
OCR desde calle	❌	✅
GPS	❌	✅
Enriquecimiento	Básico	Avanzado
IA conversacional	❌	✅
Seguimiento automático	❌	✅
Recomendaciones comerciales	❌	✅
Analytics	Básico	Avanzado
Automatización avanzada	❌	✅
24. Punto de transición a V2

Después de octubre de 2026, la V2 debe comenzar con una evaluación de:

Costo por contacto
Costo por Lead
Costo por venta
Tasa de respuesta
Tasa de interés
Conversión a venta
Ingresos generados
ROI

La decisión de escalar no debería basarse únicamente en:

"Podemos enviar 1000 mensajes."

Sino en:

"Enviar 1000 mensajes genera X ventas
con un costo de Y."
25. Objetivo final de la V1

La V1 debe responder cinco preguntas:

1.
¿Podemos conseguir prospectos automáticamente?

2.
¿Podemos contactar prospectos de forma escalable?

3.
¿Podemos detectar automáticamente quién está interesado?

4.
¿Podemos entregar ese interesado rápidamente a un vendedor?

5.
¿El sistema genera más ventas de las que cuesta operar?

Si las cinco respuestas son positivas, la V2 tendrá una base sólida para incorporar automatización comercial mucho más avanzada.