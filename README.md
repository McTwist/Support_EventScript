# Support - EventScript
Tackle the event system with the power of scripting

## Description
The problems with the current eventing system in Blockland is that is quite hard to share them and it gets quite cluttered when quite huge. You either need to use the Duplicator, save the build and send the file or manually copying it.

This mod will ease this issue by introducing a new scripting language called EventScript. This language is designed to make sharing easy and eventing understandable. It will also standardize the way to express events through text.

### Examples

```
# Author: Platypi / SadBlobfish
# Makes a brick change colors on a loop when toggled

# Start color changing (start with events enabled)
[x][0]onActivate->Self->playSound("Beep_Checkout.wav")
[x][33]onActivate->Self->fireRelay

# Stop color changing (start with events disabled)
[ ][0]onActivate->Self->playSound("Beep_Denied.wav")
[ ][0]onActivate->Self->cancelEvents

# Alternate between starting and stopping color changing
[x][0]onActivate->Self->toggleEventEnabled("0 1 2 3")

# Change colors
[x][0]onRelay->Self->setColor("0.898039 0.000000 0.000000 1.000000")
[x][500]onRelay->Self->setColor("0.898039 0.898039 0.000000 1.000000")
[x][1000]onRelay->Self->setColor("0.000000 0.498039 0.247059 1.000000")
[x][1500]onRelay->Self->setColor("0.200000 0.000000 0.800000 1.000000")

# Loop color changing
[2000]onRelay->Self->fireRelay

# Stop color changing when brick is blown up
onBlownUp->Self->cancelEvents
```

## Installation
Put Support_EventScript.zip into the Add-Ons folder in your Blockland folder.