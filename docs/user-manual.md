# ST_Holmes - Manual de usuario

**Version 1.0 - Windows 10/11 (64 bits)**

ST_Holmes supervisa de forma preventiva las unidades internas del equipo. Lee
la informacion SMART, conserva una cronologia local y explica los cambios que
pueden requerir atencion. No escribe en los discos y no sustituye una copia de
seguridad verificada.

## 1. Que instala ST_Holmes

El instalador deja preparados tres elementos que trabajan juntos:

- un servicio de Windows que realiza las mediciones con los permisos necesarios;
- un icono en el area de notificacion que muestra el estado y los avisos;
- un panel grafico donde se consulta cada unidad y su trayectoria.

El instalador ya incluye el componente `smartctl` de smartmontools y el entorno
de .NET. No es necesario instalar programas adicionales.

## 2. Instalacion

1. Descargue `ST_Holmes-Setup-<version>.exe` desde la publicacion oficial.
2. Ejecute el archivo y acepte la solicitud de administrador de Windows.
3. Siga el asistente hasta completar la instalacion.
4. Compruebe que aparece el icono de ST_Holmes junto al reloj.

La version publica no esta firmada digitalmente. Windows puede mostrar
"Editor desconocido" o una advertencia de Microsoft Defender SmartScreen. Si
el archivo procede de la publicacion oficial, elija **Mas informacion** y
**Ejecutar de todas formas**. Puede comparar su SHA-256 con el publicado en la
misma descarga.

Al instalarse, ST_Holmes realiza una primera medicion y despues repite la
supervision aproximadamente cada seis horas. El servicio tambien realiza una
medicion cuando se inicia, por ejemplo despues de reiniciar Windows.

## 3. El icono del area de notificacion

El color resume el estado mas importante observado:

- **Verde:** no hay incidencias actuales que requieran atencion.
- **Amarillo:** existe una condicion que conviene revisar o vigilar.
- **Rojo:** se ha detectado una condicion critica que requiere actuar.

Haga doble clic en el icono para abrir el panel. El menu contextual permite
abrirlo, probar los distintos avisos y cerrar el agente visual. Cerrar el agente
no detiene el servicio ni la supervision de los discos.

La opcion **Probar avisos** solo presenta ejemplos visuales. No crea incidencias,
no cambia la salud de una unidad y no modifica los datos de supervision.

## 4. Panel principal

La columna izquierda muestra las unidades internas actuales. Al seleccionar una
unidad, el panel presenta:

- el estado explicado con sus indicadores mas relevantes;
- las limitaciones de la medicion;
- la trayectoria observada entre capturas;
- una recomendacion proporcionada al estado;
- la cronologia de mediciones y una grafica por metrica.

**Actualizar** vuelve a leer la informacion ya guardada. No obliga al servicio a
interrogar el hardware en ese momento. Una nueva fila aparecera cuando termine
el siguiente ciclo de supervision.

### Valores no disponibles

"No disponible" no significa cero ni implica por si solo una averia. Significa
que esa unidad, protocolo o controladora no ha proporcionado el dato. Es normal,
por ejemplo, que un NVMe no publique los mismos campos ATA de sectores
reasignados o pendientes que un disco mecanico.

### Una sola medicion

Con una unica captura ST_Holmes puede describir el estado actual, pero aun no
puede calcular una trayectoria. La tendencia comienza a ser evaluable cuando
existen al menos dos mediciones comparables.

## 5. Avisos e incidencias

Cuando aparece una condicion relevante, ST_Holmes combina una notificacion de
Windows con una tarjeta explicativa. El aviso indica la unidad, la gravedad, lo
observado y la recomendacion. Cada incidencia queda tambien en el historial del
panel aunque la notificacion ya se haya cerrado.

- **Informacion:** cambio registrado sin senal de deterioro.
- **Atencion:** merece observacion, pero normalmente no exige una sustitucion inmediata.
- **Advertencia:** recomienda revisar copias de seguridad y planificar una actuacion.
- **Critico:** recomienda proteger los datos y sustituir o retirar la unidad cuanto antes.

