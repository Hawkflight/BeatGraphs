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
using System.Globalization;

/***************************************************************************************
 * BGScoreUpdater
 * 
 * The purpose of this application is to retrieve nightly scores from the appropriate
 * SPORT-reference.com webpage and enter them into the local database.  The entry of
 * these scores are done at approximately 10 AM Eastern time to allow the site to update
 * their scores.  The four sports have their scores taken in 4 separate jobs staggered
 * 5 minutes apart.
 * ************************************************************************************/

namespace BGScoreUpdater
{
    class Program
    {
        const int NON_DIV_1A = 125;// Initialization

        static void Main(string[] args)
        {
            string cbSeason2 = DateTime.Now.Year.ToString();
            string cbLeague2 = "MLB";
            //cbSeason2 = "2015";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-l")
                    cbLeague2 = args[i + 1];
                else if (args[i] == "-s")
                    cbSeason2 = args[i + 1];
            }

            if (cbLeague2 != "MLB")
            {
                if (DateTime.Now.DayOfYear < 196)
                {
                    cbSeason2 = (DateTime.Now.Year - 1).ToString();
                }
            }

            /* EVENT LOGGING */
            string sSource = "BGraphs";
            string sLog = "BGraphsLog";
            EventSourceCreationData escdSource = new EventSourceCreationData(sSource, sLog);
            escdSource.MachineName = ".";
            EventLog eLog = new EventLog();
            if (!EventLog.SourceExists(sSource, escdSource.MachineName))
                EventLog.CreateEventSource(escdSource);

            eLog.Source = sSource;

            eLog.WriteEntry("ScoreUpdater job started: " + cbSeason2 + " " + cbLeague2 + ".  Attempting to load data.", EventLogEntryType.Information, 0, (short)0);
 
            Console.WriteLine("Attempting to load the " + cbSeason2 + " season for " + cbLeague2 + "!");

            loadGames(cbSeason2, cbLeague2, eLog);
        }

        #region HTTP Request Score Loading
        /// <summary> COMPLETE
        /// This function populates the game table with the scores from the selected league and season.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void loadGames(string cbSeason2, string cbLeague2, EventLog eLog)
        {
            bool bAbort = false;
            string sRange = "", sTeam1 = "", sTeam2 = "";
            string sScore1 = "", sScore2 = "";
            int iScore1, iScore2, iSeasonID, iTeam1ID, iTeam2ID;

            #region Log NCAA Football Games
            if (cbLeague2 == "NCAAF")
            {
                string sHTML = "";
                try
                {
                    HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.sports-reference.com/cfb/years/" + cbSeason2 + "-schedule.html");
                    HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                    StreamReader srReader = new StreamReader(hwResponse.GetResponseStream());

                    try
                    {
                        sHTML = srReader.ReadToEnd();
                    }
                    catch
                    {
                        Console.WriteLine("Connection cannot be established, please try again.");
                        eLog.WriteEntry("Connection to NCAAF source data could not be established.", EventLogEntryType.Error, 100);
                        return;
                    }
                    srReader.Close();
                }
                catch
                {
                    Console.WriteLine("The " + cbSeason2 + " NCAAF season has not yet started.");
                    eLog.WriteEntry("The " + cbSeason2 + " NCAAF season data is not available.", EventLogEntryType.Warning, 400);
                    bAbort = true;
                    return;
                }

                sHTML = sHTML.Substring(sHTML.IndexOf("<tbody>") + 7);
                sHTML = sHTML.Substring(0, sHTML.IndexOf(@"</tbody>"));

                ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"</tr>"));

                iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
                ClearCFSeason(iSeasonID);

