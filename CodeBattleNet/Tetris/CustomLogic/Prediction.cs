using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisClient.CustomLogic
{
    class Prediction
    {
        private Board board;
        private readonly Element element;
        private List<Element> knowNext;
        private Command theBestPath = Command.THINKING;

        private List<PossiblePosition> positions = new List<PossiblePosition>();

        public delegate void PredicionReady(Command command);
        public event PredicionReady OnPredictionReady;

        public Prediction(Board board,Element element,List<Element> knowNext)
        {
            this.board = board;
            this.element = element;
            this.knowNext = knowNext;
        }

        public async Task start()
        {
            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            await Task.Run(() => {
                findPossiblePoints();
                clearFallingElements();
                calculatePaths();
                deleteUreacheble();
                calculateScoresAndSort();
            });

            Board display = positions[0].NextBoard.Clone();
            positions[0].Points.ForEach(u => {
                display.Set(u.X, board.InversionY(u.Y), '=');

            });
            display.Set(positions[0].Anchor.X, board.InversionY(positions[0].Anchor.Y), 'X');

            Console.WriteLine(display.ToString().ToLower().Replace('=', '@').Replace('x', 'X'));
            Console.WriteLine(theBestPath);

            Console.WriteLine(positions[0].Path);
            Console.WriteLine("Last figure " + (DateTimeOffset.Now.ToUnixTimeMilliseconds() - ms));
            Console.WriteLine("              Move: " + (positions[0].ReachType == PossiblePosition.REACH_TYPE.SLIDE ? "SLIDE" : "DROP"));
            Console.WriteLine($"WHY {positions[0].Anchor}, AND NOT ME {positions[1].Anchor}:");
            Console.WriteLine("            Height: " + positions[0].HeigthScore + " - " + positions[1].HeigthScore);
            Console.WriteLine("       Surrounding: " + positions[0].SurroundingScore + " - " + positions[1].SurroundingScore);
            Console.WriteLine("    Blocked points: " + positions[0].BlockedPointsScore * -1 + " - " + positions[1].BlockedPointsScore * -1);
            Console.WriteLine("     lines cleared: " + positions[0].LineBreakScore + " - " + positions[1].LineBreakScore);
            Console.WriteLine("             TOTAL: " + positions[0].TotalScore + " - " + positions[1].TotalScore);

            Console.WriteLine();
            theBestPath = positions[0]?.Path ?? Command.DOWN;
            positions.ForEach(u => Console.Write(u.TotalScore + " "));
            
            notifyGotReults();

            doNextPredictions();
        }

        private void clearFallingElements()
        {
            positions.ForEach(q => { 
                q.NextBoard.PredictCurrentFigurePoints().ToList().ForEach(u => {
                    if(!board.IsOutOfField(u.X, board.InversionY(u.Y)))
                        q.NextBoard.Set(u.X,u.Y, Convert.ToChar(Element.NONE));
                });
            });
        }

        private void findPossiblePoints()
        {
            List<Rotation> rotations = new List<Rotation>();
            rotations.Add(Rotation.CLOCKWIZE_0);

            switch (element)
            {
                case Element.BLUE:
                case Element.GREEN:
                case Element.RED:
                    rotations.Add(Rotation.CLOCKWIZE_270);
                    break;
                case Element.ORANGE:
                case Element.CYAN:
                case Element.PURPLE:
                    rotations.Add(Rotation.CLOCKWIZE_90);
                    rotations.Add(Rotation.CLOCKWIZE_180);
                    rotations.Add(Rotation.CLOCKWIZE_270);
                    break;
                case Element.YELLOW:
                default:
                    break;
            }

            //TODO оптимизировать и не искать в воздухе
            for (int x = 0; x < board.Size; x++)
            {
                for (int y = board.Size - 1; y >= 0; y--)
                {
                    rotations.ForEach(u => {
                        Point anchor = new Point(x, y);
                        Point[] points = FigureRotator.PredictCurrentFigurePoints(u, anchor, element);
                        if (couldBePlaced(points))
                        {
                            positions.Add(new PossiblePosition()
                            {
                                Element = element,
                                Anchor = anchor,
                                Points = points.ToList(),
                                Rotation = u,
                                NextBoard = board.Clone()
                            });
                        }
                    });

                }
            }
        }

        private bool couldBePlaced(Point[] points)
        {
            bool invalidPos = false;

            //x,Point
            Dictionary<int, Point> lowestPoints = new Dictionary<int, Point>();

            points.ToList().ForEach(u =>
            {
                //Проверка выхода за границы
                if (board.IsOutOfField(u.X, u.Y))
                {
                    invalidPos = true;
                    return;
                }

                //Проверка пересечений
                if (board.GetAt(u.X, board.InversionY(u.Y)) != Element.NONE)
                {
                    invalidPos = true;
                    return;
                }

                //Записываем самые нижние точки фигуры для каджого столбца
                if (!lowestPoints.Keys.Contains(u.X))
                    lowestPoints.Add(u.X, u);
                else
                    if (lowestPoints[u.X].Y < u.Y)
                    lowestPoints[u.X] = u;
            });
            if (invalidPos)
                return false;

            foreach (Point p in lowestPoints.Values)
            {
                Point under = p.ShiftTop();
                //Стоит на полу
                if (under.IsOutOf(board.Size))
                    return true;
                //Стоит на фигуре
                if (board.GetAt(under.X, board.InversionY(under.Y)) != Element.NONE)
                    return true;
            }
            return false;
        }

        private void calculatePaths()
        {
            straightDropCheck();
            easySlideCheck();
            //TODO повороты на месте и скольжение
            //Может это проще, чем кажется?

            List<PossiblePosition> slideBros = new List<PossiblePosition>();

            positions.ForEach(u => {
                switch (u.ReachType)
                {
                    case PossiblePosition.REACH_TYPE.STRAIGHT:

                        Command path;
                        if (element != Element.YELLOW)
                        {
                            path = u.Rotation == Rotation.CLOCKWIZE_90 ? Command.ROTATE_CLOCKWISE_90 :
                                    u.Rotation == Rotation.CLOCKWIZE_270 ? Command.ROTATE_CLOCKWISE_270 :
                                    u.Rotation == Rotation.CLOCKWIZE_0 ? Command.ROTATE_CLOCKWISE_180 :
                                    null;
                        }
                        else
                        {
                            path = null;
                        }

                        int xDiff = board.GetCurrentFigurePoint().X - u.Anchor.X;
                        Command side = xDiff > 0 ? Command.LEFT : Command.RIGHT;
                        xDiff = Math.Abs(xDiff);
                        
                        //Маленький костыль
                        for (xDiff += 0; xDiff > 0; xDiff--)
                        {
                            if (path == null)
                                path = side;
                            else
                                path = path.Then(side);
                        }
                        if (path == null)
                            path = Command.DOWN;
                        else
                            path = path.Then(Command.DOWN);
                        u.Path = path;
                        break;

                    case PossiblePosition.REACH_TYPE.SLIDE:
                        if (u.SlideBro.Path == null)
                            slideBros.Add(u);
                        else
                        {
                            u.Path = buildSlidePath(u);
                        }
                        break;
                    case PossiblePosition.REACH_TYPE.FAST_ROTATE:
                    case PossiblePosition.REACH_TYPE.NONE:
                    default:
                        //TODO
                        break;
                }
            });

            //Иногда дpузей надо подождать
            slideBros.ForEach(u => {
                u.Path = buildSlidePath(u);
            });
        }

        private Command buildSlidePath(PossiblePosition issuer)
        {
            Command path = issuer.SlideBro.Path.Then(Command.THINKING);
            int xDiff = issuer.Anchor.X - issuer.SlideBro.Anchor.X;
            Command side = xDiff < 0 ? Command.LEFT : Command.RIGHT;
            xDiff = Math.Abs(xDiff);

            //Маленький костыль
            for (xDiff += 0; xDiff > 0; xDiff--)
            {
                if (path == null)
                    path = side;
                else
                    path = path.Then(side);
            }
            /*if (path == null)
                path = Command.DOWN;
            else
                path = path.Then(Command.DOWN);*/
            return path;
            
        }

        private void deleteUreacheble()
        {
            List<PossiblePosition> useless = new List<PossiblePosition>();
            positions.ForEach(u => {
                if (u.ReachType == PossiblePosition.REACH_TYPE.NONE)
                    useless.Add(u);
            });
            useless.ForEach(u => positions.Remove(u));
        }

        //Проверяет прямое падение
        private void straightDropCheck()
        {
            positions.ForEach(u => {
                bool isClear = true;
                u.Points.ForEach(p => {
                    if (!isClear)
                        return;
                    for(int y = 0; y < p.Y; y++)
                    {
                        if (u.NextBoard.GetAt(p.X, board.InversionY(y)) != Element.NONE && y<p.Y)
                        {
                            isClear = false;
                            return;
                        }
                    }
                });
                u.ReachType = isClear ? PossiblePosition.REACH_TYPE.STRAIGHT : PossiblePosition.REACH_TYPE.NONE;
            });
        }

        private void easySlideCheck()
        {
            positions.ForEach(u => {
                if (u.ReachType == PossiblePosition.REACH_TYPE.STRAIGHT)
                    return;
                PossiblePosition bro = null;
                positions.FindAll(p =>
                p.ReachType == PossiblePosition.REACH_TYPE.STRAIGHT &&
                p.Rotation == u.Rotation &&
                p.Anchor.Y == u.Anchor.Y).ToList().ForEach(p => {
                    if (bro != null)
                        return;
                    bool accessible = true;
                    for(int i = 0; i < 4; i++)
                    {
                        int from = Math.Min(u.Points[i].X, p.Points[i].X);
                        int to = Math.Max(u.Points[i].X, p.Points[i].X);
                        for(int x = from; x <= to; x++)
                        {
                            if(u.NextBoard.GetAt(x,board.InversionY(u.Points[i].Y)) != Element.NONE)
                            {
                                accessible = false;
                                break;
                            }
                        }
                    }
                    if (accessible)
                        bro = p;
                });
                u.SlideBro = bro;
                u.ReachType = bro == null ? PossiblePosition.REACH_TYPE.NONE : PossiblePosition.REACH_TYPE.SLIDE; 
            });
        }

        private void calculateScoresAndSort()
        {
            checkSurroundings();
            updateBoardsAndCountBrokenLines();
            calculateRelativeHeightScore();
            calculateBlockedPoints();

            //Ставим лучший в начало, а худший - в конец
            positions = positions.OrderByDescending(u => u.TotalScore).ToList();
        }

        private void checkSurroundings()
        {
            positions.ForEach(u => {
                List<Point> foundOutside = new List<Point>();
                List<Point> foundInside = new List<Point>();
                u.Points.ForEach(e =>
                {
                    List<List<Point>> found = u.NextBoard.GetNearNotAir(e);

                    found[0].ForEach(q =>
                    {
                        if (!foundOutside.Contains(q))
                            foundOutside.Add(q);
                    });
                    found[1].ForEach(q =>
                    {
                        if (!foundInside.Contains(q))
                            foundInside.Add(q);
                    });
                });
                u.SurroundingScore = ((PossiblePosition.SURROUNDING_SCORE * foundInside.Count) + (PossiblePosition.SURROUNDING_SCORE/2 * foundOutside.Count)) 
                    * (u.ReachType == PossiblePosition.REACH_TYPE.SLIDE? PossiblePosition.SLIDE_BOOSTER : 1);
            });
        }

        public void calculateRelativeHeightScore()
        {
            //индекс в positions,максимальная высота
            Dictionary<int, int> heights = new Dictionary<int, int>();
            int LowerMostPoint = 0; // Координата Y самой низкой "высокой" точки среди всех позиций 
            int HighestPoint = 0; // Положение самой высокой точки в рассматриваемой позиции 

            // словарь хранит координату Y самой высокой точки позиции 
            for (int i = 0; i < positions.Count; i++)
            {
                // Ищем самую высокую точку рассматриваемой позиции
                if (positions[i].Points.Count > 0)
                {
                    HighestPoint = positions[i].Points[0].Y;
                    for (int j = 1; j < positions[i].Points.Count; j++)
                    {
                        if (positions[i].Points[j].Y < HighestPoint) // Знак <, так как начало координат в левом верхнем углу 
                            HighestPoint = positions[i].Points[j].Y;
                        if (positions[i].Points[j].Y <= 1)
                            HighestPoint = PossiblePosition.FORBIDDEN_AT_ANY_COST;
                    }
                }
                else
                {
                    //Уничтожение без остатка привествуется
                    HighestPoint = board.Size -1;
                }
                

                heights.Add(i, HighestPoint);

                // Ищем координату Y самой низкой "высокой" точки среди всех позиций 
                if (LowerMostPoint < HighestPoint)
                    LowerMostPoint = HighestPoint;
            }

            // Когда найдена самая нижняя "высокая" точка, пересчитываем штрафные очки 
            for (int i = 0; i < positions.Count; i++)
                positions[i].HeigthScore = Math.Abs(heights[i] - LowerMostPoint) * PossiblePosition.HEIGHT_SCORE;
        }

        public void calculateBlockedPoints()
        {
            //Будем пользоваться упрощенной формулой
            //Чекаем недоступные снизу блоки
            positions.ForEach(u => {
                int blocked = 0;
                int forbiddenModifier = 0;
                u.Points.ForEach(q => {
                    Point pointer = q.ShiftTop();
                    
                    if (u.Points.Contains(pointer))
                        return;

                    while (pointer.Y < board.Size)
                    {
                        if (board.IsFree(pointer.X, board.InversionY(pointer.Y)))
                        {
                            blocked++;
                            if(pointer.X==0 || pointer.X == board.Size - 1)
                            {
                                forbiddenModifier = PossiblePosition.FORBIDDEN_AT_ANY_COST;
                            }
                        }
                        else
                            break;
                        pointer = pointer.ShiftTop();
                    }

                });

                u.BlockedPointsScore = blocked * PossiblePosition.BLOCKED_POINTS_SCORE - forbiddenModifier;
            });
        }

        public void updateBoardsAndCountBrokenLines()
        {
            positions.ForEach(u => {
                //номер строки, число пустых точек под ней
                Dictionary<int,int> brokenLines = new Dictionary<int, int>();


                //Заполняем пространство точки фигурой
                u.Points.ForEach(q => {
                    u.NextBoard.Set(q.X, board.InversionY(q.Y), Convert.ToChar(u.Element));
                });

                //Сортируем точки, чтобы было удобнее ломать линии сверху-вниз
                var points = u.Points.OrderByDescending(a => a.Y).ToList();

                //Ломаем строки
                points.ForEach(q => {
                    if (!brokenLines.Keys.Contains(q.Y))
                    {
                        if (u.NextBoard.isLineBreaking(q.Y))
                        {
                            int emptyPoints = 0;
                            if (q.Y - 1 >= 0)
                            {
                                for(int x = 0;x< board.Size; x++)
                                {
                                    if (u.NextBoard.GetAt(x, board.InversionY(q.Y - 1)) == Element.NONE)
                                        emptyPoints++;
                                }
                            }
                            if (u.NextBoard.breakLine(q.Y))
                            {
                                brokenLines.Add(q.Y,emptyPoints);
                            }
                        }
                        
                    }
                });

                brokenLines.Keys.ToList().ForEach(e => {
                    points.ForEach(q =>
                    {
                        //Очищаем точки, которые ушли с линиями
                        if (q.Y == e)
                            u.Points.Remove(q);
                        //Я очень надеюсь, что строки тут отсортированы от меньшей к большей
                        //это точно надо тестить TODO
                        //сдвигаем точки ломаемыми линиями вниз
                        else if (q.Y < e)
                            u.Points.First(a => a.Equals(q)).ShiftBottom();
                    });
                });

                if (brokenLines.Count > 1 && brokenLines.Count < 4)
                {
                    if(u.Points.Count == 0 && u.Anchor.Y <= 15)
                    {
                        //Одну линию мало смысла уничтожать
                        u.LineBreakScore =  (brokenLines.Count -1) * PossiblePosition.LINE_BREAK_SCORE;
                    }else if(u.Anchor.Y <= 7)
                    {
                        //Ценим расчищаемые наверху завалы
                        u.LineBreakScore = brokenLines.Count * (brokenLines.Values.Sum() + 1) * (-u.Anchor.Y) * (PossiblePosition.LINE_BREAK_SCORE / 30);
                    }

                }
                else if(brokenLines.Count == 4)
                {
                    //Побольше бы таких
                    u.LineBreakScore = brokenLines.Count * PossiblePosition.LINE_BREAK_SCORE;
                }else if(brokenLines.Count == 1)
                {
                    //смэээpть
                    u.LineBreakScore = PossiblePosition.LINE_BREAK_SCORE * -1;
                }
                
            });
        }

        private void notifyGotReults()
        {
            OnPredictionReady?.Invoke(theBestPath);
        }

        private void doNextPredictions()
        {
            //notнig нere
        }

    }
}
