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
  - maxHealth
  # currentHealth 
  - attackDamage
  - direction
  # currentAttackPattern
  + Initialize()
  + cycleAttackPattern()
  + takeDamage()
}

@enduml