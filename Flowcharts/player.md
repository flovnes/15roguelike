@startuml
skinparam linetype ortho
skinparam class {
    BackgroundColor White
    BorderColor Black
    FontColor Black
    ArrowColor Black
}

skinparam classAttributeIconSize 0
skinparam style strictuml

class Player {
  - health
  - attackDamage
  - direction
  # currentAttackMode
  + Initialize()
  + cycleAttackPattern()
  + takeDamage()
}

@enduml