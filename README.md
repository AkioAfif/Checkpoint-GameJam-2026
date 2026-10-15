# Checkpoint GameJam 2026

A 3D Unity game jam project (URP). This guide takes you from a **blank laptop** to **ready to make changes and push**, including setting up your AI coding agent (Claude Code or Codex/ChatGPT) so it can control the Unity Editor directly.

> Follow the steps in order. Most early-project errors come from a skipped step, usually **Git LFS**, **a different Unity version**, or **Synty not imported yet**.

---

## 0. Technical summary

| | |
|---|---|
| Unity | **6000.6.5f1** (must be this exact version) |
| Render pipeline | URP 17.6 |
| Input | Input System 1.20. Legacy input (`Input.GetKey`) is **disabled** |
| UI | uGUI + TextMeshPro |
| Art | Synty POLYGON Generic + POLYGON Starter (**not in the repo**, imported manually) |
| Repo | https://github.com/AkioAfif/Checkpoint-GameJam-2026 |
| AI | Claude Code **or** Codex (ChatGPT) + Unity MCP. Shared coding rules live in `AGENTS.md` and `.claude/rules/` |

---

## 1. Install software (one time only)

Tick everything before moving on:

- [ ] **Git**: https://git-scm.com/download/win (the default options are fine).
- [ ] **Git LFS**: usually bundled with the Git for Windows installer. Check with `git lfs version`.
- [ ] **Unity Hub**: https://unity.com/download
- [ ] **Unity 6000.6.5f1** via Unity Hub → *Installs* → *Install Editor*. If the version isn't listed, go to *Archive* or https://unity.com/releases/editor/archive. **Don't** use another version (e.g. 6000.5.x). It would upgrade the project and cause conflicts with the rest of the team.
- [ ] **IDE**, pick one:
  - Visual Studio 2026 Community with the **Game development with Unity** workload. The `.vsconfig` file in the repo offers this workload automatically.
  - JetBrains Rider.
