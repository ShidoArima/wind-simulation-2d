# wind-simulation-2d
Simple wind simulation with compute shaders for Unity. Using it in my current game so decided to extract it and make open source, for anyone to look or use.

https://github.com/user-attachments/assets/fa65e505-f823-46bc-b7bb-9467a0630872

It consists of:

- WindCompute
- GrassCompute
- WireCompute

**WindEffectorRenderer**

Used to simulate and place result in WindTexture
If any effectors specified they will be computed to DisplacementTexture and additionally added to WindTexture

**GrassComputeRenderer**

Used to render indirect grass blades or grass sprites, it requires SplineCache (Scriptable Object containing cached spline points for grass placement) and WindTexture for movement simulation.

**WireComputeRenderer**

Used to create rope/wire like objects which could react to the wind or effectors, depends on the texture provided. It's is not real collision simulation. It only uses wind as a displacement for the force, and shouldn't be used for anything but some visual sugar really.

SampleScene contains all of them as an example.

**To Do:**
- Add gradient and width curve to wire;
- Add better displacement, currently it's in viewport space which make it difficult to add damping for displacement;
- Adding damping should help also with wire collisions since right now it works only on slow objects
