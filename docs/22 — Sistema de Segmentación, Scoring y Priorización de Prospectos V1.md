📘 22 — Sistema de Segmentación, Scoring y Priorización de Prospectos V1

Producto: DIFRANI | Hunter CRM AI
Empresa inicial: Difrani
Versión: MVP V1
Objetivo: Organizar y priorizar la base de prospectos para maximizar la cantidad de oportunidades comerciales y ventas generadas.

1. Objetivo

El sistema de segmentación y scoring tendrá como finalidad responder tres preguntas:

1. ¿QUÉ TIPO DE CLIENTE ES?
2. ¿QUÉ POTENCIAL COMERCIAL TIENE?
3. ¿A QUIÉN CONTACTAMOS PRIMERO?

El sistema debe permitir que Difrani pueda trabajar simultáneamente con:

Distribuidoras
Casas de repuestos
Repuesteros
Talleres
Gomerías
Lubricentros
Revendedores
Otros comercios

La segmentación no tendrá como objetivo descartar automáticamente prospectos.

Su objetivo será:

Ordenar la operación comercial para aprovechar mejor los recursos disponibles.

2. Principio Comercial

El sistema debe partir de una premisa:

Todo prospecto válido puede convertirse en una oportunidad comercial.

Por eso, el scoring:

NO significa:
"Este cliente no sirve"

Significa:

"Este cliente podría ser más conveniente para contactar primero"

Ejemplo:

Prospecto A
Casa de repuestos
WhatsApp válido
Zona cercana
Alta recurrencia estimada
Score: 90

Prospecto B
Taller
WhatsApp válido
Zona lejana
Recurrencia media
Score: 65

Ambos pueden ser contactados.

El Prospecto A simplemente tendrá mayor prioridad.

3. Arquitectura de Segmentación
                    PROSPECTO
                        │
                        ▼
               DATOS NORMALIZADOS
                        │
                        ▼
              CLASIFICACIÓN BÁSICA
                        │
        ┌───────────────┼───────────────┐
        ▼               ▼               ▼
      Tipo            Tamaño         Ubicación
        │               │               │
        └───────────────┼───────────────┘
                        ▼
                 Potencial Comercial
                        │
                        ▼
                     SCORE
                        │
                        ▼
                   PRIORIDAD
                        │
                        ▼
                    CAMPAÑA
4. Segmentación Principal

Cada prospecto tendrá un segmento principal.

DISTRIBUTOR
AUTO_PARTS_STORE
WORKSHOP
LUBRICENTRO
TIRE_SHOP
RESELLER
OTHER
UNKNOWN
5. Segmento — Distribuidoras

Características:

Compra potencialmente recurrente
Volumen elevado
Puede comprar múltiples categorías
Puede distribuir a terceros
Puede convertirse en cliente estratégico

Prioridad potencial:

MUY ALTA

Pero dependerá de:

Capacidad de compra
Cobertura
Ubicación
Datos disponibles
Interés

Mensaje comercial recomendado:

Enfoque B2B

Debe destacar:

Catálogo
Disponibilidad
Variedad
Atención comercial
Capacidad de abastecimiento
Relación comercial
6. Segmento — Casas de Repuestos

Características:

Alta relevancia
Compra recurrente potencial
Necesidad de variedad
Posibilidad de reposición frecuente

Prioridad potencial:

MUY ALTA

Es uno de los segmentos principales para Difrani.

7. Segmento — Talleres

Características:

Compra relacionada con reparaciones
Necesidad de disponibilidad
Compras frecuentes
Puede recomendar productos
Puede convertirse en cliente recurrente

Prioridad potencial:

ALTA

El sistema deberá distinguir:

Taller pequeño
Taller mediano
Taller grande

Sin descartar los pequeños.

8. Segmento — Lubricentros

Características:

Alta rotación
Contacto frecuente con vehículos
Posibilidad de venta recurrente
Posibilidad de venta cruzada

Prioridad:

MEDIA / ALTA
9. Segmento — Gomerías

Características:

Contacto frecuente con propietarios de vehículos
Posibilidad de derivación
Potencial de venta de repuestos relacionados

Prioridad:

MEDIA

Podrán convertirse en:

