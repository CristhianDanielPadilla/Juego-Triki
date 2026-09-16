# Juego-Triki

Tres en raya (Triki) 2D en Unity **6000.6.0f1**, URP con **Renderer 2D**.

## Estructura

```
Assets/
  _Project/            Todo lo propio del juego (el guion bajo lo deja arriba en el Project window)
    Art/Sprites/       Sprites del tablero, fichas, UI
    Audio/
    Prefabs/
    Scenes/            Main.unity y las que vengan (Menu, Game...)
    Scripts/
      Core/            Triki.Core     - logica pura, SIN UnityEngine (noEngineReferences)
      Gameplay/        Triki.Gameplay - MonoBehaviours, input, presentacion del tablero
      UI/              Triki.UI       - HUD, menus, marcador
  Tests/EditMode/      Triki.Tests.EditMode - tests NUnit sobre Triki.Core
  Settings/            URP (RP Assets + Renderer 2D), Volume Profile e InputSystem_Actions
```

Escena de arranque: `Assets/_Project/Scenes/Main.unity` (unica en Build Settings).

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
