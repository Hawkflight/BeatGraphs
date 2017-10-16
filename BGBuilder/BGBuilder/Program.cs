using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using BeatGraphs;

/***************************************************************************************
 * BGBuilder
 * 
 * The purpose of this application is take the recently downloaded scores and build the
 * graphs and web pages based on those scores.  The files are then uploaded to the web
 * server.
 * ************************************************************************************/

namespace BGBuilder
{
    class Program
    {
        #region Member Variables
        private static ArrayList alTeams = new ArrayList();
        private static ArrayList alTeamIDs = new ArrayList();
        private static ArrayList alGames = new ArrayList();
        private static ArrayList alMatrix = new ArrayList();
        private static ArrayList alAllPaths = new ArrayList();
        private static ArrayList alBeatList = new ArrayList();
        private static ArrayList alBeatLoops = new ArrayList();
        private static ArrayList alBeatLoopList = new ArrayList();
        private static ArrayList alCurrentPath = new ArrayList();
        private static ArrayList alFailedLoopTeams = new ArrayList();
        private static ArrayList alLongestPath = new ArrayList();
        private static ArrayList alLossPoints = new ArrayList();
        private static ArrayList alWinPoints = new ArrayList();
        private static ArrayList alCountedPoints = new ArrayList();
        private static StringBuilder sbTrackProgress = new StringBuilder();
        private static EventLog eLog;
        private static string sFilePath;
        private static string cbSeason;
        private static string cbLeague;
        private static string cbMethod;
        private static string cbRange;
        #endregion

        static void Main(string[] args)
        {
            // Set manual run settings.  These will be used in place of any parameter not passed in through a command line arg
            cbSeason = DateTime.Now.Year.ToString();
            cbLeague = "NFL";
            cbMethod = "S";
            cbRange = "7";  // Range is typically the week of the season
            cbSeason = "2016";

            // Target directory for output files
            //sFilePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            sFilePath = @"C:\WebSites\www.beatgraphs.com\";

            // Take command line arguments and override manual defaults
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-l")
                    cbLeague = args[i + 1];
                else if (args[i] == "-s")
                    cbSeason = args[i + 1];
                else if (args[i] == "-m")
                    cbMethod = args[i + 1];
                else if (args[i] == "-r")
                    cbRange = args[i + 1];
                else if (args[i] == "-p")
                    sFilePath = args[i + 1];
            }

            /*
            if (cbLeague != "MLB")
            {
                if (DateTime.Now.DayOfYear < 196)
                {
                    cbSeason = (DateTime.Now.Year - 1).ToString();
                }
            }*/

            /* EVENT LOGGING */
            string sSource = "BGraphs";
            string sLog = "BGraphsLog";
            EventSourceCreationData escdSource = new EventSourceCreationData(sSource, sLog);
            escdSource.MachineName = ".";
            eLog = new EventLog();
            if (!EventLog.SourceExists(sSource, escdSource.MachineName))
                EventLog.CreateEventSource(escdSource);

            eLog.Source = sSource;

            Console.WriteLine("Attempting to load the " + cbSeason + " season for " + cbLeague + "!");
            eLog.WriteEntry("Builder job started: League (" + cbLeague + ") / Year (" + cbSeason + ") / Method (" + cbMethod + ") / Week (" + cbRange + ")", EventLogEntryType.Information, 0, (short)0);

            buildFiles();
            processFiles();
            //eLog.WriteEntry("BUILDER COMPLETED!", EventLogEntryType.Information, 0, (short)0);
        }

        #region Loading Games into Matrix
        /// <summary>
        /// This function needs review: Purpose is to load the games from the database, populate the game matrix, and resolve
        /// BeatLoops by the selected method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void buildFiles()
        {
            alLongestPath.Clear();
            alTeams.Clear();
            alMatrix.Clear();

            LoadScores();
            ResolveLoops();
            CalculateScores();
            if (cbLeague != "NCAAF" || cbMethod == "S") // Longest paths are not available for Iterative or Weighted college pages as they are too long.
                FindLongestPaths();
            //Console.WriteLine(sbTrackProgress.ToString());
            //eLog.WriteEntry(sbTrackProgress.ToString());
        }

        /// <summary> COMPLETE
        /// Loads scores from database and populates the game matrix.
        /// </summary>
        private static void LoadScores()
        {
            LoadTeams(); // Gets the teams for the current season from the DB
            LoadGames(); // Gets the completed games for the current season from the DB.  Should already be populated by BGScoreUpdater.
        }

