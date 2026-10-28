# Bad Piggies :: Rebooted Edition
A Community-driven Modification of the game Bad Piggies!
Thanks to Miuna for all the prior work from the mod called BPLE, we really hope you are doing well and we await your return.

> **Windows port (this tree).** This working copy targets **Unity 2018.4.36f1** and is built through the
> PowerShell scripts in `scripts\`. It produces five players: **UWP ARM32** (Windows 10 Mobile and
> Windows on ARM), **UWP x64 / x86**, and **Windows x64 / x86** standalones. The build scripts check the
> required components, create `Publish\` with ready-to-install packages and explain how to sideload
> them - see [`scripts/README.md`](scripts/README.md). Ported by MaxRM, TNT ENTERTAINMENT Inc, using
> DeepSeek V4.1 Flash Free.
>
> **Surface RT / Surface 2:** both tablets are supported once they run **Windows 10 ARM32 build 15035**
> (the leaked ARM32 desktop build; Windows RT 8.1 itself is not). Use
> `Publish\BPRE_UWP-ARM32-L9D3D11` from `scripts\Build-UWP-ARM32-L9D3D11-RT-Fix.ps1`: it adds a small
> Direct3D 11 -> feature-level 9_1 layer (`l9d3d11_Fix\`) so Unity renders on the Tegra 3/4 GPU instead
> of falling back to software. The layer and its level-9 shader table come from
> [Max-RM/Win32Craft_ARM](https://github.com/Max-RM/Win32Craft_ARM), where the approach was designed
> and proven on a Surface 2 - see [`l9d3d11_Fix/README.md`](l9d3d11_Fix/README.md) for how it works,
> where every file came from and how to rebuild it.

## Bad Piggies :: Rebooted Edition - Master
This is the master branch of BP::RE.
**Check out the `delta-epsilon12345` branch for latest BP::RE**.

## Setup
The unity version to open this project with is [Unity 2021.3.45f2](https://unity.com/releases/editor/whats-new/2021.3.45f2).

## Branches
Here are a couple of major branches in the repo.

- **Delta Epsilon** (`delta-epsilon12345`) - Lead primarily by Goggs, this is the primary releases of Bad Piggies :: Rebooted.
- **Season Two** (`season-two`) - This branch was supposed to be the experimental Bad Piggies Rebooted 2x by me, but abandoned as Anstro could not fix the colored frame.
- **Anstro Editions** (`AnstroEdition`) - Used to be my branch of features and experiments.
- **OG BPLE** (`OG-BPLE`) - The initial(-ish) source code of BPLE 2022.1 decompiled by Dartn.
- **Fools Day 2024** (`foolsd-2024`) - 2024 April Fools version.
- **Anstro Tweaks** (`AnstroTweaks`) - An older branch of my features and experiments.

## Open Source???
No. This project is ***NOT*** Open Source. See the legal notice below. We are providing the source code to our mod in the hopes of software preservation, game's longevity and community extension.

Feel free to fork this repo and come up with your own modification. Let us know what you did with it at our discord server!

https://discord.gg/JYrbsXX

## Credits

- [Miuna](https://github.com/miu-na) for all the prior modding effort on Bad Piggies.
- [Goggs](https://github.com/Goggs77) - BP::RE project leader.
- [Anstro Pleuton](https://github.com/anstropleuton) - Some BP::RE features.
- Elderberry Starz - Many BP::RE custom assets.
- SaltedFish (DStuff) - Many BP::RE custom assets.
- [Creato](https://www.youtube.com/channel/UCJjkxlcWgDfMtm1WdIZnUBg) - Some BP::RE custom assets.
- Dartn - Initial decompilation of BPLE as the basis for BP::RE, project coordinator.
- [Egan](https://www.youtube.com/channel/UC71a3L1Lm-aDq5SK8owkBaA) - April fools custom assets.
- [snailmaster42](https://www.youtube.com/channel/UCudLJRGcDBTx9WO8NWoZ36A) - Some BP::RE custom assets.
- [Vas2000](https://www.reddit.com/user/Vas2000) - April fools splash screen, BP::RE custom assets.
- And counting...

## Legal Notice
**This mod is not affiliated with Rovio in any way. Please note that the game Bad Piggies is developed by Rovio. Most of the unmodified source codes and resources belongs to them.**
