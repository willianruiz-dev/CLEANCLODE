# Handoff — Validaciones reactivas del formulario + teclados por campo

> **Estado: implementado.** Este documento se conserva como especificación de referencia.
> Los nombres definitivos en el código son:
> `FormUC.UpdateContinueState()`, `FormUC.AdjustText(...)`, `FormUC.ClearAutoFilledPersonalData()`,
> `FormUC.TxtFirstName_TextChanged`, `FormUC.TxtLastName_TextChanged`, `FormUC.TxtMobile_TextChanged`,
> `FormUC.TxtEmail_changed`; `VKeyboard.InputKind` con `KeyboardInputKind`, `KeyboardType.Numeric`,
> `KeyboardType.Email` y las vistas `NumericView.xaml` / `EmailView.xaml`.
> Los fragmentos de la sección 4 son la propuesta original; ante cualquier diferencia, manda el código.
>
> **Decisiones tomadas al implementar:**
> - El celular **no** bloquea en vivo el primer dígito distinto de `3`: se filtra a solo dígitos (máx. 10)
>   y el prefijo `3` se valida al enviar, para no alterar lo que el usuario ve mientras escribe.
> - El botón Continuar se deshabilita con `IsEnabled = false` sobre el `Image` existente: bloquea mouse
>   y táctil sin cambiar un solo píxel (no se puso `Opacity` ni estado "gris").
> - **Teclado numérico:** no se diseñó uno nuevo. `NumericView` reutiliza el teclado tipo clave que la app
>   ya usa en la pantalla de pago (`UI.Components.NumericKeyboard`: 1-9, 0, `X` y borrar) y con el mismo
>   tamaño explícito que allí se usa (`Width="348" Height="404"`). El tamaño debe ir explícito: si se deja
>   automático, el `Viewbox` interno del teclado se encoge hasta la caja que le ofrece la ventana del teclado
>   virtual y queda en miniatura.
> - **Teclado de correo:** alfabeto estándar + fila con `@`, `.`, `_`, `-` a la vista y fila de dominios
>   frecuentes (`@gmail.com`, `@hotmail.com`, `@outlook.com`, `@yahoo.com`) que insertan el texto completo
>   de una pulsación para abreviar la escritura. El botón `?$#,` abre los símbolos y `abc/123` regresa al
>   teclado de origen.
> - **Limpieza de campos:** los datos autocompletados desde la API se borran cuando el documento deja de
>   corresponder a ese usuario (se borra, baja de 6 dígitos o la consulta devuelve que no existe). Solo se
>   limpian los campos que provinieron de la consulta, no lo que el usuario escribió a mano.
>
> **Pendiente:** la validación en Windows de la sección 7. El entorno de revisión no tiene SDK `dotnet`
> y WPF no compila fuera de Windows, así que el build y la prueba manual quedan para el equipo.

Documento para el agente que ejecutará el cambio. Está redactado para pegarse tal cual como instrucción.

---

## 0. Contexto y restricciones duras

- Repo: `willianruiz-dev/CLEANCLODE` (GitHub). Rama base: `main`, commit `36ee1d66314aa37fb0cacc6a8d23d728be9b7a81`.
- Crea tu propia rama desde `main`:
  ```bash
  git checkout main && git pull
  git checkout -b fix/validaciones-formulario
  git push -u origin fix/validaciones-formulario
  ```
- Proyecto: `WPFUTHospitalVeterinario/src/WPFHospitalVeterinarioUT/` — WPF, `net6.0-windows`, `PlatformTarget x86`, `Nullable enable`, `ImplicitUsings enable`, MaterialDesignInXAML.
- **NO** modifiques `Domain/Peripherals/**`, ni `Domain/ApiService/**`, ni el proyecto `.csproj`, ni assets (fuentes/imágenes/videos), ni el instalador.
- **NO** cambies diseño visual: nada de tamaños, márgenes, fuentes, colores, recursos, imágenes ni estructura XAML. El layout es un `Viewbox` con un `Grid` de `1080x1920`; se mantiene intacto.
- Solo se permiten en XAML **atributos nuevos** sobre controles existentes (eventos / propiedades adjuntas). No agregues ni quites elementos visuales.
- No hay SDK `dotnet` en el entorno de revisión y **no existe proyecto de tests**. La verificación real es build + prueba manual en Windows (sección 7).

## 1. Archivos que vas a tocar

