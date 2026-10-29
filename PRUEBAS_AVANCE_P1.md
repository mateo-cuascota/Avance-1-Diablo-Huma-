# Diablo Huma: Guardián de los Andes · Avance Parcial 1

## Idea del juego
Plataformas 2D con identidad andina ecuatoriana. Durante el **Inti Raymi**, los **Supay** (espíritus de la oscuridad) roban los **Inti**, los soles sagrados. El **Diablo Huma**, guardián de los Andes, usa su **látigo sagrado** para vencerlos, recuperar todos los Inti y devolver la luz a su pueblo.

**Flujo:** Menú principal → Historia (6 escenas narradas) → Nivel 1 → Victoria o Game Over → Reintentar o volver al menú.

## Cómo se cumple cada punto de la rúbrica

| Punto | Implementación | Archivos |
|---|---|---|
| **Idea del juego** | Menú con título y personajes, e historia con texto animado letra por letra, fundidos y zoom del fondo | `MenuPrincipal.cs`, `Historia.cs` |
| **Movimiento fluido** | Aceleración y frenado, salto variable, *coyote time*, *jump buffer*, caída rápida, detector de suelo con `OverlapBox`, Rigidbody interpolado y material sin fricción | `MovimientoJugador.cs` |
| **Animaciones** | Personaje: idle, caminar, saltar y latigazo. Supay: flotar, herido y disparar (Animator con 3 estados). Cóndor que aletea. Bola de fuego animada. Monedas que giran | `Generados/Animaciones/*` |
| **Colisiones y detectores** | Látigo con hitbox (`IGolpeable`), pisotón sobre enemigos, daño con empuje, pinchos, caída al vacío, monedas, power-ups, puntos de control y meta bloqueada hasta tener todos los Inti | `EnemigoSupay.cs`, `Pinchos.cs`, `PuntoControl.cs`, `MetaNivel.cs` |
| **Mecánica original: máscara de dos caras** | Tecla C: cara **Inti** (sol, látigo normal) o **Killa** (luna: látigo de fuego con doble daño, más salto y más velocidad). Nace del Aya Huma real, cuya máscara tiene dos caras | `MascaraDual.cs` |
| **Gráficos** | Suelo de piedra andina (muro inca) con pasto, en mosaico; HUD con Canvas e íconos (máscara = vidas, sol = Inti, sol/luna = cara actual) y botones en pausa y final | `HUDJuego.cs`, `HUD_Canvas` |
| **Jefe final** | Gran Supay con 3 ataques (abanico de fuego, embestida y lluvia de rocas), fase 2 furiosa, barra de vida y arena que se cierra | `JefeSupay.cs`, `ZonaJefe.cs` |
| **Ambiente** | Fondo parallax de 4 capas (cielo, nubes, colinas con terrazas y pasto en primer plano), *hit-stop*, sacudida de cámara, polvo y 10 sonidos sintetizados por código | `CapaParallax.cs`, `Audio/Generados` |
| **Instanciación** | Rocas que caen (generador), bolas de fuego del Supay tirador, power-ups que sueltan los enemigos y efectos de partículas (`Destello`) | `GeneradorRocas.cs`, `ProyectilFuego.cs`, `PowerUp.cs`, `Destello.cs` |
| **Pruebas y depuración** | Lista de pruebas (abajo), errores corregidos y *Gizmos* para ver en la escena el área del látigo, el detector de suelo, la patrulla y el rango de visión | |

## Errores encontrados y corregidos
1. **Detección de suelo:** `OnCollisionExit2D` ponía `enSuelo = false` al dejar de tocar *cualquier* objeto (por ejemplo, una pared), lo que impedía saltar. Ahora se usa una caja bajo los pies.
2. **`ReaparicionJugador`:** daba `NullReferenceException` si `puntoInicio` no estaba asignado. Ahora usa la posición inicial.
3. **Rendimiento:** se recorrían los parámetros de todos los Animator en cada frame. Ahora se revisan una sola vez en `Awake`.
4. **Látigo decorativo:** solo hacía la animación y el sonido. Ahora daña enemigos, rocas y bolas de fuego.
5. **El personaje se pegaba a las paredes** en el aire por la fricción. Se agregó un material sin fricción.
6. **La cámara temblaba** al seguir al jugador. Se activó la interpolación y se añadieron límites.
7. **API obsoleta de Unity 6.6:** `FindFirstObjectByType` se reemplazó por `FindAnyObjectByType`.

## Herramientas de prueba (dentro del juego)
| Tecla | Función |
|---|---|
| **F1** | Panel de depuración: FPS, posición, velocidad, si está en el suelo, cara, vidas, Inti, rocas y fuegos activos |
| **F2** | Teletransporte al siguiente punto de control |
| **F3** | Restaura las vidas |
| **F4** | Teletransporte a la arena del jefe |

En la vista *Scene*, al seleccionar un objeto se dibujan *Gizmos* con el área del látigo, el detector de suelo, la patrulla de los Supay, el rango de los tiradores, la zona de rocas y la altura del jefe.

