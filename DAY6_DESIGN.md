# Day 6: The Cost of Success (Chapter 6)

Story outline mapping: Day 1 = Prologue + Chapter 0, Day 2 = Chapter 1, Day 3 = Chapter 2 by hand, Day 4 = the sawit
power and the first trees, Day 5 = Chapter 3 (half the forest goes), a two-week skip, then **Day 6 = Chapter 6: the
consequences begin**.

Premise: the new crops disrupt the ecosystem. The river turns murky, the soil dries, the neighbours' harvests drop. The
MC's plantation is still hugely profitable. The MC must choose: stop, or keep expanding. Expanding pays at once; the
damage comes slowly.

Status (2026-10-31): **built** in `FixScene_Day6` (see `.claude/rules/project-settings.md`, Day 6 section). Where the
build differs from the beats below:
- Rigel stands in front of **his own garden** behind his house (the map has no paddy); the garden is dried grey, and his
  talk says "garden". Beat 7 (`Day6_RigelAfter`) is folded into the end of `Rigel_Day6`.
- The river is a darker, murky tint (a copy of its material), not a clear brown; the bottle still fills with ordinary
  Water. No `MurkyWater` item.
- Taking the Lv.3 seeds hands over no item yet (Lv.3 is left for later); the choice is stored in `WorldState`.
- The night fade reads "That night..." after the choice ("That evening..." is the fade into the Fabricator scene).

## 1. Arc and rules

Success → the slip → doubt → the evidence → the bill → the choice.

The MC wakes debt-free and richer than anyone in the village. The Distributor's slip ("don't mind the red in the
leaves") cracks the MC's denial: from that line on, the MC is allowed to *see*. The afternoon is an investigation the
player walks through the map's own places (the old garden, the river, the neighbours' yards, Rigel's paddy). The evening
puts two numbers side by side: the profit (now, large) and the damage (slow, spreading). The day ends on the choice.

- **The red rule.** Through Day 5 and Day 6's morning, the MC never mentions the red to anyone, not even in a
  monologue; only villagers do. That is why the Distributor knowing about it is a clue. After his slip the MC may notice
  and think about the red, but never tells the villagers what he said: the slip stays the player's clue.
- **No option is labelled good or bad.** Both endings close on the MC's own line, not a moral.
- Target length: 12–15 minutes (the built day is about 5).

## 2. Beat sheet

