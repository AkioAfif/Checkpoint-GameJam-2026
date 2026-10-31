# Day 5: Half the Forest (and the road to Day 6: The Offer)

Story outline mapping: Day 1 = Prologue + Chapter 0, Day 2 = Chapter 1, Day 3 = Chapter 2 by hand, Day 4 = the sawit power and the first trees, **Day 5 = Chapter 3 (the forest goes, the land starts to change)**, a two-week skip, then **Day 6 = the opening of Chapter 4 (the Distributor's new crop and his slip)**.

Where the story is headed:
- The debt gets paid off.
- Half of the forest becomes plantation.
- The Distributor comes back, congratulates the MC and pitches a far more profitable crop. He doesn't push; he shows the projected earnings and reminds the MC how hard the road here was.
- **The clue:** he already knows about the environmental changes, before the MC has said a word about them.

Day 5 is built (`FixScene_Day5`). Day 6 is design only.

## 1. Scope and pacing

Day 5 is the morning after Day 4. There is no caption and no skip, and `StartDay(5)` leaves 15 days. One sawit gave the MC enough strength for four plots yesterday; today one isn't enough. So the MC eats the whole harvest, tears the world for three seconds, rockets into the sky and comes down fist first, and a ripe plantation of 48 beds stands where the forest edge was. Then the land starts to answer back: the mutation that only flickered in Day 2's palms now shows up in the wild plants. The villagers notice. The MC doesn't, or won't.

**The rule that makes Day 6 work:** the MC never mentions the red to anyone, not even in a monologue. Only villagers talk about it, and only to the MC.

Kept small for the jam:
- One new scene, copied from Day 4.
- One director (`Day5Director`) and one set piece (`PlantationSequence`), which reuses `LandQTE` (hold + mash), `WorldGlitchVisual` (with a `severity` knob), `SmashImpactVisual` and `ImaginationVisual`.
- The plantation is the map's own `garden_3x4` model, tiled 2 × 2 with a FarmPlot on every bed. No new trees are planted.
- The rest is small:
  - three procedural poses (`SupermanFly`, `SupermanDive`, `GroundPunch`);
  - a graybox bird;
  - `MutationSpreadVisual` (material swaps to `MutantPulse`, Day 2's red).

Left for later:
- Day 6 itself.
- Sawit Lv.3 (the new crop).
- The red spreading further (more plants, the river).
- Rigel's own arc (he stays away from the MC after the bridge).

## 2. Beat sheet (Day 5)

| # | Where | Beat | Player does | Objective |
|---|---|---|---|---|
| 0 | Bedroom | The savings book: about 28M left, 15 days. "Fifteen days. One field won't do it." / "But two arms might." | Read | Check your savings book |
| 1 | Garden + Day 4's field | Last night's garden wakes up ripe. Harvest. The ICAN crate is **locked** ("Not yet... I need every bunch.") | Harvest | Harvest 4 Lv.2 sawit bunches |
| 2 | Wish shot | `CM_Day5Wish` looks from over the pond south across the marked land. The see-through tiled gardens with ripe palms twinkle in, and the land's trees fade out. "Four plots yesterday..." / "What if all of that was palms?" | Watch | (cutscene) |
| 3 | Anywhere | "One sawit gave me the strength for four plots." / "Let's see what one does today." Then "[E] Eat Sawit" with nothing targeted, and hold [E] 1 s (Eat pose). One bunch is taken from the bag. The ground doesn't change on its own | Eat, hold | Eat a Lv.2 sawit bunch |
| 4 | The dig marker | "...I feel a little stronger." / "Strong enough to dig a whole field by hand? Only one way to find out." A glowing disc with a bobbing diamond marks a spot south of the pond. On it, "[E] Dig": a hard mash (8 presses × 3 blows) in the bare-fisted `Crouch` pose, dirt flying with each blow. `CM_Day5TinyBed`: **one** garden bed pushes up. "Haah... haah..." / "All that... for one little bed?!" / "By hand this will take forever. I need more sawit. A lot more." | Walk, mash | Dig the land south of the pond |
| 5 | Anywhere | [E] again. A mash QTE of 3 presses per bunch, one fill per bunch left in the bag, a chomp per bunch | Mash | Eat the rest of your Lv.2 sawit |
| 6 | Everywhere | **A severe world glitch, exactly 3.0 s**, full screen (`severity` 1.6, jolts up to 0.9 m) | Watch | (cutscene) |
| 6b | Old TV | **The childhood show**, 5.5 s: the world dims and an old TV ("CAPTAIN HARVEST", "A show from when I was little...") shows a cartoon superhero dropping out of the sky fist first, landing in a crouch with a crater and dust, flowers blooming outward from the crater, then standing with both arms raised. "...Captain Harvest." / "The show I watched every morning when I was little." / "He jumped down from the sky, and flowers grew wherever he landed." / "If he could do it..." | Watch | (cutscene) |
| 6c | The jump marker | A second marker at the land's north edge, below the pond. Walking onto it: "Okay, let's try this." | Walk | Stand on the marker by the pond |
| 7 | The sky | **Launch:** the MC shoots up from the marker about **520 m** over 9 s, in the `SupermanFly` pose (body along the flight, one fist punched up and out). `CM_Day5Launch` chases from behind. The sky changes with altitude (camera background and fog): morning blue → deep blue → the white of the **cloud layer** (puffs at 140–175 m that the MC bursts through) → navy stratosphere → black **space** past 380 m (no fog, stars), and from 420 m a big curved **Earth** with a pale haze rim fills the bottom of the frame. **Sky run:** [A]/[D] steer across 3 lanes; 12 rows up the climb with floating Lv.2 bunches to catch and sweeping birds to dodge (a hit knocks one loose). "Sawit ×n" on the HUD; the catches go in the bag at the top | Steer | (minigame) |
| 8 | Peak | **No cut:** the launch camera simply stops following and holds its last position and angle. The MC hangs 0.35 s, flips head-down and drops out of the bottom of the frame, the Earth below | Watch | (cutscene) |
| 9 | Dive | A hard cut to `CM_Day5Dive`, chasing from above (the Earth backdrop hidden, the sky colours running back down the bands). The MC is in the `SupermanDive` pose, fists first, speeding into the land's middle over 2.4 s | Watch | (cutscene) |
| 10 | Impact | `GroundPunch`. An earthquake: a rumble impulse (force 2.2, 1.4 s), a FOV punch, hit-stop and big dust rings. The land's 7 map plants vanish, the 4 garden tiles heave up and 48 ripe Lv.2 palms spring up. `CM_Day5Plantation`: "...Whoa." / "A whole plantation. Ripe. In one punch." | Watch | (cutscene) |
| 11 | Edge shot | **The mutation spreads, behind the MC's back.** `CM_Day5Edge` looks over the plantation at the trees and bush just past its east edge, which flicker blood-red again and again while the MC says "Half a day's work!" / "Time to sell, and see how everyone in the village is doing." | Watch | (cutscene) |
| 12 | Village | The crate unlocks. **The villagers have noticed:** Ijah, Ratna and Dimas each have a Day 5 talk about something red, and the MC talks each one away. Each talk counts once | Sell, talk to 3 | Sell sawit at the ICAN crate · Check on Nek Ijah, Bu Ratna and Dimas (0/3) |
| 13 | Plantation, dusk | "That evening..." The savings book: "The best day yet." / "At this rate the debt is gone before the deadline." | Read | (cutscene) |
| 14 | Edge shot, night | The same edge pulses red in the dark. "Half the forest by the end of the month..." / "Nobody in this village will ever owe anyone again." | Watch | (cutscene) |
| 15 | Home | Walk home. For now the door leads to the end card (`HouseInterior_Stub`) until Day 6 exists | Enter the house | Go home |

Rigel isn't in Day 5 (switched off): after the bridge he keeps away.

## 3. Dialogue script (Day 5)

Seeded by `Day5DataBuilder`. Edit the `.asset` files afterwards.

| Asset | Lines |
|---|---|
| `Day5_Morning` | MC: "Fifteen days. One field won't do it." (HeavySigh) / "But two arms might." (WideSmile) |
| `Day5_Wish` | MC: "Four plots yesterday..." / "What if all of that was palms?" (WideSmile) |
| `Day5_EatOne` | MC: "One sawit gave me the strength for four plots." / "Let's see what one does today." |
| `Day5_DigIdea` | MC: "...I feel a little stronger." / "Strong enough to dig a whole field by hand? Only one way to find out." |
| `Day5_TinyGrowth` | MC: "Haah... haah..." (HeavySigh) / "All that... for one little bed?!" (Surprised) / "By hand this will take forever. I need more sawit. A lot more." (WideSmile) |
| `Day5_NoBunch` | MC: "I need Lv.2 sawit for this. The palms should have some." |
| `Day5_Hallucination` | MC: "...Captain Harvest." / "The show I watched every morning when I was little." / "He jumped down from the sky, and flowers grew wherever he landed." / "If he could do it..." (WideSmile) |
| `Day5_Marker` | MC: "Okay, let's try this." (WideSmile) |
| `Day5_Reveal` | MC: "...Whoa." (Surprised) / "A whole plantation. Ripe. In one punch." (Cheer) |
| `Day5_FieldDone` | MC: "Half a day's work!" (Cheer) / "Time to sell, and see how everyone in the village is doing." |
| `Ijah_Day5` | Ijah: "The flowers by your new field went the colour of rust overnight." (TiltHead) / "Flowers don't do that, child." / MC: "New soil, Nek. It'll settle." (WideSmile) |
| `Ratna_Day5` | Ratna: "Dimas brought home a leaf from the forest edge. It was red..." / "And warm. Like a hand." (HeavySigh) / MC: "Kids and their stories, Bu Ratna." (WideSmile) |
| `Dimas_Day5` | Dimas: "I dug up a worm by the river and it was RED. Glowing red!" (Cheer) / "Is that treasure?" / MC: "Just the dry season, Dimas. Dig somewhere else for a while." |
| `Day5_EveningMath` | MC: "The best day yet." / "At this rate the debt is gone before the deadline." (Cheer) |
| `Day5_Night` | MC: "Half the forest by the end of the month..." / "Nobody in this village will ever owe anyone again." |

## 4. Money

| | Value |
|---|---|
| After Day 4 | about 28M, 16 days |
| Day 5 morning | 15 days (`StartDay(5)`) |
| The eaten bunches | worth nothing (eaten) |
| The sky run | +1 Lv.2 bunch per catch (1,000,000 each when sold) |
| The plantation | 48 ripe Lv.2 palms, 48,000,000 if every one is harvested and sold, which is more than the whole debt. Day 6's "debt paid" follows from it |
| Played alone | `Day5SO.testStartingDebt` 28M, 16 days; every plot in the ground starts ripe with Lv.2 |

## 5. Layout (FixScene_Day5)

- **The marked land:** x 13.5 → 32.7, z −47.5 → −32.9.
  - It lies south of Pond_3 and Day 4's field, between the river and the dense forest band, and was confirmed with a top-down capture.
  - It is filled by **2 × 2 `garden_3x4` tiles** (9.6 × 7.3 m each, yaw 180), each set on the highest ground under it.
  - A `FarmPlot` sits on each of the 48 beds (local x ±1.15 / ±3.45, z 0 / ±2.3), with its own soil off.
  - The map plants on the land (`Forest_Tree_1_028`, `_3_033`, `_3_036`, `_3_039`, `Forest_Bush_2_002`, `_3_024`, `_2_035`) disappear at the impact.
- **The single bed (and the dig marker):** the first tile's middle-row bed nearest the take-off, cut from the model (`Art/Models/Yard/Garden3x4_Bed_Day5`), at (21.75, −36.55). The north row was in reach of the pond's water spot, which took the [E] press. The MC kneels 1.1 m north of it.
- **The jump marker:** `LaunchPoint` (23.1, −33.6). Both markers are `GroundMarkerVisual` (a glowing disc and a bobbing diamond), shown only while the sequence waits there.
- **The childhood show:** a studio at (0, −300, 60) under the map (grass disc, two lollipop trees, the crater decal, 27 Flower-item copies in three rings, a graybox superhero) filmed by `ShowCamera` into `Art/RenderTextures/ChildhoodShow.renderTexture` (shadows off: the terrain is above it), shown by `HUD/ChildhoodShowUI`.
- **The sky:** `Day5_Graybox/Sky`: 60 unlit cloud puffs at 140–175 m round the climb; a star mesh (`Art/Models/Day5/Day5_Stars`, 260 cubes 3 km out); the Earth, a 3 km-radius sphere whose top is at 400 m with a generated texture (`Art/Textures/Day5Earth.png`) and a haze shell. `SkyAscentVisual` bands: 20 m ground colour, 90 m blue, 145 m cloud white, 200 m deep blue, 290 m navy, 380 m near-black (space: fog off, stars on), 520 m black; the Earth shows from 420 m and hides on the dive. The launch and dive cameras clip at 9 km.
- **Points:**
  - take-off `LaunchPoint` (23.1, −33.6), facing south (the jump marker);
  - `DigSpot` (21.75, −36.55) with `DigStandPoint` 1.1 m north of it;
  - impact `ImpactPoint` (23.1, −40.2), the land's middle, facing north;
  - evening (14.6, −31.8).
- **Mutation:** `Forest_Bush_3_030`, `Forest_Tree_3_003`, `Forest_Tree_2_005` and `Forest_Tree_3_021`, just past the plantation's east edge. `CM_Day5Edge` films them by day and at night, from (14, 7, −29) toward (38, 1.5, −39).
- **The sky run:**
  - 12 rows up the 520 m climb; lanes 3.2 m apart along world west (the launch camera's screen-right);
  - a catch radius of 1.4 m;
  - the bunch model ×2.2;
  - birds are `Prefabs/Day5/SkyBird` (a graybox body and beak with flapping wings).
- **Removed from Day 4's scene:** its director root and UI, the trees it felled, and its ghosts. Its field, bridge and fence gap stay.

## 6. The skip: "Two weeks later..." (before Day 6, not built)

The bedroom plays the caption, and the savings book reads **Debt: PAID**. Half of the forest is plantation now. The MC is proud and worn out.

To build: a second `WorldState` skip (`SkipTime` only runs once), and a "paid" line in the book.

## 7. Day 6: The Offer (design only)

1. **Morning.** Debt-free. A wide shot of half the forest as rows of Lv.2 palms. The red pulse now shows in daylight on the wild plants along every plantation edge. Villagers are quieter, and some step away when the MC walks up.
2. **He's back.** The Distributor's truck arrives (Day 1's arrival, reused). Distributor: "Thirty days, and you did it in twenty-eight. I've never been wrong about a farmer."
3. **The pitch, with no pressure.** He opens his ledger case on the projected earnings for **Sawit Lv.3**:
   - Distributor: "Four times the yield per palm. I'm not asking you to decide today."
   - Distributor: "But remember the first night: one empty bottle and a debt bigger than this village. Look how far you came."
4. **The clue** (lens-glare line, `GameDataBuilder.Glaring`):
   - Distributor: "And don't mind the red in the leaves. That's just the land learning to keep up with you."
   - MC: "...I never told you about—"
   - Distributor: "Think it over. I'll be by the Fabricator."
   - The camera holds on his glasses.
5. **Hook:** whether to take the Lv.3 seed is the player's choice, later.

The clue only works because of Day 5's rule: the player has watched every conversation, so they know the MC never told him.

## 8. Testing

- Play `FixScene_Day5` on its own.
- Or tick `BedroomDirector.isTestingDay5` in HouseInterior; it fakes Days 1–4. The book then shows Day 4's skip without Day 4's sales, and the bag holds no Lv.2 bunches until the harvest.
- After a map change, rebuild in order: Day 2 → 3 → 4 → **Tools > FixScene > Build Day 5 Scene**.
