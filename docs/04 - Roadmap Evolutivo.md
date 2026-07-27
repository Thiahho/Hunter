📕 04 - Roadmap Evolutivo.md

Producto: Hunter CRM AI
Versión del documento: 1.0
Estado: Planificación
Propósito: Definir la evolución del producto desde el MVP inicial hasta una plataforma SaaS comercial escalable.

1. Objetivo

El roadmap evolutivo define cómo Hunter CRM AI crecerá progresivamente desde una primera versión enfocada en validar el modelo comercial hasta convertirse en una plataforma integral de prospección e inteligencia comercial.

El desarrollo se dividirá en fases para evitar incorporar funcionalidades prematuramente y mantener el foco en la validación del producto.

La evolución se basará en tres principios:

Validar antes de escalar.
Medir antes de automatizar.
Automatizar progresivamente las tareas repetitivas, manteniendo al humano en las decisiones comerciales importantes.
2. Fases del Producto
FASE 1
MVP
Julio → Octubre 2026
        │
        ▼
Validar prospección
y generación de leads
        │
        ▼
FASE 2
Post-Octubre 2026
        │
        ▼
Optimizar costos
y automatización
        │
        ▼
FASE 3
Escalabilidad
        │
        ▼
Inteligencia Comercial
        │
        ▼
FASE 4
Expansión
        │
        ▼
SaaS Multiempresa

Las fechas posteriores a octubre son orientativas y dependerán de los resultados obtenidos en la Fase 1.

3. FASE 1 — MVP
Periodo

Julio → Octubre 2026

Objetivo

Validar si Hunter puede generar oportunidades comerciales reales a partir de prospectos descubiertos automáticamente.

La V1 debe ser simple, funcional y medible.

No busca automatizar todo el proceso comercial.

Busca comprobar:

Prospecto
    ↓
Contacto
    ↓
Respuesta
    ↓
Interés
    ↓
Lead
    ↓
Humano
    ↓
Venta
Módulos
3.1 Core

Base del sistema.

Organizaciones.
Usuarios.
Roles.
Configuración.
Autenticación.
Multi-tenancy.
3.2 Prospect Factory

Descubrimiento de empresas.

Fuentes iniciales:

Google Places.
Directorios públicos.
OpenStreetMap como complemento.
Webs públicas.
Importación CSV.

Funciones:

Descubrimiento.
Normalización.
Deduplicación.
Validación.
Enriquecimiento básico.
3.3 Prospect Pool

Administración centralizada de prospectos.

Estados iniciales:

DISCOVERED
READY
CONTACTED
RESPONDED
INTERESTED
CUSTOMER
LOST
3.4 Campaign Engine

Permite:

Crear campañas.
Seleccionar prospectos.
Definir mensajes.
Ejecutar envíos.
Registrar actividad.
3.5 Interest Detector

La IA identifica si existe interés comercial.

Ejemplo:

"Sí, pasame información."

Resultado:

INTERESTED
3.6 Human Handoff

Cuando un prospecto demuestra interés:

Bot
 ↓
Lead
 ↓
Vendedor
 ↓
Conversación
 ↓
Cotización
 ↓
Venta

La venta final queda en manos del equipo humano.

3.7 Dashboard Básico

Métricas:

Prospectos encontrados.
Prospectos válidos.
Prospectos contactados.
Respuestas.
Interesados.
Leads.
Ventas.
Fuera de la Fase 1

No se desarrollará:

IA negociadora.
IA cotizadora.
IA cerrando ventas.
Seguimiento automático avanzado.
Lead Scoring predictivo.
Sales Intelligence.
Hunter Mobile.
Optimización automática de campañas.
Automatización multicanal avanzada.

Estas funcionalidades se trasladan a fases posteriores.

4. FASE 2 — Optimización Post-Octubre
Periodo

Desde octubre de 2026

Objetivo

Optimizar el sistema utilizando los datos obtenidos durante el MVP.

La V1 nos permitirá conocer:

Qué prospectos responden.
Qué mensajes funcionan.
Qué canales convierten.
Qué categorías compran.
Qué campañas generan ventas.

