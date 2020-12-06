using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TetrisClient.CustomLogic
{
    //Его предсказания всегда верны, ага
    class Oracul
    {

        private Command currentCommand = Command.THINKING;
        private long lastTick;
        private List<int> lastDelays = new List<int>();

        public void updateInfo(Board board)
        {
            //TODO обновляем новую известную последнюю фигуру
            //Очищаем все ветки, которые идут мимо нее
            if (lastTick == 0)
            {
                lastTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                int diff = Convert.ToInt32(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTick);
                if (lastDelays.Count == 10)
                {
                    lastDelays.Remove(lastDelays.First());
                }
                lastDelays.Add(diff);
                lastTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (lastDelays.Count > 0)
                {
                    //Console.WriteLine("Avg tick: " + lastDelays.Average());
                    //Console.WriteLine("last tick: " + lastDelays.Last());
                }

            }

            Prediction prediction = new Prediction(board, board.GetCurrentFigureType(), board.GetFutureFigures());
            prediction.OnPredictionReady += registerNewResults;
            prediction.start();
        }

        private void registerNewResults(Command command)
        {
            //TODO когда ветка заканчивает слой вычислений, то мы вызываем этот метод
            currentCommand = command;
        }

        public async Task<Command> getTheBestCommand()
        {
            //TODO вернуть лучшую доступную сейчас команду
            //Убедись, что ты не возвращаешь команду только потому, что 
            //для нее просчитано больше зодов вперед. конкуренция должна быть честной
            //Также мы обрезаем те вычисления, которые идут мимо лучшей ветки
            Command result = Command.THINKING;
            await Task.Run(() => {
                int a = 1;
            });
            Thread.Sleep(100); //Даем время алгоритму
            result = currentCommand;
            currentCommand = Command.THINKING;
            Console.WriteLine("Results.. OK");
            
            return result;
        }
    }
}