| Archivo | Rol |
|---|---|
| `Domain/Validation/PersonalInformationValidator.cs` | Reglas puras (sin WPF). Ya existe; se amplía. |
| `Presentation/UserControls/Flows/FormUC.xaml` | Formulario. Solo atributos nuevos. |
| `Presentation/UserControls/Flows/FormUC.xaml.cs` | Sanitización en vivo, estado del botón, teclado por campo. |
| `Presentation/Shared/Components/Keyboard/Types/KeyboardType.cs` | Enum de tipos de teclado. |
| `Presentation/Shared/Components/Keyboard/Converters/KeyboardTypeConverter.cs` | Mapea tipo → vista. |
| `Presentation/Shared/Components/Keyboard/ViewModels/VirtualKeyboardViewModel.cs` | Tipo inicial del teclado. |
| `Presentation/Shared/Components/Keyboard/VKeyboard.cs` | Propiedad adjunta por campo + `OpenAsync` con tipo. |
| `Presentation/Shared/Components/Keyboard/Views/NumericView.xaml(.cs)` | **Nuevo**: teclado numérico. |
| `Presentation/Shared/Components/Keyboard/Views/EmailView.xaml(.cs)` | **Nuevo**: teclado de correo. |
| `App.xaml.cs` | Línea 22: `VKeyboard.Listen<TextBox>(e => e.Text);` — se conserva esa llamada. |

## 2. Estado actual (lo que ya existe, no lo rompas)

- Formulario en `FormUC.xaml`:
  - `ComboBox x:Name="TypeDocument"` con `SelectionChanged="OnSelectionChanged"` (items con `Tag` 1..4).
  - `TextBox Text="{Binding Document, Mode=TwoWay}" TextChanged="TxtDocument_TextChanged"`.
  - `TextBox` de `FirstName`, `LastName`, `Mobile`, `Email` (este último con `TextChanged="TxtEmail_changed"`), todos con `Style="{StaticResource TextBoxStyle}"` y `CaretBrush="Transparent"`.
  - Botón continuar: `Image x:Name="BtnForm"` con `MouseDown="BtnForm_MouseDown"` y `Visibility="Hidden"`. Su visibilidad hoy la controla el `ToggleButton` de la política de datos (`ToggleButton_Checked`).
- `DataContext` = `_ts.customFlows.generaLInformationClient` (`GeneralInformationClient` en `Domain/UIServices/Transaction.cs`), con propiedades `Document`, `DocumentType`, `FirstName`, `LastName`, `Mobile`, `Email` e `INotifyPropertyChanged`.
- `TxtDocument_TextChanged` hace búsqueda de usuario contra la API con debounce de 400 ms, cancela consultas previas y compara `textBox.Text` contra el valor digitado para descartar respuestas viejas. **Esa lógica se conserva tal cual.**
- `BtnForm_MouseDown` ya valida con `PersonalInformationValidator.GetValidationError(...)` y muestra `InfoModal`; luego crea/actualiza usuario en API y navega a `ReferenceToPayUC`.
- Teclado virtual global: `App.xaml.cs` registra `VKeyboard.Listen<TextBox>(e => e.Text)`, que abre `DefaultKeyboardHost` (ventana maximizada) con `VirtualKeyboardViewModel` para **todos** los `TextBox` de la app. Hoy solo existen `KeyboardType.Alphabet` y `KeyboardType.Special`; el `@` solo está en la vista de caracteres especiales (hay que cambiar de vista con la tecla `abc/123`).
- Control táctil alterno: `Presentation/Shared/Components/NumericKeyboard.xaml` (evento `KeyboardPressed`, tags `1..9`, `0`, `Remove`, `Clear`), usado únicamente en `ReferenceToPayUC.xaml`. No lo reutilices en el formulario (cambiaría el diseño); sirve solo como referencia de estilo.

## 3. Reglas de negocio exactas (criterios de aceptación)

1. Validaciones **reactivas**: el usuario no puede dejar valores inválidos escritos, y el botón continuar refleja el estado en cada pulsación.
2. Celular colombiano: **exactamente 10 dígitos, solo números, empezando por `3`** → regex `^3\d{9}$`.
3. Documento: **solo dígitos**.
4. Nombres y apellidos: **sin números** (se permiten letras con tildes/ñ, espacios, apóstrofo y guion).
5. Correo: formato válido (`usuario@dominio.tld`).
6. Continuar habilitado **solo** si el formulario completo es válido (y la política de datos aceptada, como hoy).
7. Teclado **numérico** al tocar documento y celular.
8. Teclado **especializado para correo** (con `@` y `.` accesibles en la primera vista).
9. Todo debe funcionar con **mouse y con pantalla táctil**.
10. **Diseño visual idéntico** al actual.