La Fase 2 utilizará esta información para mejorar la eficiencia.

4.1 IA Comercial

La IA dejará de limitarse a detectar interés.

Podrá participar en conversaciones simples y frecuentes.

Ejemplos:

¿Qué productos trabajan?

¿Hacen distribución?

¿Cómo trabajan con talleres?

La IA responderá utilizando información configurada por la empresa.

Cuando detecte intención comercial clara:

IA
 ↓
Detecta oportunidad
 ↓
Human Handoff
 ↓
Vendedor
4.2 Optimización de Costos

El sistema tendrá en cuenta el costo de cada canal.

Prospecto
    ↓
Analizar canales disponibles
    ↓
WhatsApp
Email
Otros canales
    ↓
Seleccionar estrategia

La decisión dependerá de:

Costo.
Probabilidad de respuesta.
Historial del prospecto.
Canal disponible.
Prioridad comercial.

El objetivo será maximizar:

Ventas generadas por costo de prospección.

4.3 Lead Scoring

Los prospectos comenzarán a recibir una puntuación.

Ejemplo:

Score: 92

Alta prioridad

Factores posibles:

Categoría.
Ubicación.
Tamaño estimado.
Canales disponibles.
Historial de respuesta.
Interacciones anteriores.
Comportamiento durante la conversación.
4.4 Analytics Avanzado

El sistema permitirá analizar:

Campaña
    ↓
Prospectos
    ↓
Respuestas
    ↓
Interesados
    ↓
Leads
    ↓
Ventas

Se podrán comparar:

Ciudades.
Provincias.
Categorías.
Mensajes.
Horarios.
Canales.
Campañas.
5. FASE 3 — Inteligencia Comercial
Objetivo

Convertir los datos acumulados en recomendaciones comerciales.

Hunter dejará de ser solamente una herramienta de automatización y comenzará a funcionar como un sistema de inteligencia comercial.

5.1 Sales Intelligence

El sistema podrá identificar patrones.

Ejemplo:

Casas de repuestos
+
Buenos Aires
+
Mensaje A
+
Horario 10:00
=
Alta conversión

La plataforma podrá recomendar estrategias similares.

5.2 Scoring Predictivo

Con suficientes datos históricos:

Prospecto
    ↓
Modelo predictivo
    ↓
Probabilidad de respuesta
    ↓
Probabilidad de interés
    ↓
Probabilidad de venta

Ejemplo:

Probabilidad de compra

87%

El vendedor podrá priorizar oportunidades de mayor potencial.

5.3 Optimización Automática

El sistema podrá aprender de las campañas.

Ejemplo:

Mensaje A
Conversión: 12%

Mensaje B
Conversión: 21%

Mensaje C
Conversión: 32%

Hunter podrá recomendar o seleccionar automáticamente el mensaje de mejor rendimiento.

La automatización deberá mantener límites configurables para evitar comportamientos no deseados.

6. FASE 4 — Hunter Mobile
Objetivo

Permitir que los equipos comerciales capturen prospectos desde el mundo físico.

Este módulo será independiente del MVP y se conectará al mismo Prospect Pool.

Flujo
📷 Foto
🎤 Audio
📍 GPS
    ↓
Hunter Mobile
    ↓
OCR
    ↓
IA
    ↓
Búsqueda de coincidencia
    ↓
Enriquecimiento
    ↓
Confirmación
    ↓
Prospect Pool
Ejemplo

Un vendedor encuentra un negocio durante un viaje.

Envía:

Foto del local.
Ubicación.
Nota de voz opcional.

Hunter identifica:

Nombre
Teléfono
Dirección
Categoría
Web
Redes

Y crea el prospecto.

Evolución futura

Hunter Mobile podría convertirse en una herramienta de relevamiento comercial aplicable a múltiples industrias.

No estará limitada a repuestos automotores.

7. FASE 5 — CRM Inteligente
Objetivo

Evolucionar desde la prospección hacia la gestión inteligente de relaciones comerciales.

El sistema podrá recomendar acciones.

Ejemplo:

"Este cliente lleva 60 días sin comprar."

"Este cliente suele comprar cada 15 días."

"Este prospecto respondió positivamente pero aún no recibió cotización."

"Este cliente tiene alta probabilidad de recompra."

8. FASE 6 — Expansión SaaS

Hunter se convertirá progresivamente en una plataforma multiempresa.

Hunter
│
├── Organización A
│
├── Organización B
│
├── Organización C
│
└── Organización D

Cada organización tendrá:

Usuarios.
Prospectos.
Campañas.
Leads.
Configuración.
Canales.
Integraciones.

Los datos estarán aislados por tenant.

9. Futuras Integraciones

El sistema podrá incorporar progresivamente:

WhatsApp
Telegram
Email
SMS
CRM externos
ERP
E-commerce
Google
Meta
Sistemas de gestión

Las integraciones deberán implementarse mediante adaptadores independientes.

10. Parking Lot

Ideas registradas para futuras versiones.

Estas funcionalidades no forman parte del MVP.

Prospección
Nuevas fuentes de datos.
Nuevos directorios.
Descubrimiento geográfico avanzado.
Enriquecimiento automático.
IA
IA conversacional.
IA negociadora.
IA de cotización.
IA predictiva.
IA de seguimiento.
Hunter Mobile
Foto.
OCR.
Audio.
GPS.
Reconocimiento de negocios.
Modo viaje.
Comercial
CRM avanzado.
Automatización de seguimiento.
Recompra.
Reactivación de clientes.
Inteligencia
Scoring predictivo.
Predicción de ventas.
Recomendación de campañas.
Optimización automática.
Plataforma
Marketplace de integraciones.
API pública.
Aplicación móvil.
Planes SaaS.
Multiidioma.
Multi-moneda.
11. Evolución de la Automatización

La evolución prevista será:

V1
Automatización básica
        ↓
Humano cierra

V2
IA asiste
        ↓
Humano supervisa

V3
IA recomienda
        ↓
Humano decide

V4
IA automatiza tareas
        ↓
Humano controla

V5
Inteligencia comercial
        ↓
Humano toma decisiones estratégicas

El principio central será:

La automatización aumenta la capacidad del equipo comercial; no elimina la necesidad de un equipo comercial.

12. Criterios para avanzar de fase

No se avanzará únicamente por calendario.

Cada fase deberá cumplir objetivos medibles.

V1 → V2

Se requiere demostrar:

Prospectos obtenidos.
Contactos válidos.
Respuestas.
Interesados.
Leads.
Ventas.
Costo por oportunidad.
Costo por venta.
V2 → V3

Se requiere contar con suficiente volumen histórico para generar conclusiones confiables sobre:

Conversión.
Canales.
Categorías.
Mensajes.
Comportamiento.
V3 → V4

Se evaluará:

Demanda de nuevos clientes.
Madurez del producto.
Estabilidad técnica.
Capacidad multiempresa.
Viabilidad comercial del SaaS.
13. Visión Final

La evolución de Hunter CRM AI será:

              DESCUBRIR
                  ↓
              CONTACTAR
                  ↓
               CALIFICAR
                  ↓
              ENTREGAR LEAD
                  ↓
                VENDER
                  ↓
                MEDIR
                  ↓
               APRENDER
                  ↓
              OPTIMIZAR
                  ↓
              PREDECIR
                  ↓
              ESCALAR

El producto final será una plataforma que conecte prospección, automatización, inteligencia artificial y gestión comercial, capaz de generar oportunidades continuamente y ayudar a los equipos de ventas a concentrarse en las conversaciones que realmente pueden convertirse en negocio.

Resumen de los 4 documentos

Con este documento quedan definidos los cuatro pilares:

📘 01 - Visión del Producto
        ↓
¿Qué queremos construir y por qué?

📗 02 - PRD MVP V1
        ↓
¿Qué vamos a construir primero?

📙 03 - Arquitectura Técnica
        ↓
¿Cómo vamos a construirlo?

📕 04 - Roadmap Evolutivo
        ↓
¿Cómo crecerá después de la V1?