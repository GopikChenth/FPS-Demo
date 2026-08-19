# 🎯 FPS-Demo

[![Unity 6](https://img.shields.io/badge/Unity-6000.5.8f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-17.5.0-blueviolet.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![Netcode](https://img.shields.io/badge/Netcode-DOTS%20%2F%20Entities%206.5.0-orange.svg)](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
[![UI](https://img.shields.io/badge/UI-UI%20Toolkit-success.svg)](https://docs.unity3d.com/Manual/UIElements.html)

A high-performance, multiplayer first-person shooter built in **Unity 6** using **Netcode for Entities (DOTS / ECS)**, **Universal Render Pipeline (URP)**, and **UI Toolkit**. Features client-side prediction, modular weapon attachments, procedural weapon sway/recoil spring mechanics, dynamic ADS sight alignment, and fluid movement.

---

## ⚡ Quick Start Guide (For Collaborators & Friends)

Follow these steps to clone the project, open it in Unity, and start playing:

### 1. Prerequisites
- **Git** installed on your system.
- **Unity Hub** installed.
- **Unity Editor 6000.5.8f1** (Unity 6) installed with:
  - *Universal Windows Platform / Mac / Linux Build Support* (depending on your OS).

### 2. Clone the Repository
Open your terminal or Git Bash and clone the repository:
```bash
git clone https://github.com/GopikChenth/FPS-Demo.git
```

### 3. Open in Unity Hub
1. Open **Unity Hub**.
2. Click **Add** ➔ **Add project from disk**.
3. Select the cloned `FPS-Demo` folder.
4. Ensure the Editor version is set to **`6000.5.8f1`** (Unity 6).
5. Click on the project to launch.
   > **Note:** The initial import may take a few minutes as Unity compiles the ECS packages, shaders, and regenerates the local `Library` cache.

### 4. Running the Game
1. In the Project window, navigate to `Assets/Scenes/`.
2. Open **`MainMenu.unity`**.
3. Press the **Play** (▶) button in the Unity Editor toolbar.

---

## 🎮 How to Play & Test Multiplayer

### Option A: Local Testing with Multiplayer Play Mode (MPPM)
You can test host and client instances concurrently inside the Unity Editor:
1. Go to **Window ➔ Multiplayer ➔ PlayMode Tools** (or click the MPPM widget in the toolbar).
2. Set virtual player count to **2 Players** (1 Main Editor + 1 Virtual Clone).
3. Press **Play**:
   - On Window 1: Enter your name, select your character, and click **Start Host**.
   - On Window 2: Enter player name and click **Connect to Server** or **Join**.

### Option B: LAN / Direct Connect
1. **Player 1 (Host)**:
   - In the Main Menu, switch Connection Mode to **Direct Connect**.
   - Click **Start Host**.
2. **Player 2 (Client)**:
   - In the Main Menu, switch Connection Mode to **Direct Connect**.
   - Enter Player 1's local IP address in the session/IP field.
   - Click **Connect to Server**.

---

## ⌨️ Controls

| Action | Keyboard & Mouse | Gamepad |
| :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` | Left Stick |
| **Look / Aim** | `Mouse` | Right Stick |
| **Fire / Shoot** | `Left Mouse Button` | Right Trigger (`RT` / `R2`) |
| **Aim Down Sights (ADS)** | `Right Mouse Button` (Hold) | Left Trigger (`LT` / `L2`) |
| **Sprint** | `Left Shift` | Left Stick Press (`L3`) |
| **Slide** | `C` / `Ctrl` *(while sprinting)* | `B` / `Circle` *(while sprinting)* |
| **Crouch** | `C` / `Left Ctrl` | `B` / `Circle` |
| **Jump** | `Spacebar` | `A` / `Cross` |
| **Reload** | `R` | `X` / `Square` |
| **Scoreboard** | `Tab` | View / Select |
| **Pause Menu** | `Esc` | Start / Options |

---

## 🛠️ Key Gameplay & Technical Features

- **Server-Authoritative Networking (ECS / DOTS)**: Powered by Unity's high-performance Netcode for Entities with server rollbacks and client-side prediction.
- **Weapon Attachment System**:
  - **Optics / Sights** (e.g., Reflex Sight): Custom sight alignments and dynamic FOV zoom magnification.
  - **Muzzle Devices** (e.g., Tactical Suppressor, Compensators): Recoil reduction, flash dampening, and audio tuning.
  - **Foregrips** (e.g., Vertical Foregrip): Stabilized horizontal and vertical recoil spring kicks.
- **Procedural Weapon Mechanics**:
  - Harmonic spring recoil and position kick.
  - Movement and mouse velocity weapon sway.
  - Procedural camera roll during strafing and sliding.
- **Modern UI Toolkit Integration**:
  - Responsive HUD with dynamic reticle expansion/fading during ADS.
  - Live kill feed, action log, and scoreboard overlay.
  - Crisp vector-styled main menu and pause interfaces.

---

## 📁 Repository Structure

```text
FPS-Demo/
├── Assets/
│   ├── Data/                  # ScriptableObjects (Weapon Attachments, Profiles)
│   ├── Prefabs/               # Player characters, weapon models, projectiles
│   ├── Scenes/                # MainMenu, GameScene, Persistents, SubScenes
│   ├── Scripts/
│   │   ├── Gameplay/          # Player movement, cameras, weapons & attachments
│   │   ├── GhostBridge/       # Netcode ECS Ghost serialization & authoring
│   │   └── UI/                # UI Toolkit presenters, HUD, and menu controllers
│   ├── Settings/              # URP graphics and volume profiles
│   └── UI Toolkit/            # UXML templates and USS style sheets
├── Packages/                  # Package manifests and dependency locks
└── ProjectSettings/           # Unity engine, physics, tag, and input mappings
```

---

## ❓ Troubleshooting

- **Missing Assemblies / Compilation Errors**:
  - Verify that you are using **Unity 6000.5.8f1**.
  - In Unity, go to `Edit ➔ Preferences ➔ External Tools` and click **Regenerate project files**.
- **Can't Connect in Multiplayer**:
  - Ensure Windows Firewall or your local antivirus is not blocking incoming UDP ports for Unity Netcode.
  - Make sure both machines are on the same local subnet when using LAN direct connect.
