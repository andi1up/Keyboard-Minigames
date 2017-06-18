using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CUE.NET;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Keyboard.Enums;

/**
 * This piece of code written by http://www.reddit.com/user/mythicmaniac
 */

namespace KeyboardMinigames
{
    public class SnakeGame : Game
    {
        public static int[,] ScreenMap = new int[10, 4] {
            {14, 26, 38, 51},
            {15, 27, 39, 52},
            {16, 28, 40, 53},
            {17, 29, 41, 54},
            {18, 30, 42, 55},
            {19, 31, 43, 56},
            {20, 32, 44, 57},
            {21, 33, 45, 58},
            {22, 34, 46, 59},
            {23, 35, 47, 60}
        };

        public static int[] ProgressMap = new int[6] {
            117,
            118,
            114,
            115,
            110,
            111
        };

        public KeyboardInterface Keyboard;

        public Direction CurrentDirection;
        public Direction LastDirection;
        public Random Random;
        public bool Running;

        public Food Food;
        public SnakePiece Head;


        public int LoseTimer;
        public int WinTimer;
        public int Length;
        public int WinThreshold;

        public readonly int SleepTime = 200;

        public List<Direction> MovementBuffer;

        public SnakeGame()
        {
            Keyboard = new KeyboardInterface(CueSDK.KeyboardSDK);
            MovementBuffer = new List<Direction>();
        }

        public void AddDirectionToBuffer(Direction dir)
        {
            if(dir != LastDirection && !IsOpposite(dir, LastDirection) && WinTimer == 0 && LoseTimer == 0)
            {
                MovementBuffer.Add(dir);
                LastDirection = dir;
            }
        }

        public bool IsOpposite(Direction dir1, Direction dir2)
        {
            return ((short)dir1 ^ (short)dir2) == 0x1000;
        }

        protected override void KeyboardInput(int keycode)
        {
            if (keycode == 37)
                AddDirectionToBuffer(Direction.LEFT);
            if (keycode == 38)
                AddDirectionToBuffer(Direction.UP);
            if (keycode == 39)
                AddDirectionToBuffer(Direction.RIGHT);
            if (keycode == 40)
                AddDirectionToBuffer(Direction.DOWN);
            if (keycode == 27)
            {
                Running = false;
                Application.Exit();
            }
        }

        public void Initialize()
        {
            LoseTimer = 0;
            WinTimer = 0;
            Length = 1;
            WinThreshold = 30;
            Head = new SnakePiece(0, 0);
            CurrentDirection = Direction.RIGHT;
            LastDirection = Direction.RIGHT;
            Random = new Random();
            SpawnFood();
        }

        public override void Run()
        {
            Running = true;
            Initialize();
            var watch = new Stopwatch();
            var time = 0;
            while (Running)
            {
                watch.Restart();
                if (MovementBuffer.Count > 0 && WinTimer == 0 && LoseTimer == 0)
                {
                    for (int i = 0; i < MovementBuffer.Count; i++)
                    {
                        CurrentDirection = MovementBuffer[0];
                        MovementBuffer.RemoveAt(0);
                        Update();
                    }
                }
                else
                {
                    Update();
                }
                Keyboard.Update();
                watch.Stop();
                time = (int)(SleepTime - watch.Elapsed.TotalMilliseconds);
                if (time < 0)
                    time = 0;
                Thread.Sleep(time);
            }
        }

        public void Update()
        {
            if (LoseTimer > 0)
            {
                LoseTimer--;
                if (LoseTimer == 0)
                    Initialize();
                RefreshLoseScreen();
                return;
            }
            if (WinTimer > 0)
            {
                WinTimer--;
                if (WinTimer == 0)
                    Initialize();
                RefreshWinScreen();
                return;
            }

            Head.Move(CurrentDirection);

            var piece = Head.Child;
            while(piece != null)
            {
                if(piece.X == Head.X && piece.Y == Head.Y)
                {
                    GameLose();
                    RefreshLoseScreen();
                    return;
                }
                piece = piece.Child;
            }

            if(Head.X == Food.X && Head.Y == Food.Y)
            {
                Head.Grow();
                Length++;
                SpawnFood();
                if (Length >= WinThreshold)
                {
                    GameWin();
                    RefreshWinScreen();
                    return;
                }
            }

            RefreshScreen();
        }

