📘 21 — Sistema de Prospección y Obtención de Prospectos V1

Producto: Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Objetivo: Obtener la mayor cantidad posible de prospectos comerciales válidos para posteriormente convertirlos en clientes mediante contacto automatizado y cierre humano.

1. Objetivo del Sistema

El sistema de prospección tiene como objetivo construir una base comercial de potenciales compradores para Difrani.

La estrategia no debe limitarse a grandes empresas.

Se buscarán:

Micro clientes
Clientes pequeños
Clientes medianos
Clientes grandes
Distribuidores
Casas de repuestos
Repuesteros
Talleres
Gomerías
Lubricentros
Comercios relacionados

También se contemplará:

Clientes cercanos
Clientes de media distancia
Clientes lejanos
Clientes locales
Clientes regionales
Clientes nacionales

El criterio principal será:

Maximizar la cantidad de oportunidades comerciales válidas y permitir que el proceso comercial determine cuáles se convierten en ventas.

2. Principio Comercial

No se descartará automáticamente un prospecto por:

Tamaño
Ubicación
Recurrencia estimada
Cantidad de empleados
Volumen estimado

Un negocio pequeño puede convertirse en:

Cliente recurrente
Cliente de alto margen
Cliente recomendado
Cliente distribuidor

Por lo tanto, V1 buscará:

MÁXIMA COBERTURA
        ↓
DATOS VÁLIDOS
        ↓
CONTACTO
        ↓
INTERÉS
        ↓
HUMANO
        ↓
VENTA
3. Fuentes de Prospectos

La V1 utilizará múltiples fuentes.

                    PROSPECTOS
                        │
        ┌───────────────┼────────────────┐
        ▼               ▼                ▼
    Fuentes          Captura           Carga
    Públicas         Manual            Propia
        │               │                │
        ▼               ▼                ▼
   Directorios      Calle              CSV
   Mapas            Eventos            Excel
   Web              Visitas            CRM

Las fuentes se clasificarán como:

SOURCE_PUBLIC
SOURCE_MANUAL
SOURCE_IMPORT
SOURCE_PARTNER
SOURCE_INTERNAL
4. Fuente A — Google Maps / Google Business

Puede ser una de las fuentes más importantes para descubrir negocios.

Búsquedas:

Casa de repuestos
Repuestos automotor
Distribuidora de repuestos
Autopartes
Taller mecánico
Lubricentro
Gomería
Casa de suspensión
Frenos
Embragues

También se buscará por ubicación:

Moreno
Merlo
Ituzaingó
Morón
Castelar
Haedo
Paso del Rey
Rafael Castillo

y posteriormente:

Buenos Aires
Zona Oeste
Provincia
Argentina
5. Fuente B — Directorios Públicos

Se podrán utilizar directorios donde la información empresarial sea pública.

Ejemplos conceptuales:

Directorios comerciales
Cámaras empresariales
Guías comerciales
Directorios sectoriales
Listados empresariales

Los datos deberán incorporarse al sistema respetando:

Términos de uso
Políticas de acceso
Restricciones legales
6. Fuente C — Sitios Web Públicos

Se podrán identificar negocios mediante:

Sitios corporativos
Páginas de contacto
Catálogos públicos
Páginas comerciales
Directorios públicos

Datos posibles:

Nombre
Empresa
Teléfono
WhatsApp
Email
Dirección
Ciudad
Web
Categoría
7. Fuente D — Redes Sociales

Las redes sociales pueden servir principalmente para:

Descubrir negocios
Validar negocios
Identificar actividad
Encontrar canales de contacto

Por ejemplo:

Instagram
Facebook
TikTok
LinkedIn

El sistema deberá diferenciar:

DESCUBRIMIENTO

de:

CONTACTO AUTOMÁTICO

Encontrar un negocio en una red social no significa automáticamente que se pueda realizar envío masivo mediante dicha plataforma.

8. Fuente E — Importación CSV

V1 debe permitir cargar:

CSV
Excel

Formato mínimo:

business_name
phone
whatsapp
email
address
city
province
category
source

Ejemplo:

business_name,phone,city,category,source
Repuestos Oeste,+54911XXXXXXX,Merlo,AUTO_PARTS_STORE,MANUAL
Distribuidora X,+54911XXXXXXX,Moreno,DISTRIBUTOR,CSV
9. Fuente F — Prospección Manual

Un vendedor podrá cargar prospectos directamente.

Ejemplo:

Vendedor encuentra:
Repuestos El Norte

Carga:
Nombre
Teléfono
Dirección
Categoría
Observación

Resultado:

Nuevo Prospecto
        ↓
Validación
        ↓
CRM
10. Fuente G — Captura desde la Calle

Este será un módulo adicional y no formará parte del MVP principal.