Cliente
Referidor
Partner comercial
10. Segmento — Revendedores

Características:

Compra para reventa
Sensibilidad al precio
Potencial de volumen
Posibilidad de recurrencia

Prioridad:

ALTA
11. Segmento — Otros

Se utilizará cuando el sistema no pueda clasificar correctamente.

OTHER

Estos prospectos no deben descartarse.

Se mantendrán disponibles para:

Revisión
Reclasificación
Campañas generales
12. Segmento — Desconocido

Cuando no exista suficiente información:

UNKNOWN

Ejemplo:

Nombre:
Autopartes X

Teléfono:
+54...

Sin categoría

Resultado:

UNKNOWN

Puede ingresar a:

Campaña general

o pasar por:

Revisión manual
13. Tamaño del Negocio

El tamaño será:

UNKNOWN
MICRO
SMALL
MEDIUM
LARGE

No se recomienda inferir automáticamente un tamaño únicamente por:

Cantidad de seguidores
Tamaño del local
Cantidad de empleados estimada

Esos datos pueden servir como señales, pero no como verdad absoluta.

14. Micro

Ejemplo:

Taller pequeño
Casa de repuestos pequeña
Negocio familiar

Potencial:

Volumen bajo
Recurrencia potencial media

No debe excluirse.

15. Pequeño

Ejemplo:

Casa de repuestos local
Taller establecido
Lubricentro

Potencial:

Volumen medio
Recurrencia media / alta
16. Mediano

Ejemplo:

Casa de repuestos consolidada
Distribuidora regional
Cadena pequeña

Potencial:

Volumen medio / alto
Recurrencia alta
17. Grande

Ejemplo:

Gran distribuidora
Cadena de repuestos
Empresa con múltiples sucursales

Potencial:

Volumen alto
Recurrencia alta

Pero:

Mayor dificultad de adquisición
Mayor ciclo de venta

Por lo tanto, un cliente grande no siempre debe tener prioridad absoluta.

18. Potencial de Recurrencia
UNKNOWN
LOW
MEDIUM
HIGH
HIGH
Distribuidora
Casa de repuestos
Revendedor
Taller con alta actividad
MEDIUM
Lubricentro
Gomería
Taller pequeño
LOW
Particular
Compra ocasional
UNKNOWN

Cuando no existe información suficiente.

19. Distancia

La ubicación se clasificará:

LOCAL
NEAR
MEDIUM
FAR
UNKNOWN

La distancia debe calcularse respecto a:

Centro logístico
Sucursal
Depósito
Zona de distribución

No necesariamente respecto a la oficina administrativa.

20. Distancia y Prioridad

La distancia no debe utilizarse como criterio absoluto.

Ejemplo:

Cliente cercano
+
Bajo volumen
+
Baja recurrencia

puede ser menos atractivo que:

Cliente lejano
+
Alto volumen
+
Alta recurrencia

Por eso:

DISTANCIA

será solamente una variable del scoring.

21. Calidad del Dato

Se utilizará:

A
B
C
D
A
Empresa
Teléfono
WhatsApp
Dirección
Categoría
B
Empresa
Teléfono
Categoría
C
Empresa
Teléfono
D
Datos incompletos
22. Contactabilidad

La contactabilidad tendrá un peso importante.

Ejemplo:

WhatsApp válido
→ Muy alta

Teléfono válido
→ Alta

Email válido
→ Media

Sin canal válido
→ Muy baja

Un prospecto con alta intención potencial pero sin canal de contacto válido será difícil de convertir.

23. Score V1

El score será de:

0 a 100

Se propone:

Factor	Peso
Tipo de cliente	25
Potencial de recurrencia	20
Contactabilidad	20
Calidad del dato	15
Tamaño	10
Distancia	10
Total	100
24. Score — Tipo de Cliente

Ejemplo:

Distribuidora       25
Casa de repuestos   25
Revendedor          22
Taller              20
Lubricentro         17
Gomería             15
Otro                10
Desconocido          5

Estos valores pueden ajustarse según resultados reales.

