using UnityEngine.UI;

namespace Basic.UI
{
	public class UIRaycastTarget : MaskableGraphic
	{
		protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
	}
}