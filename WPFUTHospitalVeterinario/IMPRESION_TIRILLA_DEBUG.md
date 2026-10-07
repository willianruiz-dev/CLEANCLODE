# Impresión de la tirilla en modo Debug (PDF)

## Qué hace

| Compilación | Cómo imprime | Resultado |
|---|---|---|
| **`NO_PERIPHERALS`** (Debug) | El **propio programa genera el PDF**; no usa ninguna impresora ni driver | `Receipts\tirilla-AAAAMMDD-HHMMSS.pdf`: una sola página que mide **exactamente el ancho de la w80 (80 mm)** y el alto del contenido. Se abre solo para revisarla. |
| **Release** (sin `NO_PERIPHERALS`) | **w80** (cola del sistema) | Igual que siempre: `PrintDocument` → w80 → `MonitorPrintJobs`. El camino del PDF **no se compila**. |

El modo se elige con la constante de compilación `NO_PERIPHERALS`, que el proyecto ya define para
Debug (ver `WPFHospitalVeterinarioUT.csproj`), igual que el resto del código.
**No se usa nada de `App.config`** y **no hace falta instalar ninguna impresora**.

## Por qué el PDF no depende de "Microsoft Print to PDF"

Al principio la tirilla se enviaba a esa impresora de Windows. El problema es que **el driver decide
el tamaño de la página**: ignoraba el tamaño de tirilla solicitado y guardaba una **hoja carta/A4**,
por lo que el contenido se estiraba y se desbordaba (no se veía la tirilla, solo un fragmento).

Ahora el PDF lo escribe el programa (`BuildReceiptPdf`): un PDF 1.4 de una sola página cuya `MediaBox`
mide lo que la tirilla — **80 mm de ancho** por el alto del contenido — con la tirilla incrustada como
imagen JPEG. Al no intervenir ningún driver, el tamaño es siempre el correcto.

## Cómo se arma el PDF

- **Lienzo**: la tirilla se dibuja con las coordenadas de diseño de `BuildPrint` (pensadas a 96 ppp),
  en un lienzo de **96 ppp x 3 = 288 ppp** para que el texto salga nítido. El tamaño físico no cambia.
- **Página**: ancho de la w80 (80 mm) y alto del contenido, calculados desde el lienzo.
- **Contenido**: la imagen ocupa la página completa (`q W 0 0 H 0 0 cm /Im0 Do Q`), sin márgenes.
- **Imágenes de la tirilla**: nunca más anchas que la tirilla, conservando proporción. La cabecera
  (`Voucher.png`) mide 828 x 242 px = 219 mm a 96 ppp, es decir más del doble del ancho de la tirilla:
  ajustada ocupa 80 x 23 mm, y su alto encaja justo antes de la primera línea de texto (`y = 105`).
- **Respaldo**: si el PDF no se pudiera escribir, la tirilla se guarda como **PNG** con el mismo dibujo.

## Ajustes (constantes en `PrintService`)

```csharp
private const string PdfOutputFolder    = "Receipts";   // relativa al ejecutable, o ruta absoluta
private const bool   PdfOpenAfterPrint  = true;         // abre el PDF al terminar
private const double ReceiptRenderScale = 3.0;          // 96 ppp x 3 = 288 ppp (nitidez)
private const double ReceiptDesignDpi   = 96.0;         // resolución de diseño de la tirilla
private const double ReceiptWidthMm     = 80.0;         // ancho de la tirilla de la w80
```

`PdfOutputFolder` acepta una ruta absoluta, por ejemplo `D:\TirillasPruebas`, y `PdfOpenAfterPrint`
en `false` deja el archivo sin abrirlo.

## Cómo probarlo

1. Compilar en Debug (que define `NO_PERIPHERALS`) y ejecutar.
2. Hacer una transacción hasta la pantalla final y pulsar imprimir.
3. Al terminar aparece `Receipts\tirilla-AAAAMMDD-HHMMSS.pdf` y se abre solo.
4. Comparar contra una tirilla real: el ancho (80 mm) y el tamaño del texto deben coincidir.

## Si algo no sale como se espera

El proceso nunca falla en silencio: deja rastro en `Logs\Log_application\LogAAAA-MM-DD.json`
(junto al ejecutable). Buscar las líneas que empiezan con `Sin periféricos`:

| Línea del log | Significa |
|---|---|
| `Sin periféricos: generando la tirilla como PDF con el tamaño de la w80.` | Se está usando el camino del PDF. **Si no aparece, se compiló Release** (ahí va directo a la w80). |
| `Sin periféricos: tirilla generada en '...pdf' (80 x N mm, MxP px).` | Todo correcto; la ruta y el tamaño de página están ahí. |
| `No se pudo generar el PDF de la tirilla: <motivo>` | Falló el PDF y quedó la tirilla como PNG. |

### Sobre la escala del texto

El texto se coloca a tamaño real: los 8 pt de `BuildPrint` son 8 pt en el PDF. Si al comparar con una
tirilla física el texto se viera más grande o más pequeño, el único ajuste es `ReceiptDesignDpi`
(hoy 96): subirlo a 203 o 300 recalcula la tirilla completa en la misma proporción.
