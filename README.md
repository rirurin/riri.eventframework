# Event Framework for Persona 3 Reload

A [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II) modding framework for editing existing events 
and adding new events into the Unreal Engine Persona games. This additionally adds support for loading levels that
don't exist in the level registry.

Downloads are available from the Releases button on the right or from the *Download Mods* screen in Reloaded-II.

## Supported Games

- [Persona 3 Reload](https://store.steampowered.com/app/2161700/Persona_3_Reload/) (Unreal Engine 4.27)

## Purpose

Persona 3 Reload contains a file called `EvtPreDataAsset` which contains a list of every event in the game that it will load. Event Framework allows for editing and adding events without having to store an edited copy of `EvtPreDataAsset` itself, which allows for mod merging (mods can edit the list while retaining the changes from previous mods).

## Usage

To edit or add an event, Event Frameworks checks for *PRE* files, which are [YAML formatted](https://en.wikipedia.org/wiki/YAML) files that store
initialization information for the respective event. 
This must be placed within the `UnrealEssentials` folder in the same directory where each file's level sequence is defined (e.g `UnrealEssentials/P3R/Content/Xrd777/Events/Cinema/Event_Cmmu_002_050_C/PRE_EVENT_Cmmu_002_050_C.yml` for *Yukari Rank 5*).

*PRE* files can contain the following information:

- **EventLevel**: A path to the file that contains the event's base level. This is the level that contains the level sequence itself along with objects that are used within the level sequence.
```yaml
EventLevel: "/Game/Xrd777/Events/Cinema/Event_Cmmu_301_010_C/LV_Event_Cmmu_301_010_C"
```
- **EventSublevels**: A list of objects to define various levels to load in addition to the base level. Each entry in the list contains the following entries:
    - **EventBGLevels**: A list of paths which state what background levels will get loaded
    - **BGFieldSeasonSubLevel**: A path to a level containing season specific assets
    - **BGFieldSoundSubLevel**: A path to a level containing all the sound cues
```yaml
EventSublevels: [
  {
    EventBGLevels: [ "/Game/Xrd777/Maps/Field/F101/BG/LV_F101_113_001_BG" ],
    BGFieldSeasonSubLevel: "",
    BGFieldSoundSubLevel: "/Game/Xrd777/Maps/Field/Sound/LV_F101_113_Sound"
  }
]
```
- **LightScenarioSublevels**: A list of paths for light levels to load in addition to the base level.
```yaml
LightScenarioSublevels: [
  "/Game/Xrd777/Maps/Field/F101/BG/LV_F101_113_BG_Noon_Only"
]
```
- **DungeonSublevel**: An object to define loading Tartarus levels, containing the fields **EventBGFloorLevel** and **BGEnvironmentSubLevel**. By default, both of these fields are empty.
```yaml
# From Event_Fild_501_001_C
DungeonSublevel: {
    EventBGFloorLevel: "/Game/Xrd777/Maps/Field/Dungeon/Floor/LV_DFLR_03_095",
    BGEnvironmentSubLevel: "/Game/Xrd777/Maps/Field/Dungeon/Environments/LV_DENV_03_01"
}
```
- **bDisableAutoLoadFirstLightingScenarioLevel**: A boolean (true/false) to disable automatically loading the first entry in the light scenario level list. This defaults to false.
- **bForceDisableUseCurrentTimeZone**: A boolean (true/false) to stop the current time being used. This defaults to false.
- **ForcedCldTimeZoneValue**: Set the event to happen on a particular time of day. If this is not included, it will default to 0, which will use the current time instead. \
The following numbers correspond to the values:

| Number | Time of Day |
| - | - |
| 1 | Early Morning |
| 2 | Morning |
| 3 | AM |
| 4 | Noon |
| 5 | PM |
| 6 | After School |
| 7 | Night |
| 8 | Shadow |
| 9 | Midnight |

```yaml
ForcedCldTimeZoneValue: 4 # Set the event to happen on Noon
```

- **ForceMonth**: Set the event to happen on a particular month. If this is not included, it will default to 0, which will use the current month instead.
```yaml
ForceMonth: 8 # Set the event to happen on August
```
- **ForceDay**: Set the event to happen on a particular day. If this is not included, it will default to 0, which will use the current month instead.
```yaml
ForceDay: 5 # Set the event to happen on the fifth day of the month
```

Here's what `Event_Cmmu_002_050_C`'s entry in `EvtPreDataAsset` would look like as a PRE yaml:

```yaml
EventLevel: "/Game/Xrd777/Events/Cinema/Event_Cmmu_002_050_C/LV_Event_Cmmu_002_050_C"
EventSublevels: [
  {
    EventBGLevels: [
      "/Game/Xrd777/Maps/Field/F105/BG/LV_F105_102_002_BG",
      "/Game/Xrd777/Maps/Field/F105/BG/LV_F105_102_001_BG"
    ],
    BGFieldSeasonSubLevel: "",
    BGFieldSoundSubLevel: "/Game/Xrd777/Maps/Field/Sound/LV_F105_102_Sound"
  }
]
LightScenarioSublevels: [ "/Game/Xrd777/Maps/Field/F105/BG/LV_F105_102_BG_Noon_Only" ]
```

## Resources for Creating Custom Events

- [Event Editing and Creation in Persona 3 Reload (Easy Tutorial) (Life Hack)](https://docs.google.com/document/d/1Qj69u0HXyHMi5RJzlu-NEjj4HmBwEBuGZD6gHl98ei4) by **Rirurin** ([Bluesky](https://bsky.app/profile/riri.wtf))
- [Persona 3 Reload (P3R) Event Editing Notes for Dummies](https://docs.google.com/document/d/1iScs5l6DaQZUb3EU4ll_vr9C6hWdHJZACIV0xpgzc3w) by **Smerz**
- [P3R Character Bustup ID References](https://docs.google.com/document/d/1ml9qboqYa_yL0MWfvPTVuwgNzXBspUcbAoG8smz3yRQ) by **Smerz**