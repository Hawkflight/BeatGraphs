using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatGraphs
{
    class BeatWin
    {
        private int winner;
        private int loser;
        private int points;

        public BeatWin(int w, int l, int p)
        {
            winner = w;
            loser = l;
            points = p;
        }
    }
}
