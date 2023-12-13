# Screen space shadows

Implementation of screen space shadows in OpenGL. Final project for the Computer Graphics unit of
the Bachelor's in Videogames at Universidade Lusófona.

Screen space shadows (also known as contact shadows) is a technique used to add extra shadow detail,
particularly around objects that are very close to each other. It complements the more general
shadowmap technique.

For each light, a ray is cast (in screen space) from the pixel to be drawn to the light's position.
At each step, the ray's current depth is compared to the value written on the depth buffer for that
position in screen space. If the ray's depth is larger (occluded from the camera), that pixel should
be in shadow.

## References

- [Inside Bend: Screen Space Shadows](https://www.bendstudio.com/blog/inside-bend-screen-space-shadows/)
- [Contact Shadows | Unreal Engine 4.27 Documentation](https://docs.unrealengine.com/4.27/en-US/BuildingWorlds/LightingAndShadows/ContactShadows/) 
- [ExileCon Dev Talk - Evolving Path of Exile's Renderer](https://www.youtube.com/watch?v=whyJzrVEgVc)
- [Screen space shadows](https://panoskarabelas.com/posts/screen_space_shadows/), a blog post by
[Panos Karabelas](https://panoskarabelas.com/)

> TODO: adapt rest of readme as needed

## Installation

It is possible nothing additional is needed, NuGet is used to pull the OpenTK project. In case something is needed:

## Usage

* Clone and use directly. Just need to change the function that is passed to the OpenTKApp.Run method to change the behaviour function.

## Licenses

Engine code developed by [Diogo de Andrade][DAndrade] and [Nuno Fachada][NFachada]; it is made available under the [Mozilla Public License 2.0][MPLv2].

Code uses:

* [OpenTK], licensed under the [MIT] license
* Grass texture by Charlotte Baglioni/Dario Barresi, under the [CC0] license
* Cubemap by [Emil Persson], under the [CC-BY3.0] license

All the text and documentation (i.e., non-code files) are made available under
the [Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International
License][CC BY-NC-SA 4.0].

[MPLv2]:https://opensource.org/licenses/MPL-2.0
[CC BY-NC-SA 4.0]:https://creativecommons.org/licenses/by-nc-sa/4.0/
[CC-BY3.0]:https://creativecommons.org/licenses/by/3.0/
[CC0]:https://creativecommons.org/publicdomain/zero/1.0/
[Ap2]:https://opensource.org/licenses/Apache-2.0
[OpenTK]:https://opentk.net/
[MIT]:https://opensource.org/license/mit/
[DAndrade]:https://github.com/DiogoDeAndrade
[NFachada]:https://github.com/fakenmc
[Emil Persson]:http://www.humus.name/
