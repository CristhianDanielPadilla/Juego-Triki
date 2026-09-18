# Juego-Triki

Tres en raya (Triki) 2D en Unity **6000.6.0f1**, URP con **Renderer 2D**.

## Estructura

```
Assets/
  _Project/            Todo lo propio del juego (el guion bajo lo deja arriba en el Project window)
    Art/Sprites/       Sprites del tablero, fichas, UI
    Audio/
    Prefabs/
    Scenes/            Menu.unity (arranque) y Game.unity
    Scripts/
      Core/            Triki.Core     - logica pura, SIN UnityEngine (noEngineReferences)
      Gameplay/        Triki.Gameplay - MonoBehaviours, input, tablero, guardado del historico
      UI/              Triki.UI       - menu, HUD, navegacion entre escenas
    UI/                UI Toolkit: UXML, Triki.uss (paleta), TrikiTheme.tss, TrikiPanelSettings
  Tests/EditMode/      Triki.Tests.EditMode - NUnit sobre Core, persistencia y contrato UXML
  Settings/            URP (RP Assets + Renderer 2D), Volume Profile e InputSystem_Actions
```

Build Settings: `Menu` (indice 0) y `Game`. Los nombres de escena viven solo en
`Triki.UI.SceneNavigator`; si se renombra una escena, se cambia ahi.

## UI

- UI Toolkit, no uGUI. Los scripts buscan elementos por `name`; `UiContractTests` falla si un
  UXML pierde un nombre que el codigo usa. Al renombrar en UXML, actualizar codigo y test.
- Los colores de jugador estan en `Triki.uss` y en `BoardView` (inspector): mantenerlos iguales.
- Pantallas: `CameraFitter` (en la camara de Game) encaja el tablero en la zona segura dejando
  franjas arriba/abajo para el HUD (`_topReserved`/`_bottomReserved`, fraccion de alto). Si el HUD
  crece, subir esas reservas. `SafeAreaPadding` aplica la zona segura a cada UIDocument.
  La matematica esta en funciones puras (`CameraFit`, `SafeAreaInsets`) con tests.
- `SceneWiringTests` abre las escenas en preview y falla si falta un script o una referencia.
  Si se edita una escena a mano (YAML), correr los tests antes de commitear.
- Los clics del tablero entran por InputSystem (`<Pointer>/press`), al margen de UI Toolkit, asi
  que la UI no los detiene sola. El area sensible de las casillas de abajo (`_pickRadius`) sobresale
  del tablero y se solapa con los botones del HUD, asi que `GameHud` implementa `IPointerBlocker`
  (interfaz en Gameplay, para no invertir las dependencias) y `GameController` le pregunta antes de
  tocar el tablero. El filtro es `panel.Pick`: **todo lo que no sea un boton lleva
  `picking-mode="Ignore"` en `GameHud.uxml`**, o se tragaria los clics. `UiContractTests` lo exige.
- Historico (`MatchHistory`, Core), tres registros **independientes** (`HistorySection`):
  - `Overall` (pestaña General): todas las partidas por color, sin importar el modo. Incluye las
    de v0.1.x, que no guardaban el modo.
  - `VsAi` (`AiMatchStats`): victorias/derrotas/empates del humano por dificultad **y por el color
    que llevaba** (el cuadro tiene un grupo de columnas por color).
    - Las partidas de v0.3.0 y anteriores no guardaban el color: se quedan en el hueco
      `Player.None` (`ColorlessGames`). Cuentan en los totales por dificultad pero no en ninguna
      de las dos columnas, asi que Rojo + Azul puede no dar el total. La pestaña lo avisa con
      `ai-colorless`, que solo se ve si hay alguna. No inventar un reparto: el dato no existe.
  - `TwoPlayer` (`MatchStats`): por color.
  - Cada partida se anota en `Overall` y en la de su modo (`HistoryRecorder.Record`).
  - Borrar (`MatchHistory.Clear(section)`) solo vacia ese registro; los demas no cambian, por eso
    General puede no coincidir con la suma de los otros tras borrar.
- UI del historico: `HistoryView` pinta las pestañas y emite `DeleteRequested`; `MainMenuController`
  pide confirmacion con `ConfirmDialog` (modal, foco en Cancelar, Escape cancela) y borra releyendo
  del disco. La tabla por color es la plantilla `UI/ColorStatsTable.uxml`, instanciada dos veces:
  buscar sus nombres dentro de cada instancia, no en la raiz.
