using UnityEngine;
using UnityEngine.UI;

namespace mitaywalle.UICircleSegmentedNamespace
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UICircleSegmented : MaskableGraphic
	{
        #region Vars
		private static Vector2[] _positionsTemp = new Vector2[4];
		private static Vector2[] _uvTemp = new Vector2[4];

		private static Vector2[] _defaultUV = new Vector2[4]
		{
			new Vector2(0, 1),
			new Vector2(1, 1),
			new Vector2(1, 0),
			new Vector2(0, 0),
		};

		public Sprite overrideSprite;
		[SerializeField] private float m_thickness = 5;
		[Range(0, 1), SerializeField] private float m_FillAmount = 1;
		[SerializeField] private bool m_invertFill;
		[SerializeField] private bool m_fillLastSprite = true;
		[SerializeField] private bool m_fillLastSegment = true;
		[SerializeField, Range(-360, 360)] private float m_GlobalDegreeOffset;
		[SerializeField, Range(1, 360)] private float m_MaxDegree = 360;
		[Range(1, 360), SerializeField] private int m_SegmentsCount = 360;
		[Range(1, 360), SerializeField] private int m_SegmentsPerSpriteCount = 1;
		[SerializeField, Range(-10, 10)] private float m_segmentDegreeOffset;
		[SerializeField] private UnityEngine.Gradient m_Gradient = CreateDefaultValidGradient();
		[SerializeField] private UnityEngine.Gradient m_Diagonal_Gradient = CreateDefaultValidGradient();
		[SerializeField, Range(-360, 360)] private float m_GradientDegreeOffset;

		protected UIVertexPool _vertexPool = new UIVertexPool(100);
		private float lastSegmentT;
		private float lastSubSegmentT;
		private int visibleSegmentsCount;
		public float fillAmount
		{
			get => m_FillAmount;
			set
			{
				m_FillAmount = value;
				SetAllDirty();
			}
		}
        #endregion

        #region overrides
		/// <summary>
		/// Image's texture comes from the UnityEngine.Image.
		/// </summary>
		public override Texture mainTexture
		{
			get
			{
				if (overrideSprite == null)
				{
					if (material != null && material.mainTexture != null)
					{
						return material.mainTexture;
					}

					return s_WhiteTexture;
				}

				return overrideSprite.texture;
			}
		}
        #endregion

		protected override void OnPopulateMesh(VertexHelper vh)
		{
            #region Prepare values
			float width = rectTransform.rect.width;
			Vector2 pivot = rectTransform.pivot;
			float outer = -pivot.x * width;
			float inner = -pivot.x * width + m_thickness;

			vh.Clear();

			float fill = m_FillAmount;

			float degreesPerSegment = m_MaxDegree / m_SegmentsCount;
			float degreeOffset = m_GlobalDegreeOffset;
			if (m_invertFill)
			{
				degreesPerSegment *= -1;
				degreeOffset *= -1;
				degreeOffset -= m_MaxDegree;
			}

			visibleSegmentsCount = Mathf.CeilToInt(m_SegmentsCount * fill);

			_uvTemp[0] = _defaultUV[0];
			_uvTemp[1] = _defaultUV[1];
			_uvTemp[2] = _defaultUV[2];
			_uvTemp[3] = _defaultUV[3];

			lastSegmentT = m_FillAmount * m_SegmentsCount;
			if (lastSegmentT % 1 != 0)
			{
				lastSegmentT -= Mathf.FloorToInt(lastSegmentT);
			}
			else
			{
				lastSegmentT = 1;
			}

			float currentDegrees = 0;
            #endregion

			for (int i = 0; i < visibleSegmentsCount; i++)
			{
				bool isLast = i == visibleSegmentsCount - 1;
				bool isOneStepSegment = m_SegmentsPerSpriteCount == 1;
				float segmentFill = 1;
				currentDegrees = i * degreesPerSegment + degreeOffset - m_segmentDegreeOffset / 2;
				float nextDegrees = (i + 1) * degreesPerSegment + degreeOffset + m_segmentDegreeOffset / 2;

				if (m_fillLastSegment && isLast)
				{
					segmentFill = lastSegmentT;
				}

				if (isOneStepSegment)
				{
					DrawSegment(currentDegrees, nextDegrees, outer, inner, vh, 0, 1, segmentFill);
				}
				else
				{
					int subSegmentCount = !isLast || !m_fillLastSprite ? m_SegmentsPerSpriteCount : Mathf.CeilToInt(m_SegmentsPerSpriteCount * lastSegmentT);

					float degreesStart = currentDegrees;
					float degreesDelta = (nextDegrees - currentDegrees) / m_SegmentsPerSpriteCount;
					float degreesEnd = currentDegrees + degreesDelta;

					lastSubSegmentT = m_SegmentsPerSpriteCount * lastSegmentT;
					lastSubSegmentT = lastSegmentT * m_SegmentsPerSpriteCount;
					if (lastSubSegmentT % 1 != 0)
					{
						lastSubSegmentT -= Mathf.FloorToInt(lastSubSegmentT);
					}
					else
					{
						lastSubSegmentT = 1;
					}

					for (int j = 0; j < subSegmentCount; j++)
					{
						float subSegmentT = 1;

						bool isLastSubSegment = isLast && j == subSegmentCount - 1;
						if (m_fillLastSegment && isLastSubSegment)
						{
							subSegmentT = lastSubSegmentT;
						}

						float uvStart = (float)j / m_SegmentsPerSpriteCount;
						float uvEnd = (float)(j + 1) / m_SegmentsPerSpriteCount;
						DrawSegment(degreesStart, degreesEnd, outer, inner, vh, uvStart, uvEnd, subSegmentT);
						degreesStart += degreesDelta;
						degreesEnd += degreesDelta;
					}
				}
			}
		}

        #region Helpers
		protected Color GetColor(float degrees, bool isDrawingOuter = false)
		{
			float degreeOffset = m_GlobalDegreeOffset;
			if (m_invertFill) degreeOffset *= -1;

			float t = (degrees + m_GradientDegreeOffset + 360 - m_GlobalDegreeOffset) % 360 / m_MaxDegree;
			float t2;
			if (isDrawingOuter)
				//t2 = (degrees) / m_MaxDegree;
				t2 = 1 - t;
			else
				t2 = t;
			return m_Gradient.Evaluate(t) * m_Diagonal_Gradient.Evaluate(t2) * color;
		}

		protected void DrawSegment(float degreesStart, float degreesEnd, float outer, float inner, VertexHelper vh, float uvStart, float uvEnd, float t = 1)
		{
			_positionsTemp[0] = DataToPosition(outer, degreesStart);
			_positionsTemp[1] = DataToPosition(inner, degreesStart);
			_positionsTemp[2] = DataToPosition(inner, _positionsTemp[1], degreesEnd, t);
			_positionsTemp[3] = DataToPosition(outer, _positionsTemp[0], degreesEnd, t);
			uvEnd = Mathf.Lerp(uvStart, uvEnd, t);

			_uvTemp[2] = new Vector2(uvEnd, 0);
			_uvTemp[3] = new Vector2(uvEnd, 1);
			_uvTemp[0] = new Vector2(uvStart, 1);
			_uvTemp[1] = new Vector2(uvStart, 0);

			vh.AddUIVertexQuad(CreateQuad(_positionsTemp, _uvTemp, GetColor(degreesStart), GetColor(degreesEnd), GetColor(degreesStart, true), GetColor(degreesEnd, true)));
		}

		protected UIVertex[] CreateQuad(Vector2[] vertices, Vector2[] uvs, Color leftInnerColor, Color rightInnerColor, Color leftOuterColor, Color rightOuterColor)
		{
			UIVertex[] vbo = _vertexPool.Get();
			for (int i = 0; i < vertices.Length; i++)
			{
				var vert = UIVertex.simpleVert;
				vert.position = vertices[i];
				vert.uv0 = uvs[i];
				vert.normal = Vector3.forward;
				vbo[i] = vert;
			}

			vbo[0].color = leftInnerColor;
			vbo[1].color = leftOuterColor;
			vbo[2].color = rightOuterColor;
			vbo[3].color = rightInnerColor;

			return vbo;
		}

		private Vector2 DataToPosition(float radius, float degrees)
		{
			float rad = Mathf.Deg2Rad * (degrees + 180);
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);
			return new Vector2(radius * cos, radius * sin);
		}

		private Vector2 DataToPosition(float radius, Vector3 start, float degreesEnd, float t)
		{
			float rad = Mathf.Deg2Rad * (degreesEnd + 180);
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);
			return Vector2.Lerp(start, new Vector2(radius * cos, radius * sin), t);
		}

		private static UnityEngine.Gradient CreateDefaultValidGradient()
		{
			return new UnityEngine.Gradient
			{
				alphaKeys = new[] { new GradientAlphaKey(1, 1) },
				colorKeys = new[] { new GradientColorKey(new Color(1, 1, 1, 1), 1) }
			};
		}
        #endregion
	}
}