25. Score — Recurrencia
HIGH       20
MEDIUM     13
LOW         6
UNKNOWN     3
26. Score — Contactabilidad
WhatsApp válido + Teléfono       20
WhatsApp válido                  18
Teléfono válido                  15
Email válido                     10
Contacto parcial                  5
Sin contacto válido               0
27. Score — Calidad
A       15
B       12
C        8
D        3
28. Score — Tamaño
LARGE       10
MEDIUM       8
SMALL        6
MICRO        4
UNKNOWN      2

El tamaño tiene un peso bajo para evitar perjudicar a negocios pequeños.

29. Score — Distancia
LOCAL        10
NEAR          8
MEDIUM        6
FAR           4
UNKNOWN       2

La distancia nunca debe eliminar automáticamente un prospecto.

30. Rangos de Prioridad
80-100
PRIORITY_A
60-79
PRIORITY_B
40-59
PRIORITY_C
0-39
PRIORITY_D
31. Prioridad A

Características:

Alto potencial
Datos confiables
Canal disponible

Acción:

Contactar primero
32. Prioridad B

Características:

Potencial medio / alto
Datos suficientes

Acción:

Contactar segunda tanda
33. Prioridad C

Características:

Potencial incierto
Datos incompletos

Acción:

Contactar cuando exista capacidad
34. Prioridad D

Características:

Datos débiles
Potencial desconocido
Canal poco claro

Acción:

No descartar
Mantener en base
Intentar enriquecer datos
35. Reglas de Exclusión

Un prospecto no debe ser contactado si:

Está en Suppression List

o:

No tiene ningún canal de contacto válido

o:

Es claramente inválido

o:

El contacto está bloqueado

o:

Existe una restricción legal o contractual aplicable
36. Segmentación por Campaña

El prospecto podrá pertenecer a múltiples campañas.

Ejemplo:

Repuestos Oeste

Segmento:
AUTO_PARTS_STORE

Campaña 1:
Presentación Difrani

Campaña 2:
Nueva línea de productos

Campaña 3:
Oferta comercial

Por lo tanto:

PROSPECTO

y:

CAMPAÑA

son entidades independientes.

37. Segmentos Dinámicos

El sistema podrá crear segmentos mediante reglas.

Ejemplo:

Tipo = AUTO_PARTS_STORE
AND
Provincia = Buenos Aires
AND
WhatsApp = VALID

Resultado:

CASAS DE REPUESTOS CONTACTABLES

Otro ejemplo:

Category = DISTRIBUTOR
AND
Recurrence = HIGH
AND
Score >= 70

Resultado:

DISTRIBUIDORES PRIORITARIOS
38. Segmentos Comerciales V1

Se recomienda comenzar con:

SEGMENTO 01
Distribuidoras

SEGMENTO 02
Casas de repuestos

SEGMENTO 03
Talleres

SEGMENTO 04
Lubricentros

SEGMENTO 05
Gomerías

SEGMENTO 06
Revendedores

SEGMENTO 07
Prospectos generales
39. Estrategia de Prioridad

La operación diaria puede funcionar:

PRIORITY A
   ↓
PRIORITY B
   ↓
PRIORITY C
   ↓
PRIORITY D

Pero cada campaña puede cambiar la prioridad.

Por ejemplo:

Campaña Distribuidores

priorizará:

DISTRIBUTOR

Mientras que:

Campaña Talleres

priorizará:

WORKSHOP
40. Score Comercial vs Score Operativo

Se recomienda separar dos conceptos.

Commercial Score

Potencial de convertirse en cliente.

0-100
Operational Priority

Qué tan conveniente es contactarlo ahora.

A
B
C
D

Esto permite que:

Cliente potencialmente excelente

pero:

Sin WhatsApp

tenga:

Commercial Score: 90
Operational Priority: C
41. Recalculo del Score

El score no será permanente.

Se actualizará cuando existan nuevos datos.

Ejemplo:

Prospecto
Score 55

Después:

Se valida WhatsApp

Nuevo score:

Score 70

Después:

Responde

Nuevo estado:

INTERESTED

Después:

Compra

Se convierte en:

CUSTOMER
42. Datos Reales vs Inferencias

Cada dato debe indicar su origen.

Ejemplo:

Category:
WORKSHOP

Source:
MANUAL