- El historico se guarda en `Application.persistentDataPath/triki-stats.json` (`StatsRepository`),
  formato `version: 3`. En Windows: `%USERPROFILE%\AppData\LocalLow\CDCompany\Juego-Triki\`.
  - Versiones del formato: 1 (v0.1.x, sin modo de juego) se lee solo a `Overall`; 2 (v0.2.0-v0.3.0)
    trae el modo pero no el color de las partidas contra la IA, que entran como "sin color";
    3 reparte esas partidas por color.
  - **Cada version sigue escribiendo los campos de la anterior.** El bloque `vsAi` (totales por
    dificultad, sin color) ya no se lee, pero se escribe para que v0.2.x y v0.3.0 no se encuentren
    el registro contra la IA a cero al abrir un archivo nuevo. Al añadir un formato, hacer lo mismo.
  - Se guarda escribiendo un `.tmp` y reemplazando con `File.Replace`, que es atomico: si el juego
    muere a mitad, queda intacto el historico anterior. No volver a borrar y mover.
  - Compañia `CDCompany` e identificador `com.cdcompany.juegotriki` (Player Settings). v0.1.0 salio
    con `DefaultCompany`: si falta el historico, `StatsRepository` copia el de
    `LocalLow\DefaultCompany\Juego-Triki` (el original queda como respaldo). Las preferencias en
    PlayerPrefs (registro, clave por compañia) no se migran: volvieron a sus valores por defecto.
  - Cambiar compañia o producto vuelve a mover los datos: actualizar `LegacyCompanyName` o ampliar
    la migracion.

## Reglas de arquitectura

- **La logica del juego vive en `Triki.Core` y no referencia UnityEngine.** Tablero, turnos,
  deteccion de victoria y IA son C# puro: se testean en milisegundos sin abrir Play Mode.
- Las dependencias entre assemblies van en una sola direccion: `UI -> Gameplay -> Core`.
  Si algo en Core necesita saber de Gameplay, esta mal puesto; se invierte con un evento o interfaz.
- Nada de `GameObject.Find`, `FindObjectOfType` ni `SendMessage` en runtime: referencias
  serializadas en el inspector o inyectadas al inicializar.
- Nada de asignaciones por frame en `Update` (LINQ, `new`, strings concatenados). El tablero es
  3x3: todo cabe en arrays de tamano fijo reutilizados.
- Un cambio de estado del juego = un evento que la capa visual escucha. La UI no calcula reglas.
- El juego se limita a 60 fps con vSync apagado (`FrameRateLimiter`, `[RuntimeInitializeOnLoadMethod]`).
  El tablero esta quieto casi todo el tiempo: sin limite se dibujan nueve circulos a cientos de fps.
  El limite solo funciona con `vSyncCount = 0`; si se vuelve a activar, Unity ignora `targetFrameRate`.

## Convenciones

- Namespaces `Triki.Core`, `Triki.Gameplay`, `Triki.UI` (ya configurados como rootNamespace).
- Campos privados serializados: `[SerializeField] private Foo m_Foo;` -> preferimos `_foo`
  con `[SerializeField]`. Publicos en PascalCase, sin campos publicos mutables.
- Cada script nuevo va dentro de la carpeta de su assembly; si no encaja en ninguna, primero
  discutimos donde va (probablemente es senal de que falta una capa).

## Comandos

- Tests EditMode desde el editor: Window > General > Test Runner.
- Tests sin editor (Unity cerrado), el mismo script que usa la CI (deja los resultados en
  `test-results/`, que no se commitea):
  `powershell -ExecutionPolicy Bypass -File .github/scripts/run-editmode-tests.ps1`
  - Por dentro: `Unity.exe -batchmode -nographics -projectPath <ruta> -runTests -testPlatform EditMode ...`
    lanzado con `Start-Process -Wait` (Unity.exe es app de ventana y no bloquea).
- Build de Windows (Unity cerrado):
  `Unity.exe -batchmode -nographics -quit -projectPath <ruta> -buildTarget Win64 -buildWindows64Player <carpeta>\Juego-Triki.exe -logFile <log>`
  - Compilar fuera de OneDrive y con ruta corta: hay archivos del build que pasan de 260 caracteres.
  - No distribuir la carpeta `*_BackUpThisFolder_ButDontShipItWithYourGame`.
  - Al compilar, Unity reescribe `PC_RPAsset` (filtrado de variantes de shader) y otros settings:
    commitear esos cambios. Si activa `UnityConnectSettings` (`m_Enabled: 1`), revertirlo.
- El proyecto NO se puede editar por fuera mientras el editor de Unity esta abierto:
  `ProjectSettings/*.asset` y las escenas los reescribe Unity al guardar/cerrar.

## Flujo de trabajo (GitHub Flow)

- `main` siempre compila, pasa los tests y es jugable. Nada se sube directo a `main`.
  - GitHub no permite proteger ramas en repos privados con el plan gratuito; lo bloquea un hook
    local `.git/hooks/pre-push` (tambien ejecuta Git LFS). Los hooks no se versionan: en un clon
    nuevo hay que volver a crearlo.
- Cada tarea:
  1. `git switch main` y `git pull --ff-only`.
  2. Rama corta: `feature/<tema>`, `fix/<tema>`, `docs/<tema>` o `chore/<tema>`.
  3. Commits pequeños; tests EditMode en verde antes de abrir el PR.
  4. PR a `main`, merge con **merge commit** (squash/rebase cambian los SHA y rompen ramas
     apiladas encima) y borrar la rama.
- **Cambiar de rama o hacer pull con Unity CERRADO** si cambian assets: git reescribe escenas,
  settings y `manifest.json` bajo el editor. Ojo: `git switch main` con un `main` local
  desactualizado deja el disco en la version vieja hasta hacer el pull.
- Escenas y prefabs se fusionan con UnityYAMLMerge (config local `merge.unityyamlmerge`, ruta
  ligada a la version del editor: actualizarla si cambia Unity).
- Identidad git configurada en el repo (`user.name`/`user.email` con el correo noreply de GitHub).

## Versiones y releases

- Versionado semantico con etiquetas anotadas en `main` (`vMAJOR.MINOR.PATCH`). La version del
  juego esta en Player Settings (`bundleVersion` en `ProjectSettings.asset`) y debe coincidir con
  la etiqueta: subirla en un PR antes de etiquetar.
- Publicadas como release de GitHub, con el build de Windows x64 en zip:
  - `v0.1.0`: primera version jugable (compañia `DefaultCompany`).
  - `v0.1.1`: compañia `CDCompany`, identificador `com.cdcompany.juegotriki` y migracion del
    historico a la nueva carpeta de datos.
  - `v0.2.0`: historico con pestañas General / Contra la IA / Dos jugadores y borrado por
    registro con confirmacion (formato de archivo 2).
  - `v0.2.1`: boton "Eliminar registro" mas compacto. La etiqueta se rehizo antes de publicar: la
    primera `v0.2.1` (nunca publicada) incluia un boton de "borrar todo" que se descarto.
  - `v0.3.0`: **cambian las reglas** - la primera ficha de la partida no puede ir al centro, que
    era una victoria forzada para quien empezaba. Ademas, limite de 60 fps, los botones del HUD
    dejan de colarle el clic al tablero y el historico se guarda de forma atomica.
- Para una release: tests en verde en `main` -> build de Windows desde la etiqueta -> zip sin la
  carpeta "DontShip" -> `gh release create vX.Y.Z <zip> --notes-file <notas>`.

## CI (GitHub Actions)

- `.github/workflows/tests.yml`: tests EditMode en cada PR, en cada push a `main` y a mano
  (Actions > Tests > Run workflow). Resumen en la pagina de la ejecucion; XML y log como artefacto.
- La logica esta en `.github/scripts/run-editmode-tests.ps1`, guardado en **UTF-8 con BOM**
  (Windows PowerShell 5.1 lee como ANSI los scripts sin BOM y rompe las tildes; `.editorconfig`
  lo exige para `*.ps1`). Los pasos corren con `powershell -ExecutionPolicy Bypass -File` porque la
  politica de la PC bloquea scripts; con ese shell hay que terminar con `exit $LASTEXITCODE`.
- Corre en un **runner self-hosted** (esta PC, etiquetas `self-hosted, Windows, unity`) usando el
  Unity instalado (`C:\Program Files\Unity\Hub\Editor\<version de ProjectVersion.txt>`).
  - Por que no GameCI en la nube: las licencias Personal de Unity 6 van ligadas a la maquina
    (`Machine bindings don't match`) y la activacion con correo/contraseña no pasa el 2FA.
  - Si la PC esta apagada, los jobs quedan en cola (GitHub los cancela a las 24 h).
  - El runner usa su propia copia del repo (fuera de OneDrive), no la carpeta del editor.
  - Conserva `Library` entre ejecuciones (`clean: false` + `git clean -ffdx -e /Library/`).
  - No usar este runner si el repo pasa a ser publico: ejecutaria codigo de PRs ajenos.
- Registrar el runner (una vez): Settings > Actions > Runners > New self-hosted runner > Windows,
  seguir los comandos que da GitHub en `C:\actions-runner`, y en `config.cmd` añadir la etiqueta
  `unity` como *etiqueta adicional* (no como nombre). Si falta, se añade con
  `gh api -X POST repos/<owner>/<repo>/actions/runners/<id>/labels -f 'labels[]=unity'`; los jobs
  que ya estaban en cola hay que cancelarlos y relanzarlos. Ejecutarlo con `run.cmd` o como
  servicio con la cuenta del usuario (la licencia de Unity es por usuario; con la cuenta por
  defecto del servicio Unity no estaria activado).
- El repo no tiene secretos: el runner usa la licencia de Unity de la PC.

## Reglas del juego (resumen)

- Colocacion (3 fichas por jugador) -> movimiento por aristas. Gana quien hace linea (en cualquier
  fase) o deja al rival sin movimientos.
- **La primera ficha de la partida no puede ir al centro** (`TrikiRules.BanCenterOpening`, activa
  por defecto; apagarla devuelve el juego de v0.2.x). Sin esa regla el juego esta resuelto: quien
  abre en el centro gana siempre (Dificil contra Dificil, 40 de 40; abrir en un borde regala la
  partida al rival). Con ella, el juego perfecto acaba en tablas.
  - El veto lo aplican `TrikiGame.TryPlace` (devuelve `PlaceResult.ForbiddenOpening`) y
    `TrikiAi.GenerateMoves`. Si cambia uno, cambiar el otro.
  - `TrikiGame.ForbiddenCell` dice que casilla esta vetada ahora mismo (`TrikiGame.NoCell` = -1 si
    ninguna). `BoardView.ShowBlockedCell` la apaga y el HUD lo explica: si no se ve, el jugador
    pulsa el centro y parece que el juego no responde.
- Empate en la fase de movimiento: misma posicion (fichas + quien mueve) 3 veces, o 60
  movimientos sin ganador. Valores en `TrikiRules`; la victoria tiene prioridad sobre el empate.

## Arte y audio

- Todo el arte y el sonido actuales se generan por codigo (`SpriteFactory`, `ToneSynth`) y son
  sustituibles: cada sprite/clip tiene un campo en el inspector (`BoardView`, `AudioFeedback`);
  si se asigna, se usa ese asset y no se genera nada.
  - Fichas: sprite en escala de grises (se tiñe con el color del jugador).
  - Placa del tablero: se dibuja en modo Sliced, el sprite debe tener bordes 9-slice.
  - Poner los assets en `Art/Sprites` y `Audio`.
- Animaciones en `BoardView` (aparecer, deslizar, crecer) con arrays fijos; un tween por ficha y
  `Update` no hace nada si no hay tweens. `CompleteAnimations()` las termina (tests/capturas).
- Destruir objetos con `SpriteFactory.SafeDestroy` (Destroy en Play, DestroyImmediate en editor).
- Sonido on/off en `AudioPreferences` (PlayerPrefs + `AudioListener.volume`).

## IA

- `Triki.Core/AI/TrikiAi`: negamax + alfa-beta sobre enteros, buffers creados una vez (sin
  asignaciones por jugada). Facil = 50 % azar + profundidad 1, Normal = 3, Dificil = 8.
- Su `Apply` debe replicar el orden de reglas de `TrikiGame` (linea -> bloqueo -> limite) y su
  `GenerateMoves`, el veto del centro en la apertura. Si cambian las reglas, cambiar ambos.
  La repeticion no se modela en la busqueda: con el centro vetado las partidas llegan al final,
  asi que la IA puede repetir posiciones sin ver que esta forzando tablas.
- Recibe `System.Random` para poder fijar la semilla en tests.
- La configuracion de partida (modo, dificultad, color) va del menu al juego por `PlayerPrefs`
  (`MatchSettingsStore`), no por estaticos: el proyecto no recarga el dominio al entrar en Play.
- `GameController` hace jugar a la IA tras `_aiMoveDelay` y bloquea el input en su turno.
