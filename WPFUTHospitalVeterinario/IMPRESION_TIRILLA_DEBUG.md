# Impresión de la tirilla en modo Debug (PDF)

## Qué hace

| Compilación | Cómo imprime | Resultado |
|---|---|---|
| **`NO_PERIPHERALS`** (Debug) | El **propio programa genera el PDF**; no usa ninguna impresora ni driver | **Hoja A4** (`210 x 297 mm`) con la **tirilla en la esquina superior izquierda, a su tamaño real de 80 mm**, sin estirarla. Se abre solo para revisarla. |
| **Release** (sin `NO_PERIPHERALS`) | **w80** (cola del sistema) | Igual que siempre: `PrintDocument` → w80 → `MonitorPrintJobs`. El camino del PDF **no se compila**. |

El modo se elige con la constante de compilación `NO_PERIPHERALS`, que el proyecto ya define para
Debug (ver `WPFHospitalVeterinarioUT.csproj`), igual que el resto del código.
**No se usa nada de `App.config`** y **no hace falta instalar ninguna impresora**.

## Por qué el PDF no depende de "Microsoft Print to PDF"

Al principio la tirilla se enviaba a esa impresora de Windows. El problema es que **el driver decide
el tamaño de la página**: ignoraba el tamaño de tirilla solicitado y guardaba una **hoja carta/A4**,
por lo que el contenido se estiraba y se desbordaba (no se veía la tirilla, solo un fragmento).

Ahora el PDF lo escribe el programa (`BuildReceiptPdf`): un PDF 1.4 de una sola página con `MediaBox`
de **A4 (210 x 297 mm)**, en el que la tirilla —incrustada como imagen JPEG de 80 mm de ancho— va en la
esquina superior izquierda **a su tamaño real**. Al no intervenir ningún driver, el tamaño es siempre
el correcto.

## Cómo se arma el PDF

- **Hoja**: A4 (210 x 297 mm), con 5 mm de margen.
- **Tirilla**: en la esquina superior izquierda, **a tamaño físico real (80 mm de ancho)**. No se
  estira ni se deforma: al no escalarse, sus filas quedan separadas igual que en la tirilla de la w80.
- **Lienzo**: mide **exactamente el ancho de la tirilla** (302 px de diseño = 80 mm) por el alto del
  contenido. La tirilla se dibuja con las coordenadas de diseño de `BuildPrint` (pensadas a 96 ppp)
  sobre un lienzo de **96 ppp x 3 = 288 ppp** para que el texto salga nítido sin cambiar su tamaño.
  **Ojo con la resolución del lienzo** (ver *La trampa de la resolución* más abajo): el dibujo se hace
  siempre a 96 ppp y la resolución real (288 ppp) se marca al final, solo como dato de la imagen.
- **Contenido**: la imagen se coloca con `q W 0 0 H 5mm 5mm cm /Im0 Do Q`, es decir con su tamaño real
  y desplazada a la esquina (el origen del PDF está abajo a la izquierda).
- **Imágenes de la tirilla**: nunca más anchas que la tirilla, conservando proporción. La cabecera
  (`Voucher.png`) mide 828 x 242 px = 219 mm a 96 ppp, más del doble del ancho de la tirilla: ajustada
  ocupa 80 x 23 mm y su alto encaja justo antes de la primera línea de texto (`y = 105`).
- **Si la tirilla fuera más alta que la hoja**, la hoja crece para no cortarla.
- **Respaldo**: si el PDF no se pudiera escribir, la tirilla se guarda como **PNG** con el mismo dibujo.

## Ajustes (constantes en `PrintService`)

```csharp
private const string PdfOutputFolder    = "Receipts";   // relativa al ejecutable, o ruta absoluta
private const bool   PdfOpenAfterPrint  = true;         // abre el PDF al terminar
private const double ReceiptRenderScale = 3.0;          // 96 ppp x 3 = 288 ppp (nitidez)
private const double ReceiptDesignDpi   = 96.0;         // resolución de diseño de la tirilla
private const double ReceiptWidthMm     = 80.0;         // ancho de la tirilla de la w80
private const double PdfPageWidthMm     = 210.0;        // hoja A4 del PDF
private const double PdfPageHeightMm    = 297.0;
private const double PdfPageMarginMm    = 5.0;          // margen de la hoja
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
| `Sin periféricos: tirilla generada en '...'. La tirilla mide 80,0 x N mm y va en la esquina de una hoja de 210 x 297 mm.` | Todo correcto; la ruta y los tamaños están ahí. |
| `No se pudo generar el PDF de la tirilla: <motivo>` | Falló el PDF y quedó la tirilla como PNG. |

### Sobre la escala del texto

El texto se coloca a tamaño real: los 8 pt de `BuildPrint` son 8 pt en el PDF. Si al comparar con una
tirilla física el texto se viera más grande o más pequeño, el único ajuste es `ReceiptDesignDpi`
(hoy 96): subirlo a 203 o 300 recalcula la tirilla completa en la misma proporción.

### La trampa de la resolución (por qué el texto salía gigante)

Las fuentes se crean en **puntos** (`new Font("Arial", 8, FontStyle.Bold)`) y GDI+ las convierte a
píxeles usando la resolución *del lienzo*, no la del diseño. Si el lienzo se creaba ya a 288 ppp y
además se le aplicaba `ScaleTransform(3, 3)`, cada 8 pt se dibujaba **3 veces más grande**:
8,5 mm de alto en vez de 2,8 mm. Con líneas cada 4 mm, el texto se pisaba entre sí y se salía del
ancho de la tirilla (ilegible).

Regla que hay que respetar:

1. `canvas.SetResolution(96, 96)` **antes** de `Graphics.FromImage(canvas)`.
2. Dibujar con `ScaleTransform(3, 3)` —de ahí sale la nitidez—.
3. `canvas.SetResolution(288, 288)` **después** de dibujar, ya solo como metadato del archivo.

Lo mismo vale para `DrawImage(imagen, x, y)` (el QR): GDI+ usa la resolución del lienzo contra la de la
imagen; con el lienzo a 96 ppp la dibuja 1:1 con el diseño.

### Líneas que no caben

La tirilla mide lo que la w80 (80 mm) y el diseño no se cambia, así que una línea más ancha que la
tirilla se corta igual que en el papel. Hoy pasa con la **dirección del pie**
(`Calle 20 Sur n° 23 a - 160 Barrio miramar`): empieza en la columna de valores (mitad de la tirilla)
y mide 52 mm, así que termina en 92 mm y se corta a los 80 mm — igual que en la tirilla física.
Si se quisiera completa habría que cambiar el diseño de la tirilla (columna de valores o tamaño de
letra), y eso no se toca sin autorización.
