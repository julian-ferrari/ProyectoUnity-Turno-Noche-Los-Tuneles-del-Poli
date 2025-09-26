using UnityEngine;
using System.Collections;

public class GuardTester : MonoBehaviour
{
    public GuardAI guardToTest;  // ← Este es el campo que debería aparecer
    public Transform testPlayer; // ← Este es el otro campo

    void OnGUI()
    {
        if (guardToTest == null) return;

        // Tamaño y posición ajustados (derecha, más compacto)
        int panelWidth = 250;  // Ancho reducido
        int panelHeight = 200; // Alto reducido
        int rightMargin = 10;  // Margen derecho
        int topMargin = 10;    // Margen superior

        // Panel en esquina superior derecha
        GUILayout.BeginArea(new Rect(Screen.width - panelWidth - rightMargin, topMargin, panelWidth, panelHeight));

        // Fondo semi-transparente para mejor visibilidad
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);

        GUILayout.Label("PRUEBAS GUARDIA - POC", GUILayout.Height(20));
        GUILayout.Label("Estado: " + guardToTest.currentState, GUILayout.Height(15));
        GUILayout.Label("Alerta: " + guardToTest.alertnessLevel.ToString("F1"), GUILayout.Height(15));

        // Espaciado reducido entre botones
        GUILayout.Space(5);

        // Botones más compactos
        if (GUILayout.Button("Forzar Detección (T)", GUILayout.Height(25)))
        {
            guardToTest.StartChasing();
        }

        if (GUILayout.Button("Toggle Patrulla (P)", GUILayout.Height(25)))
        {
            guardToTest.patrolEnabled = !guardToTest.patrolEnabled;
        }


        // Mostrar estado de la patrulla
        GUILayout.Label("Patrulla: " + (guardToTest.patrolEnabled ? "ACTIVA" : "INACTIVA"), GUILayout.Height(15));

        GUILayout.EndArea();
    }
}