o:

RecurrencePotential:
HIGH

Source:
INFERRED

o:

BusinessSize:
MEDIUM

Source:
AI_ESTIMATED

Esto permitirá diferenciar:

Dato confirmado

de:

Dato estimado
43. Evolución del Score

V1:

Reglas fijas

V2:

Reglas configurables

V3:

Machine Learning / IA

La evolución será:

REGLAS
 ↓
DATOS
 ↓
RESULTADOS
 ↓
APRENDIZAJE
 ↓
PREDICCIÓN
44. Scoring Basado en Resultados

A futuro, el sistema podrá descubrir:

Casa de repuestos
+
Zona Oeste
+
WhatsApp
+
Mensaje A

genera:

12% conversión

Mientras:

Taller
+
Zona Oeste
+
WhatsApp
+
Mensaje A

genera:

4% conversión

El sistema podrá ajustar automáticamente la prioridad.

45. Score Dinámico Futuro
Base Score
+
Engagement Score
+
Response Score
+
Purchase Probability

Ejemplo:

Score base          70
Respondió           +10
Pidió información   +10
Cotizó              +5
Compró              +20

El valor máximo deberá limitarse a:

100
46. Score de Engagement

Se podrán medir:

Respondió
Pidió información
Pidió catálogo
Pidió precios
Pidió condiciones
Solicitó contacto humano

Estos comportamientos indican:

Mayor intención comercial
47. Lead Score

Una vez que el prospecto demuestra interés:

PROSPECT
 ↓
RESPONDED
 ↓
INTERESTED
 ↓
LEAD

el scoring debe cambiar.

Ya no se trata únicamente de:

"Potencial"

sino de:

"Probabilidad de cierre"
48. Priorización de Leads

Los Leads deben priorizarse por:

Interés
+
Valor potencial
+
Recurrencia
+
Urgencia

Ejemplo:

Lead A
"Pasame precios y catálogo"
→ MUY ALTO

Lead B
"Qué venden?"
→ ALTO

Lead C
"Ok"
→ MEDIO

Lead D
"Gracias"
→ BAJO
49. Score de Oportunidad

V1:

NEW
INTERESTED
HOT
WARM
COLD

Ejemplo:

HOT
Pidió precio
Pidió catálogo
Quiere comprar

WARM
Mostró interés
Pero no pidió cotización

COLD
Respondió poco
50. Flujo Completo
PROSPECTO
    │
    ▼
SEGMENTACIÓN
    │
    ▼
SCORING
    │
    ▼
PRIORIDAD
    │
    ▼
CAMPAÑA
    │
    ▼
CONTACTO
    │
    ▼
RESPUESTA
    │
    ▼
INTERÉS
    │
    ▼
LEAD SCORE
    │
    ▼
HUMANO
    │
    ▼
VENTA
    │
    ▼
DATOS REALES
    │
    ▼
MEJORA DEL SCORE
51. Ejemplo Completo
Empresa:
Repuestos Oeste

Categoría:
Casa de repuestos

Tamaño:
Mediano

Recurrencia:
Alta

Ubicación:
Zona Oeste

WhatsApp:
Válido

Calidad:
A

Score:

Tipo                 25
Recurrencia          20
Contactabilidad      20
Calidad              15
Tamaño                8
Distancia              8
-------------------------
TOTAL                96

Resultado:

PRIORITY_A
52. Ejemplo de Prospecto Pequeño
Empresa:
Taller Juan

Categoría:
Taller

Tamaño:
Micro

Recurrencia:
Media

WhatsApp:
Válido

Calidad:
B

Score:

Tipo                 20
Recurrencia          13
Contactabilidad      18
Calidad              12
Tamaño                4
Distancia              8
-------------------------
TOTAL                75

Resultado:

PRIORITY_B

Este prospecto sí debe ser contactado.

53. Ejemplo de Cliente Lejano
Distribuidora Nacional

Categoría:
Distribuidora

Tamaño:
Grande

Recurrencia:
Alta

Ubicación:
Lejana

WhatsApp:
Válido

Calidad:
A

Resultado:

Tipo                 25
Recurrencia          20
Contactabilidad      20
Calidad              15
Tamaño               10
Distancia              4
-------------------------
TOTAL                94

