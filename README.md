# LMProgramming Utilities (com.lm.pro)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A collection of essential C# utilities and helpers designed to streamline common tasks in Unity game development. These are components and extensions I frequently reuse across my projects.

## Overview

This package aims to provide robust, easy-to-use solutions for everyday programming challenges in Unity, from object pooling and input management to grid manipulation and custom data structures.

## Features

Here's a quick rundown of what's included:

* **EasyPool:** A simple, efficient object pooling system to manage and reuse game objects, reducing garbage collection and improving performance.
* **Graph:** Basic generic graph data structure implementation (nodes and edges) for pathfinding, AI, or procedural generation tasks.
* **SimpleSoundManager:** A straightforward manager for playing and managing 2D and 3D audio clips with basic controls.
* **DefaultDictionary:** A dictionary implementation similar to Python's `defaultdict`, which provides a default value for keys that haven't been set yet.
* **Image Orientation Helpers:** Utilities to assist with image orientation tasks, potentially for UI or texture manipulation.
* **GameInput:** A streamlined wrapper for quickly accessing and managing player inputs (keyboard, mouse, touches). Features easy get object under pointer, double click detection, is pointer over UI and more.
* **Grid Utilities:**
  * **GridMarcher:** An algorithm to collect all integer grid points along a line segment from point A to point B on a 2D grid.
  * **GridRegionFinder:** An algorithm to identify "islands" or contiguous regions of similar cells/pixels in a 2D grid.
* **Math & Vector Extensions:** A set of extension methods for common math operations and `Vector2`/`Vector3` manipulations to simplify calculations.
* **SimpleTimer:** A basic timer class for managing time-based events and countdowns.

## Requirements

* **Unity 6000.0 or higher**.
* **Git Dependency Resolver for Unity:** This tool is required to fetch the git-based dependencies.

## Dependencies

This package relies on the following external libraries, which will be automatically resolved if you have the Git Dependency Resolver installed:

* **UniTask (`com.cysharp.unitask`):** For improved asynchronous programming.
* **Extenject (Zenject) (`com.svermeulen.extenject`):** For dependency injection.
* **Addressables (`com.unity.addressables`):** For managing game assets.

## Installation

1. **Install Git Dependency Resolver (if you haven't already):**
    * Follow the installation instructions from the [Git Dependency Resolver GitHub repository](https://github.com/mob-sakai/GitDependencyResolverForUnity). This usually involves adding a scoped registry or downloading a `.unitypackage`.

2. **Install LMProgramming Utilities via Unity Package Manager:**
    * In Unity, open the Package Manager (`Window > Package Manager`).
    * Click the `+` button in the top-left corner.
    * Select "Add package from git URL..."
    * Enter the following URL and click "Add":

        ```sh
        https://github.com/lmProgramming/lm-pro-package.git
        ```

    * The Git Dependency Resolver should then automatically fetch UniTask and Extenject.

## Contributing

Feel free to contribute with issues and pull requets :)  
I will be happy if anyone gets to use this besides me

## Development

Always bump the version in `package.json` when making changes.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details. The MIT License is a permissive, open-source license that grants users the freedom to use, modify, and distribute software, even for commercial purposes, with minimal restrictions.