## 4. Implementación

### 4.1 Validador (`Domain/Validation/PersonalInformationValidator.cs`)

Mantén la clase estática, sin dependencias de WPF, y **el mismo orden de mensajes** (para no alterar el flujo de modales). Propuesta:

```csharp
using System.Text.RegularExpressions;

namespace Domain.Validation
{
    /// <summary>
    /// Centraliza las reglas de entrada del formulario de información personal.
    /// No realiza navegación ni muestra UI, por lo que las reglas pueden probarse de forma aislada.
    /// </summary>
    public static class PersonalInformationValidator
    {
        // usuario@dominio.tld — sin espacios, con TLD alfabético de 2+ caracteres
        private static readonly Regex EmailPattern = new(
            @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));

        // Celular Colombia: 10 dígitos, inicia en 3
        private static readonly Regex MobilePattern = new(
            @"^3\d{9}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Letras (incluye tildes/ñ vía \p{L}), espacios, apóstrofo, guion y punto.
        private static readonly Regex NamePattern = new(
            @"^[\p{L}][\p{L}\s'\-\.]*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));

        private const int MaxDocumentLength = 15;
        private const int MaxNameLength = 60;

        public static string? GetValidationError(
            bool hasDocumentType,
            string? document,
            string? firstName,
            string? lastName,
            string? mobile,
            string? email)
        {
            document = document?.Trim();
            firstName = firstName?.Trim();
            lastName = lastName?.Trim();
            mobile = mobile?.Trim();
            email = email?.Trim();

            if (!hasDocumentType)
                return "Por favor, seleccione su tipo de documento.";
            if (string.IsNullOrWhiteSpace(document))
                return "Por favor, ingrese su número de documento.";
            if (!IsNumeric(document) || document.Length > MaxDocumentLength)
                return "Por favor, ingrese un número de documento válido (solo números).";
            if (string.IsNullOrWhiteSpace(firstName))
                return "Por favor, ingrese sus nombres.";
            if (!IsValidName(firstName) || firstName.Length > MaxNameLength)
                return "Por favor, ingrese sus nombres sin números ni símbolos.";
            if (string.IsNullOrWhiteSpace(lastName))
                return "Por favor, ingrese sus apellidos.";
            if (!IsValidName(lastName) || lastName.Length > MaxNameLength)
                return "Por favor, ingrese sus apellidos sin números ni símbolos.";
            if (string.IsNullOrWhiteSpace(mobile))
                return "Por favor, ingrese su número de celular.";
            if (!MobilePattern.IsMatch(mobile))
                return "Por favor, ingrese un celular válido: 10 dígitos y debe comenzar por 3.";
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !EmailPattern.IsMatch(email))
                return "Por favor, ingrese un correo electrónico válido.";

            return null;
        }

        public static bool IsNumeric(string value) => !string.IsNullOrEmpty(value) && value.All(char.IsDigit);

        public static bool IsValidName(string value) =>
            !string.IsNullOrWhiteSpace(value) && NamePattern.IsMatch(value) && value.Any(char.IsLetter);
    }
}
```

Notas:
- `AssingUser`/`AssignUser` de `FormUC.xaml.cs` escribe datos que vienen de la API; el validador debe aceptarlos (por eso el patrón de nombres es permisivo con tildes y espacios dobles no).
- No agregues reglas nuevas que el usuario no pidió (por ejemplo, longitud mínima de documento). El debounce de la API ya usa `>= 6` para consultar; eso se queda igual.

### 4.2 Sanitización reactiva en `FormUC.xaml.cs`

Objetivo: el usuario **no puede** dejar caracteres inválidos, sin tocar el diseño. Patrón recomendado (evita bucles de `TextChanged`):

