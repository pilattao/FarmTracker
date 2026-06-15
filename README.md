# FarmTracker

A standalone [ExileCore2](https://github.com/) overlay plugin for Path of Exile 2 that shows your live **farm profit per map and per hour**.

## What it does

FarmTracker tracks the value of loot you pick up (priced via NinjaPricer) and shows profit in real time:

```
profit = income (value of loot picked up) − map cost − in-map spend
```

The session is **perpetual** — it starts when you enable the plugin and runs continuously. There is no Start/Stop. The only control is **Reset**, which archives the current session to disk and begins a fresh one.

The overlay is a minimal 2-line HUD that expands into a **live loot pickup log** — each pickup appears as its own row (not summed into stacks), showing a colored dot (by item category/rarity), item name, count, and value.

Key features:

- **In-map AND out-of-map time tracking** — the HUD shows in-map time and total time separately, and computes **effective profit/hour** (based only on in-map time) alongside the raw profit/hour.
- **In-map currency consumption detected as map cost** — if you pick up currency in a map and then spend it there (e.g. an Exalted Orb used on a strongbox), FarmTracker detects the in-map stack decrease and subtracts it from profit as a "Spent" row. Out-of-map decreases (stash dumps) are ignored, so dumping and re-pulling loot never inflates or deflates income.
- **Full on-disk persistence** — every session (summary, per-map data, and full loot log) is written to JSON files under the plugin's config directory (`sessions/` subfolder). Sessions survive ExileCore2 restarts. Past sessions are pruned to a configurable cap (default 50); history can be inspected as JSON files on disk.

## Requirements

- **ExileCore2** (PoE2 overlay framework).
- **NinjaPricer** must be loaded. NinjaPricer is required for valuation: without it, income is not counted and the window shows a banner telling you to load it.

## Install

1. Clone or copy this repo into your ExileCore2 plugins folder as `Plugins/Source/FarmTracker`.
2. Restart ExileCore2.

The plugin builds against the ExileCore2 assemblies. The reference DLLs are not committed — they live locally in a gitignored `lib/` folder.

## Usage

1. Enable the plugin in the ExileCore2 settings.
2. Bind the **Toggle window hotkey** so you can show/hide the overlay in-game.
3. Set **Map cost (ex)** to your typical per-map investment (map + scarabs/etc.).
4. Run maps. The session starts automatically. Income accrues live as you pick up loot; each pickup appears in the expanded loot log. Profit is shown as `income − map cost − in-map spend`.
5. Use **Reset** to archive the current session to disk and start a fresh one. There is no Start/Stop — the session is always running while the plugin is enabled.

## Settings

- **Enable** — turn the plugin on/off.
- **Show window** (+ **Toggle window hotkey**) — open/close the overlay, bind a hotkey to toggle it.
- **Expanded by default** — show the loot log expanded when the overlay opens.
- **Map cost (ex)** — base cost subtracted from each detected map (map + scarabs/etc.).
- **Min item value to count (ex)** — ignore picked-up items below this value in income and the loot log.
- **Max stored sessions** — how many past sessions to keep on disk; older ones are pruned automatically (default 50).
- **Debug logging** — log area changes and diagnostics.

## Notes

- **Sessions are persisted to disk.** Each session is written as JSON under the plugin's config directory (`sessions/` subfolder) and survives ExileCore2 restarts. Only the last N sessions are kept (see **Max stored sessions**).
- Income is measured by a per-item high-water mark, so it never double-counts loot you dump to stash and re-pull. The tradeoff: if you spend part of a stack and later pick up more of the same stack, only the amount above your previous peak is counted — income is biased slightly downward rather than risk over-counting.
- **Min item value to count (ex)** filters on an item's whole-stack value, not its per-unit value.
- **In-map currency consumption is detected as map cost.** A stack decrease or disappearance observed while you are inside a map is treated as "spent on the map" and subtracted from profit. A stack decrease observed outside a map (e.g. in hideout after a stash dump) is treated as a stash transfer and is ignored — it does not create a Spent row.

## Manual test checklist (Windows / in-game)
- [ ] Enable Debug logging; enter maps and hideout → `[area]` lines classify map vs town/hideout.
- [ ] Pick up currency in a map → loot rows appear (dot + name + value); session & current-map income rise.
- [ ] Use a picked currency on the map (e.g. Exalt on a strongbox) → a "−" Spent row appears, current map cost rises, profit drops by its value (not inflated).
- [ ] Dump loot to stash in hideout → no Spent row, profit unchanged (no double counting on re-pull).
- [ ] Change zones repeatedly → income does NOT jump by your whole inventory value (entity ids stable).
- [ ] Effective ex/hr (in-map) reads higher than raw ex/hr when you spend time in town; in/total timers look right.
- [ ] Reset → current session archives to disk (a JSON appears under the plugin config dir), a fresh session starts, and if you reset while on a map the current map is still tracked.
- [ ] Restart ExileCore2 → the prior session file is present and well-formed; only the last N (Max stored sessions) are kept.
- [ ] Without NinjaPricer → banner shows, no income counted.
