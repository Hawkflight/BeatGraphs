using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatGraphs
{
    class Game
    {
        public string winner;
        public string loser;
        public double pointDiff;

        public Game(string win, string lose, double points)
        {
            winner = win;
            loser = lose;
            pointDiff = points;
        }
    }
}
