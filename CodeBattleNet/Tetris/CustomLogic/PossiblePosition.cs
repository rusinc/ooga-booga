using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisClient.CustomLogic
{
    class PossiblePosition
    {
        public const int FORBIDDEN_AT_ANY_COST = -999;

        public const double SURROUNDING_SCORE = 1; //Больше очков?
        public const double HEIGHT_SCORE = 2;
        public const double BLOCKED_POINTS_SCORE = 15; //Настроить так, чтобы можно было использовать для автодетекта канала?
        public const double LINE_BREAK_SCORE = 150; // Выдается, только если уничтожить фигуру

        public const double SLIDE_BOOSTER = 1.0;

        public const double TETRIS_COLUMN_BLOCK_SCORE = 0; //Удалить?

        public PossiblePosition SlideBro { get; set; }

        public enum REACH_TYPE
        {
            STRAIGHT,
            FAST_ROTATE,
            SLIDE,
            NONE
        }

        #region Фигура. Положение и занимаемое пространство
        public Point Anchor { get; set; }
        public List<Point> Points { get; set; }
        public Rotation Rotation { get; set; }
        public Element Element { get; set; }
        #endregion

        //Стакан после установки фигуры (могут быть сломаны строки или просто добавятся блоки)
        public Board NextBoard { get; set; }

        #region Доступность и путь
        public REACH_TYPE reachType = REACH_TYPE.NONE;

        public REACH_TYPE ReachType
        {
            get
            {
                return reachType;
            }
            set
            {
                if (reachType == REACH_TYPE.NONE || value == REACH_TYPE.STRAIGHT)
                    reachType = value;
            }
        }

        public Command Path { get; set; }
        #endregion

        #region Очки. 
        private double heigthScore = 0;
        private double surroundingScore = 0;
        private double blockedPointsScore = 0;
        private double tetrisColumnBlockScore = 0;
        private double lineBreakScore = 0;

        public double SurroundingScore {
            get {
                return surroundingScore;
            }
            set
            {
                surroundingScore = value;
                updateTotalScore();
            }
        }
        public double HeigthScore
        {
            get
            {
                return heigthScore;
            }
            set
            {
                heigthScore = value;
                updateTotalScore();
            }
        }
        public double BlockedPointsScore
        {
            get
            {
                return blockedPointsScore;
            }
            set
            {
                blockedPointsScore = value;
                updateTotalScore();
            }
        }
        public double TetrisColumnBlockScore
        {
            get
            {
                return tetrisColumnBlockScore;
            }
            set
            {
                tetrisColumnBlockScore = value;
                updateTotalScore();
            }
        }
        public double LineBreakScore
        {
            get
            {
                return lineBreakScore;
            }
            set
            {
                lineBreakScore = value;
                updateTotalScore();
            }
        }
        public double TotalScore { get; set; }

        private void updateTotalScore()
        {
            TotalScore =
                + SurroundingScore
                + LineBreakScore
                - HeigthScore
                - BlockedPointsScore
                - TetrisColumnBlockScore;
        }
        #endregion
    }
}
