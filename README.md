# SGSG-runtime

**SGSG Client for XR Interaction and Visualization**

This repository contains the Unity client implementation of our **Stroke-Guided 3D Scene Graph (SGSG)** system. It allows users to draw 3D strokes in VR (using **PICO 4**) to annotate or correct semantic elements in a scene.

---

## Overview

The system enables:
- Real-time stroke drawing using XR controllers
- Multiple stroke types (Type0 / Type1 / Type2) for different interaction intents
- Automatic stroke storage as JSON (`manual_strokes.json`)
- Communication with a backend inference server
- Visualization of user input and interaction metrics
- Scene graph updating and rendering
  


---

## Requirements

| Category | Specification |
|-----------|---------------|
| **Unity Version** | 2022.1.16f1c1 |
| **Target Platform** | PICO 4 (arm x64 Android) |
| **Connection** | USB cable link via **PICO Developer Center** (Streaming Assistant) |
| **Operating System** | Windows 10 / 11 (tested) |
| **XR Mode** | OpenXR backend |
| **Render Pipeline** | Built-in pipeline (no URP/HDRP required) |

---

## Dependencies (Unity Packages)
This project relies on several built-in and external Unity packages.
When you open the project in Unity, these packages will automatically be restored via the Unity Package Manager.

Below is the verified package list used in this version of SGSG-Client:
| Category | Package | Version | Description |
|-----------|----------|----------|-------------|
| **Microsoft Mixed Reality Toolkit (MRTK)** | Mixed Reality OpenXR Plugin | 1.5.1 | Enables OpenXR support for MRTK. |
|  | Mixed Reality Toolkit Extensions | 2.8.2 | Core extension components for MRTK. |
|  | Mixed Reality Toolkit Foundation | 2.8.2 | Main foundation layer for MRTK features. |
|  | Mixed Reality Toolkit Standard Assets | 2.8.2 | Default prefabs, shaders, and utilities. |
|  | Mixed Reality Toolkit Tools | 2.8.2 | Toolkit utilities and configuration tools. |
|  | MRTK Graphics Tools | 0.4.0 | Additional shaders and materials for MRTK. |
| **PICO Integration** | PICO Live Preview | 1.0.5 | Enables real-time preview and debugging on PICO devices. |
|  | PICO OpenXR Plugin | 1.3.3 | PICO’s OpenXR runtime integration for Unity. |
| **Unity Official Packages** | XR Interaction Toolkit | 3.1.1 | Provides XR input, ray interactor, and controller interaction system. |
|  | OpenXR Plugin | 1.14.1 | Unity’s OpenXR backend supporting PICO and other headsets. |
|  | Animation Rigging | 1.1.1 | Enables runtime bone constraints for XR avatars. |
|  | Shader Graph | 13.1.8 | Node-based shader authoring tool. |
|  | TextMeshPro | 3.0.6 | Advanced text rendering support. |
|  | Timeline | 1.7.1 | Timeline animation sequencing tool. |
|  | FBX Exporter | 4.1.3 | Exports Unity assets and animations to FBX format. |
|  | Visual Scripting | 1.7.8 | Node-based scripting system. |
|  | Unity UI (uGUI) | 1.0.0 | Built-in Unity UI system. |
|  | Test Framework | 1.1.33 | Unit testing utilities. |
|  | Version Control | 1.17.2 | Unity Plastic SCM integration. |
|  | Visual Studio Code Editor | 1.2.5 | Integration with VS Code. |
|  | Visual Studio Editor | 2.0.16 | Integration with Visual Studio. |
|  | JetBrains Rider Editor | 3.0.15 | Integration with JetBrains Rider IDE. |

---

> **Note:**  
> The package list above reflects the full development environment used during the project. Not all of these packages are strictly required to run the demo, as some were included for testing or auxiliary tools.
