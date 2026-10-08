Unity Version
Unity 6.4 (6000.4.9f1)

How to Run the Scene
Open the project in Unity 6000.4.9f1.

Navigate to your scenes folder and open SampleScene 1.

Press the Play button at the top of the editor.
(Note - I have used a Play Mode Save package So Any changes made to serialized fields in the Inspector during Play Mode will be saved)

Controls
Drag & Drop: Click/Tap and hold the movable container block at the bottom of the screen to move it using physics-based velocity.

Catching Sand: Drag the block directly underneath the upper static container. Release the mouse or tap to snap the block to the grid. If placed close enough to the upper container, the gate will open and sand will dynamically arc into your block.

Interrupt Flow: Click and drag the active container away at any time to instantly seal the upper gate and stop the flow of sand.

Sand Implementation Overview
This game achieves fluid, hundreds-of-particles sand physics while maintaining high mobile performance by utilizing a hybrid system:

Cellular Automata Physics: Rather than using heavy Unity Rigidbody2D particles, the sand operates on a pixel grid (Texture2D). The SandSimulations script evaluates an integer array (Empty = 0, Sand = 1, Wall = 2) several times per frame. Sand applies rules to fall straight down or slide diagonally if blocked, stacking naturally.

Framerate Independence: A time accumulator decouples the grid physics from the rendering framerate. This guarantees the sand falls at the exact same rate on a 144hz PC monitor as it does on a battery-saving 30 FPS mobile phone.

The "Fill" Illusion: To avoid the massive performance cost of dragging a bucket full of active grid pixels, the movable block does not physically hold the sand. When a sand pixel hits the designated target area, it is destroyed and added to a caught percentage. This percentage scrubs the normalized time of a sprite-sheet Animator, creating the seamless visual illusion of the block filling up.

Refund System: If sand is in mid-air when the player pulls the block away, any spilled sand is immediately recycled and teleported back into the upper static container, ensuring the level can always be completed 100%.