                foreach (string sGame in alGames)
                {
                    if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n" || sGame.Contains("Playoffs"))
                        continue;

                    ArrayList alLines = new ArrayList(Regex.Split(sGame, "/td><td"));

                    if ((string)alLines[1] == "")
                        alLines.RemoveAt(1);

                    sRange = parseLine(alLines[0]); //Week

                    sTeam1 = parseLine(alLines[4]); //Winning Team
                    sTeam2 = parseLine(alLines[7]); //Losing Team
                    if (parseLine(alLines[5]) == "" || parseLine(alLines[8]) == "") //Game hasn't been played
                        continue;
                    iScore1 = int.Parse(parseLine(alLines[5])); //Winning Score
                    iScore2 = int.Parse(parseLine(alLines[8])); //Losing Score      
                    iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                    iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                    
                    if (iTeam1ID == -1 && iTeam2ID == -1)
                    {
                        bAbort = true;
                        break;
                    }
                    else if (iTeam1ID == -1)
                    {
                        iTeam1ID = NON_DIV_1A;
                    }
                    else if (iTeam2ID == -1)
                    {
                        iTeam2ID = NON_DIV_1A;
                    }

                    InsertCFGame(iSeasonID, int.Parse(sRange), iTeam1ID, iScore1, iTeam2ID, iScore2);
                }
            }
            #endregion
            #region Log NFL Games
            else if (cbLeague2 == "NFL")
            {
                string sHTML = "";

                try
                {
                    HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.pro-football-reference.com/years/" + cbSeason2 + "/games.htm");
                    HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                    StreamReader srReader = new StreamReader(hwResponse.GetResponseStream());

                    try
                    {
                        sHTML = srReader.ReadToEnd();
                    }
                    catch
                    {
                        Console.WriteLine("Connection cannot be established, please try again.");
                        eLog.WriteEntry("Connection to NFL source data could not be established.", EventLogEntryType.Error, 100);
                        return;
                    }
                    srReader.Close();

                    if (sHTML.Contains("404 - File Not Found"))
                    {
                        Console.WriteLine("The " + cbSeason2 + " NFL season has not yet started.");
                        eLog.WriteEntry("The " + cbSeason2 + " NFL season page does not exist.", EventLogEntryType.Warning, 400);
                        bAbort = true;
                    }
                }
                catch
                {
                    Console.WriteLine("The " + cbSeason2 + " NFL season has not yet started.");
                    eLog.WriteEntry("The " + cbSeason2 + " NFL season data is not available.", EventLogEntryType.Warning, 400);
                    bAbort = true;
                    return;
                }

                sHTML = sHTML.Substring(sHTML.IndexOf("<tbody>") + 7);
                sHTML = sHTML.Substring(0, sHTML.IndexOf(@"</tbody>"));

                ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"</tr>"));

                iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
                ClearSeason(iSeasonID);

                foreach (string sGame in alGames)
                {
                    if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n" || sGame.Contains("Playoffs"))
                        continue;
                    
                    ArrayList alLines = new ArrayList(Regex.Split(sGame.Replace("/th>", "/td>"), "/td><"));

                    if ((string)alLines[1] == "")
                        alLines.RemoveAt(1);

                    sRange = parseLine(alLines[0]); //Week

                    if (sRange == "WildCard")
                        sRange = "501";
                    else if (sRange == "Division")
                        sRange = "502";
                    else if (sRange == "ConfChamp")
                        sRange = "503";
                    else if (sRange == "SuperBowl")
                        sRange = "504";

                    if (parseLine(alLines[9]) == "")
                        continue;

                    sTeam1 = parseLine(alLines[4]); //Winning Team
                    sTeam2 = parseLine(alLines[6]); //Losing Team
                    iScore1 = int.Parse(parseLine(alLines[8])); //Winning Score
                    iScore2 = int.Parse(parseLine(alLines[9])); //Losing Score         
                    iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                    iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                    if (iTeam1ID == -1 || iTeam2ID == -1)
                    {
                        bAbort = true;
                        break;
                    }

                    InsertGame(iSeasonID, int.Parse(sRange), iTeam1ID, iScore1, iTeam2ID, iScore2);
                    
                }
            }
            #endregion
            #region Log NHL Games
            else if (cbLeague2 == "NHL")
            {
                string sHTML = "", sHTMLPlayoffs;
                int iStartDay = -1;
                if (cbSeason2 == "2004")
                {
                    Console.WriteLine("The 2004-05 NHL season was cancelled.");
                    return;
                }

                try
                {
                    HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.hockey-reference.com/leagues/NHL_" + (int.Parse(cbSeason2) + 1) + "_games.html");
                    HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                    StreamReader srReader = new StreamReader(hwResponse.GetResponseStream());

                    try
                    {
                        sHTML = srReader.ReadToEnd();
                    }
                    catch
                    {
                        Console.WriteLine("Connection cannot be established, please try again.");
                        eLog.WriteEntry("Connection to NHL source data could not be established.", EventLogEntryType.Error, 101);
                        return;
                    }
                    srReader.Close();

                    if (sHTML.Contains("404 - File Not Found"))
                    {
                        Console.WriteLine("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NHL season has not yet started.");
                        eLog.WriteEntry("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NHL season page does not exist.", EventLogEntryType.Warning, 401);
                        bAbort = true;
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NHL season has not yet started.");
                    eLog.WriteEntry("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NHL season data is not available.", EventLogEntryType.Warning, 401);
                    return;
                }

                sHTMLPlayoffs = sHTML;
                sHTML = sHTML.Substring(sHTML.IndexOf("<tbody>") + 7);
                sHTML = sHTML.Substring(0, sHTML.IndexOf(@"</tbody>"));

                ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"</tr>"));

                iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
                ClearSeason(iSeasonID);

                foreach (string sGame in alGames)
                {
                    if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n")
                        continue;

                    ArrayList alLines = new ArrayList(Regex.Split(sGame, "><td"));

                    if ((string)alLines[1] == "")
                        alLines.RemoveAt(1);

                    sRange = parseLine(alLines[0]); //Week

                    sTeam1 = parseLine(alLines[1]); //Winning Team
                    sTeam2 = parseLine(alLines[3]); //Losing Team
                    sScore1 = parseLine(alLines[2]); //Winning Score
                    sScore2 = parseLine(alLines[4]); //Losing Score
                    if (sScore1 == "" || sScore2 == "")
                        continue;
                    iScore1 = int.Parse(sScore1);
                    iScore2 = int.Parse(sScore2);
                    iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                    iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                    if (iTeam1ID == -1 || iTeam2ID == -1)
                    {
                        bAbort = true;
                        break;
                    }
                    DateTime dtGame = DateTime.Parse(sRange);
                    int iDayOfYear = dtGame.DayOfYear;
                    if (iDayOfYear < 225) //225 is an arbitary number chosen to represent the a split during the offseason
                    {
                        if (DateTime.IsLeapYear(dtGame.Year - 1))
                            iDayOfYear += 366;
                        else
                            iDayOfYear += 365;
                    }

                    if (iStartDay == -1)
                        iStartDay = iDayOfYear;

                    iDayOfYear = iDayOfYear - iStartDay;
                    iDayOfYear = (iDayOfYear / 7) + 1;

                    InsertGame(iSeasonID, iDayOfYear, iTeam1ID, iScore1, iTeam2ID, iScore2);
                }

                //Process Playoff Games
                sHTMLPlayoffs = sHTMLPlayoffs.Substring(sHTMLPlayoffs.IndexOf("</tbody>"));
                if (sHTMLPlayoffs.IndexOf("<tbody>") > 0)
                {
                    sHTMLPlayoffs = sHTMLPlayoffs.Substring(sHTMLPlayoffs.IndexOf("<tbody>") + 7);
                    sHTMLPlayoffs = sHTMLPlayoffs.Substring(0, sHTMLPlayoffs.IndexOf("</tbody>"));
                    SortedList slMatchups = new SortedList();
                    alGames = new ArrayList(Regex.Split(sHTMLPlayoffs, @"</tr>"));
                    int iRange = 0;

                    foreach (string sGame in alGames)
                    {
                        if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n")
                            continue;

                        ArrayList alLines = new ArrayList(Regex.Split(sGame, "><td"));

                        if ((string)alLines[0] == "")
                            alLines.RemoveAt(1);

                        sTeam1 = parseLine(alLines[1]); //Winning Team
                        sTeam2 = parseLine(alLines[3]); //Losing Team
                        sScore1 = parseLine(alLines[2]); //Winning Score
                        sScore2 = parseLine(alLines[4]); //Losing Score

                        if (sScore1 == "" || sScore2 == "")
                            continue;
                        iScore1 = int.Parse(sScore1);
                        iScore2 = int.Parse(sScore2);
                        iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                        iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                        if (iTeam1ID == -1 || iTeam2ID == -1)
                        {
                            bAbort = true;
                            break;
                        }

                        if (!slMatchups.Contains(iTeam1ID + "|" + iTeam2ID))
                        {
                            iRange = getRange(slMatchups, iTeam1ID, iTeam2ID);

                            slMatchups.Add(iTeam1ID + "|" + iTeam2ID, iRange);
                            slMatchups.Add(iTeam2ID + "|" + iTeam1ID, iRange);
                        }
                        else
                        {
                            iRange = (int)slMatchups[iTeam1ID + "|" + iTeam2ID];
                        }

                        if (iSeasonID >= 158 && iSeasonID <= 162) //1970-1974 NHL seasons had 3 playoff rounds instead of 4
                            iRange++;
                        
                        InsertGame(iSeasonID, iRange, iTeam1ID, iScore1, iTeam2ID, iScore2);
                    }
                }
            }
            #endregion
            #region Log NBA Games
            else if (cbLeague2 == "NBA")
            {
                string sHTML = "", sHTMLPlayoffs = "";
                int iStartDay = -1;
                string strMonth;
                bool bInPlayoffs = false;
                bool bClearedSeason = false;
                SortedList slMatchups = new SortedList();

                for (int iMonthTracker = 10; iMonthTracker != 7; iMonthTracker++)
                {
                    if (iMonthTracker == 13)
                        iMonthTracker = 1;

                    try
                    {
                        strMonth = "-" + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(iMonthTracker).ToLower();
                        HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.basketball-reference.com/leagues/NBA_" + (int.Parse(cbSeason2) + 1) + "_games" + strMonth + ".html");
                        HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                        StreamReader srReader = new StreamReader(hwResponse.GetResponseStream());

                        try
                        {
                            sHTML = srReader.ReadToEnd();
                        }
                        catch
                        {
                            Console.WriteLine("Connection cannot be established, please try again.");
                            eLog.WriteEntry("Connection to NBA source data could not be established.", EventLogEntryType.Error, 102);
                            return;
                        }
                        srReader.Close();

                        if (sHTML.Contains("404 - File Not Found"))
                        {
                            Console.WriteLine("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NBA season has not yet started.");
                            eLog.WriteEntry("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NBA season page does not exist.", EventLogEntryType.Warning, 402);
                            bAbort = true;
                        }
                    }
                    catch
                    {
                        Console.WriteLine("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NBA season has not yet started.");
                        eLog.WriteEntry("The " + cbSeason2 + "-" + (int.Parse(cbSeason2) + 1) + " NBA season data is not available.", EventLogEntryType.Warning, 402);
                        bAbort = true;
                        return;
                    }

                    sHTML = sHTML.Substring(sHTML.IndexOf("<tbody>") + 7);
                    sHTML = sHTML.Substring(0, sHTML.IndexOf(@"</tbody>"));

                    ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"</tr>"));

                    iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
                    if (!bClearedSeason)
                    {
                        ClearSeason(iSeasonID);
                        bClearedSeason = true;
                    }

                    foreach (string sGame in alGames)
                    {
                        if (sGame.Contains("Playoffs") || bInPlayoffs)
                        {
                            sHTMLPlayoffs = sHTML.Substring(sHTML.IndexOf("Playoffs") + 8, sHTML.Length - (sHTML.IndexOf("Playoffs") + 9));
                            sHTMLPlayoffs = "</tbody><tbody>" + sHTMLPlayoffs + "</tbody>"; // Adding the tbody parts to allow the playoff portion to work for the month the playoffs start, regular season consumed original tbody tags.
                            bInPlayoffs = true;
                            break;
                        }

                        if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n")
                            continue;

                        ArrayList alLines = new ArrayList(Regex.Split(sGame, "><td"));

                        if ((string)alLines[1] == "")
                            alLines.RemoveAt(1);

                        sRange = parseLine(alLines[0]); //Week

                        sTeam1 = parseLine(alLines[2]); //Winning Team
                        sTeam2 = parseLine(alLines[4]); //Losing Team
                        sScore1 = parseLine(alLines[3]); //Winning Score
                        sScore2 = parseLine(alLines[5]); //Losing Score
                        if (sScore1 == "" || sScore2 == "")
                            continue;
                        iScore1 = int.Parse(sScore1);
                        iScore2 = int.Parse(sScore2);
                        iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                        iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                        if (iTeam1ID == -1 || iTeam2ID == -1)
                        {
                            bAbort = true;
                            break;
                        }
                        DateTime dtGame = DateTime.Parse(sRange);
                        int iDayOfYear = dtGame.DayOfYear;
                        if (iDayOfYear < 225) //225 is an arbitary number chosen to represent the split during the offseason
                        {
                            if (DateTime.IsLeapYear(dtGame.Year - 1))
                                iDayOfYear += 366;
                            else
                                iDayOfYear += 365;
                        }

                        if (iStartDay == -1)
                            iStartDay = iDayOfYear;

                        iDayOfYear = iDayOfYear - iStartDay;
                        iDayOfYear = (iDayOfYear / 7) + 1;

                        InsertGame(iSeasonID, iDayOfYear, iTeam1ID, iScore1, iTeam2ID, iScore2);
                    }

                    //Process Playoff Games
                    if (bInPlayoffs)
                    {
                        sHTMLPlayoffs = sHTMLPlayoffs.Substring(sHTMLPlayoffs.IndexOf("</tbody>"));
                        if (sHTMLPlayoffs.IndexOf("<tbody>") > 0)
                        {
                            sHTMLPlayoffs = sHTMLPlayoffs.Substring(sHTMLPlayoffs.IndexOf("<tbody>") + 7);
                            sHTMLPlayoffs = sHTMLPlayoffs.Substring(0, sHTMLPlayoffs.IndexOf("</tbody>"));
                            alGames = new ArrayList(Regex.Split(sHTMLPlayoffs, @"</tr>"));
                            int iRange = 0;

                            foreach (string sGame in alGames)
                            {
                                if (sGame == "" || sGame.Contains("thead") || sGame == "\n" || sGame == "\n\n" || sGame == "</th>")
                                    continue;

                                ArrayList alLines = new ArrayList(Regex.Split(sGame, "><td"));

                                if ((string)alLines[0] == "")
                                    alLines.RemoveAt(1);

                                sTeam1 = parseLine(alLines[2]); //Winning Team
                                sTeam2 = parseLine(alLines[4]); //Losing Team
                                sScore1 = parseLine(alLines[3]); //Winning Score
                                sScore2 = parseLine(alLines[5]); //Losing Score

                                if (sScore1 == "" || sScore2 == "")
                                    continue;
                                iScore1 = int.Parse(sScore1);
                                iScore2 = int.Parse(sScore2);
                                iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                                iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                                if (iTeam1ID == -1 || iTeam2ID == -1)
                                {
                                    bAbort = true;
                                    break;
                                }

                                if (!slMatchups.Contains(iTeam1ID + "|" + iTeam2ID))
                                {
                                    iRange = getRange(slMatchups, iTeam1ID, iTeam2ID);

                                    slMatchups.Add(iTeam1ID + "|" + iTeam2ID, iRange);
                                    slMatchups.Add(iTeam2ID + "|" + iTeam1ID, iRange);
                                }
                                else
                                {
                                    iRange = (int)slMatchups[iTeam1ID + "|" + iTeam2ID];
                                }

                                if (iSeasonID >= 79 && iSeasonID <= 82) //1970-1973 NBA seasons had 3 playoff rounds instead of 4
                                    iRange++;
                                else if (iSeasonID == 77 || iSeasonID == 78) //1974-1975 NBA seasons have byes that need to be adjusted for manually
                                {
                                    Console.WriteLine("This NBA season has playoff byes that need to be manually adjusted.");
                                    //eLog.WriteEntry("This NBA season has playoff byes that need to be manually adjusted.", EventLogEntryType.Warning, 702);
                                }

                                InsertGame(iSeasonID, iRange, iTeam1ID, iScore1, iTeam2ID, iScore2);
                            }
                        }
                    }
                }
            }
            #endregion
            #region Log MLB Games
            else if (cbLeague2 == "MLB")
            {
                ArrayList alTeamList = getMLBTeams(cbSeason2);
                ArrayList alTeamsCompleted = new ArrayList();
                int iStartDay = -1;

                iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
                ClearSeason(iSeasonID);

                foreach (string sTeamID in alTeamList)
                {
                    string sCurrentTeam = getRefAbbr(sTeamID);
                    alTeamsCompleted.Add(sCurrentTeam);
                    string sHTML = "";

                    try
                    {
                        HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.baseball-reference.com/teams/" + sCurrentTeam + "/" + cbSeason2 + "-schedule-scores.shtml");
                        HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                        StreamReader srReader = new StreamReader(hwResponse.GetResponseStream());

                        try
                        {
                            sHTML = srReader.ReadToEnd();
                        }
                        catch
                        {
                            Console.WriteLine("Connection cannot be established for " + sCurrentTeam + ", please try again.");
                            eLog.WriteEntry("Connection to MLB source data could not be established.", EventLogEntryType.Error, 103);
                            return;
                        }
                        srReader.Close();

                        if (sHTML.Contains("404 - File Not Found"))
                        {
                            Console.WriteLine("The " + cbSeason2 + " MLB season has not yet started.");
                            eLog.WriteEntry("The " + cbSeason2 + " MLB season page does not exist.", EventLogEntryType.Warning, 403);
                            return;
                        }
                    }
                    catch
                    {
                        Console.WriteLine("The " + cbSeason2 + " MLB season has not yet started.");
                        eLog.WriteEntry("The " + cbSeason2 + " MLB season data is not available.", EventLogEntryType.Warning, 403);
                        bAbort = true;
                        return;
                    }

                    sHTML = sHTML.Substring(sHTML.IndexOf("<tbody>") + 7);
                    sHTML = sHTML.Substring(0, sHTML.IndexOf(@"</tbody>"));

                    ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"</tr>"));

                    foreach (string sGame in alGames)
                    {
                        if (sGame == "" || sGame.Contains("thead") || sGame == "\n")
                            continue;

                        ArrayList alLines = new ArrayList(Regex.Split(sGame, "><td"));

                        if (alLines.Count < 2)
                            continue;

                        if ((string)alLines[1] == "")
                            alLines.RemoveAt(1);

                        if (alLines.Count < 15)  //Invalid line
                            continue;

                        if (parseLine(alLines[2]) == "") //Postseason game
                            continue;

                        sRange = parseLine(alLines[1]); //Week

                        sTeam1 = parseLine(alLines[3]); //Winning Team
                        sTeam2 = parseLine(alLines[5]); //Losing Team

                        if (alTeamsCompleted.Contains(sTeam2))
                            continue;

                        sScore1 = parseLine(alLines[7]); //Winning Score
                        sScore2 = parseLine(alLines[8]); //Losing Score
                        if (sScore1 == "" || sScore2 == "")
                            continue;
                        iScore1 = int.Parse(sScore1);
                        iScore2 = int.Parse(sScore2);
                        iTeam1ID = getTeamID(sTeam1, cbLeague2, cbSeason2);
                        iTeam2ID = getTeamID(sTeam2, cbLeague2, cbSeason2);
                        if (iTeam1ID == -1 || iTeam2ID == -1)
                        {
                            bAbort = true;
                            break;
                        }
                        DateTime dtGame = DateTime.Parse(sRange + " " + cbSeason2);
                        int iDayOfYear = dtGame.DayOfYear;

                        if (iStartDay == -1)
                            iStartDay = iDayOfYear;

                        iDayOfYear = iDayOfYear - iStartDay;
                        iDayOfYear = (iDayOfYear / 7) + 1;
                        if (iDayOfYear < 1)
                            iDayOfYear = 1;

                        InsertGame(iSeasonID, iDayOfYear, iTeam1ID, iScore1, iTeam2ID, iScore2);
                    }
                    Console.WriteLine(sCurrentTeam + " completed." + Environment.NewLine);
                }
                
                if (cbSeason2 != "1994") //Strike year, playoffs cancelled
                {
                    ArrayList alSeries = new ArrayList();
                    if (int.Parse(cbSeason2) > 1994) //Divisional Playoff round established
                    {
                        alSeries.Add("ALDS1");
                        alSeries.Add("ALDS2");
                        alSeries.Add("NLDS1");
                        alSeries.Add("NLDS2");
                    }
                    else if (int.Parse(cbSeason2) == 1981)
                    {
                        alSeries.Add("AEDIV");
                        alSeries.Add("AWDIV");
                        alSeries.Add("NEDIV");
                        alSeries.Add("NWDIV");
                    }
                    alSeries.Add("ALCS");
                    alSeries.Add("NLCS");
                    alSeries.Add("WS");

                    Console.WriteLine("Trying to load playoffs." + Environment.NewLine);
                    foreach (string sSeries in alSeries)
                    {
                        LoadSeriesGames(sSeries, cbSeason2, cbLeague2, ref eLog);
                    }
                }
            }
            #endregion

            if (bAbort)
            {
                Console.WriteLine("Season " + cbSeason2 + " aborted.  Day/Week: " + sRange + " - Teams: " + sTeam1 + " vs. " + sTeam2);
                eLog.WriteEntry("RUN ABORTED: Season " + cbLeague2 + " " + cbSeason2 + " aborted.  Week: " + sRange + " - Teams: " + sTeam1 + " vs. " + sTeam2, EventLogEntryType.Error, 200);
            }
            else
            {
                Console.WriteLine("Season " + cbSeason2 + " loaded.");
                eLog.WriteEntry("RUN COMPLETE: Season " + cbLeague2 + " " + cbSeason2 + " load complete.", EventLogEntryType.Information, 1);
            }
        }

        private static int getRange(SortedList slMatchups, int iTeam1, int iTeam2)
        {
            int iMaxRange = 500;
            IList ilKeys;

            ilKeys = slMatchups.GetKeyList();
            foreach (string sKey in ilKeys)
            {
                if ((int.Parse(sKey.Split('|')[0]) == iTeam1 || int.Parse(sKey.Split('|')[0]) == iTeam2) && (int)slMatchups[sKey] > iMaxRange)
                {
                    iMaxRange = (int)slMatchups[sKey];
                }
            }

            return iMaxRange + 1;
        }

        private static void LoadSeriesGames(string sSeries, string cbSeason2, string cbLeague2, ref EventLog eLog)
        {
            string sHTML = "", sTeam1 = "", sTeam2 = "", sScore1 = "", sScore2 = "", sRange = "";
            StreamReader srReader = null;
            int iSeasonID;

            try
            { 
                HttpWebRequest hwRequest = (HttpWebRequest)WebRequest.Create("http://www.baseball-reference.com/postseason/" + cbSeason2 + "_" + sSeries + ".shtml");
                HttpWebResponse hwResponse = (HttpWebResponse)hwRequest.GetResponse();
                srReader = new StreamReader(hwResponse.GetResponseStream());
                iSeasonID = FindSeason(int.Parse(cbSeason2), cbLeague2);
            }
            catch
            {
                Console.WriteLine("Connection cannot be established for " + cbSeason2 + "_" + sSeries + ", please try again.");
                eLog.WriteEntry("Connection to MLB source data could not be established.", EventLogEntryType.Error, 103);
                return;
            }

            try
            {
                if (srReader != null)
                    sHTML = srReader.ReadToEnd();
            }
            catch
            {
                Console.WriteLine("Connection cannot be established for " + cbSeason2 + "_" + sSeries + ", please try again.");
                eLog.WriteEntry("Connection to MLB source data could not be established.", EventLogEntryType.Error, 103);
                return;
            }
            srReader.Close();

            if (sHTML.Contains("404 - File Not Found"))
            {
                Console.WriteLine("The " + cbSeason2 + " MLB playoffs have not yet completed.");
                eLog.WriteEntry("The " + cbSeason2 + " MLB playoffs page does not exist.", EventLogEntryType.Warning, 403);
                return;
            }

            ArrayList alGames = new ArrayList(Regex.Split(sHTML, @"<PRE>"));
            alGames.RemoveAt(0);

            foreach (string sGame in alGames)
            {
                ArrayList alLines = new ArrayList(Regex.Split(sGame, @"\n"));
                int iIndex = 0, iMarker = -1;

                foreach (string sLine in alLines)
                {
                    ArrayList alBits = new ArrayList(Regex.Split(sLine, @" "));

                    while (alBits.Contains(""))
                        alBits.Remove("");

                    if (alBits.Count > 0)
                    {
                        if (iMarker == -1 && (string)alBits[alBits.Count - 1] == "E")
                        {
                            iMarker = alLines.IndexOf(sLine);
                        }

                        if (iMarker != -1)
                        {
                            if (iIndex == iMarker + 2)
                            {
                                //AWAY TEAM'S LINE
                                sScore1 = (string)alBits[alBits.Count - 3];
                            }
                            else if (iIndex == iMarker + 3)
                            {
                                //HOME TEAM'S LINE
                                sScore2 = (string)alBits[alBits.Count - 3];
                            }
                            else if (iIndex == iMarker + 5)
                            {
                                //FIND AWAY TEAM ABBR
                                sTeam1 = (string)alBits[alBits.IndexOf("-") - 1];
                            }
                            else if (iIndex == iMarker + 6)
                            {
                                //FIND HOME TEAM ABBR
                                sTeam2 = (string)alBits[alBits.IndexOf("-") - 1];
                            }
                        }
                    }

                    iIndex++;
                }

                if (sSeries == "ALDS1" || sSeries == "ALDS2" || sSeries == "NLDS1" || sSeries == "NLDS2" || 
                    sSeries == "AEDIV" || sSeries == "AWDIV" || sSeries == "NEDIV" || sSeries == "NWDIV")
                    sRange = "502";
                else if (sSeries == "ALCS" || sSeries == "NLCS")
                    sRange = "503";
                else if (sSeries == "WS")
                    sRange = "504";
                else
                    sRange = "599";

                InsertGame(iSeasonID, int.Parse(sRange), getTeamID(sTeam1, cbLeague2, cbSeason2), int.Parse(sScore1), getTeamID(sTeam2, cbLeague2, cbSeason2), int.Parse(sScore2));
            }
        }

        private static string getRefAbbr(string sTeamID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;
            string sRefAbbr = "";

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@FranchiseID", SqlDbType.NVarChar, 50, ParameterDirection.Input, sTeamID);
            SQLDBA.ExecuteSqlSP("Select_Team_RefAbbr", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                sRefAbbr = sqlDR.GetSqlValue(0).ToString();
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return sRefAbbr;
        }

        private static ArrayList getMLBTeams(string sSeason)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[2];
            SqlDataReader sqlDR;
            ArrayList alTeams = new ArrayList();

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, "MLB");
            sqlParam[1] = SQLDBA.CreateParameter("@Season", SqlDbType.NVarChar, 50, ParameterDirection.Input, sSeason);
            SQLDBA.ExecuteSqlSP("Select_Teams", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    alTeams.Add(sqlDR.GetSqlValue(0).ToString());
                }
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return alTeams;
        }

        private static int getTeamID(string sTeam, string cbLeague2, string cbSeason2)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[3];
            SqlDataReader sqlDR;
            int iTeam = -1;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbLeague2);
            sqlParam[1] = SQLDBA.CreateParameter("@TeamName", SqlDbType.NVarChar, 50, ParameterDirection.Input, sTeam);
            sqlParam[2] = SQLDBA.CreateParameter("@Season", SqlDbType.NVarChar, 50, ParameterDirection.Input, cbSeason2);
            if (cbLeague2 == "MLB")
                SQLDBA.ExecuteSqlSP("Select_TeamID_By_RefAbbr", sqlParam, out sqlDR);
            else if (cbLeague2 == "NCAAF")
                SQLDBA.ExecuteSqlSP("Select_NCAA_TeamID", sqlParam, out sqlDR);
            else
                SQLDBA.ExecuteSqlSP("Select_TeamID_By_Full_Name", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                iTeam = int.Parse(sqlDR.GetSqlValue(0).ToString());
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return iTeam;
        }

        private static string parseLine(object oLine)
        {
            string sLine = (string)oLine;
            if (sLine == "")
                return sLine;

            if (sLine.Contains("<tr >"))
                sLine = sLine.Substring(sLine.IndexOf("<tr >") + 5);
            if (sLine.Contains("th>"))
                sLine = sLine.Substring(sLine.IndexOf("th>") + 3);
            if (sLine.Contains("<a"))
                sLine = sLine.Substring(sLine.IndexOf("<a"));
            if (sLine.Contains("<strong>"))
                sLine = sLine.Substring(sLine.IndexOf("<strong>") + 7);
            sLine = sLine.Substring(sLine.IndexOf(">") + 1);
            sLine = sLine.Substring(0, sLine.IndexOf("<"));
            return sLine;
        }

        private static int FindSeason(int iSeason, string sLeague)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[2];
            SqlDataReader sqlDR;
            int iSeasonID;

            sqlParam[0] = SQLDBA.CreateParameter("@Year", SqlDbType.Int, 64, ParameterDirection.Input, iSeason);
            sqlParam[1] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, sLeague);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_SeasonID", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                iSeasonID = int.Parse(sqlDR.GetSqlValue(0).ToString());
            }
            else
                iSeasonID = -1;

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return iSeasonID;
        }

        private static int FindTeam(string sAbbreviation)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;
            int iTeamID;

            sqlParam[0] = SQLDBA.CreateParameter("@Abbreviation", SqlDbType.NVarChar, 50, ParameterDirection.Input, sAbbreviation);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_TeamID", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                iTeamID = int.Parse(sqlDR.GetSqlValue(0).ToString());
            }
            else
                iTeamID = -1;

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return iTeamID;
        }

        private static bool GameExists(int iSeasonID, int iWeek, int iTeam1, int iTeam2)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[4];
            SqlDataReader sqlDR;
            bool bExists = false;

            sqlParam[0] = SQLDBA.CreateParameter("@Season", SqlDbType.Int, 64, ParameterDirection.Input, iSeasonID);
            sqlParam[1] = SQLDBA.CreateParameter("@Week", SqlDbType.Int, 64, ParameterDirection.Input, iWeek);
            sqlParam[2] = SQLDBA.CreateParameter("@Team1", SqlDbType.Int, 64, ParameterDirection.Input, iTeam1);
            sqlParam[3] = SQLDBA.CreateParameter("@Team2", SqlDbType.Int, 64, ParameterDirection.Input, iTeam2);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_Game", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
                bExists = true;

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();

            return bExists;
        }

        private static void ClearSeason(int iSeasonID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];

            sqlParam[0] = SQLDBA.CreateParameter("@SeasonID", SqlDbType.Int, 64, ParameterDirection.Input, iSeasonID);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Clear_Season", sqlParam);

            SQLDBA.Close();
            SQLDBA.Dispose();
        }

        private static void ClearCFSeason(int iSeasonID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];

            sqlParam[0] = SQLDBA.CreateParameter("@SeasonID", SqlDbType.Int, 64, ParameterDirection.Input, iSeasonID);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Clear_CFSeason", sqlParam);

            SQLDBA.Close();
            SQLDBA.Dispose();
        }

        private static void InsertGame(int iSeasonID, int iWeek, int iTeam1, int iScore1, int iTeam2, int iScore2)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[6];

            sqlParam[0] = SQLDBA.CreateParameter("@SeasonID", SqlDbType.Int, 64, ParameterDirection.Input, iSeasonID);
            sqlParam[1] = SQLDBA.CreateParameter("@WeekID", SqlDbType.Int, 64, ParameterDirection.Input, iWeek);
            sqlParam[2] = SQLDBA.CreateParameter("@AwayID", SqlDbType.Int, 64, ParameterDirection.Input, iTeam1);
            sqlParam[3] = SQLDBA.CreateParameter("@AwayScore", SqlDbType.Int, 64, ParameterDirection.Input, iScore1);
            sqlParam[4] = SQLDBA.CreateParameter("@HomeID", SqlDbType.Int, 64, ParameterDirection.Input, iTeam2);
            sqlParam[5] = SQLDBA.CreateParameter("@HomeScore", SqlDbType.Int, 64, ParameterDirection.Input, iScore2);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Insert_Game", sqlParam);

            SQLDBA.Close();
            SQLDBA.Dispose();
        }

        private static void InsertCFGame(int iSeasonID, int iWeek, int iTeam1, int iScore1, int iTeam2, int iScore2)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[6];

            sqlParam[0] = SQLDBA.CreateParameter("@SeasonID", SqlDbType.Int, 64, ParameterDirection.Input, iSeasonID);
            sqlParam[1] = SQLDBA.CreateParameter("@WeekID", SqlDbType.Int, 64, ParameterDirection.Input, iWeek);
            sqlParam[2] = SQLDBA.CreateParameter("@AwayID", SqlDbType.Int, 64, ParameterDirection.Input, iTeam1);
            sqlParam[3] = SQLDBA.CreateParameter("@AwayScore", SqlDbType.Int, 64, ParameterDirection.Input, iScore1);
            sqlParam[4] = SQLDBA.CreateParameter("@HomeID", SqlDbType.Int, 64, ParameterDirection.Input, iTeam2);
            sqlParam[5] = SQLDBA.CreateParameter("@HomeScore", SqlDbType.Int, 64, ParameterDirection.Input, iScore2);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Insert_CFGame", sqlParam);

            SQLDBA.Close();
            SQLDBA.Dispose();
        }

        private static Team GetTeam(int iTeamID)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;
            Team tTeam;

            sqlParam[0] = SQLDBA.CreateParameter("@FranchiseID", SqlDbType.NVarChar, 50, ParameterDirection.Input, iTeamID);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_Team", sqlParam, out sqlDR);

            if (sqlDR.HasRows)
            {
                sqlDR.Read();
                tTeam = new Team(iTeamID);
            }
            else
            {
                tTeam = null;
            }

            sqlDR.Close();
            SQLDBA.Close();
            sqlDR.Dispose();
            SQLDBA.Dispose();

            return tTeam;
        }
        #endregion
    }
}