La idea:

Persona
 ↓
Ve comercio
 ↓
Toma fotografía
 ↓
Envía imagen al bot

La IA analiza:

Nombre
Teléfono
Dirección
Cartel
Categoría

Ejemplo:

Foto
 ↓
OCR
 ↓
IA
 ↓
Datos estructurados

Resultado:

{
  "businessName": "Repuestos El Norte",
  "phone": "+54911...",
  "address": "Av. ...",
  "category": "AUTO_PARTS_STORE"
}

Este módulo se desarrollará posteriormente.

11. Módulo Extra — Captura Conversacional

Otra alternativa será enviar texto al bot.

Ejemplo:

Repuestos El Norte
Moreno
011-XXXX-XXXX
Casa de repuestos

El sistema convierte:

Texto libre
 ↓
IA
 ↓
Datos estructurados
 ↓
Validación
 ↓
Prospecto
12. Arquitectura de Captura
                   FUENTE
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
       Manual       CSV         Imagen
          │           │           │
          └───────────┼───────────┘
                      ▼
                  Normalizador
                      │
                      ▼
                    OCR/IA
                      │
                      ▼
                  Validación
                      │
                      ▼
                  Duplicados
                      │
                      ▼
                   Hunter
13. Normalización

Antes de guardar:

Nombre
Teléfono
WhatsApp
Email
Dirección
Ciudad
Provincia

deberán normalizarse.

Ejemplo:

011 15 5555 5555

convertirse en:

+5491155555555

Siempre que sea posible.

14. Normalización de Empresas

Ejemplo:

REPUESTOS OESTE
Repuestos Oeste
repuestos oeste srl

El sistema deberá intentar detectar que podrían representar:

Mismo negocio

Pero no eliminar automáticamente sin confirmación cuando exista incertidumbre.

15. Detección de Duplicados

Se deberán comparar:

Teléfono
WhatsApp
Email
Nombre
Dirección

Ejemplo:

Prospecto A
Repuestos Oeste
+54911XXXXXXX

Prospecto B
Repuestos Oeste Autopartes
+54911XXXXXXX

Resultado:

POSIBLE DUPLICADO
16. Estados del Prospecto
NEW
VALIDATED
READY
CONTACTED
RESPONDED
INTERESTED
LEAD
CUSTOMER
NOT_INTERESTED
SUPPRESSED
INVALID
17. Clasificación Comercial

Cada prospecto puede tener:

category
businessSize
recurrencePotential
distance

Ejemplo:

{
  "category": "DISTRIBUTOR",
  "businessSize": "MEDIUM",
  "recurrencePotential": "HIGH",
  "distance": "FAR"
}

Pero estas variables serán:

Datos de segmentación, no criterios automáticos para descartar.

18. Categorías

V1:

DISTRIBUTOR
AUTO_PARTS_STORE
WORKSHOP
LUBRICENTRO
TIRE_SHOP
RESELLER
OTHER
UNKNOWN

Posteriormente:

BODY_SHOP
FLEET
CAR_DEALER
SERVICE_CENTER
WHOLESALER
19. Tamaño del Cliente
UNKNOWN
MICRO
SMALL
MEDIUM
LARGE

Inicialmente:

UNKNOWN

será válido.

No se debe obligar al sistema a inventar un tamaño.

20. Potencial de Recurrencia
UNKNOWN
LOW
MEDIUM
HIGH

Ejemplo:

Distribuidor
→ HIGH

Casa de repuestos
→ HIGH

Taller
→ MEDIUM / HIGH

Particular
→ LOW

Estos valores serán orientativos.

21. Distancia

Se podrá calcular:

LOCAL
NEAR
MEDIUM
FAR

La distancia no será utilizada inicialmente para excluir.

Ejemplo:

Cliente cercano
→ Venta directa

Cliente lejano
→ Venta por distribución / logística
22. Score de Prospecto

V1 puede utilizar un score simple.

Ejemplo:

Teléfono válido          +20
WhatsApp identificado    +20
Empresa identificada     +10
Categoría conocida       +10
Dirección válida         +10
Web encontrada           +10
Email                    +10
Fuente confiable         +10

Total:

100 puntos

Clasificación:

80-100 → Alta calidad
50-79  → Media calidad
0-49   → Baja calidad

El score:

Prioriza, pero no elimina automáticamente.

23. Priorización

El sistema puede ordenar:

Score
 ↓
Categoría
 ↓
Ubicación
 ↓
Potencial

Pero la estrategia comercial seguirá siendo:

MAXIMIZAR OPORTUNIDADES
24. Validación de Teléfono

Se deberá verificar:

Formato
Longitud
Código país
Prefijo

Idealmente:

Argentina
+54

