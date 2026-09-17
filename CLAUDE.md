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
- El historico se guarda en `Application.persistentDataPath/triki-stats.json` (`StatsRepository`).

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

## Convenciones

- Namespaces `Triki.Core`, `Triki.Gameplay`, `Triki.UI` (ya configurados como rootNamespace).
- Campos privados serializados: `[SerializeField] private Foo m_Foo;` -> preferimos `_foo`
  con `[SerializeField]`. Publicos en PascalCase, sin campos publicos mutables.
- Cada script nuevo va dentro de la carpeta de su assembly; si no encaja en ninguna, primero
  discutimos donde va (probablemente es senal de que falta una capa).

## Comandos

- Tests EditMode desde el editor: Window > General > Test Runner.
- Tests sin editor (Unity cerrado; PowerShell, usar `Start-Process -Wait` porque Unity.exe no bloquea):
  `"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -nographics -projectPath <ruta> -runTests -testPlatform EditMode -testResults <xml> -logFile <log>`
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
- Siguiente paso sugerido: etiqueta `v0.1.0`.

## CI (GitHub Actions)

- `.github/workflows/tests.yml`: tests EditMode en cada PR, en cada push a `main` y a mano
  (Actions > Tests > Run workflow). Resumen en la pagina de la ejecucion; XML y log como artefacto.
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
  `unity`. Ejecutarlo con `run.cmd` o como servicio con la cuenta del usuario (la licencia de Unity
  es por usuario; con la cuenta por defecto del servicio Unity no estaria activado).
- Los secretos `UNITY_*` de la version con GameCI ya no se usan y se pueden borrar.

## Reglas del juego (resumen)

- Colocacion (3 fichas por jugador) -> movimiento por aristas. Gana quien hace linea (en cualquier
  fase) o deja al rival sin movimientos.
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
- Su `Apply` debe replicar el orden de reglas de `TrikiGame` (linea -> bloqueo -> limite). Si cambian
  las reglas, cambiar ambos. La repeticion no se modela en la busqueda.
- Recibe `System.Random` para poder fijar la semilla en tests.
- La configuracion de partida (modo, dificultad, color) va del menu al juego por `PlayerPrefs`
  (`MatchSettingsStore`), no por estaticos: el proyecto no recarga el dominio al entrar en Play.
- `GameController` hace jugar a la IA tras `_aiMoveDelay` y bloquea el input en su turno.
