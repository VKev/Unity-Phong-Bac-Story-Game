# PhongBac GAME

Story-driven Unity prototype built in two weeks, with chapter-based progression, interactive choices, and lightweight cinematic moments to support the narrative beats.

## Highlights
- Three chapter scenes (`Assets/Scenes/Chapter 1-3.unity`) with timeline-driven panel events for fades, dialogue text, and camera moves.
- Choice and interaction system (`InteractionUITrigger.*`, `ItemSelectInteraction`) for branching UI prompts and gating.
- Player controller and camera flow powered by the Unity Input System (`PlayerController.inputactions`) and Cinemachine.
- Audio mixers and timeline handlers for in-world cues, phone/voice moments, and after-credit roll support.
- QuickOutline and TextMesh Pro for readable UI and object emphasis.

## Tech Stack
- Unity `6000.3.2f1`
- C# gameplay scripts
- Unity Input System (`.inputactions`)
- Cinemachine cameras
- TextMesh Pro for narrative/UI text
- Unity Audio: mixers (`LowVolume.mixer`, `Phone.mixer`) and clip-driven events
- QuickOutline for highlight effects

## How to Run
- Open the project in Unity `6000.3.2f1`.
- Load `Assets/Scenes/Chapter 1.unity` (start of the story) and press Play.
- Input bindings are defined in `Assets/PlayerController.inputactions`; adjust bindings there if needed.

## Showcase
![Story beat 1](ProfilerCaptures/storygame1.webp)
![Story beat 2](ProfilerCaptures/storygame2.webp)
![Story beat 3](ProfilerCaptures/storygame3.webp)
![Story beat 4](ProfilerCaptures/storygame4.webp)
![Story beat 5](ProfilerCaptures/storygame5.webp)
![Story beat 6](ProfilerCaptures/storygame6.webp)
![Story beat 7](ProfilerCaptures/storygame7.webp)
![Story beat 8](ProfilerCaptures/storygame8.webp)
![Story beat 9](ProfilerCaptures/storygame9.webp)

## Development Notes
- Built in ~two weeks; scoped for fast iteration on narrative flow and presentation.
- Timeline utilities (`PanelTimeline*`) encapsulate fades, audio triggers, camera locks/rotations, and display-time events to keep chapter scripting simple.
