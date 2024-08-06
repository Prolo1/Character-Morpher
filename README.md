[![Latest release](https://img.shields.io/github/release/Prolo1/Character-Morpher.svg?style=flat)](https://github.com/Prolo1/Character-Morpher/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Prolo1/Character-Morpher/total.svg?style=flat)](https://github.com/Prolo1/Character-Morpher/releases)

[![Ko-Fi](https://img.shields.io/badge/Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/prolo)
[![BuyMeACoffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/prolo)

# Character Morpher

## Table Of Contents

1. [Description](#description)
2. [Linked Mods](#linked-mods)
3. [Setup](#setup)
4. [Features](#features)
5. [Planned](#planned)
6. [How To Use](#how-to-use)
7. [Examples](#examples)
8. [Known Issues](#known-issues)
69. [Issues / Requests?](#issues)

## Description

This mod allows the user to set a morph target and change any character to look like the target (with a few sliders in character maker). ~~As of now the changes in character maker are applied to every character (may change that later)~~ (I changed it). It also ~~only works on female cards, not actively planning on both genders (I might though)~~ (forgot to change that, it has worked for a while now 😜).

## Linked Mods

The mod versions used are from the latest versions of [Better Repack](https://dl.betterrepack.com/public/)

* [Illusion API](https://github.com/IllusionMods/IllusionModdingAPI)
* [BepisPlugins](https://github.com/IllusionMods/BepisPlugins)
* [ABMX](https://github.com/ManlyMarco/ABMX) (this mod basically made it all possible)

## Setup

1. Download [Better Repack](https://dl.betterrepack.com/public/) or add all linked mods to your game
2. Download the latest update of [this mod](https://github.com/Prolo1/Character-Morpher/releases/latest/) for your game
3. Extract the zip file
4. Place the extracted "BepinEx" folder into the main directory of your game (where the game .exe is)
5. Run

## Features

* Morph body features
* Morph face features
* Morph ABMX body features
* Morph ABMX face features
* Added QoL file explorer search for morph target in maker
* Added QoL easy morph buttons
* Added Hotkeys to enable/disable mod (changeable)
* Can choose to enable/disable in-game use (this affects all but male character[s])
* Can choose to enable/disable use in male maker
* Morph data can be saved to the card w/o editing the original character data  
* Can create preset slot saves  
* Can choose to only morph characters with data saved to them in-game  
* Cards save "As Seen" when saved (for people that make adjustments before saving)  
* Added Hotkeys to change between preset slots (changeable)  
* Added button that goes back to original look in maker (How it looked when loaded)
* Added Compatibility with Studio (New!)
* Added character-specific enable toggles that are saved to each card (loading a game with toggle disabled means the character will not be morphed) (New!)

## Planned

* Adding more sliders over time (i.e. skin colour, voice... etc.)
* ¯\\_(ツ)_/¯

## How To Use

1. Open "Settings" then find "Plugin Settings"
2. Find "Character Morpher" and add the path to the character you want to use as a morph target (or use the new in-maker file explorer option)
3. Open up character maker (in-game or from the main menu)
4. Go to "Chara Morph" under personality settings
5. Use the sliders to morph different aspects of the character
69. Enjoy!

## Examples

![example gif](https://github.com/Prolo1/Example-images/blob/main/example%20chara%20morph%20v2.gif?raw=true)
![HS2 image](https://github.com/Prolo1/Example-images/blob/main/Screenshot%202022-03-29%20191817.png?raw=true)
![HS2 Studio 1](https://github.com/Prolo1/Example-images/blob/main/CharaMorph/studio%201.png)
![HS2 Studio 2](https://github.com/Prolo1/Example-images/blob/main/CharaMorph/studio%202.png)


## Known Issues

* ~~changing morph values to far extremes may cause issues loading subsequent characters in maker. When this happens just close maker and open it back up (this isn't a problem in game since you can't use the sliders)~~ (Fixed this to the best of my knowledge)
* ~~With KK[S], trying to change OG face and body sliders may change bust values to the original state, that's because the original sliders don't update when values change internally so keep that in mind (using the character morpher sliders will turn it back)~~ (This should be fixed too)
* some people when using sliders, may have seen the sliders snap back to their original values on release. this is most likely a config issue which should be fixable in 2 ways (I'm not quite sure). Option 1, press the "Save Default" button in Maker, and close and open the Maker if it doesn't work immediately. Option 2, find the mod config file in `"root_game_folder/BepInEx/Config/prolo.chararmorpher.cfg"` and delete it while the game is off **(THIS WILL DELETE ALL SAVED SETTINGS)**, then re-launch the game.

## Issues / Requests? <a name="issues"></a>

Want to request a new feature or report a bug you found? If you find any mod issues, you can add it to the [Git Issues](https://github.com/Prolo1/Character-Morpher/issues) page and if you have any requests or cool ideas add it to the [Discussions](https://github.com/Prolo1/Character-Morpher/discussions) page and I'll see what I can do. You can also message me on [discord](https://discordapp.com/users/1067927438836891708) if you so choose.  
