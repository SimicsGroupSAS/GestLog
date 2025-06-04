# Guía Visual: Barra de Progreso Suavizada

Este documento complementa el manual de pruebas con ejemplos visuales de cómo debería verse la barra de progreso en diferentes estados.

## Visualización del Tiempo Restante

La estimación del tiempo restante debe estar ubicada en la parte superior derecha del área de estado, junto al mensaje de progreso, como se muestra a continuación:

```
[Procesando archivos... 45%]                [Tiempo restante: 2m 30s]
[===========================>                                       ]
```

## Apariencia de la Barra de Progreso

### Inicial (0-25%)
- Color: Azul claro (#17A2B8)
- Texto de tiempo: "Calculando tiempo..." o "Tiempo restante: XXs"

### Intermedio (25-50%)
- Color: Verde (#28A745)
- Texto de tiempo: "Tiempo restante: XXm XXs"

### Avanzado (50-75%)
- Color: Azul (#007BFF)
- Texto de tiempo: Actualización continua del tiempo restante

### Final (75-99%)
- Color: Naranja (#E67E22)
- Texto de tiempo: En transición a valor cero

### Completado (100%)
- Color: Verde (#28A745)
- Texto de tiempo: "Completado"

## Animaciones y Transiciones

La transición entre colores debe ser suave, sin cambios bruscos. El movimiento de la barra debe ser fluido, sin saltos o pausas perceptibles.

## Estados Especiales

### Cancelación
- La barra debe quedarse en la posición actual
- Texto de tiempo: "Cancelado"

### Error
- La barra debe quedarse en la posición actual
- Texto de tiempo: "Error"

## Notas sobre la Precisión

Es normal que la estimación de tiempo fluctúe ligeramente durante el proceso, pero debe estabilizarse a medida que avanza. La precisión final debe cumplir con las métricas establecidas en el manual de pruebas:

- Desviación < 20% para todo el proceso
- Desviación < 10% en el último 20% del proceso
