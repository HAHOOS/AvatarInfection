<p align="center">
    <img align="center" src="https://raw.githubusercontent.com/HAHOOS/AvatarInfection/refs/heads/master/AvatarInfection/Media/Icon.png" width=64px height=64px>
    <h1 align="center">AvatarInfection</h1>
</p>

A LabFusion gamemode where a virus is released, but it's rather special... It turns the infected people into a selected avatar! This gamemode offers a lot of customazibility compared to other gamemodes.

<h2 align="center">Features</h2>

- Ability to set any avatar of your choice to be the infected
  - You can also set the avatar to a random one (and also filter the avatars!) or set it to the avatar the first chosen infected player has!
- Fully customizable stats and avatar height for all teams, as well as the option to disallow the usage of guns
- Most of the things are handled server-side, making the gamemode less vulnerable to cheaters
- Two available types of infecting: TOUCH and DEATH
- **and much, much more**

<h2 align="center">Known Issues</h2>

- Settings save when exiting the game, even tho it's not meant to

<h2 align="center">FAQ</h2>

### Q: What is Infected Children?

**A:** Infected Children is a team (cool, right, that's the only thing u need to know). The people assigned to the team are those who got infected by others, while the people randomly selected at the start of the gamemode are in the Infected team, not Infected Children. Infected Children can have separate stats and avatar from Infected. You can "disable" the team by enabling "Sync With Infected" in `Infected Children Stats` and disabling the option "Separate From Main" in the `Infected Children Avatar` section.

### Q: What does it mean that "Avatar is not public"?

**A:** For LabFusion to be able to download avatars of other players, the avatar must have a Mod ID assigned to it. That gets assigned **only** when installing the avatar through the in-game mod.io installer. Manually installing mods by dragging files does not associate the avatar with a Mod ID. To resolve the issue with the avatar not being public, simply download it through the in-game installer.

### Q: You said you fixed the mod not working with Quest, but it still doesn't work!

**A:** The mod does not support the Epic Games version of LabFusion. If you are encountering issues even when not using the EOS version, report it on [Github!](https://github.com/HAHOOS/AvatarInfection)

### Q: My avatar is not changing for some reason when getting infected, why?

**A:** Make sure you do not have any code mod installed that blocks the level changing avatar behaviour (PullCordForceChange). The mod relies on it to give a bit of an "effect". This logic might be changed in the future.

<h2 align="center">Credits</h2>

[**Lakatrazz**](https://github.com/Lakatrazz) - This gamemode is a heavily modified version of the included in Fusion gamemode Hide & Seek which was made by Lakatrazz. Also LabFusion, which the gamemode is for, was created by Lakatrazz!

[**Mash**](https://github.com/mashedram) - After the release of v1.0.0 of the mod, Mash provided me with tips on how to improve the code for my mods (for example not having ALL the code in one file, i think it was like 2200 lines in `Infection.cs` at the time). I'm really thankful that you told me that, all of my code now is much more maintainable!!!

[**FirEmerald**](https://github.com/FirEmerald) - The stats changing logic comes from their mod - [AvatarStatsLoader](https://thunderstore.io/c/bonelab/p/FirEmerald/AvatarStatsLoader/). If not for that, it would have taken me A LOT of time to figure out how to do it!

[**Whaley**](https://mod.io/g/bonelab/u/googleuser2t9wsr) - Provided me mathematical equations which fixed the issue with big avatars not working well with low upper strength. Thank you so much!!

### Thank You To Testers

- [Tλ²rek](https://github.com/TarekLP)
- Breadskate
- [MrRandom](https://mod.io/g/bonelab/u/bmarcel2007)
- EXAnimated
- TCU
- And to all of the other people that helped during testing or reported bugs!

**If you have helped during the testing of AvatarInfection, but you're not listed here, DM me on Discord (@hahoos) and I'll add you!**

Thank you for helping me test AvatarInfection to make it work as good as possible. Without your help this wouldn't be possible!
