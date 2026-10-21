# Day 3: Room to Grow

Story outline mapping: Day 1 = Prologue + Chapter 0, Day 2 = Chapter 1, **Day 3 = Chapter 2 plus a hook into Chapter 3**. Day 4 picks up Chapter 3 (the first deforestation).

## 1. Scope and pacing

Day 3 plays all of Chapter 2. The MC expands their land within normal limits, revenue goes up, and the villagers are happy. The day closes on the problem: the deadline is too close for the field to cover it. The last beat is the MC looking at the forest, which sets up Chapter 3 without building any tree cutting yet.

Kept small for the jam:
- One new scene (`Level01_Day3`), copied from Day 2's village, using the same assets.
- No new mechanic code: the land purchase is Darto's dialogue ending, and the new plots are ordinary `FarmPlot`s.
- New content: one item (Lv.2 bunch), one ripe palm model, one sign, one strip of land, and 7 short dialogues.

Left for later: Sawit Lv.3 / further mutation (Chapter 4), the lender NPC, deforestation (Day 4), and villager criticism (Chapter 3).

## 2. Beat sheet

| # | Where | Beat | Player does | Objective |
|---|---|---|---|---|
| 0 | Bedroom | A "Two weeks later..." caption, then the savings book shows two weeks of payments and 16 days left | Read the book | Check your savings book |
| 1 | Village | Morning: all 9 field plots are heavy with ripe Lv.2 palms | Harvest | Harvest 4 Lv.2 bunches (more is fine) |
| 2 | ICAN crate | Sell; MC: "The resin palms sell for even more" | Sell | Sell the sawit at the ICAN crate |
| 3 | For Sale sign | Darto sells his father's strip by the trees. Paying makes the fence and 4 plots appear | Talk to Darto | Talk to Darto by the For Sale sign |
| 4 | Fabricator, new strip | One press makes 4 Lv.2 seeds; plant the new strip. Ratna, Ijah and Rigel have optional congratulation talks | Press, plant | Press Sawit Lv.2 seeds · Plant 4 seeds in the new field |
| 5 | Field edge, dusk | "That evening..." The savings book shows a big day but bad math. The camera turns to the forest past the strip | Read, watch | (cutscene) |
| 6 | Home | Walk home | Enter the house | Go home |

## 3. Dialogue script

**Day3_Morning** (MC, after the savings book)
- MC: "Two weeks of harvests... and it barely dents the number."
- MC: "Sixteen days. I need more land."

**Day3_Sold** (MC, after the first sale)
- MC: "The resin palms sell for even more than the old ones." (Surprised)
- MC: "If I had more field, I could double it." (Cheer)

**Darto_SellsLand** (at the sign)
- Darto: "So you're the one feeding half of ICAN's trucks!" (Cheer)
- Darto: "That strip by the trees was my father's. My knees can't work it anymore."
- MC: "How much, Pak Darto?"
- Darto: "For you? Four million, and you keep the village proud." (Nod)
- MC: "Deal."
- Darto: "Just stop where the old trees start. That forest was here before any of us."

**Ratna_Day3**: "The whole village is talking about your sawit! My boy wants to be a farmer now." (WideSmile) / MC: "Thank you, Bu Ratna!"

**Ijah_Day3**: "Ah, the young one who saved the farm. Come eat with us sometime." (Nod) / MC: "I will, Nek Ijah. Once the debt is paid."

**Rigel_Day3**
- Rigel: "Fine. You won the bet."
- Rigel: "...this week." (TiltHead)
- MC: "Next week too."

**Day3_EveningMath** (MC, after the evening savings book)
- MC: "A good day... and half of it went straight back into the land."
- MC: "Two weeks of harvests barely moved the debt. Sixteen more days won't do it either."
- MC: "The field is too small. Even with Darto's strip." (HeavySigh)

(No exact numbers in the lines, because the player may harvest more than the 4 the checklist asks for. The savings book shows the real figures.)

**Day3_ForestHook** (MC, camera on the trees)
- MC: "All that land... just sitting there."
- MC: "Darto said stop at the trees. But the trees don't pay debts."

## 4. Money (all values live in `Day3SO`)

| | Value |
|---|---|
| Debt after Day 2 | ~46–47M (48.5M minus Day 2's sales) |
| Paid during the two-week skip | 9,000,000 |
| Days until due | 30 → 16 |
| Lv.2 bunch | 1,000,000 each, all 9 sold = 9,000,000 |
| Darto's land | 4,000,000, paid out of today's sales |
| Evening | ~32M left with 16 days to go; the last two weeks only paid 9M |

## 5. Greybox layout (Level01_Day3)

Coordinates are in Level01 world space. The MC's field is centred at (23, 5.5), 8.6 m square, so it spans x 18.7–27.3 and z 1.2–9.8. Day 2's forest edge runs x 16.5–28.5, z 14.5–26.

- **Darto's strip**: untilled soil at about x 18.5–27.5, z 10.8–13.8, squeezed between the field and the trees. Fence spans run along the west side and the north side. The north fence is the line Darto asks the MC not to cross, with the forest right behind it. The south side stays open toward the field.
- **Plots**: 4 `FarmPlot`s in a row along z 12.3 (x 19.6–26.4, 2.25 m apart), hidden until the purchase. The old field only accepts Lv.1 seeds on Day 3, so the new seeds go here.
- **For Sale sign**: a post and board reading "DIJUAL / For Sale" at (20.6, 10.4) on the strip's front edge. Darto stands beside it at (19.4, 10.2).
- **Forest lane**: x 19–22 stays walkable, so the resin stumps are still reachable.
- **Night shot** (`CM_Day3Forest`): from (20.5, 5, 5.5) looking north over the planted strip into the dark pines.

## 6. What Day 3 leaves for Day 4

- The MC has decided, privately, that the trees are next.
- Darto's "stop where the old trees start" becomes the promise the MC breaks in Chapter 3.
- Rigel's "...this week" leaves the rivalry open, ready for his suspicion in Chapter 4.