## Lista de pruebas (marca ✔ al probar)

### Máscara de dos caras
- [ ] C cambia entre INTI y KILLA: cambia el ícono del HUD, el escenario anochece en KILLA y suena el cambio
- [ ] La cabeza del Diablo Huma gira al cambiar de cara
- [ ] Con INTI el látigo es normal: un Supay cae en 2 golpes
- [ ] Con KILLA el látigo brilla en naranja, lanza llamas y hace doble daño: un Supay cae en 1 golpe
- [ ] Con KILLA el Diablo Huma corre más rápido y salta más alto
- [ ] Los carteles del tutorial aparecen una vez (x≈8, x≈25 y la entrada a la arena)
- [ ] El Gran Supay tiene 6 de vida: con látigo de fuego cae en 3 golpes
- [ ] Las plataformas de la arena se alcanzan con un salto normal desde el suelo

### Gráficos y HUD
- [ ] Los suelos y plataformas son de piedra con pasto y no se ven cortes en el mosaico
- [ ] Las vidas son máscaras que se oscurecen al perderlas y el choclo las recupera
- [ ] En pausa, victoria y derrota los botones Continuar, Reintentar y Menú funcionan con el mouse

### Menú e historia
- [ ] El juego inicia en el menú y suena la música
- [ ] ↑ ↓ y el mouse cambian el botón seleccionado
- [ ] "Controles" abre el panel y ESC lo cierra
- [ ] "Jugar" hace un fundido y pasa a la historia
- [ ] ESPACIO completa el texto y luego avanza a la siguiente escena
- [ ] ESC salta la historia y carga el Nivel 1

### Movimiento
- [ ] Acelera y frena suave, sin deslizarse demasiado
- [ ] Un toque corto de salto da un salto bajo; mantenerlo da un salto alto
- [ ] Puede saltar justo después de salir de un borde (coyote time)
- [ ] Si presiona saltar un poco antes de aterrizar, el salto igual sale (buffer)
- [ ] No se queda pegado a las paredes de las plataformas
- [ ] Las animaciones de idle, caminar y saltar cambian correctamente

### Combate y colisiones
- [ ] El látigo (X) vence a un Supay en 2 golpes y a un tirador en 3
- [ ] Al saltar sobre un Supay muere y el jugador rebota
- [ ] Tocar un Supay quita 1 vida, empuja al jugador y lo hace parpadear
- [ ] Durante el parpadeo no recibe más daño
- [ ] El tirador dispara solo cuando el jugador está cerca
- [ ] El látigo destruye bolas de fuego y rocas
- [ ] Las rocas tiemblan antes de caer y se rompen al tocar el suelo
- [ ] Pinchos y caída al vacío regresan al último punto de control

### Objetos
- [ ] Cada Inti suma al contador y desaparece con animación
- [ ] La bandera cambia a la del Ecuador y aparece "¡Punto de control!"
- [ ] El jugador empieza con 5 vidas, el choclo da +1 (máximo 6) y al entrar a la arena del jefe recupera todas
- [ ] La pluma de cóndor da doble salto por 10 s (se ve el contador)
- [ ] Los enemigos a veces sueltan power-ups

### Fondo con profundidad (parallax)
- [ ] Al caminar, el cielo se mueve muy lento, las colinas más rápido y el pasto del frente más rápido que el jugador
- [ ] Las nubes avanzan solas aunque el jugador esté quieto
- [ ] El fondo cubre todo el nivel, sin bordes ni cortes visibles

### Sensación de golpe y sonidos
- [ ] Al golpear o vencer a un Supay la imagen se congela un instante y la cámara tiembla
- [ ] Al recibir daño se oye el sonido de daño y la cámara se sacude
- [ ] Al saltar y al aterrizar desde lo alto sale polvo de los pies y se oye un sonido
- [ ] Se escuchan los sonidos de disparo, roca rota, punto de control y power-up

### Jefe final: Gran Supay
- [ ] Al entrar en la arena aparecen los muros morados, la cámara se fija y el jefe ruge
- [ ] Aparece la barra de vida del jefe y la música se acelera
- [ ] Abanico: tiembla y luego lanza 5 bolas de fuego hacia el jugador
- [ ] Embestida: tiembla encima del jugador, cae en picada y queda aturdido
- [ ] Lluvia de rocas: caen rocas por toda la arena
- [ ] El látigo le quita 1 de vida y el pisotón (desde las plataformas) le quita 2
- [ ] Con la mitad de vida aparece el mensaje "¡furioso!" y ataca un poco más rápido
- [ ] Al morir explota varias veces, suelta un choclo y los muros desaparecen

### Fin del nivel
- [ ] La meta sin todos los Inti muestra cuántos faltan
- [ ] La meta con el jefe vivo dice "Derrota al Gran Supay para abrir la meta"
- [ ] Con todos los Inti y el jefe vencido aparece "¡Nivel completado!" con el tiempo y los Supay vencidos
- [ ] Con 0 vidas aparece "Game Over"
- [ ] R reinicia, M vuelve al menú y ESC pausa o continúa
