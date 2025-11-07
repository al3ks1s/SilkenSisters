# Silken Sisters - Playtest version

#### /!\ Disclaimer : Expect the mod to be broken in some parts as it is a test version. Please report any issue you find at https://github.com/al3ks1s/SilkenSisters/issues
#### Use a save you don't care to lose in case this mod breaks things, a 100% save at the Organ is provided with the mod files so that you can test things out. Install it next to the other save files in `AppData\LocalLow\Team Cherry\Hollow Knight Silksong`

A Hollow Knight: Silksong mod adding a memory fight featuring both Lace and Phantom together.

To access the fight you need to:
- Be in act 3
- Have defeated both Lace 2 and Phantom
- Have the Elegy of the Deep needolin skill

Access the fight by playing Elegy of the Deep at Phantom's tank.

For now, the bosses have their vanilla behaviors, there will probably be work on that in the future.

## Known bugs

- Lace can throw you out of the arena, use Lalt+H to teleport back in the middle
- Sometimes, the deep memory zone will not appear properly, restart the game

## Manual installation

This mod requires the following mods to function: 
- BepinEx version 5.4.2304 
- FSMUtils 0.3.1
- I18N 1.0.2

1. Download BepinEx and extract it in the game's directory
2. Run the game once to generate the necessary files
3. Download and install the mod FSMUtils into the BepinEx/plugins folder
4. Download and install the mod I18N into the BepinEx/plugins folder
5. Download and install this mod into the BepinEx/plugins folder into its own "SilkenSisters" folder

The expected file structure is as follow :
```
BepInEx/
﻿	plugins/
		SilkenSisters/
			<SilkenSisters files>
		silksong_modding-I18N-1.0.1/
			<I18N files>
		﻿﻿silksong_modding-Silksong_FsmUtil-0.3.0/
		﻿﻿﻿﻿	<FSMUtil files>