        /// <summary> COMPLETE
        /// Loads teams into alTeams ArrayList from the selected season and league
        /// </summary>
        private static void LoadTeams()
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[2];
            SqlDataReader sqlDR;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@Year", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbSeason);
            sqlParam[1] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbLeague);
            SQLDBA.ExecuteSqlSP("Select_Teams_By_Year", sqlParam, out sqlDR);
            // Get all teams for the league in the specified year

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    // Add each team to the score matrix and to the team list
                    alMatrix.Add(new Team(int.Parse(SQLDBA.sqlGet(sqlDR, "FranchiseID")), cbLeague));
                    alTeams.Add(int.Parse(SQLDBA.sqlGet(sqlDR, "FranchiseID")));
                }

                // Now that we know how many teams there are, we can add each team's list of scores against the rest of the league, defaulted to 0
                foreach (Team tTeam in alMatrix)
                {
                    tTeam.BuildScoreList(alMatrix.Count);
                }
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();
        }

        /// <summary> COMPLETE
        /// Loads games from the database and enters scores into the game matrix (alMatrix).
        /// </summary>
        private static void LoadGames()
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[3];
            SqlDataReader sqlDR;
            int iTeamAway, iTeamHome;
            double dScoreAway, dScoreHome;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@Year", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbSeason);
            sqlParam[1] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbLeague);
            sqlParam[2] = SQLDBA.CreateParameter("@Range", SqlDbType.NVarChar, 10, ParameterDirection.Input, cbRange);
            SQLDBA.ExecuteSqlSP("Select_Games", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                //int iMaxWeek = -1;
                while (sqlDR.Read())
                {
                    iTeamAway = int.Parse(SQLDBA.sqlGet(sqlDR, "AwayID"));
                    iTeamHome = int.Parse(SQLDBA.sqlGet(sqlDR, "HomeID"));
                    dScoreAway = double.Parse(SQLDBA.sqlGet(sqlDR, "AwayScore"));
                    dScoreHome = double.Parse(SQLDBA.sqlGet(sqlDR, "HomeScore"));

                    if (dScoreAway != dScoreHome)
                    {
                        if (cbMethod == "S" || cbMethod == "I")
                        {
                            if (dScoreAway > dScoreHome)
                            {
                                dScoreAway = 1;
                                dScoreHome = 0;
                            }
                            else
                            {
                                dScoreHome = 1;
                                dScoreAway = 0;
                            }
                        }

                        ((Team)alMatrix[alTeams.IndexOf(iTeamAway)]).AddScore(alTeams.IndexOf(iTeamHome), dScoreAway);
                        ((Team)alMatrix[alTeams.IndexOf(iTeamHome)]).AddScore(alTeams.IndexOf(iTeamAway), dScoreHome);
                    }
                }
            }

            //Reduce teams with lower score than opponent to 0 and higher team by the same amount.
            //Doing this also removes "season splits" also known as "2-team loops".
            for (int i = 0; i < alMatrix.Count; i++)
            {
                for (int j = 0; j < alMatrix.Count; j++)
                {
                    if ((double)((Team)alMatrix[i]).ScoreList[j] >= (double)((Team)alMatrix[j]).ScoreList[i])
                    {
                        ((Team)alMatrix[i]).ScoreList[j] = (double)((Team)alMatrix[i]).ScoreList[j] - (double)((Team)alMatrix[j]).ScoreList[i];
                        ((Team)alMatrix[j]).ScoreList[i] = (double)0;
                    }
                    else
                    {
                        ((Team)alMatrix[j]).ScoreList[i] = (double)((Team)alMatrix[j]).ScoreList[i] - (double)((Team)alMatrix[i]).ScoreList[j];
                        ((Team)alMatrix[i]).ScoreList[j] = (double)0;
                    }
                }
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            //butProcess.Enabled = true;
            Console.WriteLine("Games loaded from database.\r\n");
            //eLog.WriteEntry("Games loaded from database.\r\n");
        }
        #endregion

        #region Find and Resolve Loops
        /// <summary> COMPLETE
        /// This function performs the necessary steps to reduce the game matrix to a state where it has no loops.
        /// Accounting of paths and loops is taken as the process happens.
        /// </summary>
        private static void ResolveLoops()
        {
            int iLoopSize;
            string sTeamA;
            string sTeamB;

            iLoopSize = 3;  // Loops of size 3 are the smallest we can have, so let's start there (Size 2 loops, "head-to-head splits", are reduced automatically in score reporting)
            alAllPaths.Clear();  // Empty the tracker.  This will be cleared for each time we increase the loop size
            sbTrackProgress = new StringBuilder();  // Log for finding the BeatLoops

            // While there are loops of any size
            while (isLoop())
            {
                sbTrackProgress.Append(string.Concat("We\'re starting off with these ambiguous BeatWins for LoopSize ", iLoopSize, ":\r\n"));
                alBeatLoops.Clear();  // Clears the amount of BeatLoops of this size

                // Go through all of the teams
                for (int i = 0; i < alTeams.Count; i++)
                {
                    // We have to clear the current path tracker outside of FindPathHelper because it is recursive.
                    alCurrentPath.Clear();
                    FindPathHelper((int)alTeams[i], (int)alTeams[i], iLoopSize); // Find all BeatLoops of a specific size from a team to itself
                }
                // That will have populated alAllPaths with all of the loops of the size in question

                // For each loop in the list
                for (int i = 0; i < alAllPaths.Count; i++)
                {
                    for (int j = i + 1; j < alAllPaths.Count; j++)
                    {
                        // Compare it to every other loop.  If the two loops contain the same teams, remove the duplicate loop from the list.
                        if (EqualPath(((BeatPath)alAllPaths[i]).GetTrail().Split('/'), ((BeatPath)alAllPaths[j]).GetTrail().Split('/')))
                        {
                            alAllPaths.RemoveAt(j--);
                        }
                    }

                    // Any loops that survive this long are logged, once for the list to be broken, one for the list to report later.
                    alBeatLoops.Add((BeatPath)alAllPaths[i]);
                    alBeatLoopList.Add((BeatPath)alAllPaths[i]);

                    // Record all BeatWins involved in all of the BeatLoops
                    for (int j = 0; j < ((BeatPath)alAllPaths[i]).Length() - 1; j++)
                    {
                        sTeamA = ((BeatPath)alAllPaths[i]).GetAt(j);
                        sTeamB = ((BeatPath)alAllPaths[i]).GetAt(j + 1);

                        // Only add the BeatWin to the list if it's not already there, otherwise, build a list of all links involved in loops at this size
                        if (!InBeatList(sTeamA, sTeamB))
                        {
                            sbTrackProgress.Append(string.Concat(sTeamA, " &rarr; ", sTeamB, "\r\n"));
                            alBeatList.Add(new Game(sTeamA, sTeamB, (((double)((Team)alMatrix[alTeams.IndexOf(int.Parse(sTeamA))]).ScoreList[alTeams.IndexOf(int.Parse(sTeamB))]))));
                        }  // alBeatList is a list of links and their weights that will be used to resolve the loops
                    }
                }

                // Resolve loops as long as there are loops to resolve
                while (alBeatLoops.Count > 0)
                {
                    if (cbMethod == "I")
                        ResolveIterative();
                    else
                        ResolveStandard(); // All other methods use the Standard method to resolve loops, the difference is the weights given to the links
                }
                iLoopSize++; // Increase the loop size for the next pass
                alAllPaths.Clear(); // Clear the list of paths
            }
        }

        /// <summary> COMPLETE
        /// Determines whether or not any loops exist in the matrix.
        /// </summary>
        /// <returns>true if there are loops, false if there are not</returns>
        private static bool isLoop()
        {
            for (int i = 0; i < alTeams.Count; i++)
            {
                if (isPath((int)alTeams[i], (int)alTeams[i]))
                    return true;
            }
            return false;
        }

        /// <summary> COMPLETE
        /// Determines if a BeatPath exists from the origin team to the end team
        /// </summary>
        /// <param name="OriginTeam">The team of origin</param>
        /// <param name="EndTeam">The target team</param>
        /// <returns>true if a path exists, false if not</returns>
        private static bool isPath(int OriginTeam, int EndTeam)
        {
            alCurrentPath.Clear();
            alFailedLoopTeams.Clear();
            return isPathHelper(OriginTeam, OriginTeam, EndTeam, alTeams.Count); // Tries to find a path from the origin team to the end team with a maximum path size of all teams in the league.
        }

        /// <summary> COMPLETE
        /// Recursive function to determine if there is a path from the origin team to the end team.
        /// </summary>
        /// <param name="OriginTeam"></param>
        /// <param name="CurrentTeam"></param>
        /// <param name="EndTeam"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private static bool isPathHelper(int OriginTeam, int CurrentTeam, int EndTeam, int maxLength)
        {
            int iCurrentIndex = alTeams.IndexOf(CurrentTeam);

            if ((((double)((Team)alMatrix[iCurrentIndex]).ScoreList[alTeams.IndexOf(EndTeam)])) > 0)
            {
                return true;
            }
            else
            {
                alCurrentPath.Add(CurrentTeam);

                for (int i = 0; i < ((Team)alMatrix[iCurrentIndex]).ScoreList.Count; i++)
                {
                    if ((alCurrentPath.Count < maxLength) && !alFailedLoopTeams.Contains((int)alTeams[i]))
                    {
                        if (!alCurrentPath.Contains((int)alTeams[i]) && (((double)((Team)alMatrix[iCurrentIndex]).ScoreList[i]) > 0))
                        {
                            if (isPathHelper(OriginTeam, (int)alTeams[i], EndTeam, maxLength))
                                return true;
                        }
                    }
                }

                alFailedLoopTeams.Add(CurrentTeam);
                alCurrentPath.Remove(CurrentTeam);
            }
            return false;
        }

        /// <summary> COMPLETE
        /// Catalogs all paths
        /// </summary>
        /// <param name="CurrentTeam"></param>
        /// <param name="EndTeam"></param>
        /// <param name="iMaxPathSize"></param>
        private static void FindPathHelper(int CurrentTeam, int EndTeam, int iMaxPathSize)
        {
            int iCurrentIndex = alTeams.IndexOf(CurrentTeam);

            alCurrentPath.Add(CurrentTeam);
            if ((((double)((Team)alMatrix[iCurrentIndex]).ScoreList[alTeams.IndexOf(EndTeam)])) > 0)
            {
                alCurrentPath.Add(EndTeam);
                alAllPaths.Add(new BeatPath(alCurrentPath));
                if (alLongestPath.Count > 0 && alCurrentPath.Count > ((ArrayList)alLongestPath[0]).Count)
                    alLongestPath.Clear();
                if (alLongestPath.Count == 0 || alCurrentPath.Count >= ((ArrayList)alLongestPath[0]).Count)
                    alLongestPath.Add(alCurrentPath);
                alCurrentPath.RemoveAt(alCurrentPath.LastIndexOf(EndTeam));
            }

            if (alCurrentPath.Count < iMaxPathSize)
            {
                for (int i = 0; i < ((Team)alMatrix[iCurrentIndex]).ScoreList.Count; i++)
                {
                    if (((double)((Team)alMatrix[iCurrentIndex]).ScoreList[i]) > 0 && !alCurrentPath.Contains((int)alTeams[i]))
                    {
                        FindPathHelper((int)alTeams[i], EndTeam, iMaxPathSize);
                    }
                }
            }
            alCurrentPath.RemoveAt(alCurrentPath.LastIndexOf(CurrentTeam));
        }

        /// <summary> COMPLETE
        /// Tests two paths for equality.  Used to check if two BeatLoops are the same.
        /// </summary>
        /// <param name="PathA"></param>
        /// <param name="PathB"></param>
        /// <returns>If the two paths contain the same teams it returns true, else false</returns>
        private static bool EqualPath(string[] PathA, string[] PathB)
        {
            // We need an easy way to manage the teams in the trail, so we're going to make some local array lists to handle them.
            ArrayList alPathA = new ArrayList();
            ArrayList alPathB = new ArrayList();

            // If the paths don't have the same amount of teams, we know the loops aren't equal, so we can return early.
            if (PathA.Length != PathB.Length)
                return false;
            else
            {
                // Copy the paths into the array lists
                for (int i = 0; i < PathA.Length; i++)
                {
                    alPathA.Add(PathA[i]);
                    alPathB.Add(PathB[i]);
                }

                // All BeatLoops have the same team in the first and last positions.  If they start in different locations, 
                // the duplicates have to be removed in order to properly compare the trails.
                alPathA.RemoveAt(0);
                alPathB.RemoveAt(0);

                // Until Path A is empty (Count should be reduced on each iteration)
                for (int i = 0; i < alPathA.Count; )
                {
                    // If Path A contains the team that's at the beginning of Path B..
                    if (alPathA.Contains((string)alPathB[0]))
                    {
                        // ..remove that team from both paths (reducing Path A's count)
                        alPathA.Remove((string)alPathB[0]);
                        alPathB.Remove((string)alPathB[0]);
                    }
                    else
                    {
                        // If Path B has a team not in Path A, the loops are not equal and we can quit
                        return false;
                    }
                }   // Continue reducing the paths until Path A has Count = 0
            }

            return true;  // If we get here, the paths are equal.
        }

        /// <summary> COMPLETE
        /// Checks to see if the BeatWin of sTeamA->sTeamB exists in alBeatList
        /// </summary>
        /// <param name="sTeamA"></param>
        /// <param name="sTeamB"></param>
        /// <returns></returns>
        private static bool InBeatList(string sTeamA, string sTeamB)
        {
            foreach (Game gBeatWin in alBeatList)
            {
                if (gBeatWin.winner == sTeamA && gBeatWin.loser == sTeamB)
                    return true;
            }
            return false;
        }

        /// <summary> COMPLETE
        /// Resolves loops using the Standard method
        /// </summary>
        private static void ResolveStandard()
        {
            string sTeamA;
            string sTeamB;
            int iTeamA;
            int iTeamB;
            double dMinPoints = FindMinPoints();

            sbTrackProgress.Append(string.Concat("Found minimum points of ", dMinPoints, " and subtracting from all involved BeatLoop strengths.\r\n"));
            sbTrackProgress.Append("These BeatLoops were broken in this pass:\r\n");

            for (int i = 0; i < alBeatList.Count; i++)
            {
                iTeamA = alTeams.IndexOf(int.Parse(((Game)alBeatList[i]).winner));
                iTeamB = alTeams.IndexOf(int.Parse(((Game)alBeatList[i]).loser));
                ((Team)alMatrix[iTeamA]).ScoreList[iTeamB] = ((double)((Team)alMatrix[iTeamA]).ScoreList[iTeamB]) - dMinPoints;
            }

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                if (LoopContainsZero(((BeatPath)alBeatLoops[i])))
                {
                    sbTrackProgress.Append(string.Concat(((BeatPath)alBeatLoops[i]).GetTrail(), "\r\n"));
                    alBeatLoops.RemoveAt(i--);
                }
            }
            sbTrackProgress.Append("Which leaves these ambiguous games on the BeatList:\r\n");
            alBeatList.Clear();

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                for (int j = 0; j < (((BeatPath)alBeatLoops[i]).Length() - 1); j++)
                {
                    sTeamA = ((BeatPath)alBeatLoops[i]).GetAt(j);
                    sTeamB = ((BeatPath)alBeatLoops[i]).GetAt(j + 1);
                    if (!InBeatList(sTeamA, sTeamB))
                    {
                        sbTrackProgress.Append(string.Concat(sTeamA, " &rarr; ", sTeamB, "\r\n"));
                        alBeatList.Add(new Game(sTeamA, sTeamB, (((double)((Team)alMatrix[alTeams.IndexOf(int.Parse(sTeamA))]).ScoreList[alTeams.IndexOf(int.Parse(sTeamB))]))));
                    }
                }
            }
        }

        /// <summary> COMPLETE
        /// Resolves loops using the Iterative method
        /// </summary>
        private static void ResolveIterative()
        {
            string sTeamA;
            string sTeamB;
            int iTeamA;
            int iTeamB;
            double dMinWeight = FindMinWeight();

            sbTrackProgress.Append(string.Concat("Found minimum weight of ", dMinWeight, " and subtracting from all involved BeatLoop strengths.\r\n"));
            sbTrackProgress.Append("These BeatLoops were broken in this pass:\r\n");

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                for (int j = 0; j < (((BeatPath)alBeatLoops[i]).Length() - 1); j++)
                {
                    iTeamA = alTeams.IndexOf(int.Parse(((BeatPath)alBeatLoops[i]).GetAt(j)));
                    iTeamB = alTeams.IndexOf(int.Parse(((BeatPath)alBeatLoops[i]).GetAt(j + 1)));
                    ((Team)alMatrix[iTeamA]).ScoreList[iTeamB] = (double)((Team)alMatrix[iTeamA]).ScoreList[iTeamB] - dMinWeight;
                    if ((double)((Team)alMatrix[iTeamA]).ScoreList[iTeamB] < 0.00001)
                        ((Team)alMatrix[iTeamA]).ScoreList[iTeamB] = (double)0;
                }
            }

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                if (LoopContainsZero(((BeatPath)alBeatLoops[i])))
                {
                    sbTrackProgress.Append(string.Concat(((BeatPath)alBeatLoops[i]).GetTrail(), "\r\n"));
                    alBeatLoops.RemoveAt(i--);
                }
            }
            sbTrackProgress.Append("Which leaves these ambiguous games on the BeatList:\r\n");
            alBeatList.Clear();

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                for (int j = 0; j < (((BeatPath)alBeatLoops[i]).Length() - 1); j++)
                {
                    sTeamA = ((BeatPath)alBeatLoops[i]).GetAt(j);
                    sTeamB = ((BeatPath)alBeatLoops[i]).GetAt(j + 1);
                    if (!InBeatList(sTeamA, sTeamB))
                    {
                        sbTrackProgress.Append(string.Concat(sTeamA, " &rarr; ", sTeamB, "\r\n"));
                        alBeatList.Add(new Game(sTeamA, sTeamB, (((double)((Team)alMatrix[alTeams.IndexOf(int.Parse(sTeamA))]).ScoreList[alTeams.IndexOf(int.Parse(sTeamB))]))));
                    }
                }
            }
        }

        /// <summary> COMPLETE
        /// Finds the value of the BeatWin with the lowest points
        /// </summary>
        /// <returns></returns>
        private static double FindMinPoints()
        {
            int iMinIndex;
            double dMinPoints;
            iMinIndex = 0;
            dMinPoints = ((Game)alBeatList[0]).pointDiff;

            for (int i = 0; i < alBeatList.Count; i++)
            {
                if (((Game)alBeatList[i]).pointDiff < dMinPoints)
                {
                    iMinIndex = i;
                    dMinPoints = ((Game)alBeatList[i]).pointDiff;
                }
            }
            return dMinPoints;
        }

        /// <summary> COMPLETE
        /// Finds the lowest BeatWin weight for the Iterative resolution method
        /// </summary>
        /// <returns></returns>
        private static double FindMinWeight()
        {
            ArrayList alBeatWeights = new ArrayList();
            double dMinWeight = 9999;
            int iTeamA;
            int iTeamB;

            for (int i = 0; i < alMatrix.Count; i++)
            {
                alBeatWeights.Add(new ArrayList());
                foreach (Team tTeam in alMatrix)
                {
                    ((ArrayList)alBeatWeights[i]).Add((double)0);
                }
            }

            for (int i = 0; i < alBeatLoops.Count; i++)
            {
                for (int j = 0; j < (((BeatPath)alBeatLoops[i]).Length() - 1); j++)
                {
                    iTeamA = alTeams.IndexOf(int.Parse(((BeatPath)alBeatLoops[i]).GetAt(j)));
                    iTeamB = alTeams.IndexOf(int.Parse(((BeatPath)alBeatLoops[i]).GetAt(j + 1)));
                    ((ArrayList)alBeatWeights[iTeamA])[iTeamB] = (double)((ArrayList)alBeatWeights[iTeamA])[iTeamB] + 1.0;
                    if (((double)((Team)alMatrix[iTeamA]).ScoreList[iTeamB] / (double)((ArrayList)alBeatWeights[iTeamA])[iTeamB]) < dMinWeight)
                        dMinWeight = (double)((Team)alMatrix[iTeamA]).ScoreList[iTeamB] / (double)((ArrayList)alBeatWeights[iTeamA])[iTeamB];
                }
            }

            return dMinWeight;
        }

        /// <summary> COMPLETE
        /// Examines each BeatWin in a BeatPath and looks for any with a score of 0, meaning it has been eliminated during
        /// loop resolution.
        /// </summary>
        /// <param name="bpPath"></param>
        /// <returns>Returns true if the path contains a BeatWin of score 0, false otherwise</returns>
        private static bool LoopContainsZero(BeatPath bpPath)
        {
            for (int i = 0; i < bpPath.Length() - 1; i++)
            {
                if ((((double)((Team)alMatrix[alTeams.IndexOf(int.Parse(bpPath.GetAt(i)))]).ScoreList[alTeams.IndexOf(int.Parse(bpPath.GetAt(i + 1)))])) == 0)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Longest Path and Score Building
        private static void FindLongestPaths()
        {
            alLongestPath.Clear();
            alCurrentPath.Clear();

            for (int i = 0; i < alMatrix.Count; i++)
            {
                if ((double)alLossPoints[i] == (double)0)
                {
                    alCurrentPath.Add((int)alTeams[i]);
                    for (int j = 0; j < ((Team)alMatrix[i]).ScoreList.Count; j++)
                    {
                        if ((double)((Team)alMatrix[i]).ScoreList[j] > 0)
                        {
                            FindLongPathHelper((int)alTeams[j]);
                        }
                    }
                    alCurrentPath.RemoveAt(alCurrentPath.Count - 1);
                }
            }
        }

        private static void FindLongPathHelper(int CurrentTeam)
        {
            int iCurrentIndex = alTeams.IndexOf(CurrentTeam);

            alCurrentPath.Add(CurrentTeam);
            if (alLongestPath.Count == 0)
            {
                alLongestPath.Add(alCurrentPath.Clone());
            }
            else if (((ArrayList)alLongestPath[0]).Count == alCurrentPath.Count)
            {
                alLongestPath.Add(alCurrentPath.Clone());
            }
            else if (((ArrayList)alLongestPath[0]).Count < alCurrentPath.Count)
            {
                alLongestPath.Clear();
                alLongestPath.Add(alCurrentPath.Clone());
            }

            for (int i = 0; i < ((Team)alMatrix[iCurrentIndex]).ScoreList.Count; i++)
            {
                if (!alCurrentPath.Contains((int)alTeams[i]) && (((double)((Team)alMatrix[iCurrentIndex]).ScoreList[i]) > 0))
                {
                    FindLongPathHelper((int)alTeams[i]);
                }
            }

            alCurrentPath.RemoveAt(alCurrentPath.Count - 1);
        }

        private static void CalculateScores()
        {
            string sScoreString;
            double dMaxScore;
            double dMinScore;
            double dScoreRange;
            double dScore;

            alWinPoints.Clear();
            alLossPoints.Clear();
            alCountedPoints.Clear();

            for (int i = 0; i < alTeams.Count; i++)
            {
                alWinPoints.Add(0.0);
                alLossPoints.Add(0.0);
                alCountedPoints.Add(false);
            }

            CountPaths();
            dMaxScore = (double)alWinPoints[0];
            dMinScore = (double)alLossPoints[0];
            for (int i = 0; i < alWinPoints.Count; i++)
            {
                if ((double)alWinPoints[i] > dMaxScore)
                    dMaxScore = (double)alWinPoints[i];
                if ((double)alLossPoints[i] > dMinScore)
                    dMinScore = (double)alLossPoints[i];
            }
            dScoreRange = dMaxScore + dMinScore;
            for (int i = 0; i < alMatrix.Count; i++)
            {
                sScoreString = "";
                dScore = (double)alWinPoints[i] - (double)alLossPoints[i];
                if (dScore < 0)
                {
                    sScoreString += "-";
                    dScore = (Math.Sqrt(((-dScore) * 100) / dScoreRange) * 100) / 100;
                }
                else
                {
                    dScore = (Math.Sqrt((dScore * 100) / dScoreRange) * 100) / 100;
                }
                sScoreString += dScore;
                ((Team)alMatrix[i]).score = sScoreString;
            }
        }

        private static void CountPaths()
        {
            for (int i = 0; i < alTeams.Count; i++)
            {
                if (!(bool)alCountedPoints[i])
                {
                    CountPathHelper(alTeams.IndexOf((int)alTeams[i]));
                }
            }

            for (int i = 0; i < alTeams.Count; i++)
            {
                alCountedPoints[i] = false;
            }

            for (int i = 0; i < alTeams.Count; i++)
            {
                if (!(bool)alCountedPoints[i])
                {
                    CountLossPathHelper(alTeams.IndexOf((int)alTeams[i]));
                }
            }
        }

        private static void CountPathHelper(int iTeamIndex)
        {
            for (int i = 0; i < alMatrix.Count; i++)
            {
                if ((double)((Team)alMatrix[iTeamIndex]).ScoreList[i] > 0)
                {
                    if (!(bool)alCountedPoints[i])
                        CountPathHelper(i);
                    alWinPoints[iTeamIndex] = (double)alWinPoints[iTeamIndex] + (double)alWinPoints[i] + (double)((Team)alMatrix[iTeamIndex]).ScoreList[i];
                }
            }
            alCountedPoints[iTeamIndex] = true;
        }

        private static void CountLossPathHelper(int iTeamIndex)
        {
            for (int i = 0; i < alMatrix.Count; i++)
            {
                if ((double)((Team)alMatrix[i]).ScoreList[iTeamIndex] > 0)
                {
                    if (!(bool)alCountedPoints[i])
                        CountLossPathHelper(i);
                    alLossPoints[iTeamIndex] = (double)alLossPoints[iTeamIndex] + (double)alLossPoints[i] + (double)((Team)alMatrix[i]).ScoreList[iTeamIndex];
                }
            }
            alCountedPoints[iTeamIndex] = true;
        }
        #endregion

        #region Print Web and Graph Files
        private static void processFiles()
        {
            string sPath = Process.GetCurrentProcess().MainModule.FileName;
            sPath = Path.GetDirectoryName(sPath.Substring(0,sPath.IndexOf("Code")));

            if (!Directory.Exists(sPath + "\\" + cbLeague))
                Directory.CreateDirectory(sPath + "\\" + cbLeague);
            if (!Directory.Exists(sPath + "\\" + cbLeague + "\\" + cbMethod))
                Directory.CreateDirectory(sPath + "\\" + cbLeague + "\\" + cbMethod);
            if (!Directory.Exists(sPath + "\\" + cbLeague + "\\" + cbMethod + "\\" + cbSeason))
                Directory.CreateDirectory(sPath + "\\" + cbLeague + "\\" + cbMethod + "\\" + cbSeason);

            string sImageFile = sPath + "\\" + cbLeague + "\\" + cbMethod + "\\" + cbSeason + "\\" + cbRange + ".png";
            string sCommand = string.Concat(new object[] {"-Tpng -o\"", sImageFile, "\" -Kdot \"", sFilePath + "GraphOut.txt\""});
            printWebContent();
            printGraphFile();

            try
            {
                //Process.Start(sFilePath + "/dot.exe", sCommand);
                if (File.Exists(@"C:\Websites\www.beatgraphs.com\GraphOut.txt"))
                {
                    Process pGraphVis = new Process();
                    pGraphVis.StartInfo.FileName = @"C:\Program Files (x86)\Graphviz2.38\bin\dot.exe";
                    pGraphVis.StartInfo.Arguments = sCommand;
                    pGraphVis.StartInfo.RedirectStandardOutput = true;
                    pGraphVis.StartInfo.UseShellExecute = false;
                    pGraphVis.StartInfo.CreateNoWindow = true;
                    pGraphVis.Start();
                    pGraphVis.WaitForExit();

                    ftpFiles();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Graph creation failed: " + ex.Message);
                Console.Write(ex.StackTrace);
                Console.ReadKey();
                eLog.WriteEntry("Graph creation failed: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }     
        }

        private static string getImage(int iFranchiseID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[2];
            SqlDataReader sqlDR;
            string sImage = @"images/" + cbLeague + "/";

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@Year", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbSeason);
            sqlParam[1] = SQLDBA.CreateParameter("@FranchiseID", SqlDbType.NVarChar, 50, ParameterDirection.Input, iFranchiseID.ToString());
            SQLDBA.ExecuteSqlSP("Select_Team_Image", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                sImage += SQLDBA.sqlGet(sqlDR, "IconURL");
            }

            sqlDR.Close();
            sqlDR.Dispose();
            SQLDBA.Close();
            SQLDBA.Dispose();

            return sImage + ".png";
        }

        private static void ftpFiles()
        {
            try
            {
                Console.WriteLine("Creating League Directory");
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + cbLeague);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                response.Close();
            }
            catch (Exception ex)
            {
                //eLog.WriteEntry("Failed to create league directory: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }
            try
            {
                Console.WriteLine("Creating Method Directory");
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + cbLeague + "/" + cbMethod);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                response.Close();
            }
            catch (Exception ex)
            {
                //eLog.WriteEntry("Failed to create method directory: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }
            try
            {
                Console.WriteLine("Creating Season Directory");
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + cbLeague + "/" + cbMethod + "/" + cbSeason);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                response.Close();
            }
            catch (Exception ex)
            {
                //eLog.WriteEntry("Failed to create season directory: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }

            try
            {
                // Copy the contents of the web file to the request stream.
                if (File.Exists(sFilePath + "/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".php"))
                {
                    Console.WriteLine("Uploading Web File");
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".php");
                    request.Method = WebRequestMethods.Ftp.UploadFile;

                    // This example assumes the FTP site uses anonymous logon.
                    request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");

                    byte[] fileContents = File.ReadAllBytes(sFilePath + "/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".php");

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    Console.WriteLine("Upload Web File Complete, status {0}" + response.StatusDescription);
                    //eLog.WriteEntry("Upload Web File Complete, status {0}" + response.StatusDescription);

                    response.Close();
                }
                else
                {
                    Console.WriteLine("Web File could not be found.  Upload failed.");
                    eLog.WriteEntry("Web File could not be found.  Upload failed.", EventLogEntryType.Error, 0, (short)0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Web File could not be uploaded. " + ex.Message);
                eLog.WriteEntry("Failed to create league directory: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }

            try
            {
                // Copy the contents of the graph file to the request stream.
                if (File.Exists(sFilePath + "/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".png"))
                {
                    Console.WriteLine("Uploading Graph File");
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".png");
                    request.Method = WebRequestMethods.Ftp.UploadFile;

                    // This example assumes the FTP site uses anonymous logon.
                    request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");

                    byte[] fileContents = File.ReadAllBytes(sFilePath + "/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".png");

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    Console.WriteLine("Upload Graph File Complete, status {0}" + response.StatusDescription);
                    //eLog.WriteEntry("Upload Graph File Complete, status {0}" + response.StatusDescription);

                    response.Close();
                }
                else
                {
                    Console.WriteLine("Graph File could not be found.  Upload failed.");
                    eLog.WriteEntry("Web File could not be found.  Upload failed.", EventLogEntryType.Error, 0, (short)0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Graph File could not be uploaded. " + ex.Message);
                eLog.WriteEntry("Graph File could not be uploaded: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
            }
        }

        private static void printGraphFile()
        {
            StringBuilder sbOut = new StringBuilder();
            TextWriter twOut = new StreamWriter(sFilePath + "GraphOut.txt", false);
            ArrayList alScores = new ArrayList();
            ArrayList alTiers = new ArrayList();
            ArrayList alTeamsLeft = new ArrayList();
            string sConfColor = "#000000"; //Default black outline
            string sDivColor = "#FFFFFF"; //Default white fill
            double dMinScore;
            bool bNewTier;

            sbOut.Append("digraph beatgraphs {");
            sbOut.Append("\n\tnode [label=\"\\N\", shape=\"box\", style=\"filled, rounded\", fontsize=\"10\"];");
            if (cbLeague != "NCAAF")
            {
                sbOut.Append("\n\tgraph [nodesep=\"0.1\", ranksep=\"0.3\", size=\"8,12\"];");
            }
            else
            {
                sbOut.Append("\n\tgraph [nodesep=\"0.1\", ranksep=\"0.3\"];");
            }
            sbOut.Append("\n\tedge [arrowsize=\"0.5\"];\n");

            CalculateScores();

            foreach (Team tTeam in alMatrix)
            {
                alScores.Add(double.Parse(tTeam.score));
                alTeamsLeft.Add(tTeam.franchiseID);
                #region Assign Colors
                switch (tTeam.conference)
                {
                    case "EAST":
                    case "AFC":
                    case "AL":
                    case "WALE":
                        sConfColor = "#FF0000"; //Red outline
                        break;
                    case "WEST":
                    case "NFC":
                    case "NL":
                    case "CAMP":
                        sConfColor = "#0000FF"; //Blue outline
                        break;
                    case "ACC":
                        sDivColor = "#FFCCCC"; //Pink fill
                        sConfColor = "#FF0000"; //Red outline
                        break;
                    case "Mid-American":
                        sDivColor = "#FFDECC";
                        sConfColor = "#FF7E00"; //Orange outline
                        break;
                    case "Mountain West":
                        sDivColor = "#D7D1CC";
                        sConfColor = "#775700"; //Brown outline
                        break;
                    case "WAC":
                    case "American Athletic": //WAC dissolved in 2012, replaced by AAC in 2013
                        sDivColor = "#DBDEC7";
                        sConfColor = "#C5BE97"; //Tan outline
                        break;
                    case "Big Ten":
                        sDivColor = "#FFFFCC";
                        sConfColor = "#FFFF00"; //Yellow outline
                        break;
                    case "Big East":
                        sDivColor = "#CCFFCC";
                        sConfColor = "#00FF00"; //Green outline
                        break;
                    case "Sun Belt":
                        sDivColor = "#AAFFC2";
                        sConfColor = "#00FFA2"; //Aqua outline
                        break;
                    case "SEC":
                        sDivColor = "#CCCCFF";
                        sConfColor = "#0000FF"; //Blue outline
                        break;
                    case "Conference USA":
                        sDivColor = "#CCFFFF";
                        sConfColor = "#00FFFF"; //Cyan outline
                        break;
                    case "Pac-12":
                        sDivColor = "#DFB7DE";
                        sConfColor = "#9F279E"; //Purple outline
                        break;
                    case "Big 12":
                        sDivColor = "#FFCCFF";
                        sConfColor = "#FF00FF"; //Magenta outline
                        break;
                    case "Independent":
                    default:
                        sDivColor = "#CCCCCC";
                        sConfColor = "#000000"; //Black outline
                        break;
                }

                switch (tTeam.division)
                {
                    case "AFCE":
                    case "ALE":
                    case "ATL":
                    case "EAST":
                        sDivColor = "#FBB4AE"; //Red fill
                        break;
                    case "AFCS":
                        sDivColor = "#FDDAEC"; //Pink fill
                        break;
                    case "AFCN":
                    case "AFCC":
                    case "ALC":
                    case "SE":
                        sDivColor = "#FED9A6"; //Orange fill
                        break;
                    case "AFCW":
                    case "ALW":
                    case "ADA":
                    case "MET":
                    case "NE":
                        sDivColor = "#FFFFCC"; //Yellow fill
                        break;
                    case "NFCE":
                    case "NLE":
                    case "MID":
                        sDivColor = "#B3E2CD"; //Green fill
                        break;
                    case "NFCS":
                        sDivColor = "#E0ECF4"; //Cyan fill
                        break;
                    case "NFCW":
                    case "NLW":
                    case "PAC":
                    case "WEST":
                    case "SMY":
                        sDivColor = "#CAB2D6"; //Blue fill
                        break;
                    case "NFCN":
                    case "NFCC":
                    case "NLC":
                    case "SW":
                    case "NW":
                        sDivColor = "#9EBCDA"; //Purple fill
                        break;
                    case "CEN":
                        if (tTeam.conference == "EAST")
                            sDivColor = "#FFFFCC"; //Yellow fill
                        else
                            sDivColor = "#B3E2CD"; //Green fill
                        break;
                    case "NOR":
                    case "PAT":
                        if (tTeam.conference == "WALE")
                            sDivColor = "#FBB4AE"; //Red fill
                        else
                            sDivColor = "#B3E2CD"; //Green fill
                        break;
                    default:
                        if (cbLeague != "NCAAF") //Only use default if not for NCAA since those div colors are assigned above.
                        {
                            sDivColor = "#FFFFFF"; //White fill
                        }
                        break;
                }
                #endregion

                if (cbLeague == "NCAAF")
                {
                    sbOut.Append("\n\t\"" + tTeam.abbreviation.Replace(" ", "") + "\" [fillcolor=\"" + sDivColor + "\"][color=\"" + sConfColor + "\"][label=<<TABLE border='0' cellpadding='0' cellspacing='0'><TR><TD>" + tTeam.abbreviation + "</TD></TR></TABLE>>];");
                }
                else
                {
                    sbOut.Append("\n\t\"" + tTeam.abbreviation.Replace(" ", "") + "\" [fillcolor=\"" + sDivColor + "\"][color=\"" + sConfColor + "\"][label=<<TABLE border='0' cellpadding='0' cellspacing='0'><TR><TD><IMG SRC='C:\\WebSites\\www.beatgraphs.com\\" + getImage(tTeam.franchiseID) + "'/></TD></TR><TR><TD>" + tTeam.abbreviation + "</TD></TR></TABLE>>];");
                }
            }
            sbOut.Append("\n");

            #region Determine Graph Tiers
            alScores.Sort();

            alTiers.Add(new ArrayList());

            while (alTeamsLeft.Count > 0)
            {
                ArrayList alTeamsToAdd = new ArrayList();
                dMinScore = (double)alScores[0];

                foreach (Team tTeam in alMatrix)
                {
                    if (double.Parse(tTeam.score) == dMinScore)
                    {
                        alTeamsToAdd.Add(tTeam.franchiseID);
                        alTeamsLeft.Remove(tTeam.franchiseID);
                    }
                }

                while (alScores.Contains(dMinScore))
                    alScores.Remove(dMinScore);

                bNewTier = false;
                foreach (int iFromTeam in alTeamsToAdd)
                {
                    foreach (int iToTeam in (ArrayList)alTiers[0])
                    {
                        if (isPath(iFromTeam, iToTeam))
                        {
                            bNewTier = true;
                        }
                    }
                }

                if (bNewTier)
                    alTiers.Insert(0, new ArrayList());

                foreach (int iTeam in alTeamsToAdd)
                {
                    ((ArrayList)alTiers[0]).Add(iTeam);
                }
            }

            foreach (ArrayList alTier in alTiers)
            {
                sbOut.Append("\n\t{rank=same;");

                foreach (int iTeam in alTier)
                {
                    sbOut.Append(" \"" + ((Team)alMatrix[alTeams.IndexOf(iTeam)]).abbreviation.Replace(" ", "") + "\"");
                }

                sbOut.Append("}");
            }
            #endregion

            sbOut.Append("\n");
            foreach (Team tTeam in alMatrix)
            {
                for (int i = 0; i < tTeam.ScoreList.Count; i++)
                {
                    if ((double)tTeam.ScoreList[i] > 0)
                    {
                        double dTemp = (double)tTeam.ScoreList[i];
                        tTeam.ScoreList[i] = (double)0;
                        if (!isPath(tTeam.franchiseID, (int)alTeams[i])) //If there is no indirect BeatPath, draw this arrow.
                        {
                            sbOut.Append("\n\"" + tTeam.abbreviation.Replace(" ","") + "\"->\"" + ((Team)alMatrix[i]).abbreviation.Replace(" ","") + "\"");
                            if (cbMethod == "I")
                            {
                                if (dTemp >= 1.5)
                                {
                                    sbOut.Append(" [style=bold]");
                                }
                                else if (dTemp < 0.5)
                                {
                                    sbOut.Append(" [style=dotted]");
                                }
                                else if (dTemp < 1)
                                {
                                    sbOut.Append(" [style=dashed]");
                                }
                            }
                            else if (cbMethod == "W")
                            {
                                if (cbLeague == "MLB")
                                {
                                    if (dTemp < 5)
                                    {
                                        sbOut.Append(" [color=red]");
                                    }
                                    else if (dTemp >= 15)
                                    {
                                        sbOut.Append(" [color=blue]");
                                    }
                                }
                                else if (cbLeague == "NBA")
                                {
                                    if (dTemp < 6)
                                    {
                                        sbOut.Append(" [color=red]");
                                    }
                                    else if (dTemp >= 20)
                                    {
                                        sbOut.Append(" [color=blue]");
                                    }
                                }
                                else if (cbLeague == "NFL" || cbLeague == "NCAAF")
                                {
                                    if (dTemp < 7)
                                    {
                                        sbOut.Append(" [color=red]");
                                    }
                                    else if (dTemp >= 21)
                                    {
                                        sbOut.Append(" [color=blue]");
                                    }
                                }
                                else if (cbLeague == "NHL")
                                {
                                    if (dTemp < 2)
                                    {
                                        sbOut.Append(" [color=red]");
                                    }
                                    else if (dTemp >= 4)
                                    {
                                        sbOut.Append(" [color=blue]");
                                    }
                                }
                            }
                            sbOut.Append(";");
                        }
                        tTeam.ScoreList[i] = dTemp;
                    }
                }
            }

            sbOut.Append("\n}");

            twOut.Write(sbOut.ToString());
            twOut.Close();
        }

        private static void printWebContent()
        {
            StringBuilder sbOut = new StringBuilder();
            StringBuilder sbTop5 = new StringBuilder();
            TextWriter twOut = new StreamWriter(sFilePath + "/" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".php", false);

            //Output Longest Paths
            sbOut.Append("<p><br><b>The longest BeatPaths this week are:</b><br>\n");
            foreach (ArrayList alPath in alLongestPath)
            {
                for (int i = 0; i < alPath.Count; i++)
                {
                    sbOut.Append(((Team)alMatrix[alTeams.IndexOf((int)alPath[i])]).abbreviation);
                    if (i + 1 < alPath.Count)
                        sbOut.Append(" &rarr; ");
                    else
                        sbOut.Append("<br>\n");
                }
            }

            //Output Winless and Lossless Teams
            sbOut.Append("<br><b>These teams have no surviving BeatLosses:</b><br>\n");
            bool bLess, bFirst = true;
            foreach (Team tTeam in alMatrix)
            {
                int iMyIndex = (int)alTeams.IndexOf(tTeam.franchiseID);
                bLess = true;
                foreach (Team tTeam2 in alMatrix)
                {
                    if ((double)tTeam2.ScoreList[iMyIndex] > 0)
                    {
                        bLess = false;
                        continue;
                    }
                }

                if (bLess)
                {
                    if (!bFirst)
                        sbOut.Append(", " + tTeam.abbreviation);
                    else
                    {
                        sbOut.Append(tTeam.abbreviation);
                        bFirst = false;
                    }
                }
            }

            sbOut.Append("\n<br>\n<br><b>These teams have no surviving BeatWins:</b><br>\n");
            bFirst = true;
            foreach (Team tTeam in alMatrix)
            {
                bLess = true;
                foreach (double dScore in tTeam.ScoreList)
                {
                    if (dScore > 0)
                    {
                        bLess = false;
                        continue;
                    }
                }

                if (bLess)
                {
                    if (!bFirst)
                        sbOut.Append(", " + tTeam.abbreviation);
                    else
                    {
                        sbOut.Append(tTeam.abbreviation);
                        bFirst = false;
                    }
                }
            }

            //Output BeatLoops
            sbOut.Append("\n<br>\n<br><b>These were the BeatLoops resolved:</b><br>\n");
            foreach (BeatPath bpLoops in alBeatLoopList)
            {
                for (int i = 0; i < bpLoops.Length(); i++)
                {
                    if (i > 0)
                        sbOut.Append(" &rarr; ");
                    sbOut.Append(((Team)alMatrix[alTeams.IndexOf(int.Parse(bpLoops.GetAt(i)))]).abbreviation);
                }
                sbOut.Append("<br>\n");
            }
            sbOut.Append("\n</p></div><br>\n");
            sbOut.Append("<div class='pagearea'>\n");
            sbOut.Append("<div class='pagerow'>\n");
            sbOut.Append("<div class='rankarea'>\n");
            sbOut.Append("<div class='scoretable'>\n");
            sbOut.Append("<div class='scorerow'>\n");
            sbOut.Append("<div class='scoresampleheader'>Rank</div>\n");
            sbOut.Append("<div class='scoresampleheader'>Team</div>\n");
            sbOut.Append("<div class='scoresampleheader'>Score</div>\n");
            sbOut.Append("<div class='scoresampleheader'>Out</div>\n");
            sbOut.Append("<div class='scoresampleheader'>In</div>\n</div>\n");

            string sMethod = "Standard";
            if (cbMethod == "I")
                sMethod = "Iterative";
            else if (cbMethod == "W")
                sMethod = "Weighted";

            sbTop5.Append("<div class='top5'>\n");
            if (cbLeague == "NCAAF")
                sbTop5.Append("<a href=\"<? echo (\"graphs.php?league=" + cbLeague + "&method=" + cbMethod + "\"); ?>\"><div class='top5header'>" + sMethod + "<div class='toptop'> Top 25</div></div></a>\n");
            else
                sbTop5.Append("<a href=\"<? echo (\"graphs.php?league=" + cbLeague + "&method=" + cbMethod + "\"); ?>\"><div class='top5header'>" + sMethod + "<div class='toptop'> Top 5</div></div></a>\n");
            sbTop5.Append("<div class='top5row'><div class='top5subhead'>#</div><div class='top5subhead'>Team</div><div class='top5subhead'>Score</div></div>");
            
            twOut.Write(sbOut.ToString());
            sbOut.Replace("\n", Environment.NewLine);

            sbOut = new StringBuilder();

            int iRank = 0, iStored = 0;
            double dLastScore = 0;
            for (int i = 0; i < alMatrix.Count; i++)
            {
                int iMaxindex = getMaxIndex();

                sbOut.Append("\n<div class='scorerow'><div class='scoresamplecell'>");
                if (double.Parse(((Team)alMatrix[iMaxindex]).score) == dLastScore)
                {
                    iStored++;
                }
                else
                {
                    iRank += iStored + 1;
                    iStored = 0;
                }
                sbOut.Append(iRank + "</div><div class='scoresamplecell'>");
                if (cbLeague == "NCAAF")
                {
                    sbOut.Append(((Team)alMatrix[iMaxindex]).abbreviation + "</div><div class='scoresamplecell'>");
                }
                else
                {
                    sbOut.Append("<img src='" + getImage(((Team)alMatrix[iMaxindex]).franchiseID) + "'></div><div class='scoresamplecell'>");
                }
                sbOut.Append((Math.Round(double.Parse(((Team)alMatrix[iMaxindex]).score), 2).ToString("0.00")) + "</div><div class='scoresamplecell'>");
                sbOut.Append(nFormat(Math.Round((double)alWinPoints[iMaxindex], 2)) + "</div><div class='scoresamplecell'>");
                sbOut.Append(nFormat(Math.Round((double)alLossPoints[iMaxindex], 2)) + "</div></div>");

                if (i < 25 && cbLeague == "NCAAF")
                {
                    sbTop5.Append("\n<div class='top5row'><div class='top5cell'>");
                    sbTop5.Append(iRank + "</div><div class='top5cell top5midcell'>");
                    sbTop5.Append(((Team)alMatrix[iMaxindex]).shortabbrev + "</div><div class='top5cell'>");
                    sbTop5.Append((Math.Round(double.Parse(((Team)alMatrix[iMaxindex]).score), 2).ToString("0.00")) + "</div></div>");
                }
                else if (i < 5)
                {
                    sbTop5.Append("\n<div class='top5row'><div class='top5cell'>");
                    sbTop5.Append(iRank + "</div><div class='top5cell top5midcell'>");
                    sbTop5.Append("<img src='" + getImage(((Team)alMatrix[iMaxindex]).franchiseID) + "' /></div><div class='top5cell'>");
                    sbTop5.Append((Math.Round(double.Parse(((Team)alMatrix[iMaxindex]).score), 2).ToString("0.00")) + "</div></div>");
                }

                dLastScore = double.Parse(((Team)alMatrix[iMaxindex]).score);
                ((Team)alMatrix[iMaxindex]).score = "-9999999999999";
            }
            sbOut.Append("\n</div></div>");
            sbOut.Append("\n<div class='grapharea'>");
            sbOut.Append("\n<img src='" + cbLeague + "/" + cbMethod + "/" + cbSeason + "/" + cbRange + ".png' />");
            sbOut.Append("\n</div></div></div>");

            sbTop5.Append("\n</div>");
            sbTop5 = sbTop5.Replace("\n", Environment.NewLine);
            TextWriter tw5Out = new StreamWriter(sFilePath + "/" + cbLeague + "_" + cbMethod + ".php", false);
            tw5Out.Write(sbTop5.ToString());
            tw5Out.Close();
            
            twOut.Write(sbOut.ToString());
            sbOut.Replace("\n", Environment.NewLine);
            //tbOut.Text += sbOut.ToString();
            twOut.Close();
        }

        private static string nFormat(double dNum)
        {
            if (dNum == 0)
                return dNum.ToString("0");
            else if (Math.Abs(dNum) < 1)
                return dNum.ToString("0.00");
            else if (Math.Abs(dNum) < 10)
                return dNum.ToString("0.0");
            else
                return dNum.ToString("0");
        }

        private static int getMaxIndex()
        {
            int iMaxIndex = 0;
            double dMaxScore = double.Parse(((Team)alMatrix[iMaxIndex]).score);

            for (int i = 1; i < alMatrix.Count; i++)
            {
                if (double.Parse(((Team)alMatrix[i]).score) > dMaxScore)
                {
                    iMaxIndex = i;
                    dMaxScore = double.Parse(((Team)alMatrix[iMaxIndex]).score);
                }
            }

            return iMaxIndex;
        }
        #endregion
    }
}
