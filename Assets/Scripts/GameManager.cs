using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public enum HitState
{
    HIT,
    MISS,
    NOT_SHOT
}

public enum ShipOrientation
{
    HORIZONTAL,
    VERTICAL
}
public class GridSpace
{

    public Ship ship;

    public HitState hitState;

    public GridSpace()
    {
        hitState = HitState.NOT_SHOT;
    }

}

[Serializable]
public class Ship
{

    public string name;
    public GameObject model;
    public bool sunk = false;

    public bool placed = false;
    public int hits;
    public int size;
}

public class GameManager : MonoBehaviour
{

    private GridSpace[,] userBoard;
    private GridSpace[,] aiBoard;


    private void removeShipFromBoard(Ship ship, GridSpace[,] board)
    {
        for (int row = 0; row < board.GetLength(0); row++)
        {
            for (int col = 0; col < board.GetLength(1); col++)
            {
                if (board[row, col].ship == ship)
                {
                    board[row, col].ship = null;
                }
            }
        }
    }
    private void PlaceShip(Ship ship, int row, int col, GridSpace[,] board, ShipOrientation orientation)
    {
        ship.placed = true;
        removeShipFromBoard(ship, board);
        int targetRow = row;
        int targetCol = col;
        int size = ship.size;
        while (size != 0)
        {
            board[targetRow, targetCol].ship = ship;
            if (orientation == ShipOrientation.HORIZONTAL)
            {
                targetCol++;
            }
            else
            {
                targetRow++;
            }
            size--;

        }
    }

    private bool canPlaceShip(Ship ship, int row, int col, GridSpace[,] board, ShipOrientation orientation)
    {
        int targetRow = row;
        int targetCol = col;
        int size = ship.size;
        while (size != 0)
        {
            if (targetRow >= board.GetLength(0) || targetRow < 0)
            {
                return false;
            }
            if (targetCol >= board.GetLength(1) || targetCol < 0)
            {
                return false;
            }

            if (board[targetRow, targetCol].ship != null)
            {
                return false;
            }

            if (orientation == ShipOrientation.HORIZONTAL)
            {
                targetCol++;
            }
            else
            {
                targetRow++;
            }
            size--;

        }
        return true;
    }

    private void setupBoard(GridSpace[,] board)
    {
        for (int row = 0; row < board.GetLength(0); row++)
        {
            for (int col = 0; col < board.GetLength(1); col++)
            {
                board[row, col] = new GridSpace();
            }
        }
    }


    public List<Ship> PlayerShips;

    public List<Ship> AIShips;

    private class Coord
    {
        public int row;
        public int col;

        public Coord(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }

    private System.Random rand = new System.Random();
    private Coord getRandomCoord(GridSpace[,] board)
    {
        return new Coord(rand.Next(board.GetLength(0)), rand.Next(board.GetLength(1)));
    }

    private ShipOrientation getRandomOrientation()
    {
        if (rand.Next(2) == 0)
        {
            return ShipOrientation.HORIZONTAL;
        }
        return ShipOrientation.VERTICAL;
    }

    private void randomlyPopulateBoard(GridSpace[,] board, List<Ship> ships)
    {
        while (ships.Count > 0)
        {
            Ship ship = ships[0];
            ships.RemoveAt(0);
            Coord position = getRandomCoord(board);
            ShipOrientation orient = getRandomOrientation();
            while (!canPlaceShip(ship, position.row, position.col, board, orient))
            {
                position = getRandomCoord(board);
                orient = getRandomOrientation();
            }
            PlaceShip(ship, position.row, position.col, board, orient);
        }
    }


    private List<Coord> AITargets;

    private List<Coord> generateAITargets(GridSpace[,] board)
    {
        List<Coord> result = new List<Coord>();
        for (int row = 0; row < board.GetLength(0); row++)
        {
            for (int col = 0; col < board.GetLength(1); col++)
            {
                result.Add(new Coord(row, col));
            }
        }
        result.Shuffle();

        return result;
    }


    private bool allShipsPlaced()
    {
        return PlayerShips.TrueForAll(x => x.placed);
    }

    private bool allShipsSunk(List<Ship> ships)
    {
        return ships.TrueForAll(x => x.sunk);
    }

    private Ship getShipFromUserInput(string shipName)
    {
        Ship selectedShip = null;
        foreach (Ship ship in PlayerShips)
        {
            if (String.Equals(shipName, ship.name, StringComparison.OrdinalIgnoreCase))
            {
                selectedShip = ship;
                break;
            }
        }
        return selectedShip;
    }

    private int getColumnFromUserInput(string col)
    {

        if (col.Length != 1 || !Char.IsLetter(col[0]))
        {
            throw new Exception("Column identifier must be in the range A to H");
        }

        int letterValue = ((int)col[0] % 32) - 1;
        if (letterValue >= 8)
        {
            throw new Exception("Column identifier must be in the range A to H");
        }

        return letterValue;
    }

    private int getRowFromUserInput(string row)
    {
        int value;
        if (int.TryParse(row, out value))
        {
            value--;
            if (value > 7 || value < 0)
            {
                throw new Exception("Row identifier must be in the range 1 to 8");
            }
            return value;
        }
        throw new Exception("Row identifier must be a number");
    }

