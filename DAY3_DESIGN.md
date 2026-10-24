# Day 3: Room to Grow

Story outline mapping: Day 1 = Prologue + Chapter 0, Day 2 = Chapter 1, **Day 3 = Chapter 2 plus a hook into Chapter 3**. Day 4 picks up Chapter 3 (the first deforestation).

## 1. Scope and pacing

Day 3 plays all of Chapter 2. The MC expands their land (with a strength that comes from eating their own Lv.2 sawit), revenue goes up, and the villagers are happy. The day closes on the problem: the deadline is too close for the field to cover it. The last beat is the MC looking at the forest, which sets up Chapter 3 without building any tree cutting yet.

Kept small for the jam:
- One new scene (`Level01_Day3`), copied from Day 2's village, using the same assets.
- One scripted sequence (`LandSequence`) raises the new strip: a grayscale flashback, a hold-to-eat QTE, a world glitch and a mash-to-smash QTE. The new plots are ordinary `FarmPlot`s.
- New content: one item (Lv.2 bunch), one ripe palm model, one strip of land, and the dialogues below. Character poses (eat, pound, glasses) are procedural (`CutscenePoseVisual`), not clips.

Left for later: Sawit Lv.3 / further mutation (Chapter 4), the lender NPC, deforestation (Day 4), and villager criticism (Chapter 3).

## 2. Beat sheet

