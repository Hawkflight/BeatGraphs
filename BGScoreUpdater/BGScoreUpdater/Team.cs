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
        public int franchiseID;
        public string city;
        public string mascot;
        public string abbreviation;
        public string score;
        public ArrayList ScoreList;

        public Team(int iTeamID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@FranchiseID", SqlDbType.Int, 64, ParameterDirection.Input, iTeamID);
            SQLDBA.ExecuteSqlSP("Select_Team", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();

                franchiseID = int.Parse(SQLDBA.sqlGet(sqlDR, "FranchiseID"));
                city = SQLDBA.sqlGet(sqlDR, "City");
                mascot = SQLDBA.sqlGet(sqlDR, "Mascot");
                abbreviation = SQLDBA.sqlGet(sqlDR, "Abbreviation");
                ScoreList = new ArrayList();
                score = "0";
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();
        }

        public void AddScore(int iTeamIndex, double dScore)
        {
            ScoreList[iTeamIndex] = (double)ScoreList[iTeamIndex] + dScore;
        }

        public void BuildScoreList(int iTeamCount)
        {
            for (int i = 0; i < iTeamCount; i++)
            {
                ScoreList.Add((double)0);
            }
        }
    }
}
