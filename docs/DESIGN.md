# Patina

The field record of a city's outdoor art. Cool limestone ground, iron ink, and one committed color: the verdigris that bronze turns when it weathers. The interface reads like a conservator's condition report and an object label: precise, typed, measured. Nothing decorates; every mark records something about an artwork.

## Principles

- Record, don't decorate. Every element states a fact about an artwork or asks for one.
- One loud thing per screen. Usually the grade, or the photograph.
- Condition is never color alone. Every grade carries a numeral, a word and a count of filled patches.

## Color palette

Cool mineral neutrals carry the interface. Verdigris is the single primary. Bronze and slate are quiet supports. Rust is reserved for critical condition and destructive actions.

| Role | Name | Value | Use |
| --- | --- | --- | --- |
| Primary | Verdigris | #2F6F62 | Primary action per view, selection, focus ring |
| Secondary | Bronze | #8A5A2B | Secondary actions, material tags |
| Tertiary | Slate | #46617A | Map accents, treatment schedule |
| Background | Limestone | #EEF0EC | Window and page background |
| Surface | Paper | #F8F9F6 | Cards, sheets, dialogs |
| Surface variant | Plaster | #E1E5DF | Inputs, chips, table stripes |
| Text | Iron | #1C2321 | Body and headings |
| Text muted | Lead | #56605C | Captions, helper text |
| Border | Mortar | #C5CCC7 | Dividers and input outlines |
| Error | Rust | #B3261E | Critical condition, destructive actions, validation |

Dark mode is the same collection at night: wet stone, never pure black, verdigris lifted to stay legible.

## Typography

- Headings: IBM Plex Sans Condensed
- Body: IBM Plex Sans
- Data: IBM Plex Mono

| Style | Font | Size | Line height | Weight | Letter spacing |
| --- | --- | --- | --- | --- | --- |
| Display | IBM Plex Sans Condensed | 56px | 60px | 600 | -0.01em |
| H1 | IBM Plex Sans Condensed | 36px | 40px | 600 | 0 |
| H2 | IBM Plex Sans | 22px | 28px | 600 | 0 |
| Title | IBM Plex Sans | 17px | 24px | 600 | 0 |
| Body | IBM Plex Sans | 15px | 22px | 400 | 0 |
| Small | IBM Plex Sans | 13px | 18px | 400 | 0 |
| Label | IBM Plex Sans | 12px | 16px | 500 | 0.06em |
| Mono | IBM Plex Mono | 13px | 18px | 400 | 0 |

Headings stay in sentence case. Accession numbers, dates and measurements are set in mono, the way they are typed on an object label.

## Spacing

- xs: 4px
- sm: 8px
- md: 12px
- lg: 16px
- xl: 24px
- 2xl: 32px
- 3xl: 48px

## Corner radius

- sm: 6px (chips, inputs)
- md: 12px (cards, buttons)
- lg: 24px (sheets)
- full: 999px (pills)

## Motion

Exact and quiet. 150 ms for feedback, 200 ms for most transitions, 280 ms for entrances, on a single smooth ease-out curve. Motion stops entirely when the system asks for reduced motion.

## Voice

Plain and specific, from the field crew's side. "Start survey", "Add finding", "Mark done". Errors say what happened and what to do next.
