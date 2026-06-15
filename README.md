# FarmTracker

A standalone [ExileCore2](https://github.com/) overlay plugin for Path of Exile 2 that shows your live **farm profit per map and per hour**.

## What it does

FarmTracker tracks the value of loot you pick up (priced via NinjaPricer) and subtracts a user-entered map cost to show profit in real time:

```
profit = income (value of loot picked up) − map cost
```

It provides two views:

- An **auto-detected per-map view** that resets when you enter a new map and shows income/cost/profit for the current map.
- A **session view** with running totals plus **per-hour rates** (Maps/hr, Profit/hr) across all maps in the session.

## Requirements

- **ExileCore2** (PoE2 overlay framework).
- **NinjaPricer** must be loaded. NinjaPricer is required for valuation: without it, income is not counted and the window shows a banner telling you to load it.

## Install

1. Clone or copy this repo into your ExileCore2 plugins folder as `Plugins/Source/FarmTracker`.
2. Restart ExileCore2.

The plugin builds against the ExileCore2 assemblies. The reference DLLs are not committed — they live locally in a gitignored `lib/` folder.

## Usage

1. Enable the plugin in the ExileCore2 settings.
2. Bind the **toggle-window hotkey** so you can show/hide the window in-game.
3. Set **Map cost (ex)** to your typical per-map investment (map + scarabs/etc.).
4. Run maps. Income accrues live as you pick up loot; profit is shown as `income − map cost`.
5. Use **Start / Stop / Reset** to control the session.
6. You can edit the **current map's cost** directly in the window; the default cost from settings applies to new maps.

## Settings

- **Enable** — turn the plugin on/off.
- **Show window** (+ **Toggle window hotkey**) — open/close the farm-tracker window, bind a hotkey to toggle it.
- **Map cost (ex)** — default cost subtracted from each detected map.
- **Min item value to count (ex)** — ignore pickups below this value (e.g. scrolls).
- **Auto-start session on first map** — begin a session automatically when you enter your first map.
- **Debug logging** — log area changes and diagnostics (area classification, sample inventory id).

## Notes

- **Session and run history are in-memory only.** They are not persisted across ExileCore2 restarts — this is intentional for v1 scope.
- **Stop** ends the session: it freezes the rates and stops further tracking until you press **Start** or **Reset**.
- Income is measured by a per-item high-water mark, so it never double-counts loot you dump to stash and re-pull. The tradeoff: if you spend part of a stack and later pick up more of the same stack, only the amount above your previous peak is counted — income is biased slightly downward rather than risk over-counting.
- **Min item value to count (ex)** filters on an item's whole-stack value, not its per-unit value.

## Manual test checklist (Windows / in-game)

- [ ] Enable Debug logging, enter a few maps and the hideout → `[area]` lines classify map vs town/hideout correctly.
- [ ] Pick up currency/items in a map → session & current-map income rise live; profit = income − map cost.
- [ ] Dump loot to stash mid/after map → income does NOT drop (no double counting on re-pull).
- [ ] Change zones repeatedly → income does NOT jump by your whole inventory value (entity ids are stable; if it does, the id key needs changing).
- [ ] Complete maps → history rows appear; Maps/hr and Profit/hr look right; Stop freezes rates; Reset clears.
- [ ] Edit "Map cost" for the current map → profit updates; default applies to new maps.
- [ ] Without NinjaPricer → banner shows, no income counted.
