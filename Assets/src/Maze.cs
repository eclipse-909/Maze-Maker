using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Maze : MonoBehaviour
{
    #region variables
    public Grid mapGrid;
    public static Grid staticMapGrid;
    public GameObject mazeCanvas;
    public RectTransform UICanvasRectTransform;
    public static Tilemap mazeMap;
    public static Tilemap pathMap;
    public static Tilemap drawMap;
    static List<Tile> tileData;
    static List<Tile> redTileData;
    static List<Vector2Int> path;
    static bool[,] pathGrid;
    static List<Vector2Int> edges;
    static List<Vector2Int> directions;
    #endregion

    void Update()
    {
        mazeCanvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, UICanvasRectTransform.rect.width);
        mazeCanvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UICanvasRectTransform.rect.height);
        mazeCanvas.GetComponent<RectTransform>().position = UICanvasRectTransform.position;
    }

    void Awake()
    {
        directions = new List<Vector2Int> {Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down};
        //vectors and 2D arrays use a different coordinate system, so they need to be shifted right by 1
        //it's still: up, right, down, left if applied to a 2D array as arr2D(direction.x, direction.y)
        staticMapGrid = mapGrid;
        mazeMap = CreateTilemap("mazeMap", 0);
        pathMap = CreateTilemap("pathMap", 1);
        drawMap = CreateTilemap("drawMap", 2);
        tileData = new List<Tile>();
        redTileData = new List<Tile>();
        path = new List<Vector2Int>();
        edges = new List<Vector2Int>();
        //get all of the existing tiles and populate tileData
        foreach (Tile tile in Resources.FindObjectsOfTypeAll(typeof(Tile)) as Tile[])
        {
            tile.flags = TileFlags.LockAll;
            if (tile.name.Contains("red"))
                redTileData.Add(tile);
            else
                tileData.Add(tile);
        }
    }

    private Tilemap CreateTilemap(string tilemapName, int layerOrder)
    {
        GameObject go = new GameObject(tilemapName);
        Tilemap tm = go.AddComponent<Tilemap>();
        TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
        tm.tileAnchor = new Vector3(0.5f, 0.5f, 0);
        go.transform.SetParent(mapGrid.transform);
        go.transform.localPosition = Vector3.zero;
        tr.mode = TilemapRenderer.Mode.Individual;
        tr.sortingLayerID = SortingLayer.NameToID("Default");
        tr.sortingOrder = layerOrder;
        return tm;
    }

    public static void GenerateMap(int rows, int columns)
    {
        #region variables
        path = new List<Vector2Int>();
        edges = new List<Vector2Int>();
        List<Tile>[,] possibleTilesMap = new List<Tile>[rows, columns];
        List<Tile>[,] possibleRedTilesMap = new List<Tile>[rows, columns];
        Fill(possibleTilesMap, tileData);
        Fill(possibleRedTilesMap, redTileData);
        mazeMap.ClearAllTiles();
        pathMap.ClearAllTiles();
        drawMap.ClearAllTiles();
        #endregion

        #region populate edges
        for (int c = 1; c < columns - 1; c++)
        {
            edges.Add(new Vector2Int(0, c));
            edges.Add(new Vector2Int(rows - 1, c));
        }
        for (int r = 1; r < rows - 1; r++)
        {
            edges.Add(new Vector2Int(r, 0));
            edges.Add(new Vector2Int(r, columns - 1));
        }
        //randomly pick an edge to start on
        Vector2Int start = edges[Random.Range(0, edges.Count)];
        //then add the corners
        edges.Add(Vector2Int.zero);
        edges.Add(new Vector2Int(0, columns - 1));
        edges.Add(new Vector2Int(rows - 1, 0));
        edges.Add(new Vector2Int(rows - 1, columns - 1));
        #endregion

        #region generate random path
        //generate a path from start to finish
        //begins at the start and randomly generates a path
        //goes straight/right/left by incremening rows and columns randomly
        //path can't intersect itself
        //all values in path are false except the tiles on the path
        pathGrid = new bool[rows, columns];
        pathGrid[start.x, start.y] = true;
        path.Add(start);
        Vector2Int currPos = start;
        //move in a random direction
        //can't go to the next tile if that tile is outside of the array or already true
        bool canMove = true;
        List<Vector2Int>[,] possDirectionsMap = new List<Vector2Int>[rows, columns];
        Fill(possDirectionsMap, directions);
        Vector2Int[,] chosenDirectionMap = new Vector2Int[rows, columns];
        //loop through and create each tile in the path
        int numBacktracks = 0;///////////////////////////////////////////testing
        while (canMove)
        {
            //if it backtracks too much, it will simply start over and make a brand new path at the same starting location
            //sometimes it will have infinite loops, and I assume it's because it backtracks too much and gets confused
            //this is just a very simple workaround for a complex problem
            if (numBacktracks >= rows * columns)
            {
                numBacktracks = 0;
                path = new List<Vector2Int> {start};
                pathGrid = new bool[rows, columns];
                pathGrid[start.x, start.y] = true;
                Fill(possDirectionsMap, directions);
                chosenDirectionMap = new Vector2Int[rows, columns];
            }
            currPos = path[path.Count - 1];
            if (currPos.x == 1 || currPos.x == rows - 2 || currPos.y == 1 || currPos.y == columns - 2)
                canMove = Random.Range(0f, (float) (rows * columns) / (rows * 2 + columns * 2)) > path.Count / (double) (rows * columns);
            if (!canMove)
            {
                possDirectionsMap[currPos.x, currPos.y] = new List<Vector2Int> {Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down};
                for (int i = 0; i < possDirectionsMap[currPos.x, currPos.y].Count; i++)
                {
                    Vector2Int newTile = currPos + possDirectionsMap[currPos.x, currPos.y][i];
                    if (!edges.Contains(newTile) || pathGrid[newTile.x, newTile.y])
                        possDirectionsMap[currPos.x, currPos.y].RemoveAt(i--);
                }
            } else {
                for (int i = 0; i < possDirectionsMap[currPos.x, currPos.y].Count; i++)
                {
                    Vector2Int newTile = currPos + possDirectionsMap[currPos.x, currPos.y][i];
                    if (newTile.x < 1 || newTile.x >= rows - 1 || newTile.y < 1 || newTile.y >= columns - 1)
                        possDirectionsMap[currPos.x, currPos.y].RemoveAt(i--);
                    else if (pathGrid[newTile.x, newTile.y])
                        possDirectionsMap[currPos.x, currPos.y].RemoveAt(i--);
                }
            }
            if (possDirectionsMap[currPos.x, currPos.y].Count == 0 && canMove)
            {
                Vector2Int prevPos = path[path.Count - 2];
                possDirectionsMap[currPos.x, currPos.y] = new List<Vector2Int> {Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down};
                possDirectionsMap[prevPos.x, prevPos.y].RemoveAt(possDirectionsMap[prevPos.x, prevPos.y].IndexOf(chosenDirectionMap[prevPos.x, prevPos.y]));
                chosenDirectionMap[currPos.x, currPos.y] = Vector2Int.zero;
                pathGrid[currPos.x, currPos.y] = false;
                path.RemoveAt(path.Count - 1);
                numBacktracks++;///////////////////////////////////////////////////////
            } else {
                chosenDirectionMap[currPos.x, currPos.y] = possDirectionsMap[currPos.x, currPos.y][Random.Range(0, possDirectionsMap[currPos.x, currPos.y].Count)];
                Vector2Int newPos = currPos + chosenDirectionMap[currPos.x, currPos.y];
                pathGrid[newPos.x, newPos.y] = true;
                path.Add(newPos);
            }
        }
        #endregion

        #region collapse and pick tiles
        //collapse edges
        CollapseEdgeTiles(possibleTilesMap, false);
        CollapseEdgeTiles(possibleRedTilesMap, true);
        //collapse path
        for (int i = 1; i < path.Count - 2; i++)
        {
            CollapsePathTile(possibleTilesMap, path[i], path[i + 1], path[i - 1]);
            CollapsePathTile(possibleRedTilesMap, path[i], path[i + 1], path[i - 1]);
        }

        //collapse and pick remaining tiles in map
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (possibleTilesMap[r,c].Count > 1)
                {
                    CollapseTile(possibleTilesMap, new Vector2Int(r, c));
                    PickTile(possibleTilesMap, new Vector2Int(r, c));
                }
        #endregion
        
        #region backtrack
        #region remove extra paths
        bool[,] deadEnds = new bool[rows-2, columns-2];
        //solve the maze for all possible paths
        for (int r = 0; r < rows - 2; r++)
            for (int c = 0; c < columns - 2; c++)
                if (!deadEnds[r,c])
                    FindDeadEnds(possibleTilesMap, deadEnds, r, c);
        //find intersections
        List<AltPath> altPaths = new List<AltPath>();
        for (int i = 1; i < path.Count - 1; i++)
            for (int j = 0; j < 4; j++)
            {
                Vector2Int adjacentPos = path[i] + directions[j];
                if (adjacentPos.x > 0 && adjacentPos.x < rows - 1 && adjacentPos.y > 0 && adjacentPos.y < columns - 1)
                    if (possibleTilesMap[path[i].x, path[i].y][0].name[j].Equals('1') && !deadEnds[adjacentPos.x - 1, adjacentPos.y - 1] && !path.Contains(adjacentPos) && !AltPathsContainsTile(altPaths, adjacentPos))
                    {
                        AltPath newAltPath = new AltPath();
                        newAltPath.AddTile(adjacentPos);
                        newAltPath.AddIntersection(path[i], adjacentPos);
                        altPaths.Add(newAltPath);
                        GetAltPath(altPaths, possibleTilesMap, deadEnds, newAltPath, adjacentPos);
                    }
            }
        //close intersections
        while (altPaths.Count > 0)
        {
            altPaths.Remove(altPaths[0].RemoveRandomIntersections(possibleTilesMap));
        }
        #endregion remove extra paths
        
        #region remove squares
        for (int r = 1; r < rows - 2; r++)
            for (int c = 1; c < columns - 2; c++)
            {
                Tile[] tiles = {possibleTilesMap[r+1,c][0], possibleTilesMap[r,c][0], possibleTilesMap[r,c+1][0], possibleTilesMap[r+1,c+1][0]};
                bool hasSquare = true;
                for (int i = 0; i < 4 && hasSquare; i++)
                    if (!(tiles[i].name[i].Equals('1') && tiles[i].name[(i+1)%4].Equals('1')))
                        hasSquare = false;
                if (hasSquare)
                {
                    Vector2Int[] coords = {new Vector2Int(r+1,c), new Vector2Int(r,c), new Vector2Int(r,c+1), new Vector2Int(r+1,c+1)};
                    char[][] tileChars = {tiles[0].name.ToCharArray(), tiles[1].name.ToCharArray(), tiles[2].name.ToCharArray(), tiles[3].name.ToCharArray()};
                    //change tiles
                    char[] nums = {'0', '1'};
                    for (int i = 0; i < 4; i++)
                    {
                        bool thisTilePathsToNextTile = path.Contains(coords[i]) && path.Contains(coords[(i+1)%4]);
                        bool thisTileWillBe0000 = tileChars[i][(i+1)%4].Equals('0') && tileChars[i][(i+2)%4].Equals('0') && tileChars[i][(i+3)%4].Equals('0');
                        bool nextTileWillBe0000 = tileChars[(i+1)%4][(i+1)%4].Equals('0') && tileChars[(i+1)%4][(i+3)%4].Equals('0') && tileChars[(i+1)%4][i].Equals('0');
                        if (!(thisTilePathsToNextTile || thisTileWillBe0000 || nextTileWillBe0000))
                        {
                            char newNum = nums[Random.Range(0, nums.Length)];
                            tileChars[i][i] = tileChars[(i+1)%4][(i+2)%4] = newNum;
                        }
                        possibleTilesMap[coords[i].x, coords[i].y] = GetPossTiles(new string(tileChars[i]));
                        possibleTilesMap[coords[(i+1)%4].x, coords[(i+1)%4].y] = GetPossTiles(new string(tileChars[(i+1)%4]));
                    }
                }
            }
        #endregion remove squares
        
        #region open enclosures
        bool[,] accessibleTiles = new bool[rows, columns];
        int numAccessible = AccessibleMoves(possibleTilesMap, accessibleTiles, start);
        while (numAccessible < (rows - 2) * (columns - 2) + 1)
        {
            //find a false tile next to a true tile and open a path between them
            List<Vector2Int> falseTiles = new List<Vector2Int>();
            List<Vector2Int> trueDirections = new List<Vector2Int>();
            for (int r = 1; r < rows - 1; r++)
                for (int c = 1; c < columns - 1; c++)
                    if (!accessibleTiles[r,c])
                    {
                        List<Vector2Int> possTrueDirections = new List<Vector2Int>();
                        foreach (Vector2Int direction in directions)
                            if (accessibleTiles[r + direction.x, c + direction.y] && !edges.Contains(new Vector2Int(r + direction.x, c + direction.y)))
                                possTrueDirections.Add(direction);
                        if (possTrueDirections.Count > 0)
                        {
                            falseTiles.Add(new Vector2Int(r, c));
                            trueDirections.Add(possTrueDirections[Random.Range(0, possTrueDirections.Count)]);
                        }
                    }
            int falseIndex = Random.Range(0, falseTiles.Count);
            Vector2Int falseTile = falseTiles[falseIndex];
            Vector2Int trueDirection = trueDirections[falseIndex];
            Vector2Int trueTile = falseTile + trueDirection;
            int index = directions.IndexOf(trueDirection);
            char[] charFalse = possibleTilesMap[falseTile.x, falseTile.y][0].name.ToCharArray();
            char[] charTrue = possibleTilesMap[trueTile.x, trueTile.y][0].name.ToCharArray();
            charFalse[index] = '1';
            charTrue[(index + 2) % 4] = '1';
            possibleTilesMap[falseTile.x, falseTile.y] = GetPossTiles(new string(charFalse));
            possibleTilesMap[trueTile.x, trueTile.y] = GetPossTiles(new string(charTrue));
            numAccessible += AccessibleMoves(possibleTilesMap, accessibleTiles, falseTile);
        }
        #endregion open enclosures
        #endregion backtrack

        #region set tiles to Tilemap
        //set tiles to Tilemaps
        staticMapGrid.transform.localPosition = new Vector3(-2*columns, rows + (0.5f*columns), 0);
        Camera.main.orthographicSize = 3 * Mathf.Max(rows, columns);
        Camera.main.transform.localPosition = Vector3.back * 10;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                Vector3Int thisTile = new Vector3Int(c, -r);
                if (possibleTilesMap[r,c].Count > 1)
                    PickTile(possibleTilesMap, new Vector2Int(r,c));
                mazeMap.SetTile(thisTile, possibleTilesMap[r,c][0]);
                if (possibleRedTilesMap[r,c].Count == 1 && pathGrid[r,c])
                    pathMap.SetTile(thisTile, possibleRedTilesMap[r,c][0]);
            }
        mazeMap.CompressBounds();
        pathMap.CompressBounds();
        #endregion
    }

    #region wave function collapse
    private static void CollapseEdgeTiles(List<Tile>[,] possTilesMap, bool red)
    {
        CollapseEdgeTile(possTilesMap, path[0], true, red);
        CollapseEdgeTile(possTilesMap, path[path.Count - 1], true, red);
        if (!red)
            for (int i = 0; i < edges.Count; i++)
                if (possTilesMap[edges[i].x, edges[i].y].Count > 1)
                    CollapseEdgeTile(possTilesMap, edges[i], false, false);
    }

    private static void PickTile(List<Tile>[,] possTilesMap, Vector2Int coord)
    {
        //picks the actual tile to be used by getting a random tile from the possibleTiles
        if (possTilesMap[coord.x, coord.y].Count > 1)
        {
            List<Tile> weightedTiles = new List<Tile>();
            for (int i = 0; i < possTilesMap[coord.x, coord.y].Count; i++)
            {
                int numOpenings = possTilesMap[coord.x, coord.y][i].name.Split('1').Length - 1;
                string shape = "";
                switch (numOpenings)
                {
                    //higher number = appear more frequently
                    //don't make it too high, or else it will take forever to run
                    case 1: shape = "dead-end"; break;
                    case 2:
                        string name = possTilesMap[coord.x, coord.y][i].name;
                        if (name.Contains("red") || name.Contains("opn") || name.Contains("edg"))
                            name = name.Substring(3);
                        if (name[0].Equals(name[1]) || name[0].Equals(name[3]))
                            shape = "L-curve";
                        else
                            shape = "straight";
                        break;
                    case 3: shape = "T-int"; break;
                    case 4: shape = "X-int"; break;
                }
                int weight = 0;
                switch (shape)
                {
                    //higher number = appear more frequently
                    //don't make it insanely high, or it will take longer to run
                    //0 = it won't go there unless it has to
                    //default values shown in comments to the right
                    case "dead-end":weight = 1; break;//1
                    case "L-curve": weight = 2; break;//2
                    case "straight":weight = 8; break;//8
                    case "T-int":   weight = 1; break;//1
                    case "X-int":   weight = 1; break;//1
                }
                for (int j = 0; j < weight; j++)
                    weightedTiles.Add(possTilesMap[coord.x, coord.y][i]);
            }
            possTilesMap[coord.x, coord.y] = new List<Tile>();
            possTilesMap[coord.x, coord.y].Add(weightedTiles[Random.Range(0, weightedTiles.Count)]);
        }
        //collapses adjacent tiles, but doesn't pick them
        foreach (Vector2Int direction in directions)
        {
            Vector2Int newCoord = coord + direction;
            //if the new tile isn't out of bounds, and if the new tile can be further collapsed
            if (!(newCoord.x < 0 || newCoord.x >= possTilesMap.GetLength(0) || newCoord.y < 0 || newCoord.y >= possTilesMap.GetLength(1)))
                if (possTilesMap[newCoord.x, newCoord.y].Count > 1)
                    CollapseTile(possTilesMap, newCoord);
        }
    }

    private static void CollapseTile(List<Tile>[,] possTilesMap, Vector2Int coord)
    {
        //gets all adjacent tiles and collapses this tile based on possibilities of adjacent tiles
        
        //i also happens to equal the character index in the tile name that needs to match to (i+2)%4
        for (int i = 0; i < 4; i++)
        {
            Vector2Int adjacentCoord = coord + directions[i];
            //if the new tile isn't out of bounds
            if (!(adjacentCoord.x < 0 || adjacentCoord.x >= possTilesMap.GetLength(0) || adjacentCoord.y < 0 || adjacentCoord.y >= possTilesMap.GetLength(1)))
            {
                bool has0 = false;
                bool has1 = false;
                foreach (Tile tile in possTilesMap[adjacentCoord.x, adjacentCoord.y])
                {
                    string tileName = tile.name;
                    if (tileName.Contains("red") || tileName.Contains("opn") || tileName.Contains("edg"))
                        tileName = tileName.Substring(3);
                    if (tileName[(i + 2) % 4].Equals('0'))
                        has0 = true;
                    else if (tileName[(i + 2) % 4].Equals('1'))
                        has1 = true;
                    if (has0 && has1)
                        break;
                }
                if (!has0)
                    for (int j = 0; j < possTilesMap[coord.x, coord.y].Count; j++)
                    {
                        string tileName = possTilesMap[coord.x, coord.y][j].name;
                        if (tileName.Contains("red") || tileName.Contains("opn") || tileName.Contains("edg"))
                            tileName = tileName.Substring(3);
                        if (tileName[i].Equals('0'))
                            possTilesMap[coord.x, coord.y].RemoveAt(j--);
                    }
                if (!has1)
                    for (int j = 0; j < possTilesMap[coord.x, coord.y].Count; j++)
                    {
                        string tileName = possTilesMap[coord.x, coord.y][j].name;
                        if (tileName.Contains("red") || tileName.Contains("opn") || tileName.Contains("edg"))
                            tileName = tileName.Substring(3);
                        if (tileName[i].Equals('1'))
                            possTilesMap[coord.x, coord.y].RemoveAt(j--);
                    }
            }
        }
        if (!edges.Contains(coord))
            for (int i = 0; i < possTilesMap[coord.x, coord.y].Count; i++)
                if (possTilesMap[coord.x, coord.y][i].name.Contains("opn") || possTilesMap[coord.x, coord.y][i].name.Contains("edg"))
                    possTilesMap[coord.x, coord.y].RemoveAt(i--);

    }

    private static void CollapseEdgeTile(List<Tile>[,] possTilesMap, Vector2Int coord, bool opening, bool red)
    {
        bool top = coord.x == 0, right = coord.y == possTilesMap.GetLength(1) - 1, bottom = coord.x == possTilesMap.GetLength(0) - 1, left = coord.y == 0;
        string thisName = "";
        if (top)
            if (left)
                thisName = "1001";
            else if (right)
                thisName = "1100";
            else
                thisName = "1000";
        else if (bottom)
            if (left)
                thisName = "0011";
            else if (right)
                thisName = "0110";
            else
                thisName = "0010";
        else if (left)
            thisName = "0001";
        else if (right)
            thisName = "0100";
        //cannot be red, opn, or edg at the same time
        if (red)
            thisName = "red" + thisName.Substring(2) + thisName.Substring(0,2);
        else if (opening)
            thisName = "opn" + thisName.Substring(2) + thisName.Substring(0,2);
        else
            thisName = "edg" + thisName;
        for (int i = 0; i < possTilesMap[coord.x, coord.y].Count; i++)
            if (!possTilesMap[coord.x, coord.y][i].name.Equals(thisName))
                possTilesMap[coord.x, coord.y].RemoveAt(i--);
        PickTile(possTilesMap, coord);
    }

    private static void CollapsePathTile(List<Tile>[,] possTilesMap, Vector2Int coord, Vector2Int nextCoord, Vector2Int prevCoord)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2Int adjacent = coord + directions[i];
            if (adjacent == nextCoord)
                for (int j = 0; j < possTilesMap[coord.x, coord.y].Count; j++)
                {
                    string tileName = possTilesMap[coord.x, coord.y][j].name;
                    if (tileName.Contains("red") || tileName.Contains("opn") || tileName.Contains("edg"))
                        tileName = tileName.Substring(3);
                    if (!tileName[i].Equals('1'))
                        possTilesMap[coord.x, coord.y].RemoveAt(j--);
                }
            else if (adjacent != prevCoord && pathGrid[adjacent.x, adjacent.y])
                for (int j = 0; j < possTilesMap[coord.x, coord.y].Count; j++)
                {
                    string tileName = possTilesMap[coord.x, coord.y][j].name;
                    if (tileName.Contains("red") || tileName.Contains("opn") || tileName.Contains("edg"))
                        tileName = tileName.Substring(3);
                    if (tileName[i].Equals('1'))
                        possTilesMap[coord.x, coord.y].RemoveAt(j--);
                }
        }
        PickTile(possTilesMap, coord);
    }
    #endregion

    #region backtracking
    private static int AccessibleMoves(List<Tile>[,] possTilesMap, bool[,] accessibleTiles, Vector2Int coord)
    {
        int count = 1;
        accessibleTiles[coord.x, coord.y] = true;
        string name = possTilesMap[coord.x, coord.y][0].name;
        if (name.Contains("red") || name.Contains("opn") || name.Contains("edg"))
            name = name.Substring(3);
        for (int i = 0; i < 4; i++)
        {
            int newX = (coord + directions[i]).x;
            int newY = (coord + directions[i]).y;
            if (newX >= 1 && newX < possTilesMap.GetLength(0) - 1 && newY >= 1 && newY < possTilesMap.GetLength(1) - 1)
            {
                if (name[i].Equals('1') && !accessibleTiles[newX, newY])
                    count += AccessibleMoves(possTilesMap, accessibleTiles, coord + directions[i]);
            }
        }
        return count;
    }
    
    private static void FindDeadEnds(List<Tile>[,] possTilesMap, bool[,] deadEnds, int r, int c)
    {
        int openDirections = 0;
        Vector2Int direction = Vector2Int.zero;
        for (int i = 0; i < 4; i++)
        {
            Vector2Int adjacentCoord = new Vector2Int(r, c) + directions[i];
            if (adjacentCoord.x >= 0 && adjacentCoord.x < deadEnds.GetLength(0) && adjacentCoord.y >= 0 && adjacentCoord.y < deadEnds.GetLength(1))
                if (possTilesMap[r+1,c+1][0].name[i].Equals('1') || !deadEnds[adjacentCoord.x, adjacentCoord.y])
                {
                    openDirections++;
                    direction = directions[i];
                }
        }
        if (openDirections < 2)
        {
            deadEnds[r,c] = true;
            if (openDirections == 1)
                FindDeadEnds(possTilesMap, deadEnds, r + direction.x, c + direction.y);
        }
    }
    
    private static List<Tile> GetPossTiles(string tileName)
    {
        List<Tile> possTiles = new List<Tile>();
        foreach (Tile tile in tileData)
            if (tileName.Equals(tile.name))
                possTiles.Add(tile);
        return possTiles;
    }
    
    private static void GetAltPath(List<AltPath> altPaths, List<Tile>[,] possTilesMap, bool[,] deadEnds, AltPath thisAltPath, Vector2Int thisTile)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2Int adjacentTile = thisTile + directions[i];
            if (adjacentTile.x > 0 && adjacentTile.x < deadEnds.GetLength(0) && adjacentTile.y > 0 && adjacentTile.y < deadEnds.GetLength(1))
                if (possTilesMap[thisTile.x, thisTile.y][0].name[i].Equals('1') && !deadEnds[adjacentTile.x - 1, adjacentTile.y - 1] && !AltPathsContainsTile(altPaths, adjacentTile))
                    if (!path.Contains(adjacentTile))
                    {
                        thisAltPath.AddTile(adjacentTile);
                        GetAltPath(altPaths, possTilesMap, deadEnds, thisAltPath, adjacentTile);
                    } else {
                        thisAltPath.AddIntersection(adjacentTile, thisTile);
                    }
        }
    }

    private static bool AltPathsContainsTile(List<AltPath> list, Vector2Int coords)
    {
        foreach (AltPath altPath in list)
            if (altPath.tilesInPath.Contains(coords))
                return true;
        return false;
    }
    #endregion

    #region fill 2D arrays with the same value - utilizes semi-deep cloning
    private static void Fill(List<Vector2Int>[,] array, List<Vector2Int> value)
    {
        for (int r = 0; r < array.GetLength(0); r++)
            for (int c = 0; c < array.GetLength(1); c++)
            {
                List<Vector2Int> list = new List<Vector2Int>();
                foreach (Vector2Int v in value)
                    list.Add(new Vector2Int(v.x, v.y));
                array.SetValue(list, r, c);
            }
    }

    private static void Fill(List<Tile>[,] array, List<Tile> value)
    {
        for (int r = 0; r < array.GetLength(0); r++)
            for (int c = 0; c < array.GetLength(1); c++)
            {
                List<Tile> list = new List<Tile>();
                foreach (Tile t in value)
                    list.Add(t);
                array.SetValue(list, r, c);
            }
    }
    #endregion

    #region debugging
    private static void PrintPath(bool[,] pathGrid)
    {
        string str = "";
        for (int r = 0; r < pathGrid.GetLength(0); r++)
        {
            for (int c = 0; c < pathGrid.GetLength(1); c++)
            {
                Vector2Int coord = new Vector2Int(r, c);
                int index = path.IndexOf(coord);
                if (index + 1 == path.Count)
                    str += "#";
                else if (pathGrid[r,c])
                {
                    Vector2Int direction = path[index + 1] - coord;
                    if (direction == Vector2Int.left)
                        str += "^";
                    else if (direction == Vector2Int.up)
                        str += ">";
                    else if (direction == Vector2Int.right)
                        str += "v";
                    else if (direction == Vector2Int.down)
                        str += "<";
                } else
                    str += "*";
            }
            str += "\n";
        }
        Debug.Log(str);
    }

    private static void PrintPossTileMap(List<Tile>[,] possTilesMap)
    {
        string str = "";
        for (int r = 0; r < possTilesMap.GetLength(0); r++)
        {
            for (int c = 0; c < possTilesMap.GetLength(1); c++)
            {
                int count = 9;
                if (possTilesMap[r, c].Count < 9)
                    count = possTilesMap[r, c].Count;
                str += count;
            }
            str += "\n";
        }
        Debug.Log(str);
    }
    #endregion

    public class AltPath
    {
        public List<Vector2Int> tilesInPath {get;}
        public List<Intersection> intersections {get;}

        public AltPath()
        {
            tilesInPath = new List<Vector2Int>();
            intersections = new List<Intersection>();
        }

        public void AddTile(Vector2Int coords) {tilesInPath.Add(coords);}

        public void AddIntersection(Vector2Int mainCoords, Vector2Int altCoords) {intersections.Add(new Intersection(mainCoords, altCoords));}

        public AltPath RemoveRandomIntersections(List<Tile>[,] possTilesMap)
        {
            while (intersections.Count > 1)
            {
                int intIndex = Random.Range(0, intersections.Count);
                Vector2Int mainCoords = intersections[intIndex].mainPathCoords;
                Vector2Int altCoords = intersections[intIndex].altPathCoords;
                int directionIndex = directions.IndexOf(altCoords - mainCoords);
                char[] mainName = possTilesMap[mainCoords.x, mainCoords.y][0].name.ToCharArray();
                char[] altName = possTilesMap[altCoords.x, altCoords.y][0].name.ToCharArray();
                mainName[directionIndex] = altName[(directionIndex + 2) % 4] = '0';
                if (!new string(mainName).Equals("0000") && !new string(altName).Equals("0000"))
                {
                    possTilesMap[mainCoords.x, mainCoords.y] = GetPossTiles(new string(mainName));
                    possTilesMap[altCoords.x, altCoords.y] = GetPossTiles(new string(altName));
                }
                intersections.RemoveAt(intIndex);
            }
            return this;
        }

        public class Intersection
        {
            public Vector2Int mainPathCoords {get;}
            public Vector2Int altPathCoords {get;}

            public Intersection(Vector2Int mainCoords, Vector2Int altCoords)
            {
                mainPathCoords = mainCoords;
                altPathCoords = altCoords;
            }
        }
    }
}