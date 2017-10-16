using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatGraphs
{
    class Team
    {
        public int franchiseID;         // This team's FranchiseID according to the database
        public string city;             // The team's city
        public string mascot;           // The team's nickname
        public string abbreviation;     // The team's abbreviation to be displayed on the graphs
        public string shortabbrev;      // A short version of the abbreviation for NCAA Top 25 list
        public string score;            // The team's raw score to determine rankings
        public string conference;       // The conference the team is a member of (used to determine the color of the border of the cell on the graph)
        public string division;         // The division the team is a member of (used to determine the fill color of the cell on the graph)
        public ArrayList ScoreList;     // The amount of wins/points this team has over every other team in the league

        /***************************************************************************************
         * Team::Team
         * 
         * PARAMETERS
         * 
         * int iTeamID
         *     The team's Franchise ID.
         *     
         * string cbLeague
         *     The league this team is a member of
         *     
         * PURPOSE
         * 
         * This Constructor requires a franchise ID and a league marker that will allow us to 
         * look up data about the team from the database.
         * 
         * ************************************************************************************/
        public Team(int iTeamID, string cbLeague)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@FranchiseID", SqlDbType.Int, 64, ParameterDirection.Input, iTeamID);

            if (cbLeague == "NCAAF")
                SQLDBA.ExecuteSqlSP("Select_CFTeam", sqlParam, out sqlDR);
            else if (cbLeague == "BBTC")
                SQLDBA.ExecuteSqlSP("Select_BBTeam", sqlParam, out sqlDR);
            else
                SQLDBA.ExecuteSqlSP("Select_Team", sqlParam, out sqlDR);
            // Select the team's data from the database


            if (sqlDR.HasRows)
            {
                // Populate retrieved data into the member variables
                sqlDR.Read();

                franchiseID = int.Parse(SQLDBA.sqlGet(sqlDR, "FranchiseID"));
                city = SQLDBA.sqlGet(sqlDR, "City");
                mascot = SQLDBA.sqlGet(sqlDR, "Mascot");
                abbreviation = SQLDBA.sqlGet(sqlDR, "Abbreviation");
                conference = SQLDBA.sqlGet(sqlDR, "Conference");
                division = SQLDBA.sqlGet(sqlDR, "Division");
                shortabbrev = SQLDBA.sqlGet(sqlDR, "RefAbbr");
                ScoreList = new ArrayList();
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();
        }

        /***************************************************************************************
         * Team::AddScore
         * 
         * PARAMETERS
         * 
         * int iTeamIndex
         *     The index within the ScoreList of the team to add points to.
         *     
         * double dScore
         *     The amount of points to add over the indicated team.
         *     
         * PURPOSE
         * 
         * When it is determined that this team has earned BeatWins (or points in the case of
         * the Weighted method) over another team, the points are logged in the score list)
         * 
         * ************************************************************************************/
        public void AddScore(int iTeamIndex, double dScore)
        {
            ScoreList[iTeamIndex] = (double)ScoreList[iTeamIndex] + dScore;
        }

        /***************************************************************************************
         * Team::BuildScoreList
         * 
         * PARAMETERS
         * 
         * int iTeamCount
         *     The amount of teams in the league.
         *     
         * PURPOSE
         * 
         * Used to initialize the array list that contains the points this team has over every
         * other team in the league.
         * 
         * ************************************************************************************/
        public void BuildScoreList(int iTeamCount)
        {
            for (int i = 0; i < iTeamCount; i++)
            {
                ScoreList.Add((double)0);
            }
        }
    }
}