Los avisos describen evidencias SMART y tendencias; no predicen una fecha exacta
de fallo. Un disco puede fallar de forma repentina sin cambios SMART previos.

Una condicion persistente puede mantener la unidad en amarillo sin abrir nuevas
ventanas. ST_Holmes vuelve a avisar cuando el indicador aumenta, cambia de
gravedad o aparece una incidencia distinta; una medicion estable queda
registrada en la cronologia sin repetir el aviso anterior.

## 6. Unidades supervisadas

La supervision automatica se centra en HDD, SSD SATA y NVMe internos conectados
a las controladoras del equipo. Las unidades externas conectadas por USB se
ignoran deliberadamente: son dispositivos transitorios y no se incorporan a la
cronologia ni a los avisos.

Algunas controladoras pueden ocultar parte de SMART o no ser compatibles. En ese
caso ST_Holmes conserva lo que haya podido adquirir, lo identifica como parcial
y explica la limitacion sin convertirla automaticamente en una averia.

## 7. Datos, privacidad y seguridad

Todos los datos permanecen en el equipo. ST_Holmes no necesita una cuenta, no
envia telemetria y no sube capturas a Internet.

- Historial y base de datos: `%ProgramData%\ST_Holmes`
- Estado de notificaciones por usuario: `%LocalAppData%\ST_Holmes`
- Programa: `%ProgramFiles%\ST_Holmes`

Las consultas a SMART son de solo lectura. ST_Holmes no inicia pruebas SMART
largas, no cambia la configuracion del dispositivo y no escribe datos en las
unidades supervisadas.

## 8. Actualizacion y desinstalacion

Para actualizar, ejecute el instalador de la nueva version. La instalacion se
actualiza en el mismo lugar y conserva el historial.

Para desinstalar, abra **Configuracion > Aplicaciones > Aplicaciones instaladas**,
busque ST_Holmes y elija **Desinstalar**. El historial de `%ProgramData%` se
conserva para evitar la perdida accidental de evidencias. Puede eliminar esa
carpeta manualmente mas adelante si esta seguro de que ya no la necesita.

## 9. Solucion de problemas

### No aparece el icono

Espere unos segundos tras iniciar sesion. Si no aparece, abra ST_Holmes desde el
menu Inicio. Reiniciar Windows tambien vuelve a iniciar el agente y el servicio.

### El panel no muestra una captura nueva

El boton Actualizar no inicia una medicion; solo recarga el catalogo. Espere al
siguiente ciclo. El servicio realiza ademas un ciclo al arrancar Windows.

### Una unidad no aparece

Compruebe primero si es externa por USB, ya que esas unidades se excluyen. Si es
interna, la controladora puede no exponer SMART o el dispositivo puede no estar
disponible para Windows.

### Aparecen campos como "No disponible"

Es una limitacion del dato que publica el dispositivo o la controladora, no una
equivalencia con cero y tampoco una prueba de fallo.

### Windows muestra una advertencia al instalar

La version gratuita se distribuye sin certificado de firma de codigo. Descargue
siempre desde la publicacion oficial y verifique el SHA-256 publicado.

## 10. Buenas practicas

- Mantenga al menos una copia de seguridad desconectada o independiente.
- Compruebe periodicamente que las copias se pueden restaurar.
- No espere a que aparezca un aviso critico para proteger datos importantes.
- Ante un aviso rojo, reduzca el uso de la unidad y copie primero lo irremplazable.
- Conserve este manual junto al instalador para futuras reinstalaciones.

## 11. Alcance y soporte

ST_Holmes es software gratuito y de codigo abierto. El repositorio contiene el
codigo, las instrucciones de compilacion y el registro de incidencias del
proyecto. No ofrece garantia de que un dispositivo no vaya a fallar ni sustituye
el diagnostico profesional cuando los datos son irremplazables.

Repositorio oficial: `https://github.com/juancarHub/ST_Holmes`