| # | Where | Beat | Player does | Objective |
|---|---|---|---|---|
| 0 | Bedroom | *(built)* "Two weeks later..." The book: Debt PAID. "Paid. Every last Koin." / "Nobody can take this house now." | Read | Check your savings book |
| 1 | Plantation | *(built)* Wide shot over half the forest as palms; the edge flickers and tears behind the MC's back. "Look at it..." | Watch | (cutscene) |
| 2 | Plantation | **First profit of their own.** Harvest a row; the crate now pays into *savings*, not the debt. The biggest money popup yet. `Day6_Profit` | Harvest, sell | Harvest 6 Lv.2 bunches · Sell at the ICAN crate |
| 3 | River, by the crate | **First sign, brushed off.** The empty bottle fills with brown water (Murky Water: no use in the Fabricator). `Day6_MurkyWater` | Fill a bottle (optional) | — |
| 4 | Front door | *(built)* The Distributor walks up the path: congratulations, the Lv.3 pitch, the first-night reminder, **the slip** (glare), "...I never told you about—", "Think it over. I'll be by the Fabricator." Hold on his glasses. | Watch | (cutscene) |
| 5 | Front door | **Doubt.** Alone. `Day6_Doubt` | Watch | See what he meant (0/5) |
| 6a | Old garden, by the house | **The soil.** The Day 1 beds: cracked, grey, one palm drooping. [E] inspect. `Day6_OldGarden` | Inspect | ↳ the old garden |
| 6b | River bank (Dimas's digging spot) | **The river.** Dimas sits by the brown water, no shovel. `Dimas_Day6` | Talk | ↳ the river |
| 6c | Darto's yard | **The neighbours' harvest.** Half-empty sacks. `Darto_Day6` | Talk | ↳ Pak Darto |
| 6d | Ijah's house | **The flowers.** Her garden brittle and colourless; her Day 3 line turned. `Ijah_Day6` | Talk | ↳ Nek Ijah |
| 6e | Rigel's paddy | **The cause.** Rigel in a dried paddy, the ditch from the river brown and low. His Day 4 subtext finally said; he asks the MC to stop. `Rigel_Day6` | Talk | ↳ Rigel |
| 7 | Rigel's paddy | The MC's answer is deliberately not a decision. `Day6_RigelAfter` | Watch | (cutscene) |
| 8 | Ratna's house | **Prices (optional).** ICAN now pays the villagers less for their own crops: the MC's success pushes their prices down too. `Ratna_Day6` | Talk | — |
| 9 | Plantation edge, dusk | "That evening..." The evening book: savings jump. `Day6_EveningBook` | Read | (cutscene) |
| 10 | Fabricator | **The bill and the offer.** The Distributor, ledger open, answers what the MC saw without being asked. `Day6_Bill` | Talk | Decide at the Fabricator |
| 11 | Fabricator | **The choice.** Two options: **Take the Lv.3 seeds** (expand) / **Refuse** (stop). | Choose | — |
| 12E | Fabricator → night | **Expand.** He hands over a pouch of Lv.3 seeds, smiling without the glare. Night shot: the edge tears harder, the river darker. `Day6_Expand` | Watch | Go home |
| 12S | Fabricator → night | **Stop.** The glare, for one beat. He leaves by the path. Night shot: the edge still tears (what's done isn't undone). `Day6_Stop` | Watch | Go home |
| 13 | Home | The door → the end card (or Day 7, which reads the choice). | Enter | Go home |

## 3. Dialogue script

Built (unchanged): `Day6_Morning`, `Day6_Wide`, `Day6_Offer`, `Distributor_Day6`. New or replaced:

| Asset | Lines |
|---|---|
| `Day6_Profit` | MC: "No lender. Every Koin of this is mine." (Cheer) |
| `Day6_MurkyWater` | MC: "Brown?" / "...Silt from upstream. Rain must've come down the hills." |
| `Day6_Doubt` | MC: "The red... He knew." / "If he knew, what else is out there I haven't looked at?" |
| `Day6_OldGarden` | MC: "This was black mud a month ago." / "Now it crumbles like bread crust." (HeavySigh) |
| `Dimas_Day6` | Dimas: "The worms are gone. Even the red ones." (Sad) / "Mum says don't put your feet in any more. It smells like rust." |
| `Darto_Day6` | Darto: "Half of last year's. Same seed, same field, same lazy farmer." (Cheer) / "...The ground's thirsty. I water it, it drinks, it's thirsty again." (HeavySigh) |
| `Ijah_Day6` | Ijah: "No jamu this week, child. Nothing to make it from." / "Good things grow slowly. Bad things too, it seems. Slow, and then all at once." |
| `Rigel_Day6` | Rigel: "Come to see your handiwork?" (Shake) / MC: "Your paddy..." (Surprised) / Rigel: "Dry. The ditch comes off the river. The river comes off the forest. You do the sums." / "Ten years I asked the elders for that forest edge. For rice. Ten years they said no." / "I thought they were old fools. They said the trees hold the water for the whole valley." (HeavySigh) / "Turns out they weren't fools. And you didn't ask anyone." / MC: "I paid off a debt bigger than this village, Rigel." / Rigel: "And the village is paying off yours now." (Shake) / "Stop planting. Let what's left of the forest stand. That's all I'm asking." |
| `Day6_RigelAfter` | MC: "I... need to think." / Rigel: "Think fast. The river won't." |
| `Ratna_Day6` | Ratna: "ICAN pays us less for our cassava now. Too much on the market, they said." / "Funny. It was always enough before your palms." |
| `Day6_EveningBook` | MC: "More money in one day than the whole village makes in a season." |
| `Day6_Bill` | Distributor (glaring): "Rivers silt up. Soil tires. It all comes back, slowly." / "Money doesn't come back. You take it when it's offered." / "Lv.3 tonight, and the rest of the forest by the end of the month. The damage takes years. The profit takes a week." |
| `Day6_Expand` | Distributor: "I've never been wrong about a farmer." (Cheer) / MC (night): "Rigel's rice will come back. Everything comes back." / "...It has to." |
| `Day6_Stop` | Distributor (glaring): "Pity." / "Farmers who stop tend to owe again. I'll be around when you do." / MC (night): "Half the forest is still half the forest." / "Tomorrow I find out how to give some of it back." |

`Day6_Night` (built) is replaced by the two endings.

## 4. Why the choice is fair

- **Expand** is genuinely tempting: the evening book just showed the profit, the Distributor offers 4× yield, and every
  piece of damage the player saw is "slow" and arguably reversible.
- **Stop** costs something visible: the Distributor's parting threat, and the edge that keeps tearing anyway. Stopping
  does not undo Days 4–5.
- Continuity it leans on: Ijah's "Good things grow slowly" (Day 3), Rigel's unspoken wish for the forest edge (Day 4's
  bridge talk: "not for rice", "that ground was supposed to be—"), Dimas's red worm (Day 5), Ratna's "ICAN's prices seem
  too good" (Day 3).

## 5. Mapping to existing systems (for the build)

- Beats 0, 1, 4: built (`Day6Director` Morning/Arrival/Offer, `Day6CameraVisual`, glare via `GameDataBuilder.Glaring`).
- Beat 2: `HarvestFruit` Lv.2 ×6 + `SellItem` objectives (Day 5's `D5_HarvestLv2` / `D5_Sell` pattern).
  `WorldState.Earn` only lowers the debt and counts `earnedToday`: with the debt at 0 it needs to add to savings.
- Beats 3, 6b: the river's murk = a Day 6 tint on a scene-local copy of `Art/Materials/RiverWater.mat`; the river's
  `ResourceNode` gives a new `MurkyWater` item.
- Beats 5–6: one Story objective ×5, villagers through `Npc.SetOverrideDialogue` and a "still to visit" set (Day 5's
  `worriedNpcSet`). The old-garden inspect: a small interactable on the Day 1 plots. Rigel has been switched off since
  Day 4 (`FixSceneDay5Builder.StripDay4`); Day 6 turns him back on in a dried paddy near `Rival_Property_House` (needs
  dressing). Darto needs a spot in his yard.
- Beats 10–11: a two-option panel modelled on `SeedChoiceUI` (A/D + confirm); the choice stored in `WorldState`
  (e.g. `ChooseLv3(bool)` / `HasTakenLv3()`, reset with the other flags).
- Beat 12: on Expand, raise `MutationSpreadVisual`'s tear values (`tearChance`, `maxJolt`, `maxStretch`); `NightVisual`
  and `NightCurfew` unchanged.

## 6. Left for later

Day 7 reacting to the choice; Lv.3 seeds as a real crop; giving land back (replanting); the lender NPC.

## 7. Gotchas (carried over from Days 3–5)

- **Teleport while upright.** Turn the MC upright *before* `Player.Teleport`: re-enabling the CharacterController
  head-down drops them through the ground.
- **`GrayboxBuild.Part` takes a LOCAL position.** The `Gameplay` root isn't at the world origin, so set world positions
  after creating a part.
- **Cut meshes keep the model's pivot.** `FixSceneDay3Builder.CutMesh` pieces keep the whole model's vertex list and
  pivot, so their bounds are the model's. Store the real position separately.
- **Pond and river interactables steal [E]** within about 2 m: keep "[E] here" spots away from water.
- **NavMesh stand points.** An NPC's agent re-seats it on the nearest NavMesh polygon after a `CutsceneActor.MoveTo`;
  put stand points on the NavMesh (the door step and the Fabricator's carve are off it).
- **Every `Day2SO` copy** (`Day2`, `Day2_CozyFarm`) needs any new book field: the day scenes read the CozyFarm copy.
- **Open another scene before rebuilding.** Builders call `SaveCurrentModifiedScenesIfUserWantsTo`; a dirty open scene
  blocks the editor with a modal dialog.
- **MCP flakiness.** Unity MCP calls often fail right after a recompile: retry. `eval` defaults to a 5 s timeout; pass
  `timeout` for builds. Play mode freezes while the editor is unfocused (`editor_focus`, `runInBackground`).
- **Same-frame state changes.** A dialogue or popup closing returns `GameState.Playing` only on the next frame.
- **Commits.** Use `git commit -F <file>`, and leave `ProjectSettings/TimeManager.asset` out (the editor rewrites it).