```csharp
private bool _isUpdatingField;

/// <summary>Normaliza el texto del campo y lo sincroniza con el modelo, sin reentrar.</summary>
private void SanitizeField(TextBox textBox, Func<string, string> sanitize, Action<string> assign)
{
    if (_isUpdatingField) return;

    var sanitized = sanitize(textBox.Text ?? string.Empty);
    _isUpdatingField = true;
    try
    {
        if (!string.Equals(textBox.Text, sanitized, StringComparison.Ordinal))
            textBox.Text = sanitized;   // re-dispara TextChanged, bloqueado por el flag
        assign(sanitized);              // sincroniza el modelo aunque el binding sea LostFocus
    }
    finally
    {
        _isUpdatingField = false;
    }

    UpdateContinueState();
}
```

Sanitizadores concretos:

```csharp
private static string KeepDigits(string value) =>
    new string(value.Where(char.IsDigit).ToArray());

private static string KeepDigitsMax(string value, int max)
{
    var digits = KeepDigits(value);
    return digits.Length > max ? digits[..max] : digits;
}

private static string KeepNameCharacters(string value)
{
    var chars = value.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c is '\'' or '-' or '.');
    return new string(chars.ToArray());  // deja que el teclado escriba tildes/ñ; corta números al instante
}

private static string KeepEmailCharacters(string value) =>
    new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
```

Enganches (atributos nuevos en `FormUC.xaml`, sin tocar nada visual):

| Campo | Handler | Sanitizador |
|---|---|---|
| Documento | ya tiene `TxtDocument_TextChanged` → amplíalo | `KeepDigits` |
| Nombres | `TextChanged="TxtFirstName_TextChanged"` (nuevo) | `KeepNameCharacters` + trim final |
| Apellidos | `TextChanged="TxtLastName_TextChanged"` (nuevo) | `KeepNameCharacters` + trim final |
| Celular | `TextChanged="TxtMobile_TextChanged"` (nuevo) | `KeepDigitsMax(..., 10)` |
| Correo | ya tiene `TxtEmail_changed` → amplíalo | `KeepEmailCharacters` |

Regla de oro para documento: **sanitiza antes** de la consulta a la API, para que la comparación `!string.Equals(textBox.Text, document, ...)` del debounce siga siendo válida. Mantén intactos `_documentLookupCancellation`, el `Task.Delay(400)` y el `Cursor`.

Sobre celular: sanitiza solo (dígitos, máx. 10) y **valida el prefijo `3` al enviar**. No insertes el `3` automáticamente ni borres el primer dígito si no es `3`: eso altera lo que el usuario ve y puede confundir en el quiosco. Si prefieres bloquear el prefijo en vivo, pregunta antes al usuario.

Sobre el binding: los `TextBox` usan `Text="{Binding X, Mode=TwoWay}"` con `UpdateSourceTrigger` por defecto (`LostFocus`). Como el teclado virtual es una ventana aparte y el código de `VKeyboard` escribe directo sobre la propiedad `Text`, **asigna también el modelo explícitamente** (`assign(sanitized)`) para que el estado del botón nunca dependa del momento en que el binding haga push.

### 4.3 Estado del botón Continuar (criterio 6)

En `FormUC.xaml.cs`:

```csharp
private void UpdateContinueState()
{
    var info = _ts.customFlows.generaLInformationClient;

    bool policyAccepted = BtnForm.Visibility == Visibility.Visible; // lo mantiene el ToggleButton
    bool formValid = PersonalInformationValidator.GetValidationError(
        TypeDocument.SelectedItem != null,
        info.Document, info.FirstName, info.LastName, info.Mobile, info.Email) == null;

    BtnForm.IsEnabled = policyAccepted && formValid;
}
```

Puntos obligatorios:
- Llama `UpdateContinueState()` al final del constructor, en `OnSelectionChanged`, en cada `TextChanged` de los cinco campos, en `ToggleButton_Checked` y **después de `AssignUser(...)`** (datos autocompletados desde la API también deben habilitar el botón).
- En el constructor suscríbete al modelo para cubrir cambios que no pasan por tus handlers:
  ```csharp
  _ts.customFlows.generaLinformationClient.PropertyChanged += (_, _) => UpdateContinueState();
  ```
- Reestructura `ToggleButton_Checked` para que cambie `Visibility` y llame a `UpdateContinueState()`; la visibilidad del botón (política) se mantiene como hoy.
- **Diseño idéntico**: un `Image` en WPF **no tiene estado visual de "deshabilitado"** por defecto, así que `IsEnabled = false` bloquea mouse y táctil sin cambiar un solo píxel. No pongas `Opacity`, no cambies `Source` ni filtros para "grisear" el botón salvo que el usuario lo pida explícitamente.
- Conserva el guard en `BtnForm_MouseDown` (validación + `InfoModal`) como defensa en profundidad, sin alterar los mensajes ni el flujo a `ReferenceToPayUC`.