Resultado:

PRIORITY_A

La distancia no impide que sea una oportunidad prioritaria.

54. Regla Comercial Principal

La lógica final será:

NO BUSCAR SOLAMENTE
"LOS MEJORES CLIENTES"

BUSCAR
"LA MAYOR CANTIDAD DE CLIENTES POTENCIALES"

Y PRIORIZAR
"LOS MÁS CONVENIENTES PARA CONTACTAR PRIMERO"
55. Métricas

El sistema deberá medir por segmento:

Prospectos
Contactados
Respuestas
Interesados
Leads
Cotizaciones
Ventas
Ingresos

Y calcular:

Response Rate
Interest Rate
Lead Rate
Conversion Rate
Revenue per Prospect
Revenue per Segment
56. Dashboard

Ejemplo:

Segmento	Prospectos	Respuestas	Leads	Ventas	Conversión
Distribuidoras	1.000	120	50	15	1,5%
Casas de repuestos	2.000	250	90	30	1,5%
Talleres	3.000	400	80	20	0,67%
Lubricentros	1.000	100	30	8	0,8%

Esto permitirá identificar:

Segmentos con mayor volumen

y:

Segmentos con mayor rentabilidad
57. Objetivo de Optimización

La optimización no será:

Mayor cantidad de mensajes

sino:

Mayor cantidad de ventas

La fórmula principal será:

Ventas generadas
÷
Prospectos contactados

Y posteriormente:

Ingresos generados
÷
Costo de adquisición
58. V1 — Implementar
✓ Segmentación por categoría
✓ Tamaño
✓ Recurrencia
✓ Distancia
✓ Calidad de datos
✓ Contactabilidad
✓ Score 0-100
✓ Prioridad A/B/C/D
✓ Segmentos dinámicos básicos
✓ Scoring de Leads
✓ Métricas por segmento
✓ Recalculo manual/automático
59. V1 — No Implementar
✗ Machine Learning
✗ Predicción avanzada
✗ Scoring autónomo por IA
✗ Optimización automática de campañas
✗ Predicción de valor de cliente
✗ Dynamic Lead Scoring avanzado
60. V2 — Después de Octubre

La evolución será:

                 DATOS
                   │
                   ▼
             HISTORIAL REAL
                   │
                   ▼
           RESULTADOS CAMPAÑAS
                   │
                   ▼
             MODELO DE IA
                   │
                   ▼
         PROBABILIDAD DE COMPRA
                   │
                   ▼
         PRIORIDAD INTELIGENTE

El sistema podrá estimar:

Probabilidad de respuesta
Probabilidad de interés
Probabilidad de compra
Valor potencial
Probabilidad de recurrencia
61. Arquitectura Final
                  PROSPECTOS
                       │
                       ▼
                SEGMENTACIÓN
                       │
                       ▼
                  SCORING
                       │
                       ▼
                  PRIORIDAD
                       │
                       ▼
                  CAMPAÑAS
                       │
                       ▼
                 RESPUESTAS
                       │
                       ▼
                 LEAD SCORE
                       │
                       ▼
                 HUMANO
                       │
                       ▼
                   VENTA
                       │
                       ▼
                  RESULTADO
                       │
                       ▼
              MEJORA DEL MODELO
62. Conclusión

El sistema V1 debe utilizar un scoring simple, transparente y modificable, evitando comenzar con una IA compleja que todavía no dispone de suficientes datos históricos.

La estrategia inicial será:

CONSEGUIR MUCHOS PROSPECTOS
          ↓
SEGMENTARLOS
          ↓
CALIFICARLOS
          ↓
PRIORIZARLOS
          ↓
CONTACTARLOS
          ↓
MEDIR RESULTADOS
          ↓
APRENDER

La información generada durante los meses previos a octubre será fundamental para la siguiente versión.

La V2 podrá utilizar los datos reales de:

Prospectos contactados
+
Respuestas
+
Intereses
+
Leads
+
Cotizaciones
+
Ventas
+
Ticket promedio
+
Recurrencia

para reemplazar progresivamente el scoring basado en reglas por un sistema de priorización predictiva.