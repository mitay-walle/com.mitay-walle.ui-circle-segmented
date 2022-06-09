using UnityEditor;
using UnityEngine;

namespace mitaywalle.UICircleSegmentedNamespace
{
	public class UICircleSegmentedCurve : MonoBehaviour
	{
		[SerializeField] AnimationCurve _curve;
		[SerializeField] UICircleSegmented _circle;
		void Update()
		{
			if (_curve.length > 0 && _circle)
			{
				
				_circle.fillAmount = _curve.Evaluate(Time.realtimeSinceStartup+10000);
				SetDirty();
			}
		}

		void SetDirty()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorUtility.SetDirty(_circle);
			}
#endif
		}
	}
}