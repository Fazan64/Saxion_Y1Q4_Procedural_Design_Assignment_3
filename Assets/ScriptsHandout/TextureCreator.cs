using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Handout {
	public enum PatternType {Noise, Gradient, CheckerBoard, Circle, Custom, Mandelbrot}

	public class TextureCreator : MonoBehaviour {
		public PatternType patternType;
		public float HorizontalMultiplier=1;
		public float VerticalMultiplier=1;
		public float HorizontalOffset=0;
		public float VerticalOffset=0;
		public Color color1;
		public Color color2;

		const int SIZE=512;
		Renderer render;
		bool initialized=false;

		// Used for the Mandelbrot fractal:
		const int maxIterations = 100;
		const float escapeLengthSquared = 4;

		void Start () {
			render = GetComponent<Renderer> ();

			// Create a new texture, with width and height equal to SIZE:
			Texture2D texture = new Texture2D (SIZE, SIZE, TextureFormat.RGBA32, false);

			// Assign it as main texture (=color data texture) to the material of the renderer:
			render.material.mainTexture = texture;

			// Fill in the color data of the texture:
			CreateTexture (texture);
			initialized = true;
		}

		void OnValidate() {
			if (initialized) {
				CreateTexture ((Texture2D)render.material.mainTexture);
			}
		}

		void CreateTexture(Texture2D tex) {
			Color[] cols = tex.GetPixels ();

			for (int x = 0; x < tex.width; x++) {
				for (int y = 0; y < tex.height; y++) {
					float u = x * 1f / tex.width;
					float v = y * 1f / tex.height;
					int index = x + y * tex.width;

					switch (patternType) {
					case PatternType.Noise:
						// TODO Make tileable perlin noise. Maybe sample along two perpendicular circles in 4D
						float lightness = Mathf.PerlinNoise((u - 0.5f) * HorizontalMultiplier, (v - 0.5f) * VerticalMultiplier);
						// black / white:
						//cols [index] = lightness * Color.white;
						// linear interpolation between two given colors:
						cols[index] = lightness * color1 + (1-lightness) * color2;
						break;
					case PatternType.Gradient:
						// from black to white:
						//cols [index] = u * Color.white;
						// linear interpolation between two given colors:
						//cols [index] = u * color1 + (1-u) * color2;
						// using the ColorGradient method below:
						cols [index] = ColorGradient (360f * u);
						break;
					case PatternType.CheckerBoard:
						// TODO: Create a two colored checkerboard here
						cols [index] = 
							color1 * (Mathf.Floor (u * HorizontalMultiplier) / HorizontalMultiplier) +
							color2 * (Mathf.Floor (v * VerticalMultiplier) / VerticalMultiplier);
						break;
					case PatternType.Circle:
						// TODO: create an actual circle here
						cols [index] = color1 * (1 - 2 * Mathf.Abs (u - 0.5f) - 2 * Mathf.Abs (v - 0.5f));
						break;
					case PatternType.Mandelbrot:
						cols [index] = Mandelbrot (
							(u - 0.5f) * HorizontalMultiplier + HorizontalOffset,
							(v - 0.5f) * VerticalMultiplier + VerticalOffset
						);
						break;
					case PatternType.Custom:
						// TODO: experiment with different patterns here
						// A real chessboard:
						cols [index] = (Mathf.Floor (u * HorizontalMultiplier) + Mathf.Floor (v * VerticalMultiplier)) % 2 == 1 ?
							color1 : color2;
						break;
					}
				}
			}
			tex.SetPixels (cols);
			tex.Apply ();
		}


		// Returns a color that changes smoothly as degrees increases from 0 to 360.
		// (If done correctly, degrees and degrees+360 give the same color.)
		Color ColorGradient(float degrees) {
			// TODO: insert a better gradient here
			/**/
			return new Color (
				Mathf.PingPong (degrees / 180 + 1, 1),
				Mathf.PingPong (degrees / 60, 1),
				Mathf.PingPong (degrees / 90, 1)
			);
			/**
			// TODO: improve this color gradient (follow the definition of *hue*), and possibly simplify the formula
			if (degrees < 60) {
				// increase red:
				return new Color (degrees / 60f, 0, 0);
			} else if (degrees < 120) {
				// decrease red, increase green:
				return new Color (1 - (degrees - 60) / 60f, (degrees - 60) / 60f, 0);
			} else if (degrees < 180) {
				// decrease green:
				return new Color (0, 1 - (degrees - 120) / 60f, 0);
			} else {
				// return  black:
				return new Color (0, 0, 0);
			}
			/**/
		}

		Color Mandelbrot(float cReal, float cImaginary) {
			int iteration = 0;

			float zReal = 0;
			float zImaginary = 0;

			while (zReal * zReal + zImaginary * zImaginary < escapeLengthSquared && iteration < maxIterations) {
				// Use Mandelbrot's magic iteration formula: z := z^2 + c 
				// (using complex number multiplication & addition - 
				//   see https://mathbitsnotebook.com/Algebra2/ComplexNumbers/CPArithmeticASM.html)
				float newZr = zReal*zReal - zImaginary*zImaginary + cReal;
				zImaginary = 2 * zReal * zImaginary + cImaginary;
				zReal = newZr;
				iteration++;
			}
			// Return a color value based on the number of iterations that were needed to "escape the circle":
			return ColorGradient (360f*iteration/maxIterations);
		}
	}
}