### 4.4 Teclado numérico y de correo (criterios 7, 8)

El teclado virtual es compartido: `App.xaml.cs:22` registra el handler de clase sobre `TextBox` y abre la ventana para **todos** los campos. Hay que tipar el teclado **por campo**, sin alterar el comportamiento por defecto del resto de la app.

1. **Enum** (`KeyboardType.cs`): agrega `Numeric` y `Email` conservando `Alphabet` y `Special`.

2. **Tipo inicial en el ViewModel** (`VirtualKeyboardViewModel.cs`): agrega un constructor
   `public VirtualKeyboardViewModel(string initialValue, KeyboardType keyboardType)` y deja el constructor actual delegando en el nuevo con `KeyboardType.Alphabet` (así no rompes nada).

3. **Vistas nuevas** (`Views/NumericView.xaml`, `Views/EmailView.xaml`): copia el patrón de `AlphabetView.xaml` (`x:ClassModifier="internal"`, `UserControl.Resources` con `KeyboardCore.xaml`, `Button` con `Style="{DynamicResource KeyboardButtonStyle}"`, `Command="{Binding AddCharacter}"`, `CommandParameter` ligado a `Content` del propio botón).
   - `NumericView`: 3x4 con `1..9`, `0` y un `RepeatButton` de borrado usando el recurso existente `BackspaceButton` (ya está en `KeyboardCore.xaml`). La barra de aceptar/cancelar/limpiar la aporta `KeyboardValueView`, así que **no** la dupliques.
   - `EmailView`: alfabeto estándar + fila inferior con `@`, `.`, `_`, `-` visibles directamente (hoy `@` obliga a cambiar a la vista especial con `abc/123`). Mantén el botón que alterna a `Special`.

4. **Converter** (`KeyboardTypeConverter.cs`): añade los dos `case` nuevos.

5. **`VKeyboard.cs`**: propiedad adjunta + sobrecarga:
   ```csharp
   public enum KeyboardInputKind { Default, Numeric, Email }

   public static readonly DependencyProperty InputKindProperty = DependencyProperty.RegisterAttached(
       "InputKind", typeof(KeyboardInputKind), typeof(VKeyboard),
       new PropertyMetadata(KeyboardInputKind.Default));

   public static void SetInputKind(DependencyObject element, KeyboardInputKind value) =>
       element.SetValue(InputKindProperty, value);

   public static KeyboardInputKind GetInputKind(DependencyObject element) =>
       (KeyboardInputKind)element.GetValue(InputKindProperty);

   public static Task<string> OpenAsync(string initialValue, KeyboardType type) { /* usa el ctor nuevo del VM */ }
   ```
   - En el handler de `Listen<T>` (que es `static`), lee `s is DependencyObject d ? GetInputKind(d) : KeyboardInputKind.Default` y decide el tipo de teclado a abrir y el filtro posterior.
   - El filtro posterior es la red de seguridad táctil: al volver del teclado, aplica el mismo sanitizador (`dígitos` para `Numeric`, sin espacios para `Email`, texto libre para `Default`) **antes** de `prop.SetValue(...)`, para que un campo numérico jamás reciba letras ni aunque el operador toque otra vista.
   - **Conserva** `App.xaml.cs:22` `VKeyboard.Listen<TextBox>(e => e.Text);` con esa firma (o actualiza la llamada en el mismo commit). Conserva también el guard `if (s is AdvancedTextBox) return;`.

6. **XAML del formulario** (`FormUC.xaml`): solo atributos nuevos, p. ej.
   ```xml
   xmlns:keyboard="clr-namespace:VirtualKeyboard.Wpf"
   ...
   <TextBox Text="{Binding Document, Mode=TwoWay}"
            TextChanged="TxtDocument_TextChanged"
            keyboard:VKeyboard.InputKind="Numeric" />
   <TextBox Text="{Binding Mobile, Mode=TwoWay}"
            TextChanged="TxtMobile_TextChanged"
            keyboard:VKeyboard.InputKind="Numeric" />
   <TextBox Text="{Binding Email, Mode=TwoWay}"
            TextChanged="TxtEmail_changed"
            keyboard:VKeyboard.InputKind="Email" />
   ```
   Nombres y apellidos se quedan en el teclado alfabético actual (`Default`).

