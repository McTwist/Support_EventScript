# Support - EventScript
Tackle the event system with the power of scripting

## Description
The problems with the current eventing system in Blockland is that is quite hard to share them and it gets quite cluttered when quite huge. You either need to use the Duplicator, save the build and send the file or manually copying it.

This mod will ease this issue by introducing a new scripting language called EventScript. This language is designed to make sharing easy and eventing understandable. It will also standardize the way to express events through text.

### Examples

```
# Normal event
[x][33]onActivate->Self->fireRelayNum("1-5 5 7", "Brick")

# Disabled and delay with no params
[ ][500]onBlownUp->Player->Kill
[x][  0]onRelay->Self->fakeKillBrick("5 10 3.4", 2)

# Separate lines
[x][56]onRelay
    ->Self
    ->setColor(
        "0.000000 0.000000 1.000000 1.000000"
    )

# Default behavior
onRelay -> "test_brick" -> setColliding (1)
setApartment -> And -> setOwner (9845)
onProjectileHit -> Projectile -> Bounce (1.3)
[]onActivate -> Self -> setVehicle("Jeep ")
```

## Installation
Put Support_EventScript.zip into the Add-Ons folder in your Blockland folder.