        public void SpawnFood()
        {
            var occupied = new List<Food>();
            var piece = Head;
            while(piece != null)
            {
                occupied.Add(new Food(piece.X, piece.Y));
                piece = piece.Child;
            }

            var unoccupied = new List<Food>();
            var flag = false;
            for (int i = 0; i < ScreenMap.GetLength(0); i++)
            {
                for(int j = 0; j < ScreenMap.GetLength(1); j++)
                {
                    flag = false;
                    for(int k = 0; k < occupied.Count; k++)
                    {
                        if(occupied[k].X == i && occupied[k].Y == j)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                         unoccupied.Add(new Food(i, j));
                }
            }

            Food = unoccupied[Random.Next(unoccupied.Count)];
        }

        public void GameWin()
        {
            Head = null;
            Food = null;
            var inSecond = 1000f / SleepTime;
            WinTimer = (int)(inSecond * 3);
        }

        public void RefreshWinScreen()
        {
            var rand = new Random();
            var colors = new int[7][];
            colors[0] = new int[] { 255, 0, 0 };
            colors[1] = new int[] { 0, 255, 0 };
            colors[2] = new int[] { 0, 0, 255 };
            colors[3] = new int[] { 255, 255, 0 };
            colors[4] = new int[] { 0, 255, 255 };
            colors[5] = new int[] { 255, 0, 255 };
            colors[6] = new int[] { 255, 255, 255 };

            for (int i = 0; i < 144; i++)
            {
                var index = rand.Next(7);
                Keyboard.SetLed(i, colors[index][0], colors[index][1], colors[index][2]);
            }

        }

        public void GameLose()
        {
            Head = null;
            Food = null;
            var inSecond = 1000f / SleepTime;
            LoseTimer = (int)(inSecond * 3);
        }

        public void RefreshLoseScreen()
        {
            for (int i = 0; i < 144; i++)
            {
                Keyboard.SetLed(i, 255, 0, 0);
            }
        }

        public void RefreshScreen()
        {
            // Set everything to blue
            for (int i = 0; i < 144; i++)
            {
                Keyboard.SetLed(i, 70, 58, 1);
            }

            // Clear level section
            for (int i = 0; i < ScreenMap.GetLength(0); i++)
            {
                for (int j = 0; j < ScreenMap.GetLength(1); j++)
                {
                    Keyboard.SetLed(ScreenMap[i, j], 0, 0, 0);
                }
            }

            // Draw food
            if (Food != null)
                Keyboard.SetLed(ScreenMap[Food.X, Food.Y], 0, 0, 255);

            // Draw snake
            var piece = Head;
            while(piece != null)
            {
                Keyboard.SetLed(ScreenMap[piece.X, piece.Y], 0, 255, 0);
                piece = piece.Child;
            }

            // Draw progress
            var progress = (int)Math.Floor((double)Length / WinThreshold * 6);
            for(int i = 0; i < ProgressMap.Length; i++)
            {
                Keyboard.SetLed(ProgressMap[i], 0, 0, 0);
                if(progress > i) Keyboard.SetLed(ProgressMap[i], 0, 255, 0);
            }

            // Arrow keys
            Keyboard.SetLed(CorsairLedId.UpArrow, 255, 0, 0);
            Keyboard.SetLed(CorsairLedId.DownArrow, 255, 0, 0);
            Keyboard.SetLed(CorsairLedId.LeftArrow, 255, 0, 0);
            Keyboard.SetLed(CorsairLedId.RightArrow, 255, 0, 0);
            
        }
    }
    
    public class Food
    {
        public int X;
        public int Y;

        public Food(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class SnakePiece
    {
        public int PreviousX;
        public int PreviousY;
        public int X;
        public int Y;
        public SnakePiece Parent;
        public SnakePiece Child;

        public const int MapLenX = 10;
        public const int MapLenY = 4;

        public SnakePiece(int x, int y)
        {
            X = x;
            Y = y;
            PreviousX = x - 1;
            PreviousY = Y;
            if (PreviousX < 0)
                PreviousX += MapLenX;
        }

        public void Grow()
        {
            if(Child != null)
            {
                Child.Grow();
                return;
            }

            var piece = new SnakePiece(PreviousX, PreviousY);
            piece.SetParent(this);
        }

        public void Move(Direction direction)
        {
            PreviousX = X;
            PreviousY = Y;

            if (direction == Direction.UP)
                Y -= 1;
            if (direction == Direction.LEFT)
                X -= 1;
            if (direction == Direction.DOWN)
                Y += 1;
            if (direction == Direction.RIGHT)
                X += 1;

            if (X > 9)
                X -= MapLenX;
            if (X < 0)
                X += MapLenX;
            if (Y > 3)
                Y -= MapLenY;
            if (Y < 0)
                Y += MapLenY;

            if (Child != null)
                Child.Move(PreviousX, PreviousY);
        }

        public void Move(int x, int y)
        {
            PreviousX = X;
            PreviousY = Y;
            X = x;
            Y = y;

            if (Child != null)
                Child.Move(PreviousX, PreviousY);
        }

        public void SetParent(SnakePiece parent)
        {
            Parent = parent;
            parent.SetChild(this);
        }

        public void SetChild(SnakePiece child)
        {
            Child = child;
        }
    }

    public enum Direction
    {
        UP = 0x0001,
        LEFT = 0x0010,
        DOWN = 0x1001,
        RIGHT = 0x1010
    }
}
