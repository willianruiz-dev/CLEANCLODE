# Impresión de la tirilla en modo Debug (PDF)

## Qué hace

| Modo | Impresora | Resultado |
|---|---|---|
| **Debug** (`NO_PERIPHERALS`) | La impresora de Windows configurada, por defecto **Microsoft Print to PDF** | Genera un **PDF** de la tirilla en la carpeta `Receipts` y lo abre para revisarlo. No necesita la w80 ni el puerto COM. |
| **Release** | **w80** (cola del sistema) | Igual que siempre: `PrintDocument` → w80 → `MonitorPrintJobs`. El camino de PDF **no se compila** en Release. |

El código está en `Domain/Peripherals/Printer/PrintService.cs`:

- `PrintService.Start()` elige el camino con `#if DEBUG`.
- `PrintDebugPdf()` (solo Debug) arma el `PrintDocument` contra la impresora de Windows.
- `PrintReceiptToPdfPage()` dibuja la tirilla en un lienzo de 96 ppp — la resolución con la que están
  pensadas las coordenadas de `BuildPrint` — y lo estira al ancho de la página. Así el PDF conserva
  **el ancho y la escala de la w80** sin importar la resolución que reporte la impresora de Windows
  (Microsoft Print to PDF suele reportar 600 ppp).
- `BuildReceiptPaperSize()` define la página: **80 mm de ancho** (o el ancho real de la w80, leído de
  su propia cola si está instalada) y **el alto que ocupa el contenido**, para que el PDF salga con la
  forma de una tirilla y no como una hoja A4.

## Configuración (`App.config`)

```xml
<add key="debugPrinterName"       value="Microsoft Print to PDF" />
<add key="debugPrintOutputFolder" value="Receipts" />
<add key="debugPrintOpenPdf"      value="true" />
```

- `debugPrinterName`: cualquier impresora de Windows que genere PDF (`Microsoft Print to PDF`,
  "Adobe PDF", etc.). Si la impresora no está instalada, se registra una advertencia y **el flujo del
  kiosco continúa**, igual que antes.
- `debugPrintOutputFolder`: relativo a la carpeta del ejecutable (`bin\Debug\net6.0-windows\Receipts`).
  Para guardarlos en un sitio fijo, usar una ruta absoluta, por ejemplo
  `D:\TirillasPruebas`.
- `debugPrintOpenPdf`: `true` abre el PDF automáticamente al terminar la impresión; `false` solo lo deja
  en la carpeta.

## Requisito en Windows

"Microsoft Print to PDF" viene incluido en Windows 10/11. Si no aparece en
**Configuración → Bluetooth y dispositivos → Impresoras y escáneres**, se habilita en
**Panel de control → Programas → Activar o desactivar características de Windows → Microsoft Print to PDF**.

Con eso ya no interviene el visor que causaba el error anterior de OneNote: la impresión se envía
explícitamente a la cola de PDF configurada, no a la impresora predeterminada del sistema.

## Cómo probarlo

1. Compilar en Debug y ejecutar (`NO_PERIPHERALS`).
2. Hacer una transacción hasta la pantalla final y pulsar imprimir.
3. Al terminar aparece `Receipts\tirilla-AAAAMMDD-HHMMSS.pdf` (y se abre si `debugPrintOpenPdf=true`).
4. Comparar el PDF contra una tirilla real: el ancho (80 mm) y el tamaño del texto deben coincidir.

En `Log_application` queda registrada cada tirilla generada, con la ruta y el tamaño en pulgadas.

## Nota sobre la escala

Las coordenadas de la tirilla son píxeles pensados a **96 ppp** (`ReceiptDesignDpi`). Si en la w80 real
el texto se viera más pequeño o más grande de lo que sale en el PDF, basta con ajustar esa constante
(por ejemplo `203` para una térmica de 203 ppp): el PDF se recalcula con la misma proporción.