| # | Where | Beat | Player does | Objective |
|---|---|---|---|---|
| 0 | Bedroom | A "Two weeks later..." caption, then the savings book shows two weeks of payments and 16 days left | Read the book | Check your savings book |
| 1 | Village | Morning: all 9 field plots are heavy with ripe Lv.2 palms | Harvest | Harvest 4 Lv.2 bunches (more is fine) |
| 2 | ICAN crate | Sell | Sell | Sell the sawit at the ICAN crate |
| 3a | Empty grass | Cutscene: the camera turns to the empty grass north of the field; 4 hologram plots blink every 0.5 s while the MC wishes for more land | Watch | (cutscene) |
| 3b | Flashback | MC realises → 1 s black → grayscale. Two-shot of the MC and the Distributor by the Fabricator, then his close-up: he pushes up his glasses (glint) and says the harvest gives a power when eaten → 1 s black → colour. MC: "Let me try eating one." | Watch | (cutscene) |
| 3c | Anywhere | "[E] Eat Sawit" (needs a Lv.2 bunch, eats one). Hard cut to Rigel: startled hop, sprints over, shouts (shaking caps). Hold [E] 1 s to finish eating | Eat, hold | Eat a Lv.2 sawit bunch |
| 3d | | Blink ×2, full-screen world glitch 0.7 s, blink ×1. MC feels different; Rigel: "...." / "This guy man..." and backs off 3 steps | Watch | (cutscene) |
| 3e | Empty grass | Walk to the grass: the MC crouches and pounds it. Mash [E]: 4 presses per blow, 5 blows, the plots rise a stage per blow. The 5th is a two-fisted overhead slam, filmed from the front, leaving a crater for 3 s. Rigel backs off, waits 1 s, backs off, "I'll pretend nothing is happening..." and runs home (gone for the day) | Walk, mash | Go to the empty grass by the forest |
| 4 | Fabricator, new strip | One press makes 4 Lv.2 seeds; plant the new strip. Ratna and Ijah have optional congratulation talks (Rigel's is unreachable once he has run home) | Press, plant | Press Sawit Lv.2 seeds · Plant 4 seeds in the new field |
| 5 | Field edge, dusk | "That evening..." The savings book shows a big day but bad math. The camera turns to the forest past the strip | Read, watch | (cutscene) |
| 6 | Home | Walk home | Enter the house | Go home |

## 3. Dialogue script

**Day3_Morning** (MC, after the savings book)
- MC: "Two weeks of harvests... and it barely dents the number."
- MC: "Sixteen days. I need more land."

**Day3_LandWish** (MC, over the empty grass)
- MC: "If only I could add more land there..."
- MC: "But I don't have the strength or the time to build a new field." (HeavySigh)

**Day3_Flashback** (the Distributor, grayscale)
- Distributor: "Besides selling it, you could just eat the harvest yourself." (Nod)
- Distributor: "You'll get a certain power that can get you out of any trouble."

**Day3_TryEat**: MC: "Let me try eating one." / **Day3_NoBunch**: MC: "I sold them all... The field should still have some."

**Day3_RigelShout**: Rigel: "WHAT ARE YOU TRYING TO DO" (Surprised, shaking letters)

**Day3_Buff**: MC: "I feel... really like a different person right now." (Cheer) / Rigel: "...." / Rigel: "This guy man..." (Shake)

**Day3_RigelPretend**: Rigel: "I'll pretend nothing is happening..." (HeavySigh)

**Ratna_Day3**: "The whole village is talking about your sawit! My boy wants to be a farmer now." (WideSmile) / MC: "Thank you, Bu Ratna!"

**Ijah_Day3**: "Ah, the young one who saved the farm. Come eat with us sometime." (Nod) / MC: "I will, Nek Ijah. Once the debt is paid."

**Rigel_Day3**
- Rigel: "Fine. You won the bet."
- Rigel: "...this week." (TiltHead)
- MC: "Next week too."

**Day3_EveningMath** (MC, after the evening savings book)
- MC: "A good day. And the new strip cost me nothing but one sawit."
- MC: "Two weeks of harvests barely moved the debt. Sixteen more days won't do it either."
- MC: "The field is still too small." (HeavySigh)

(No exact numbers in the lines, because the player may harvest more than the 4 the checklist asks for. The savings book shows the real figures.)

**Day3_ForestHook** (MC, camera on the trees)
- MC: "All that land... just sitting there."
- MC: "With this strength... the trees wouldn't take long."

## 4. Money (all values live in `Day3SO`)

| | Value |
|---|---|
| Debt after Day 2 | ~46–47M (48.5M minus Day 2's sales) |
| Paid during the two-week skip | 9,000,000 |
| Days until due | 30 → 16 |
| Lv.2 bunch | 1,000,000 each, all 9 sold = 9,000,000 |
| New strip | free (smashed out of the ground) |
| Evening | ~28M left with 16 days to go; the last two weeks only paid 9M |

## 5. Greybox layout (Level01_Day3)

Coordinates are in Level01 world space. The MC's field is centred at (23, 5.5), 8.6 m square, so it spans x 18.7–27.3 and z 1.2–9.8. Day 2's forest edge runs x 16.5–28.5, z 14.5–26.

- **New strip**: plain grass at x 18.5–27.5, z 10.8–13.8, between the field and the trees. No fence, no sign; Darto is not in the Day 3 scene.
- **Plots**: 4 `FarmPlot`s in a row along z 12.3 (x 19.6–26.4, 2.25 m apart), hidden and sunk 0.18 m until the smash (`LandRiseVisual` raises them a stage per blow). `GhostPlots` are hologram copies for the blinking wish. The old field only accepts Lv.1 seeds on Day 3, so the new seeds go here.
- **Smash point** (23, 10.5) facing north; Rigel watches from 3 m west of it and runs home to (25, -13.8).
- **Flashback**: the Distributor copy (`Npc_DistributorFlashback`) stands 1.6 m east and 1.6 m south of the Fabricator facing west, the MC 1.9 m west of him.
- **Forest lane**: x 19–22 stays walkable, so the resin stumps are still reachable.
- **Night shot** (`CM_Day3Forest`): from (20.5, 5, 5.5) looking north over the planted strip into the dark pines.

## 6. What Day 3 leaves for Day 4

- The MC has decided, privately, that the trees are next.
- The MC now has an unnatural strength from eating the Lv.2 sawit (story only, no gameplay buff yet), and the forest hook points it at the trees.
- Rigel saw it all and chose to look away, ready for his suspicion in Chapter 4.