Ejemplo:

+54911XXXXXXX
25. Validación de WhatsApp

Cuando técnicamente sea posible y esté permitido por el proveedor:

Número
 ↓
Check
 ↓
¿Existe WhatsApp?

Resultado:

WHATSAPP_VALID
WHATSAPP_INVALID
UNKNOWN

No se debe asumir que un teléfono tiene WhatsApp solo porque parece válido.

26. Validación de Email

Se podrá validar:

Formato
Dominio

Opcionalmente:

Mailbox verification

No es obligatorio para V1.

27. Regla de Datos Mínimos

Un prospecto estará listo para campaña cuando tenga:

Nombre o Empresa
+
Canal de contacto válido

Por ejemplo:

Repuestos Oeste
+54911XXXXXXX

será suficiente para entrar en el proceso.

28. Calidad de Datos

Se establecerán niveles:

A
B
C
D
A
Nombre
Teléfono
WhatsApp
Dirección
Categoría
B
Nombre
Teléfono
Categoría
C
Nombre
Teléfono
D
Datos incompletos

El objetivo será priorizar A y B.

29. Pipeline de Prospectos
FUENTE
   ↓
CAPTURA
   ↓
NORMALIZACIÓN
   ↓
VALIDACIÓN
   ↓
DEDUPLICACIÓN
   ↓
SEGMENTACIÓN
   ↓
SCORE
   ↓
READY
   ↓
CAMPAÑA
30. Importación a Hunter

Cada prospecto debe guardar:

source
sourceReference
createdAt
createdBy

Ejemplo:

{
  "source": "GOOGLE_MAPS",
  "sourceReference": "external-reference",
  "createdBy": "system"
}

Esto permitirá saber:

De dónde vino
Qué fuente funciona mejor
Qué fuente genera más ventas
31. Métrica por Fuente

El sistema deberá medir:

Prospectos obtenidos
Prospectos válidos
Contactados
Respuestas
Interesados
Leads
Ventas
Ingresos

Ejemplo:

Google Maps
1000 prospectos
 ↓
200 respuestas
 ↓
50 interesados
 ↓
10 ventas

Esto permitirá calcular:

Conversión por fuente
32. Comparación de Fuentes

Dashboard:

Fuente          Prospectos   Leads   Ventas
Google Maps       5000        150      30
CSV               2000         80      20
Manual             500         40      15
Web                3000         50       8

La estrategia deberá optimizarse según:

VENTAS GENERADAS

no solamente:

CANTIDAD DE PROSPECTOS
33. Automatización de Prospección

V1:

Fuente
 ↓
Importación
 ↓
Validación
 ↓
Hunter
 ↓
Campaña

V2:

Fuente
 ↓
Crawler / API
 ↓
Normalización
 ↓
IA
 ↓
Validación
 ↓
Scoring
 ↓
Hunter
 ↓
Campaña
34. Uso de APIs

Cuando exista una API oficial:

API
 ↓
Obtener datos
 ↓
Guardar referencia
 ↓
Normalizar

Las APIs serán preferidas frente a métodos de extracción no autorizados.

35. Scraping

El scraping podrá considerarse únicamente cuando:

El sitio lo permita
No exista prohibición aplicable
Se respeten términos de uso
Se respeten restricciones técnicas
No se evadan mecanismos de seguridad

El sistema no deberá intentar:

Evitar CAPTCHA
Evitar bloqueos
Acceder a información privada
Acceder mediante cuentas no autorizadas
36. Regla de Contactabilidad

No todos los prospectos obtenidos deben enviarse automáticamente.

Proceso:

Prospecto
 ↓
¿Datos válidos?
 ↓
¿Contacto permitido?
 ↓
¿No está suprimido?
 ↓
¿Existe canal compatible?
 ↓
READY
37. Supresión

Antes de cualquier campaña:

Prospecto
 ↓
Suppression Check

Si está bloqueado:

NO CONTACTAR

Esto debe aplicarse independientemente de la fuente.

38. Base de Prospectos V1

La estructura mínima será:

Prospect
├── Id
├── OrganizationId
├── BusinessName
├── ContactName
├── Phone
├── WhatsApp
├── Email
├── Address
├── City
├── Province
├── Country
├── Category
├── BusinessSize
├── RecurrencePotential
├── DistanceCategory
├── LeadScore
├── DataQuality
├── Source
├── Status
├── CreatedAt
└── UpdatedAt
39. Estrategia Inicial para Difrani

El sistema debe comenzar por mercados relacionados con el negocio.

Prioridad:

1. Distribuidoras
2. Casas de repuestos
3. Repuesteros
4. Talleres
5. Lubricentros
6. Gomerías
7. Revendedores

Pero sin limitarse únicamente a ellos.

