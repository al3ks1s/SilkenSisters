# Silken Sisters - Playtest version

#### /!\ Disclaimer : Expect the mod to be broken in some parts as it is a test version. Please report any issue you find at https://github.com/al3ks1s/SilkenSisters/issues

A Hollow Knight: Silksong mod adding a memory fight featuring both Lace and Phantom together.

To access the fight you need to:
- Be in act 3
- Have defeated both Lace 2 and Phantom
- Have the Elegy of the Deep needolin skill

Access the fight by playing Elegy of the Deep at Phantom's tank.

For now, the bosses have their vanilla behaviors, there will be changes on that in the future.

## Manual installation

### Manual Installation is not recommended due to the dependencies needed by the mod. Use Thunderstore, r2modman or Gale to install the mod with minimal issues.

This mod requires the following mods to function: 
- [BepinEx version](https://thunderstore.io/c/hollow-knight-silksong/p/BepInEx/BepInExPack_Silksong/)
- [FSMUtils](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/FsmUtil/)
- [I18N](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/I18N/)
- [UnityHelper](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/UnityHelper/)
- [AssetHelper](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/AssetHelper/)
- [DataManager](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/DataManager/)
- [SilksongPrepatcher](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/SilksongPrepatcher/)
- [MonoDetour BepInEx 5](https://thunderstore.io/c/hollow-knight-silksong/p/MonoDetour/MonoDetour_BepInEx_5/)
- [MonoDetour](https://thunderstore.io/c/hollow-knight-silksong/p/MonoDetour/MonoDetour/)

1. Download BepinEx and extract it in the game's directory
2. Run the game once to generate the necessary files
3. Download and install the mod FSMUtils into the BepinEx/plugins folder
4. Download and install the mod I18N into the BepinEx/plugins folder
5. Download and install this mod into the BepinEx/plugins folder into its own "SilkenSisters" folder

The expected file structure is as follow :
```
BepInEx/
	plugins/
		SilkenSisters/
			<SilkenSisters files>
		silksong_modding-I18N-1.0.2/
			<I18N files>
		silksong_modding-FsmUtil-0.3.5/
			<FSMUtil files>
		silksong_modding-AssetHelper-1.0.1/
			<AssetHelper files>
		silksong_modding-UnityHelper-1.1.1/
			<UnityHelper files>
		<Other mods>
	patchers/
		<monodetour bepinex 5 dlls directly in the folder>
		<Silksong Prepatchers files from /patchers>
	core/
		<monodetour dlls directly in the folder
```