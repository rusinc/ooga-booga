using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisClient.CustomLogic
{
    class PredictionOLD
    {
        private PredictionOLD parent;
        private Element current;
        private List<Element> nextElements = new List<Element>();
        private Command commands;
        private Board beforeBoard;
        //В случае выполнения ходов
        private Board afterBoard;
        private int failedPoints = 0;
        private int clearedLines = 0;
        private int highestPos;

        public PredictionOLD(PredictionOLD parent,Command currentCommands,Command nextCommand) : this(parent.afterBoard)
        {

            //Фигура, 
            //борда (текущая или предсказанная), 
            //известные продолжения (сначала из борды, а потом генерируемыене)
            
            //ищем возможные точки
            //Оцениваем
            //Сортируем их по оптимальности
            //Вызываем для след известной фигуры из первой позиции и так до конца известных
            //Потом вызываем дял остальных
            //Когда все закончили - закончился слой - мы приняли решение
            //Сортируем вторую фигуру по общей оптимальности
            this.parent = parent;
            commands = currentCommands;
            /*
             * алгоpитм 1: Выpавнивание, повоpот и падение
             * Учитываем число запоpотых точек, зачищенных линий и высоту веpхней точки
             * Дpугие фигуpы не учитываются
             */
            /*
             * 1) Проверяем доступные точки (можно расположить)
             * 2) Проверяем возможность падения (для начала, прямо)
             * 3) Снимаем метрики:
             * 3.1) Обхват
             * 3.2) Высота
             * 3.3) Пустоты
             * 3.4) Блокировка канала
             * 3.5) Cy
             * 4) Выбираем лучший вариант
             * TODO комплексная оценка
             */
        }

        public PredictionOLD(Board board)
        {
            beforeBoard = board;
        }

        private int getRotationMaxRelativeHeigth(Rotation rotation,Element element)
        {
            switch (element)
            {
                case Element.BLUE:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 2;
                        case Rotation.CLOCKWIZE_90:
                            return 0;
                        case Rotation.CLOCKWIZE_180:
                            return 0;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 1;
                    }
                case Element.CYAN:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 1;
                        case Rotation.CLOCKWIZE_90:
                            return 0;
                        case Rotation.CLOCKWIZE_180:
                            return 1;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 1;
                    }
                case Element.GREEN:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 0;
                        case Rotation.CLOCKWIZE_90:
                            return 1;
                        case Rotation.CLOCKWIZE_180:
                            return 1;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 1;
                    }
                case Element.ORANGE:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 1;
                        case Rotation.CLOCKWIZE_90:
                            return 1;
                        case Rotation.CLOCKWIZE_180:
                            return 1;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 0;
                    }
                case Element.PURPLE:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 0;
                        case Rotation.CLOCKWIZE_90:
                            return 1;
                        case Rotation.CLOCKWIZE_180:
                            return 1;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 1;
                    }
                case Element.RED:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 0;
                        case Rotation.CLOCKWIZE_90:
                            return 1;
                        case Rotation.CLOCKWIZE_180:
                            return 1;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 1;
                    }
                case Element.YELLOW:
                default:
                    switch (rotation)
                    {
                        case Rotation.CLOCKWIZE_0:
                            return 1;
                        case Rotation.CLOCKWIZE_90:
                            return 1;
                        case Rotation.CLOCKWIZE_180:
                            return 0;
                        case Rotation.CLOCKWIZE_270:
                        default:
                            return 0;
                    }
            }
        }

        private List<Point> availablePoints(Element element, Rotation rotation)
        {
            List<Point> points = new List<Point>();
            //TODO
           // FigureRotator.PredictCurrentFigurePoints();

            return points;
        }

        private Dictionary<Rotation, Command> getPathsTo(Element element,Point currentPos,Point destination)
        {
            Dictionary<Rotation, Command> result = new Dictionary<Rotation, Command>();

            //Проверяем прямое падение
            Dictionary<Rotation, bool> variants = straightDropVariants(element, destination);

            variants.Keys.ToList().ForEach(u => {
                if (variants[u])
                {
                    Command path = Command.THINKING;
                    int xDiff = currentPos.X - destination.X;
                    Command side = xDiff > 0 ? Command.LEFT : Command.RIGHT;
                    //Маленький костыль
                    for (xDiff+=0; Math.Abs(xDiff) > 0; xDiff--)
                    {
                        path.Then(side);
                    }
                    path.Then(Command.DOWN);
                    result.Add(u, path);
                }
            });
            //TODO повороты на месте и скольжение
            //Может это проще, чем кажется?

            return result;
        }

        //Проверяет прямое падение
        private Dictionary<Rotation,bool> straightDropVariants(Element element,Point destination)
        {
            Dictionary<Rotation, bool> result = new Dictionary<Rotation, bool>();

            List<Rotation> rotations = new List<Rotation>();
            rotations.Add(Rotation.CLOCKWIZE_0);
            rotations.Add(Rotation.CLOCKWIZE_90);
            rotations.Add(Rotation.CLOCKWIZE_180);
            rotations.Add(Rotation.CLOCKWIZE_270);

            List<Point[]> possiblePositions = new List<Point[]>();

            rotations.ForEach(u => {
                Point[] points = FigureRotator.PredictCurrentFigurePoints(u,destination,element);
                if (couldBePlaced(points))
                {
                    bool isClear = true;
                    foreach(Point p in points)
                    {
                        for(int y=p.Y; y >= 0; y++)
                        {
                            if (beforeBoard.GetAt(p.X, y) != Element.NONE)
                            {
                                isClear = false;
                                break;
                            }
                        }
                        result.Add(u, isClear);
                    }
                }
            });

            return result;
        }

        private bool couldBePlaced(Point[] points)
        {
            bool invalidPos = false;

            //x,Point
            Dictionary<int, Point> lowestPoints = new Dictionary<int, Point>();

            points.ToList().ForEach(u =>
            {
                //Проверка выхода за границы
                if (beforeBoard.IsOutOfField(u.X, u.Y))
                {
                    invalidPos = true;
                    return;
                }

                //Проверка пересечений
                if (beforeBoard.GetAt(u) != Element.NONE)
                {
                    invalidPos = true;
                    return;
                }

                //Записываем самые нижние точки фигуры для каджого столбца
                if (!lowestPoints.Keys.Contains(u.X))
                    lowestPoints.Add(u.X, u);
                else
                    if (lowestPoints[u.X].Y > u.Y)
                    lowestPoints[u.X] = u;
            });
            if (invalidPos)
                return false;

            foreach (Point p in lowestPoints.Values)
            {
                Point under = p.ShiftBottom();
                //Стоит на полу
                if (under.IsOutOf(beforeBoard.Size))
                    return true;
                //Стоит на фигуре
                if (beforeBoard.GetAt(under) != Element.NONE)
                    return true;
            }
            return false;
        }

    }
}