- [ ] **An AI coding agent**, pick one:
  - **Claude Code**: the desktop app (https://claude.com/download) or the CLI (`npm install -g @anthropic-ai/claude-code`). Requires a Claude account and version **2.1.281 or newer** (check with `claude --version`), because older versions don't read `AGENTS.md`.
  - **Codex** (ChatGPT's coding agent): the CLI (`npm install -g @openai/codex`), the Codex IDE extension, or the Codex app. Sign in with your ChatGPT account.
- [ ] **Unity CLI** (needed so the AI can control Unity). Run in **PowerShell**:
  ```powershell
  irm https://unity.com/install.ps1 | iex
  ```
  Close and reopen the terminal, then check:
  ```bash
  unity --version
  ```

---

## 2. Request access

Contact **Akio** for two things:
1. A **collaborator invite** to the GitHub repo. Accept it via email or your GitHub notifications.
2. **Access to the Synty assets** (POLYGON Generic & POLYGON Starter). They are deliberately not in git because they're ±160 MB and bound by the Asset Store license.

---

## 3. Clone the repo

```bash
git lfs install
```
```bash
git clone https://github.com/AkioAfif/Checkpoint-GameJam-2026.git
```

Run `git lfs install` **once per laptop**, and **before** cloning. If you skip it, files like `.fbx`/`.png`/`.exr` will only contain text pointers and will look broken in Unity. The fix is in [Troubleshooting](#9-troubleshooting).

**Recommended:** enable Unity Smart Merge so conflicts in scene/prefab files can be merged automatically. Run these inside the repo folder, and adjust the path if Unity is installed somewhere else:
```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
```
```bash
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
```
```bash
git config merge.unityyamlmerge.recursive binary
```

---

## 4. Open the project in Unity

1. Unity Hub → *Projects* → *Add* → *Add project from disk* → select the cloned folder.
2. Make sure the *Editor Version* column shows **6000.6.5f1**, then open it.
3. The first open can take **5–15 minutes** while Unity builds the `Library/` folder. Let it finish.
4. If a **Safe Mode** dialog appears, there's a compile error. Choose *Enter Safe Mode*, read the error in the Console, and tell the team. Don't choose *Ignore* and carry on working.

---

## 5. Import Synty (required, before opening any scene)

1. In Unity: *Window → Package Manager* → in the top-left dropdown, choose **My Assets** (sign in with the Unity account that has Synty access).
2. Download + Import **POLYGON - Generic** and **POLYGON - Starter** (Prototype).
3. Make sure they end up in:
   ```
   Assets/Synty/PolygonGeneric/
   Assets/Synty/PolygonStarter/
   Assets/Synty/SyntyPackageHelper/
   ```
   These paths must match exactly, because the existing scenes reference assets at these locations. If Synty imported somewhere else, **move** it to the paths above using Unity's Project window (not File Explorer).
4. Open `Assets/Scenes/Testing Scene.unity`. The greybox level (stairs, ramps, crates) should look normal, with **no pink**.

> ⚠️ **Don't save any scene before Synty is imported.** Saving while assets are missing can break the references for everyone.

---

## 6. Connect your AI to Unity

This lets the AI recompile, read the Console, create GameObjects/prefabs, and press Play directly in the Unity Editor. Do **6a** if you use Claude Code, **6b** if you use Codex (ChatGPT), or **6c** if you only use the normal ChatGPT/Claude chat website.

### 6a. Claude Code

1. Install the Unity plugin for Claude Code (one time):
   ```bash
   claude plugin marketplace add Unity-Technologies/unity-agent-plugin
   ```
   ```bash
   claude plugin install unity@unity-agent-plugin
   ```
2. Register the Unity MCP server with Claude Code:
   ```bash
   unity mcp configure claude-code
   ```
   If it warns that a plugin already provides an MCP server, that's normal. Choose *skip*.
3. **Open the Unity Editor with this project first**, then start Claude Code in the project folder. With the desktop app, pick the repo folder as the working folder. With the CLI, `cd` into the repo folder and run `claude`.
4. Claude Code reads `AGENTS.md` automatically, because the repo has no `CLAUDE.md`. To check, ask:
   > What do your project instructions say? Then check the connection to the Unity Editor and show the editor status and console contents.

   If Claude summarizes the rules from `AGENTS.md` **and** shows the editor status, setup is done ✅.

### 6b. Codex (ChatGPT)

1. Install the Unity plugin for Codex (one time):
   ```bash
   codex plugin marketplace add Unity-Technologies/unity-agent-plugin
   ```
   ```bash
   codex plugin add unity@unity-agent-plugin
   ```
2. Register the Unity MCP server with Codex. This also allows Codex's sandbox to reach the Unity Editor over localhost:
   ```bash
   unity mcp configure codex
   ```
3. **Open the Unity Editor with this project first**, then start Codex in the repo folder (`cd` into it and run `codex`, or open the folder in your IDE with the Codex extension).
4. Codex reads `AGENTS.md` automatically. Test it with the same prompt as in 6a. If Codex summarizes the rules **and** shows the editor status, setup is done ✅.

### 6c. Plain chat website (chatgpt.com / claude.ai) — not recommended

A chat website can't read the repo or control Unity, so the rules aren't applied automatically. If you still work this way:
1. At the start of every conversation, paste the contents of `AGENTS.md`, plus the `.claude/rules/` files for your task (see the table inside `AGENTS.md`). For any C# script, that's at least `csharp-architecture.md`, `csharp-pitfalls.md` and `code-templates.md`.
2. Copy the code into Unity yourself, then check the Console for errors and test in Play mode yourself, because the AI can't verify anything.

If something fails in 6a or 6b, run `unity doctor` in a terminal and see [Troubleshooting](#9-troubleshooting).

---

## 7. How the AI works in this project (important)

- **`AGENTS.md`** in the root is the **one shared instruction file for every AI**. Claude Code and Codex both read it automatically every session. It holds an index and 10 never-break rules.
- **`.claude/rules/`** holds the detailed rules. Despite the folder name, they are for every AI. Claude Code auto-loads them based on which files are being worked on, and `AGENTS.md` tells every other agent which ones to open:

  | File | Contents |
  |---|---|
  | `project-settings.md` | Project setting facts: versions, input actions, layers, scenes, etc. |
  | `csharp-architecture.md` | Code patterns (*Kitchen Chaos* style): 1 class = 1 job, communication through events |
  | `csharp-pitfalls.md` | Checklist for preventing Unity errors |
  | `code-templates.md` | Ready-to-use class templates |
  | `input.md`, `ui.md`, `scriptable-objects.md` | Per-area rules |
  | `unity-editor-workflow.md` | Editor, prefab and git workflow |
  | `rendering-urp.md` | URP materials & shaders |

- `.claude/settings.json` registers a hook (`.claude/hooks/check-unity-rules.sh`) that checks every C# file Claude Code edits for the never-break rules (legacy `Input`, `using UnityEditor` in runtime code, `?.`/`??`, `FindObjectOfType`) and makes Claude fix them before moving on. It runs in Git Bash, which Claude Code on Windows already requires.
- `AGENTS.md`, `.claude/rules/` and `.claude/settings.json` **are committed**, so the whole team and every AI follow the same rules.
- **`CLAUDE.md` is gitignored** (along with `CLAUDE.local.md`) and is only for **your own personal notes**. ⚠️ If you create one, its **first line must be `@AGENTS.md`**. As soon as a `CLAUDE.md` or `CLAUDE.local.md` exists, Claude Code stops reading `AGENTS.md` on its own, and that import line brings it back.
- Don't create a personal `AGENTS.md` variant or rename the shared one. Codex users read it directly.
- If you **change a project setting** (add a layer/tag, a build scene, a package, an input action), update `.claude/rules/project-settings.md` in the same commit. You can also ask your AI to do it.
- If you want to change the coding rules, discuss with the team first, since the change applies to everyone.

### Code patterns in brief (so you can follow along in reviews)
- Each gameplay object is split up: `Checkpoint` (logic) → `CheckpointVisual`, `CheckpointSound`, `CheckpointUI`. The logic **never** touches the Animator, audio, or UI.
- The logic raises `event EventHandler` events, and the Visual/Sound/UI components subscribe to them.
- Data (items, levels, audio) lives in **ScriptableObjects**.
- Input only goes through `GameInput`. Game flow is controlled by a state machine in `GameManager`.
- **Domain reload is disabled in this project** (so entering Play is faster). As a result, `static` variables don't reset on their own. Follow the templates in `code-templates.md`.

---

## 8. Daily workflow for making a new change

### a. Start
```bash
git switch main
```
```bash
git pull
```
Open Unity and wait until it finishes compiling/importing.

### b. Create a branch for your feature
```bash
git switch -c feature/feature-name
```
Example branch names: `feature/checkpoint-system`, `fix/player-falls-through-floor`.

### c. Coordinate scenes
Scene files (`.unity`) are **hard to merge**. Team rules:
- **Only one person edits a given scene at a time.** Announce it in the group chat before editing a scene.
- Build features as **prefabs** (`Assets/Prefabs/<Feature>/`) or in your own testing scene. Only place the prefab into the main scene at the end.

### d. Work with your AI
Example of a good prompt, specific about *what*, *where*, and *how it behaves*:
> Create a checkpoint system: when the player enters a checkpoint trigger, that checkpoint activates (flag animation + sound), and when the player falls below Y = -10 they respawn at the last checkpoint. Make the prefab in Assets/Prefabs/Checkpoint and place 2 of them in Testing Scene.

The AI (Claude Code or Codex) will read `AGENTS.md` and the rules in `.claude/rules/`, write the scripts, recompile, check the Console, and report back. After that, **still check yourself**:
- [ ] Unity Console: **0 errors** (red icon).
- [ ] Press **Play** and try the feature. There must be no runtime errors.
- [ ] Every `[SerializeField]` field in the Inspector is assigned (none show *None*).
- [ ] The scene is saved (Ctrl+S).

### e. Commit & push
```bash
git status
```
Make sure you **only** commit files in `Assets/`, `Packages/`, `ProjectSettings/`, `.claude/rules/`, or root files like `AGENTS.md`/`README.md`. Your personal `CLAUDE.md` is ignored automatically. Every new asset file must come with its **`.meta` file**.
```bash
git add -A
```
```bash
git commit -m "Add checkpoint system"
```
```bash
git push -u origin feature/feature-name
```
Then open a **Pull Request** to `main` on GitHub and ask one teammate to review it. You can also ask your AI to create the commit/PR for you.

### f. After the PR is merged
```bash
git switch main
```
```bash
git pull
```

---

## 9. Troubleshooting

| Symptom | Cause & fix |
|---|---|
| Broken models/textures, `.fbx`/`.png` files are only a few hundred bytes | Git LFS wasn't active when you cloned. Run `git lfs install`, then `git lfs pull`. |
| Objects in the scene are **pink/magenta** | Synty isn't imported or is in the wrong folder (see step 5), or a material uses a non-URP shader. |
| *Missing Prefab* / red objects in the Hierarchy | Synty isn't imported. Import it first, and **don't save** the scene before that. |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` | The code uses legacy input. All input must go through `GameInput` (see `.claude/rules/input.md`). |
| Events fire twice / `MissingReferenceException` after the second Play | A static variable isn't reset (domain reload is off). Follow the `RuntimeInitializeOnLoadMethod` template and unsubscribe in `OnDestroy`. |
| The AI says it can't connect to Unity | Make sure the Unity Editor is open with this project **and not in Safe Mode**. Run `unity doctor`, then restart the Claude Code/Codex session. |
| Claude Code doesn't know the project rules | You probably have a personal `CLAUDE.md` or `CLAUDE.local.md` without `@AGENTS.md` on its first line (see step 7), or Claude Code is older than 2.1.281 (`claude --version`). |
| Unity asks to upgrade the project / version mismatch | You opened it with the wrong Unity version. Close it **without upgrading**, then install 6000.6.5f1. |
| Lots of compile errors after `git pull` | Wait for Unity to finish importing. If errors remain, close Unity, delete the `Library/` folder, and reopen (slow, but safe). |
| Merge conflict in a `.unity`/`.prefab` file | Don't edit it by hand. Pick one version (`git checkout --ours/--theirs <file>`), then redo your small change in Unity. Better to prevent this through scene coordination (step 8c). |

---

## 10. Folder structure

```
Assets/
├── Scripts/
│   ├── Core/                GameManager, GameInput, Loader, SoundManager
│   ├── Player/
│   ├── <Feature>/           logic + Visual + Sound for one feature
│   ├── UI/
│   ├── Interfaces/
│   └── ScriptableObjects/   SO class definitions
├── ScriptableObjects/       data assets (.asset)
├── Prefabs/<Feature>/
├── Materials/
├── Scenes/
├── Settings/                URP assets (don't change without discussing)
└── Synty/                   third-party art, read-only, not committed
.claude/rules/               detailed coding rules for every AI (committed)
AGENTS.md                    shared AI instructions + index of the rules (committed)
CLAUDE.md                    optional personal notes, must start with @AGENTS.md (gitignored)
```

Folders under `Scripts/`, `Prefabs/`, etc. will appear as features get built.
