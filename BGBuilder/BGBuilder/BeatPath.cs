using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatGraphs
{
    class BeatPath
    {
        private string trail;
        private int length;

        public BeatPath(ArrayList alPath)
        {
            length = alPath.Count;
            trail = "";

            BuildTrail(alPath);
        }

        private void BuildTrail(ArrayList alPath)
        {
            foreach (int sTeam in alPath)
            {
                trail += "/" + sTeam;
            }

            trail = trail.Substring(1);
        }

        // Returns the number of teams in the trail
        public int Length()
        {
            return length;
        }

        // Returns the team found at the specified index of the trail
        public string GetAt(int iIndex)
        {
            return trail.Split('/')[iIndex];
        }

        // Returns the trail string
        public string GetTrail()
        {
            return trail;
        }
    }
}