### 4.5 Mouse y táctil (criterio 9)

- En WPF el input táctil se promueve a eventos de mouse; los handlers actuales son `MouseDown` / `PreviewMouseLeftButtonDown`, y así deben quedarse. **No** los cambies a `Click` ni uses `IsHitTestVisible="False"` ni `IsEnabled="False"` sobre controles que deban seguir operables.
- No agregues `TouchDown`/`StylusDown` nuevos salvo que haga falta; si necesitas uno, registra ambos (`MouseDown` + `TouchDown`, como hace `TouchableControl.cs`) para no perder ninguno de los dos modos.
- Verifica en pantalla táctil que: el teclado numérico aparece al tocar documento/celular, el de correo al tocar correo, y que ningún `TextChanged` propio interfiere con el cursor del teclado (`CaretPosition` lo maneja `AdvancedTextBox`).

## 5. Lo que NO debes hacer

- No tocar `Domain/Peripherals/**`, `Domain/ApiService/**`, `EventLogger`, `InternetConnectionManager`, `NumericKeyboard.xaml` ni las pantallas `PaymentUC`, `ReferenceToPayUC`, `PublicityUC`, `WelcomeUC`, `FinishUC`.
- No modificar `WPFHospitalVeterinarioUT.csproj` ni las constantes de compilación (`NO_PERIPHERALS` se conserva en Debug).
- No cambiar textos visibles, tamaños, márgenes, fuentes, `CornerRadius`, `FontSize`, recursos `StaticResource` ni el `Viewbox`.
- No agregar librerías NuGet ni proyectos nuevos (incluido un proyecto de tests) sin autorización.
- No reordenar ni cambiar los mensajes de validación existentes más allá de las reglas anteriores.
- No commitear binarios, `.user`, `obj/`, `bin/` ni artefactos de build.

## 6. Entrega

Un commit (o commits pequeños y temáticos) en tu rama, y un PR contra `main` con:

- Descripción del cambio por criterio (1..10) con los archivos tocados.
- Captura o confirmación explícita de que **no** hubo cambios visuales (solo atributos XAML nuevos).
- Riesgos conocidos y lo que quedó pendiente de probar en hardware.

## 7. Verificación

No hay `dotnet` en el entorno de revisión ni tests automatizados. El responsable debe ejecutar en Windows:

```powershell
dotnet restore .\src\WPFHospitalVeterinarioUT.sln
dotnet build .\src\WPFHospitalVeterinarioUT.sln -c Debug  -p:Platform=x86
dotnet build .\src\WPFHospitalVeterinarioUT.sln -c Release -p:Platform=x86
```

Matriz de pruebas manuales (Debug = `NO_PERIPHERALS`):

1. Documento: escribir/pegar `12a3x` → el campo queda `123`; se dispara la consulta a la API a partir de 6 dígitos y no hay consultas duplicadas al escribir rápido.
2. Documento con menos de 6 dígitos: sin consulta, sin error visible.
3. Celular: intentar letras o símbolos → imposible; intentar 11 dígitos → se corta en 10.
4. Celular `3123456789` → válido. `2123456789`, `31234567`, `31234567890` → botón deshabilitado y, al forzar, modal de error.
5. Nombres/Apellidos: `Juan1` → el `1` no entra; `María José`, `O'Connor`, `Muñoz-Gómez` → válidos.
6. Correo: `ana@correo` → inválido; `ana@correo.com`, `ana.perez+ut@correo.com.co` → válidos.
7. Botón Continuar: `Image` sin cambios de píxeleo, deshabilitado mientras algo sea inválido, habilitado solo con todo válido **y** toggle de política marcado.
8. Autocompletado: documento registrado en la API → se llenan nombres, apellidos, celular y correo, y el botón se habilita sin tocar nada.
9. Teclados: al tocar documento y celular abre el teclado **numérico**; al tocar correo abre el de correo con `@` y `.` visibles; nombres/apellidos siguen con el alfabético.
10. Mouse y táctil: repetir 1..9 con ambos; comparar cada pantalla contra el diseño original (mismo layout, mismos assets).
11. Output de Visual Studio sin `System.Windows.Data Error` durante todo el recorrido; sin excepciones no controladas en `Log_application`.
12. Navegación intacta: atrás/salir/política de datos y el timer de 03:00 (`STR_TIMER`) siguen funcionando.
