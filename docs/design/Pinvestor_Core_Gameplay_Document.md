# Pinvestor – Core Gameplay Document

## High Concept

Pinvestor is a turn-based roguelike investment simulator where players build a board of startups and
generate revenue by launching customer balls that bounce across them.

The game blends:

- Board building
- Light physics interaction
- Rule-bending modifiers
- Investment risk management
- Run-based replayability

Core fantasy:

Build a broken investment engine before the market collapses.

## Design Philosophy

### Strategy vs Skill Ratio

Pinvestor targets:

- 70% Strategy
- 30% Spatial Expression

Strategy determines viability.
Spatial skill improves efficiency.

A perfect bounce cannot save a bad build.
A strong build does not require perfect aiming.

## Core Run Structure

Each run:

- Starts with fixed capital
- Has a predefined number of turns
- Has a target net worth
- Begins with a Run Theme (macro rule modifier)
- Has no permanent meta-progression (for now)

## Turn Structure

Each turn consists of four phases.

### 1. Offer Phase

Player is offered:

- 3 Startup Cards
- Shop access (after resolution phase)

Each startup contains:

- Industry Tag
- Health
- Revenue Per Hit (RPH)
- Operational Cost
- Skill (with defined trigger)

Player selects one startup and places it on the board.

### 2. Placement Phase

- Board is grid-based
- Startups occupy 1x1 tiles (for now)
- No overlap
- Adjacency matters for synergy
- No rotation or freeform placement

### 3. Launch Phase

Player launches one customer ball.

Ball behavior:

- Bounces across startups
- Each hit:
  - Generates RPH
  - Reduces startup health by 1
  - Triggers "On Hit" abilities
- Ball exits when it leaves board bounds or hit cap is reached

### 4. Resolution Phase

- Apply "On Turn End" effects
- Deduct operational costs
- Remove collapsed startups
- Check net worth
- Trigger Market News (if applicable)
- Enter Booster Shop phase

## Economy System

### Revenue

Revenue per turn:

Sum of `(Hits × Modified RPH)`

Modified by:

- Industry synergies
- Booster effects
- Run theme modifiers
- Market News events

### Health & Collapse

Each startup has Health.

Each hit applies `-1 health`.

At `0 health`:

- Startup collapses
- Startup is removed from board
- Investment is lost unless cashed out earlier

### Cashout System

Player may cash out a startup before collapse:

- Receives percentage of valuation
- Startup is removed from board

Cashout creates:

- Risk management
- Timing decisions
- Bubble-style economy

## Industry System (Abstract)

Industries are categorical tags.

They enable:

- Synergies
- Booster targeting
- Market News targeting
- Run theme interactions

Industries do not define power alone.
Build composition does.

## Synergy System

Adjacency-based bonuses.

Example structural rules:

- `2+` same industry adjacent -> RPH bonus
- `3` same industry cluster -> health bonus
- Optional penalties for mixed adjacency (theme-based)

Synergies encourage:

- Specialization
- Clustering
- Strategic layout

## Booster System (Balatro-Inspired)

### Core Philosophy

Boosters are:

- Persistent
- Limited by slot count
- Purchased in shop
- Always active once owned
- Rule-bending
- Build-defining

Boosters are not:

- One-time spells
- Temporary stat potions

They redefine how the run plays.

### Booster Flow

After each turn:

- Player enters Booster Shop
- Offered 3 boosters
- May reroll (costs money)
- May sell an existing booster
- Limited slots (e.g., 3 base, expandable)

### Booster Design Principle

Boosters must:

- Modify rules, not just numbers
- Enable new archetypes
- Stack multiplicatively
- Bend systems without breaking them

They can affect:

- Economy rules
- Health rules
- Collapse logic
- Cashout mechanics
- Placement rules
- Ball interaction rules

Power comes from combinations, not single cards.

## Run Theme System

Run Themes:

- Selected at run start
- Active entire run
- Known upfront
- Modify macro conditions

Examples (abstract):

- All startups start with 1 health
- Operational costs increased
- Global RPH bonus
- Adjacency rules altered

Run Themes define identity of the run.

## Market News System

Market News is separate from Boosters and Run Themes.

Market News is:

- Automatic
- Temporary
- Unpredictable
- Global or industry-specific
- 1–2 turn duration

### When It Triggers

Can trigger:

- Randomly at end of turn
- On fixed intervals
- Based on board conditions

### Purpose

Market News:

- Introduces volatility
- Forces adaptation
- Disrupts stable engines
- Reinforces investment fantasy

Examples (abstract):

- Industry surge
- Regulatory investigation
- Supply chain disruption
- Market crash

Boosters define your engine.
Market News stress-tests it.

## System Hierarchy

Clear separation:

| System | Player Control | Duration | Purpose |
|---|---|---|---|
| Run Theme | No (pre-run) | Entire run | Macro identity |
| Boosters | Yes | Persistent | Build engine |
| Market News | No | Temporary | Volatility |

## Loss & Win Conditions

Win:

- Reach target net worth before final turn ends

Loss:

- Final turn ends below target
- Optional future: bankruptcy

## Replayability Drivers

- Random startup offers
- Booster combinations
- Run themes
- Market News volatility
- Industry specialization builds
- Cashout timing strategies

Every run should feel structurally different due to rule-bending boosters.

## What This Document Defines

This document defines:

- Core loop
- Economic structure
- Rule hierarchy
- System separation
- Strategic identity

Company cards and specific boosters will be designed within this framework, not redefine it.

