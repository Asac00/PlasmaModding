# PlasmaModding

**PlasmaModding** is a modding framework intended to provide tools and infrastructure for creating mods for the game **Plasma**, developed by *Dry Licorice*.

This project is built on **BepInEx**, which is required for the mod to function.

It now includes **[Plasma-Custom-Nodes](https://github.com/Plasma-Modding/Plasma-Custom-Nodes)** inside the project, providing integrated support for custom nodes.

## Project Status

⚠️ **This project is currently under development and is not yet functional.**  
APIs, features, and internal structures are subject to change.

## Current Features

At its current stage, PlasmaModding focuses on extending the internal data systems of the game. The project currently provides:

- **Custom nodes creation**  
- **Custom selection for the nodes' inputs**  
- A framework to **define and register new custom data types** in Plasma  
- Experimental control over the **image system** used by the game  

## Current Development Focus

The primary short-term goal of the project is to:

- **Identify and fix bugs related to the implementation of custom data types**

This step is necessary before expanding the scope of the framework.

## Planned Features

In the future, PlasmaModding aims to support more advanced modding capabilities, including:

- The ability to **add new objects to the game**
- Further extension of Plasma’s internal systems to enable richer mod interactions

## Important Notes About Harmony

If you use Harmony patches in your own mods, **do not call `Harmony.PatchAll()` yourself**, as the project already manages Harmony patches internally. Calling `PatchAll()` multiple times can cause **duplicate patch execution** and unexpected behavior.

## Disclaimer

PlasmaModding is an independent project and is **not affiliated with or endorsed by Dry Licorice**. All trademarks and game assets belong to their respective owners.
