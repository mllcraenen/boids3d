using UnityEngine;

public class FPSCounter : MonoBehaviour {
	private float deltaTime = 0.0f;

	private void Update() {
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
	}

	private void OnGUI() {
		int fps = Mathf.RoundToInt(1.0f / deltaTime);
		string text = $"FPS: {fps}";

		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.normal.textColor = Color.white;
		style.fontSize = 20;

		GUI.Label(new Rect(10, 10, 100, 50), text, style);
	}
}