40. Estrategia Geográfica

La búsqueda podrá comenzar por:

Zona Oeste

y posteriormente ampliar:

Buenos Aires
 ↓
Provincia
 ↓
Argentina

La expansión se hará según:

Capacidad logística
Demanda
Conversión
Rentabilidad
41. Estrategia de Volumen

El objetivo de V1 será construir una base inicial de:

1.000 prospectos

Luego:

5.000

Después:

10.000+

No se deberá confundir:

Prospectos capturados

con:

Prospectos contactables

ni con:

Clientes

El verdadero KPI final es:

VENTAS
42. Ciclo de Aprendizaje

Cada campaña generará información.

Prospectos
 ↓
Contactos
 ↓
Respuestas
 ↓
Interés
 ↓
Ventas
 ↓
Datos
 ↓
Mejor Segmentación
 ↓
Nueva Campaña

El sistema deberá aprender qué perfiles tienen mayor conversión.

43. Modelo de Optimización

Después de suficientes datos:

Categoría
+
Ubicación
+
Tamaño
+
Fuente
+
Mensaje

permitirá identificar:

Perfil con mayor probabilidad de compra

Por ejemplo:

Casas de repuestos
Zona Oeste
WhatsApp válido
Fuente Google Maps
Mensaje A

puede generar mayor conversión que:

Talleres
Zona Norte
Email
Mensaje B
44. V1 — Qué se Implementa
✓ Importación CSV
✓ Carga manual
✓ Normalización
✓ Detección de duplicados
✓ Validación básica
✓ Segmentación
✓ Score
✓ Fuentes
✓ Registro de origen
✓ Suppression Check
✓ Integración con campañas
45. V1 — Qué NO se Implementa
✗ Bot de captura por imagen
✗ OCR avanzado
✗ Scraping masivo automático
✗ Crawler distribuido
✗ IA de enriquecimiento masivo
✗ Geolocalización avanzada
✗ Predicción avanzada de conversión

Estos componentes quedan para una fase posterior.

46. V2 — Prospección Avanzada

Después de octubre:

Google APIs
      +
Fuentes públicas
      +
Automatización
      +
IA
      +
Captura manual
      +
Captura por imagen

Pipeline:

FUENTES
   ↓
DATA COLLECTOR
   ↓
NORMALIZACIÓN
   ↓
IA ENRICHMENT
   ↓
VALIDACIÓN
   ↓
DEDUPLICACIÓN
   ↓
SCORING
   ↓
CRM
   ↓
CAMPAÑAS
47. Módulo de Captura por Imagen — Futuro

El módulo funcionará así:

Persona en la calle
       ↓
Toma foto del local
       ↓
Envía al bot
       ↓
OCR
       ↓
Visión IA
       ↓
Extracción de datos
       ↓
Confirmación
       ↓
Hunter CRM

La IA podría devolver:

Nombre:
Repuestos El Norte

Teléfono:
011-XXXX-XXXX

Dirección:
Av. ...

Categoría:
Casa de Repuestos

El usuario confirma:

[✓ Confirmar]
[✎ Editar]
[✗ Cancelar]

y se registra.

48. Evolución de la Prospección
                 V1
                  │
                  ▼
       IMPORTACIÓN + MANUAL
                  │
                  ▼
             V1.5
                  │
                  ▼
        APIS + FUENTES PÚBLICAS
                  │
                  ▼
                 V2
                  │
                  ▼
       AUTOMATIZACIÓN AVANZADA
                  │
                  ▼
                 V3
                  │
                  ▼
       IA + ENRIQUECIMIENTO
                  │
                  ▼
       PROSPECCIÓN INTELIGENTE
49. Criterio de Éxito

El sistema de prospección será exitoso si permite:

Obtener prospectos
        ↓
Validarlos
        ↓
Evitar duplicados
        ↓
Segmentarlos
        ↓
Contactarlos
        ↓
Medir resultados
        ↓
Identificar mejores fuentes
        ↓
Aumentar ventas

La métrica final será:

Ventas generadas por cada fuente de prospectos.

50. Conclusión

La V1 debe enfocarse en construir rápidamente una base comercial utilizable, sin intentar desarrollar desde el comienzo un sistema complejo de scraping e inteligencia artificial.

La prioridad será:

CONSEGUIR DATOS
      ↓
ORDENARLOS
      ↓
VALIDARLOS
      ↓
CONTACTARLOS
      ↓
DETECTAR INTERÉS
      ↓
PASAR A HUMANO
      ↓
CERRAR VENTA

Esto permite que Difrani empiece a probar el modelo comercial antes de octubre, aprovechando la etapa inicial de costos reducidos y, al mismo tiempo, recopilando datos reales para diseñar una V2 más inteligente.