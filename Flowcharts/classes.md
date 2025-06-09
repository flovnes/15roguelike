@startuml
skinparam linetype ortho
skinparam class {
    BackgroundColor White
    BorderColor Black
    FontColor Black
    ArrowColor Black
}

skinparam class<<abstract>>Fontstyle plain

skinparam nodesep 15
skinparam ranksep 40

skinparam classAttributeIconSize 0
skinparam style strictuml

class Tile <<abstract>> {
  + gridPosition
  + type
  + virtual void Initialize()
  + virtual void OnPlayerSwap()
}

class EnvironmentTile
class PlayerTile
class EnemyTile
class HealthPickupTile
class EmptyTile
class TrapTile

EnvironmentTile -[hidden]right- PlayerTile
PlayerTile -[hidden]right- EnemyTile
EnemyTile -[hidden]right- HealthPickupTile
HealthPickupTile -[hidden]right- TrapTile
TrapTile -[hidden]right- EmptyTile

Tile <|-- EnvironmentTile
Tile <|-- PlayerTile
Tile <|-- EnemyTile
Tile <|-- HealthPickupTile
Tile <|-- TrapTile
Tile <|-- EmptyTile

EnvironmentTile : + blocksLoS

EnvironmentTile : + Initialize()
EnvironmentTile : + OnPlayerSwap()
EnemyTile : - health
EnemyTile : - attackDamage
EnemyTile : # attackPattern
EnemyTile : + Initialize()
EnemyTile : + OnPlayerSwap()
EnemyTile : + PerformAction()
EnemyTile : + TakeDamage()
PlayerTile : + OnPlayerSwap()
PlayerTile : + OnSwappedByOther()
HealthPickupTile : - healthRestore
HealthPickupTile : - isConsumed
HealthPickupTile : + OnPlayerSwap()
EmptyTile : + OnPlayerSwap()
TrapTile : - damage
TrapTile : + OnPlayerSwap()
@enduml