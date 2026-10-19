# Task: Implement "Day 1: The Crisis" Narrative Flow, Quest State Machine, and Cutscene Triggers

You are an expert Unity Developer. Your task is to build a clean, modular, and scalable C# architecture in Unity to execute the exact narrative and gameplay loop for "Day 1: The Crisis" described below.

## 1. Technical Specifications & Architecture Rules
- **Engine & Render Pipeline:** Unity (URP).
- **Input:** Unity New Input System (use an InputReader ScriptableObject or clean event-driven input handling for player movement, interaction, and dialogue progression).
- **Camera & Cutscenes:** Cinemachine (switch between virtual cameras for gameplay, dialogue, and the pickup truck cutscene) + Unity Timeline / Coroutine sequences for cartoon animations.
- **Data-Driven Design:** Use `ScriptableObjects` or JSON for Dialogue Data, Quest Steps, and Inventory Items (Plants, Water, Flowers, Sawit Seeds). Do NOT hardcode dialogue strings directly inside MonoBehaviour logic if avoidable.
- **Folder Structure:** Organize all generated scripts and assets strictly under:
  - `Assets/_Project/Scripts/Core/` (Game/Day Managers, Event Bus)
  - `Assets/_Project/Scripts/Gameplay/` (Player Interaction, Farming, Fabricator Puzzle)
  - `Assets/_Project/Scripts/Narrative/` (Dialogue System, Quest/Day1 State Machine, Cutscene Controllers)
  - `Assets/_Project/Scripts/ScriptableObjects/` (DialogueSO, QuestSO, ItemSO)

---

## 2. Day 1 State Machine & Narrative Flow (Strict Requirement)
Implement a `Day1Director` or `QuestStateMachine` that strictly enforces the following sequential states and fallback behaviors:

### State 1: Morning - The Letter (Bedroom Lock)
- **Initial Condition:** Player spawns waking up in bed (`WakeUp_Grumble` animation trigger).
- **Constraint:** The bedroom door is LOCKED. The player cannot exit the room until they interact with the letter on the bedside table.
- **Interaction (`BedsideLetter`):**
  1. Opens a UI Pop-up showing a massive debt in fictional currency.
  2. Triggers Player monologue:
     - [PLAYER]: "How am I supposed to pay off a debt this huge..."
     - [Animation]: Trigger `Heavy_Sigh`.
     - [PLAYER]: "Maybe there are still plenty of crops I can harvest today. I'll go check the field. Maybe something survived."
  3. Unlocks the bedroom door.

### State 2: Exiting the House & The Distributor Cutscene
- **Trigger:** Player exits the front door of the house.
- **Cutscene Sequence:**
  1. Disable player movement input and switch to Cutscene Cinemachine Camera.
  2. A pickup truck speeds recklessly (cartoonish/comical motion) from West to East and screeches to a halt in front of the house.
  3. The Distributor exits the vehicle with a goofy, cartoonish flip animation (`Goofy_Flip`).
  4. **Dialogue Sequence 1:**
     - THE DISTRIBUTOR: "Good morning! You must be the one who works this field. I've heard about you."
     - [PLAYER]: "Heard what?"
     - THE DISTRIBUTOR: "That you work harder than anyone in this village, and that the rain has not been kind to you." [Anim: Sympathetic_Nod]
     - THE DISTRIBUTOR: "I represent the Instansi. We help hard-working families get back on their feet. This is a Fabricator. It's yours."
     - [PLAYER]: "Mine? Are you serious? How much does it cost?"
     - THE DISTRIBUTOR: "Nothing. Not one coin." [Anim: Wide_Smile] "Put plants in, and it gives you sawit seeds. Sawit grows fast and sells well. I'll buy every one."
     - [PLAYER]: "Why would you give this away for free?"
     - THE DISTRIBUTOR: "Because when you do well, we do well. Simple. Let's try it together. Bring me three plants from your field."
  5. **Action:** The Distributor lifts the Fabricator from the pickup truck with both hands and places it in front of the player's house (`Goofy_Setup` animation). The Fabricator GameObject is now active in the world.

### State 3: Tutorial Loop - Part 1 (Gather, Fabricate, Plant)
- **Active To-Do List UI:**
  1. Collect 3 plants from your field.
  2. Make sawit seeds in the Fabricator (tutorial puzzle).
  3. Plant 2 sawit seeds.
- **Fallback / Idle Behavior:** If the player wanders off without completing the list, the day continues normally. Villagers use their repeat lines. The Distributor waits by the machine and if interacted with, says: `"Whenever you're ready. No pressure."`
- **Progression:**
  - Once the player gathers 3 plants and completes the Fabricator tutorial puzzle, trigger **Dialogue Sequence 2**:
    - THE DISTRIBUTOR: "Look at that! First try. You have a gift for this, partner."
    - [PLAYER]: [Anim: Surprised] "That was easy."
    - THE DISTRIBUTOR: "The best things usually are. Plant two seeds, and tomorrow you'll see what I mean."
    - THE DISTRIBUTOR: "Now, try gathering some water, plants, and flowers from around the village. Let's try making 3 more for today."

### State 4: Tutorial Loop - Part 2 (Village Gathering & Distributor Departure)
- **Task:** Player plants the 2 sawit seeds in the field (spawning 2 sawit saplings next to old crops) AND gathers water, plants, and flowers around the village to fabricate 3 more sawit seeds under the Distributor's guidance.
- **Completion Cutscene:** Once all tasks are finished:
  - The Distributor waves goodbye, hops into the pickup truck, slams the gas, and drives off with a goofy cartoonish truck animation.

### State 5: Post-Tutorial - Sari's Warning
- **Trigger:** After the planting/tutorial is completed, Sari waves from the bridge. Approaching/triggering her starts **Dialogue Sequence 3**:
  - SARI: "Hey! What's that giant tube in front of your house?"
  - [PLAYER]: "A Fabricator. A man from the Instansi gave it to me. For free."
  - SARI: "For free?" [Anim: Tilt_Head] "Nothing's free. Not even my grandmother's advice, and she gives that to everyone."
  - [PLAYER]: "It makes seeds. Good ones. It could pay off the debt, Sari."
  - SARI: "Then I hope it does." [Anim: Smile] "Just read the small print, okay?"

### State 6: Night - Ending the Day
- **Trigger:** Player walks back to the front door at night.
- **Dialogue Sequence 4:**
  - [PLAYER]: "With this, I'm gonna be super rich someday."
- Transition to end of Day 1 / Save World Changes (Fabricator remains in front of house, 2 sawit saplings in field).

---

## 3. Villager Ambient / Repeat Dialogue Data
Include these NPCs with their repeatable dialogue lines in the dialogue data setup:
- **NEK IJAH:** "A new machine? Hm. Plants grow best when they're tended with patience, dear."
- **BU RATNA:** "Take these spare bottles. The river water is lovely and cool today."
- **PAK DARTO:** "That bridge creaks a little, but it's sturdy. Built it myself!"
- **DIMAS:** "Hey hey! Wanna see how fast I can run? I'll race you to your farm tomorrow!"
- **THE DISTRIBUTOR (While waiting):** "Take your time, partner. The machine isn't going anywhere."

---