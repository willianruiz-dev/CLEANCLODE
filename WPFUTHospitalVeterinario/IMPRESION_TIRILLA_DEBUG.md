# Impresión de la tirilla en modo Debug (PDF)

## Qué hace

| Compilación | Impresora | Resultado |
|---|---|---|
| **`NO_PERIPHERALS`** (Debug) | **Microsoft Print to PDF** (impresora de Windows) | Genera un **PDF** de la tirilla en la carpeta `Receipts` y lo abre para revisarlo. No necesita la w80 ni el puerto COM. |
| **Release** (sin `NO_PERIPHERALS`) | **w80** (cola del sistema) | Igual que siempre: `PrintDocument` → w80 → `MonitorPrintJobs`. El camino de PDF **no se compila**. |

El modo se elige con la constante de compilación `NO_PERIPHERALS` que ya define el proyecto para
Debug (ver `WPFHospitalVeterinarioUT.csproj`), igual que el resto del código. **No se usa nada de
`App.config`.**

El código está en `Domain/Peripherals/Printer/PrintService.cs`:

- `PrintService.Start()` elige el camino con `#if NO_PERIPHERALS`.
- `PrintPdf()` (solo con `NO_PERIPHERALS`) arma el `PrintDocument` contra la impresora de Windows.
- `PrintReceiptToPdfPage()` dibuja la tirilla en un lienzo de 96 ppp — la resolución con la que están
  pensadas las coordenadas de `BuildPrint` — y lo estira al ancho de la página. Así el PDF conserva
  **el ancho y la escala de la w80** sin importar la resolución que reporte la impresora de Windows
  (Microsoft Print to PDF suele reportar 600 ppp).
- `BuildReceiptPaperSize()` define la página: **80 mm de ancho** (o el ancho real de la w80, leído de
  su propia cola si está instalada) y **el alto que ocupa el contenido**, para que el PDF salga con la
  forma de una tirilla y no como una hoja A4.

## Ajustes (constantes en `PrintService`)

```csharp
private const string PdfPrinterName = "Microsoft Print to PDF";
private const string PdfOutputFolder = "Receipts";
private const bool   PdfOpenAfterPrint = true;
```

- `PdfPrinterName`: cualquier impresora de Windows que genere PDF. Si no está instalada, se registra una
  advertencia y **el flujo del kiosco continúa**, igual que antes.
- `PdfOutputFolder`: relativo a la carpeta del ejecutable (`bin\Debug\net6.0-windows\Receipts`). Para
  guardarlos en un sitio fijo, usar una ruta absoluta, por ejemplo `D:\TirillasPruebas`.
- `PdfOpenAfterPrint`: `true` abre el PDF automáticamente al terminar; `false` solo lo deja en la carpeta.

## Requisito en Windows

"Microsoft Print to PDF" viene incluido en Windows 10/11. Si no aparece en
**Configuración → Bluetooth y dispositivos → Impresoras y escáneres**, se habilita en
**Panel de control → Programas → Activar o desactivar características de Windows → Microsoft Print to PDF**.

Con eso ya no interviene el visor que causaba el error anterior de OneNote: la impresión se envía
explícitamente a la cola de PDF configurada, no a la impresora predeterminada del sistema.

## Cómo probarlo

1. Compilar en Debug (que define `NO_PERIPHERALS`) y ejecutar.
2. Hacer una transacción hasta la pantalla final y pulsar imprimir.
3. Al terminar aparece `Receipts\tirilla-AAAAMMDD-HHMMSS.pdf` (y se abre si `PdfOpenAfterPrint`).
4. Comparar el PDF contra una tirilla real: el ancho (80 mm) y el tamaño del texto deben coincidir.

## Tamaño de la tirilla en el PDF

La tirilla se coloca **en pulgadas** dentro de la página: su dibujo está pensado a 96 ppp, así que
su tamaño físico es `píxeles / 96`. Por eso mide **siempre 80 mm de ancho y su alto real**, sin
depender de la resolución que reporte la impresora ni del tamaño de página que decida usar el driver.

