# Revisión técnica del WPF

Fecha: 2026-10-07

## Restricciones respetadas

- No se modificó `Domain/Peripherals` ni código de sus dispositivos.
- Se conserva `NO_PERIPHERALS` en Debug y el comportamiento de periféricos en Release.
- No se modificó la composición visual, tamaños, estilos, imágenes ni navegación visible.
- Se mantienen las carpetas de logs `Log_application`, `Log_integration` y `Log_peripherals`.
- Los controles continúan atendiendo los eventos existentes de mouse/táctil; esta fase no altera el diseño.

## Acciones completadas

1. **Eliminación de base local**
   - Eliminado el proyecto `LocalDataBase`, SQLite, Entity Framework y AutoMapper.
   - Eliminada la copia local de transacciones, detalles y datos personales.
   - La transacción utiliza exclusivamente las APIs existentes.
   - Eliminado el instalador de SQLite incluido en recursos.

2. **Validaciones**
   - Reglas del formulario personal centralizadas en `Domain/Validation/PersonalInformationValidator.cs`.
   - La clase no depende de WPF y queda preparada para pruebas unitarias.

3. **Cola de API e hilos**
   - Cola convertida a productor/consumidor seguro mediante `ConcurrentQueue`.
   - Se garantiza un consumidor y se limitan los reintentos transitorios.
   - Se agregó espera incremental para impedir ciclos de CPU y repetición ilimitada de logs.

4. **Logs**
   - Se conserva `EventLogger` y su estructura de carpetas.
   - La escritura a archivos ahora es atómica entre hilos.
   - Los errores internos del logger se reportan en Debug sin provocar recursión de logs.

5. **Conectividad**
   - Ping asíncrono y liberación correcta de recursos.
   - Monitoreo reducido de 500 ms a 2 s para un quiosco estable y menor consumo.
   - Cancelación controlada y protección contra monitores simultáneos.

## Revisión estática

- No quedan referencias de código/proyecto a SQLite, `LocalDataBase`, Entity Framework ni `ObjMapper`.
- Se detectaron 103 expresiones de binding. No se cambió ninguna porque requieren validación visual y de salida de binding en Windows para garantizar diseño idéntico.
- Los recursos potencialmente no referenciados de forma literal se conservaron: el proyecto copia `Assets/**` y pueden resolverse dinámicamente. No se eliminaron imágenes o fuentes sin una ejecución completa que confirme su ausencia.
- Los `async void` asociados a eventos WPF se conservaron. Los callbacks de periféricos quedaron intactos por restricción explícita.

## Validación pendiente en Windows

El entorno de revisión no dispone del SDK `dotnet`, por lo que el responsable de pruebas debe ejecutar:

```powershell
dotnet restore .\src\WPFHospitalVeterinarioUT.sln
dotnet build .\src\WPFHospitalVeterinarioUT.sln -c Debug -p:Platform=x86
dotnet build .\src\WPFHospitalVeterinarioUT.sln -c Release -p:Platform=x86
```

Pruebas manuales mínimas:

1. Debug inicia sin periféricos (`NO_PERIPHERALS`).
2. Formulario valida en el mismo orden y continúa al pago sin almacenamiento local.
3. Login, creación, actualización, detalle y calificación llegan a la API.
4. Corte y recuperación de red muestran/cierran un único modal.
5. Logs simultáneos de aplicación/API no se intercalan ni bloquean.
6. Release conserva inicialización real de periféricos.
7. Recorrido completo con mouse y en pantalla táctil, comparando cada vista con el diseño original.
8. Revisar Output de Visual Studio buscando `System.Windows.Data Error` durante todo el recorrido.
