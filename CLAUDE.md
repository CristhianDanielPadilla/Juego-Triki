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
