# Unity 3D Systems Pack

This project is a modular Unity 3D prototype pack built around reusable gameplay systems.

It includes:

- first-person / 3D player movement
- interaction system using `IInteractable`
- doors and buttons
- scene triggers
- enemy spawning
- projectile shooting
- melee attacks
- health and damage system using `IDamageable`
- enemy AI using a state machine
- UI for player health, speed, enemy info, and interact prompts
- main menu scene logic

This README explains what each script does, what it depends on, and how to set it up in Unity.

---

# Project Structure

## Core Gameplay
- `PlayerMovement.cs`
- `PlayerInteractor.cs`
- `IInteractable.cs`
- `InteractableHighlight.cs`

## Damage / Combat
- `IDamageable.cs`
- `Health.cs`
- `ProjectileShooter.cs`
- `Projectile.cs`
- `MeleeAttack.cs`

## World Interaction
- `DoorSlideUp.cs`
- `ButtonOpenDoorInteractable.cs`
- `SceneTrigger.cs`
- `EnemySpawnerArea.cs`
- `ButtonSpawnEnemiesInteractable.cs`

## Enemy AI
- `EnemyState.cs`
- `EnemyIdleState.cs`
- `EnemyChaseState.cs`
- `EnemyAttackState.cs`
- `EnemyAI.cs`

## UI
- `PlayerHUD.cs`
- `EnemyLookUI.cs`

## Menu
- `MainMenuController.cs`

---

# 1. Player Movement

## Script
`PlayerMovement.cs`

## Features
- mouse look
- walk / sprint
- smooth directional blending
- jump
- double jump
- crouch
- slide
- dodge
- wall run
- wall latch / cling
- wall jump
- FOV changes
- head bob
- strafe tilt
- debug gizmos for wall checks

## Required Components
- `CharacterController`
- camera assigned to `playerCamera`
- camera root assigned to `cameraRoot`

## Setup
1. Create your player object.
2. Add a `CharacterController`.
3. Add `PlayerMovement`.
4. Assign:
   - `playerCamera`
   - `cameraRoot`
5. Tune movement values in Inspector.

## Notes
- `parkourMask` should contain the layer used by runnable/latchable walls.
- if you use wall systems, create a layer like `ParkourWall`
- `CurrentHorizontalSpeed` is exposed for UI

---

# 2. Interaction System

## Scripts
- `PlayerInteractor.cs`
- `IInteractable.cs`
- `InteractableHighlight.cs`

## Purpose
This system allows the player to look at objects and press `E` to interact with them.

## How it works
- `PlayerInteractor` does a camera-based interaction cast
- objects implement `IInteractable`
- optional highlight appears while the object is interactable
- UI can read current interaction text through `GetCurrentInteractionText()`

## `IInteractable`
Every interactable object must implement:

- `CanInteract(PlayerInteractor interactor)`
- `Interact(PlayerInteractor interactor)`
- `GetInteractionText()`

## Setup
### Player
1. Add `PlayerInteractor` to the player.
2. Assign the player camera.
3. Set:
   - `interactDistance`
   - `interactRadius`
   - `interactMask`

### Interactable object
1. Add a collider.
2. Add a script that implements `IInteractable`.
3. Optional: add `InteractableHighlight`.

### Highlight
`InteractableHighlight` turns a separate object on/off when looked at.

Example:
```text
Button
├── Mesh
└── HighlightGlow