Si el driver ignora el tamaño de tirilla y guarda una **hoja carta/A4**, la tirilla aparece dentro de
esa hoja **a tamaño verdadero** (una tira de 80 mm), sin deformarse ni desbordarse. La línea
`Sin periféricos: página del PDF = ...` del log dice qué página se usó realmente.

El lienzo se dibuja a **96 ppp x 3 (288 ppp)** para que el texto salga nítido; el tamaño físico no cambia.

El PDF se arma también con el ancho real de la w80 (**80 mm**) y el alto que ocupa el contenido como
tamaño de página solicitado (`RawKind = 256`, tamaño definido por el usuario).

La cabecera (`Assets/Images/Voucher.png`) mide **828 x 242 px**, es decir **219 mm de ancho** a 96 ppp:
dibujada a tamaño natural desbordaba la tirilla y salía cortada. Ahora se ajusta al ancho de la
tirilla conservando la proporción (**302 x 88 px** de diseño = 80 x 23 mm), y ese alto encaja justo
antes de la primera línea de texto, que el diseño dibuja en `y = 105`.

Reglas usadas para que el PDF se vea como la tirilla:

| Elemento | Regla |
|---|---|
| Página | Ancho de la w80 (80 mm o el de su cola) y alto del contenido. |
| Lienzo de dibujo | 96 ppp; se estira al ancho de la página, así la escala no depende de los ppp que reporte la impresora (Microsoft Print to PDF suele reportar 600). |
| Imágenes | Nunca más anchas que el ancho de la tirilla, conservando proporción. |
| Texto | Tamaño real, en píxeles de diseño a 96 ppp. |
| Ancho del lienzo | Exactamente la tirilla; solo se amplía (con margen) si algún texto no cupiera. |

## Si no aparece el PDF

El proceso ya no falla en silencio: siempre deja rastro en `Logs\Log_application\LogAAAA-MM-DD.json`
(junto al ejecutable). Buscar las líneas que empiezan con `Sin periféricos`:

| Línea del log | Significa |
|---|---|
| `Sin periféricos: imprimiendo la tirilla con 'Microsoft Print to PDF'.` | Se está usando el camino del PDF (si no aparece, se compiló sin `NO_PERIPHERALS`, es decir Release). |
| `Sin periféricos: tirilla generada en '...pdf' (página de la tirilla, N bytes).` | Todo correcto. La ruta exacta está ahí. |
| `... falló la impresión con la página de la tirilla: <motivo>` | La impresora rechazó el tamaño de tirilla; el sistema **reintenta solo** con la página predeterminada. |
| `... no se pudo generar el PDF; la tirilla se guardó como imagen en '...png'.` | La impresora no produjo el archivo. Siempre queda la imagen para revisar. |
| `Sin periféricos: la impresora 'Microsoft Print to PDF' no está disponible.` | El nombre de la impresora no coincide con el instalado (revisar `PdfPrinterName`). |

Comportamiento del camino del PDF:

1. Imprime con la página del tamaño de la tirilla y **espera** a que el archivo exista y su tamaño se
   estabilice antes de darlo por bueno (la impresora escribe de forma asíncrona).
2. Si la impresora rechaza ese tamaño de página, reintenta con la página predeterminada de la impresora.
3. Si aun así no hay archivo, guarda la tirilla como **PNG** con el mismo dibujo y la abre.
4. La impresión nunca interrumpe el flujo del kiosco: `recentImpressionSuccess` queda en `true`.

## Nota sobre la escala

Las coordenadas de la tirilla son píxeles pensados a **96 ppp** (`ReceiptDesignDpi`). Si en la w80 real
el texto se viera más pequeño o más grande de lo que sale en el PDF, basta con ajustar esa constante
(por ejemplo `203` para una térmica de 203 ppp): el PDF se recalcula con la misma proporción.