    private ShipOrientation getOrientationFromUserInput(string orientation)
    {
        if (String.Equals(orientation, "vertical", StringComparison.OrdinalIgnoreCase))
        {
            return ShipOrientation.VERTICAL;
        }
        if (String.Equals(orientation, "horizontal", StringComparison.OrdinalIgnoreCase))
        {
            return ShipOrientation.HORIZONTAL;
        }

        throw new Exception("Orientations must be vertical or horizontal");
    }
    public string onMoveCommand(string shipName, string col, string row, string orientation)
    {
        if (gameState == GameState.END)
        {
            return "The game is over";
        }
        if (gameState == GameState.FIRING)
        {
            return "The placement phase is over";
        }
        Ship ship = getShipFromUserInput(shipName);
        if (ship == null)
        {
            return "That is not a valid ship name";
        }
        int parsedCol;
        int parsedRow;
        ShipOrientation parsedOrientation;
        try
        {
            parsedCol = getColumnFromUserInput(col);
            parsedRow = getRowFromUserInput(row);
            parsedOrientation = getOrientationFromUserInput(orientation);
        }
        catch (Exception e)
        {
            return e.Message;
        }
        if (!canPlaceShip(ship, parsedRow, parsedCol, userBoard, parsedOrientation))
        {
            return "That ship can't be placed there";
        }
        PlaceShip(ship, parsedRow, parsedCol, userBoard, parsedOrientation);
        return shipName + " deployed";
    }

    public string onConfirmPlacementCommand()
    {
        if (gameState == GameState.END)
        {
            return "The game is over";
        }
        if (gameState == GameState.FIRING)
        {
            return "The placement phase is over";
        }

        if (!allShipsPlaced())
        {
            return "You must place all ships before entering the combat phase";
        }

        gameState = GameState.FIRING;
        return "Prepare for combat";
    }

    public string onFireCommand(string col, string row)
    {
        if (gameState == GameState.END)
        {
            return "The game is over";
        }
        if (gameState == GameState.PLACEMENT)
        {
            return "Can't fire in placement phase";
        }
        int parsedCol;
        int parsedRow;
        try
        {
            parsedCol = getColumnFromUserInput(col);
            parsedRow = getRowFromUserInput(row);
        }
        catch (Exception e)
        {
            return e.Message;
        }

        GridSpace playerTarget = aiBoard[parsedRow, parsedCol];
        if (playerTarget.hitState == HitState.HIT)
        {
            return "You have already hit this target, Additonaly strikes are not helpful";
        }
        if (playerTarget.hitState == HitState.MISS)
        {
            return "You have already confirmed there's no one there";
        }

        Coord aiCoords = AITargets[0];
        AITargets.RemoveAt(0);
        GridSpace aiTarget = userBoard[aiCoords.row, aiCoords.col];

        FireResult playerResult = ProcessMove(playerTarget, true, col, parsedRow);
        FireResult aiResult = ProcessMove(aiTarget, false, null, null);

        if (playerResult == FireResult.WIN || aiResult == FireResult.WIN)
        {
            gameState = GameState.END;
        }

        if (playerResult == FireResult.WIN && aiResult == FireResult.WIN)
        {
            return "Both players have simultaneously died. Perhaps you will have better luck next time";
        }

        if (playerResult == FireResult.WIN)
        {
            return "All the enemy ships have been sunk. Congratulations on your victory!";
        }

        if (aiResult == FireResult.WIN)
        {
            return "All your ships have been sunk. Perhaps you will have better luck next time";
        }

        if (playerResult == FireResult.MISS && aiResult == FireResult.MISS)
        {
            return "You have both missed";
        }

        if (playerResult == FireResult.MISS && aiResult == FireResult.HIT)
        {
            return "You missed but the enemy has hit one of our ships";
        }


        if (playerResult == FireResult.MISS && aiResult == FireResult.SINK)
        {
            return "You missed but the enemy has sunk one of our ships";
        }

        if (playerResult == FireResult.HIT && aiResult == FireResult.MISS)
        {
            return "You hit and your enemy has missed";
        }

        if (playerResult == FireResult.HIT && aiResult == FireResult.HIT)
        {
            return "You have both hit ships";
        }


        if (playerResult == FireResult.HIT && aiResult == FireResult.SINK)
        {
            return "You hit the enemy they have sunk one of our ships";
        }

        if (playerResult == FireResult.SINK && aiResult == FireResult.MISS)
        {
            return "You sunk a ship and your enemy has missed";
        }

        if (playerResult == FireResult.SINK && aiResult == FireResult.HIT)
        {
            return "You sunk a ship but have been hit";
        }

        if (playerResult == FireResult.SINK && aiResult == FireResult.SINK)
        {
            return "You have both sunk ships";
        }

        return "IMPOSSIBLE OUTCOME";
    }


    private FireResult ProcessMove(GridSpace target, bool playerMove, string col, Int row)
    {
        List<Ship> enemyShipSet = playerMove ? AIShips : PlayerShips;
        if (target.ship == null)
        {
            target.hitState = HitState.MISS;
            if (playerMove)
            {
                GameObject.Find("allfog/" + col + "/" + row);
            }
            return FireResult.MISS;
        }
        // we at least hit
        target.hitState = HitState.HIT;
        target.ship.hits++;
        if (target.ship.hits == target.ship.size)
        {
            //we at least sunk
            target.ship.sunk = true;
            if (allShipsSunk(enemyShipSet))
            {
                return FireResult.WIN;
            }
            return FireResult.SINK;
        }
        return FireResult.HIT;

    }


    private enum FireResult
    {
        MISS,
        HIT,
        SINK,
        WIN
    }

    private enum GameState
    {
        PLACEMENT,
        FIRING,
        END
    }

    private GameState gameState;

    // Start is called before the first frame update
    void Start()
    {
        gameState = GameState.PLACEMENT;
        userBoard = new GridSpace[8, 8];
        aiBoard = new GridSpace[8, 8];
        setupBoard(userBoard);
        setupBoard(aiBoard);
        randomlyPopulateBoard(aiBoard, AIShips);
        AITargets = generateAITargets(userBoard);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
