using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMethods : MonoBehaviour
{
    public GameObject mazePanel;
    static bool panelShowing;
    public TMP_InputField height;
    public TMP_InputField length;
    public Button togglePath;
    public static bool pathShowing;
    public Button pencil;
    public Button eraser;

    void Awake()
    {
        pathShowing = false;
        panelShowing = false;
    }

    public void NewMaze()
    {
        panelShowing = !panelShowing;
        mazePanel.SetActive(panelShowing);
    }

    public void HideMazePanel()
    {
        panelShowing = false;
        mazePanel.SetActive(panelShowing);
    }

    public void GenerateMaze()
    {
        HideMazePanel();
        Maze.GenerateMap(Mathf.Max(3, int.Parse(height.text)), Mathf.Max(3, int.Parse(length.text)));
        HidePath();
        //deselect any brushes
    }

    public void TogglePath()
    {
        if (pathShowing)
            HidePath();
        else
            ShowPath();
    }

    private void ShowPath()
    {
        Maze.drawMap.gameObject.SetActive(false);
        Maze.pathMap.gameObject.SetActive(true);
        togglePath.GetComponentInChildren<TextMeshProUGUI>().text = "Hide Path";
        pathShowing = true;
    }

    public void HidePath()
    {
        Maze.drawMap.gameObject.SetActive(true);
        Maze.pathMap.gameObject.SetActive(false);
        togglePath.GetComponentInChildren<TextMeshProUGUI>().text = "Show Path";
        pathShowing = false;
    }

    public void Pencil()
    {
        //selects the pencil tool
        //deselects eraser
        //allows drawing
    }

    public void Draw()
    {
        //draws a red path between tiles when you hold the mouse down over them
    }

    public void Eraser()
    {
        //selects the eraser tool
        //deselects pencil
        //allows erasing
    }

    public void Erase()
    {
        //erases the red path on this tile when you hold the mouse down
    }

    public void EraseAll()
    {
        //erases all user-drawings
    }
}