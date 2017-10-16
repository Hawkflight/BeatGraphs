using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

/***************************************************************************************
 * BGRunner
 * 
 * The purpose of this application is to repeatedly run BGBuilder as necessary to update
 * all of the graphs and pages based on the new scores taken by BGScoreUpdater.  The
 * application is to be set to run only the latest week of the season, however by setting
 * the major loops to start with 1 instead of maxrange, the entire season may be done.
 * When the pages are finished, the "Top 5" page is updated and uploaded.
 * ************************************************************************************/

namespace BGRunner
{
    class Program
    {
        static StringBuilder sbFileList = new StringBuilder();
        static ArrayList alLeagues = new ArrayList();
        static ArrayList alSeasons = new ArrayList();
        static string sFilePath;
        static string league;
        static string year;
        static EventLog eLog;

        //Edit this to target a specific year.  Leave as empty strings for everyday runs.
        //static string strSeasonNFL = "2014", strSeasonNBA = "2014", strSeasonNHL = "2014", strSeasonMLB = "2014", strSeasonNCAAF = "";
        static string strSeasonNFL = "", strSeasonNBA = "", strSeasonNHL = "", strSeasonMLB = "", strSeasonNCAAF = "";

        static void Main(string[] args)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlDataReader sqlDR;
            int maxrange;
            //sFilePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            sFilePath = @"C:\WebSites\www.beatgraphs.com\";


            /* EVENT LOGGING */
            string sSource = "BGraphs";
            string sLog = "BGraphsLog";
            EventSourceCreationData escdSource = new EventSourceCreationData(sSource, sLog);
            escdSource.MachineName = ".";
            eLog = new EventLog();
            if (!EventLog.SourceExists(sSource, escdSource.MachineName))
                EventLog.CreateEventSource(escdSource);

            eLog.Source = sSource;

            eLog.WriteEntry("Runner job started.", EventLogEntryType.Information, 0, (short)0);

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_RegSeason_Graphs", out sqlDR);
            
            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    league = SQLDBA.sqlGet(sqlDR, "League");
                    year = SQLDBA.sqlGet(sqlDR, "Year");
                    maxrange = int.Parse(SQLDBA.sqlGet(sqlDR, "LastWeek"));

                    if (league == "MLB")
                    {
                        if (strSeasonMLB == "")
                            strSeasonMLB = year;
                        else if (strSeasonMLB != year)
                            continue;
                    }
                    if (league == "NBA")
                    {
                        if (strSeasonNBA == "")
                            strSeasonNBA = year;
                        else if (strSeasonNBA != year)
                            continue;
                    }
                    if (league == "NFL")
                    {
                        if (strSeasonNFL == "")
                            strSeasonNFL = year;
                        else if (strSeasonNFL != year)
                            continue;
                    }
                    if (league == "NHL")
                    {
                        if (strSeasonNHL == "")
                            strSeasonNHL = year;
                        else if (strSeasonNHL != year)
                            continue;
                    }
                    if (league == "NCAAF")
                    {
                        if (strSeasonNCAAF == "")
                            strSeasonNCAAF = year;
                        else if (strSeasonNCAAF != year)
                            continue;
                    }

                    //if (league == "NCAAF") //Use this to exclude a league or run a specific league from the run
                    //    continue;
                    //for (int i = 1; i <= maxrange; i++)                 //Use this to run the entire season
                    for (int i = maxrange; i <= maxrange; i++)        //Use this to run just the latest week, this is for everyday runs.
                    {
                        Process pBuilder = new Process();
                        pBuilder.StartInfo.FileName = @"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe";
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m S";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m S");
                        try
                        {
                            pBuilder.StartInfo.RedirectStandardOutput = true;
                            pBuilder.StartInfo.UseShellExecute = false;
                            pBuilder.StartInfo.CreateNoWindow = true;
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Standard): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m I";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m I");
                        try
                        {
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Iterative): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m W";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m W");
                        try
                        {
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Weighted): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }                       
                    }
                }
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();
            runPlayoffs();
            listLeagues();
            printFooter();
            uploadTop5s();

            eLog.WriteEntry("RUNNER COMPLETED.", EventLogEntryType.Information, 0, (short)0);
        }

        static void uploadTop5s()
        {
            for (int i = 0; i < 15; i++)
            {
                string sLeague = "", sMethod = "";
                switch (i)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 12:
                        sMethod = "S";
                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 13:
                        sMethod = "I";
                        break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 14:
                        sMethod = "W";
                        break;
                }
                switch (i)
                {
                    case 0:
                    case 4:
                    case 8:
                        sLeague = "MLB";
                        break;
                    case 1:
                    case 5:
                    case 9:
                        sLeague = "NBA";
                        break;
                    case 2:
                    case 6:
                    case 10:
                        sLeague = "NFL";
                        break;
                    case 3:
                    case 7:
                    case 11:
                        sLeague = "NHL";
                        break;
                    case 12:
                    case 13:
                    case 14:
                        sLeague = "NCAAF";
                        break;
                }
                try
                {
                    Console.WriteLine("Uploading " + sLeague + "_" + sMethod);
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/" + sLeague + "_" + sMethod + ".php");
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");

                    byte[] fileContents = File.ReadAllBytes(sFilePath + "/" + sLeague + "_" + sMethod + ".php");

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                    Console.WriteLine("Upload " + sLeague + "_" + sMethod + " Complete, status {0}" + response.StatusDescription);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Upload of " + sLeague + "_" + sMethod + " Failed: " + ex.Message);
                    eLog.WriteEntry("Runner Upload of " + sLeague + "_" + sMethod + " Failed: " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                    Console.ReadLine();
                }
            }
        }

        static void runPlayoffs()
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlDataReader sqlDR;
            int minrange, maxrange;

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_Playoff_Graphs", out sqlDR);

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    league = SQLDBA.sqlGet(sqlDR, "League");
                    year = SQLDBA.sqlGet(sqlDR, "Year");
                    minrange = int.Parse(SQLDBA.sqlGet(sqlDR, "FirstWeek"));
                    maxrange = int.Parse(SQLDBA.sqlGet(sqlDR, "LastWeek"));

                    if (league == "MLB")
                    {
                        if (strSeasonMLB != year)
                            continue;
                    }
                    if (league == "NBA")
                    {
                        if (strSeasonNBA != year)
                            continue;
                    }
                    if (league == "NFL")
                    {
                        if (strSeasonNFL != year)
                            continue;
                    }
                    if (league == "NHL")
                    {
                        if (strSeasonNHL != year)
                            continue;
                    }
                    if (league == "NCAAF")
                    {
                        continue; // Bowl games are not marked as playoff games.  They use a regular week count for the entire season.
                    }

                    //for (int i = 501; i <= maxrange; i++)       // Use this to generate all weeks.
                    for (int i = maxrange; i <= maxrange; i++) // Use this to run just the last week, used for every day runs
                    {
                        Process pBuilder = new Process();
                        pBuilder.StartInfo.FileName = @"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe";
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m S";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m S");
                        try
                        {
                            pBuilder.StartInfo.RedirectStandardOutput = true;
                            pBuilder.StartInfo.UseShellExecute = false;
                            pBuilder.StartInfo.CreateNoWindow = true;
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Standard Playoffs): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m I";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m I");
                        try
                        {
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Iterative Playoffs): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }
                        pBuilder.StartInfo.Arguments = "-l " + league + " -s " + year + " -r " + i + " -m W";
                        Console.WriteLine(@"C:\WebSites\www.beatgraphs.com\Code\BGBuilder\BGBuilder\bin\Debug\BGBuilder.exe -l " + league + " -s " + year + " -r " + i + " -m W");
                        try
                        {
                            pBuilder.Start();
                            pBuilder.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed: " + ex.Message);
                            eLog.WriteEntry("Builder job failed (Weighted Playoffs): " + ex.Message, EventLogEntryType.Error, 0, (short)0);
                        }
                    }
                }
            }

            SQLDBA.Close();
            sqlDR.Close();
            SQLDBA.Dispose();
            sqlDR.Dispose();
        }

        static void printFooter()
        {
            StringBuilder sFooter = new StringBuilder();
            sFooter.Append("<!-- Footer --><div id='footer-wrapper'><div id='footer' class='container'><div class='footertable'><div class='footerrow'>");

            sFooter.Append("<div class='footercell'><h3>Questions or comments?<br /><strong>Get in touch!</strong></h3><div class='icon fa-envelope'>");
            sFooter.Append("<a href='mailto:themoose@beatgraphs.com'>themoose@beatgraphs.com</a></div><div class='icon fa-twitter'>");
            sFooter.Append("<a href='https://twitter.com/BeatGraphs'>@BeatGraphs</a></div><div class='icon fa-reddit-alien'>");
            sFooter.Append("<a href='https://www.reddit.com/user/BeatGraphs'>/u/BeatGraphs</a></div></div>");

            sFooter.Append("<div class='footercell'><h3>Acknowledgements</h3><a href='http://curtsiffert.com/'>Curt Siffert</a> - creator of BeatPaths.com<br />");
            sFooter.Append("<a href='http://sports-reference.com'>Sports-Reference.com</a> - source for game data<br />");
            sFooter.Append("<a href='http://sportslogos.net'>SportsLogos.net</a> - source for team logos<br />");
            sFooter.Append("<a href='http://graphviz.org'>Graphviz.org</a> - graph drawing application<br />");
            sFooter.Append("<a href='http://html5up.com'>HTML5UP.com</a> - source for web templates<br />");
            sFooter.Append("<a href='http://darcymelton.com/'>Darcy Melton</a> - MOOSE logo design<br /></div>");

            sFooter.Append("<div class='footercell'><h3>Copyright Notice</h3>&copy; Hawkflight Studios 2008-<? echo date('Y') ?>. All rights reserved.<br />");
            sFooter.Append("All logos contained within this site are copyrights or trademarks of their respective teams and may not be sold without permission from each individual organization.<br />");
            sFooter.Append("</div></div></div><div id='copyright' class='container'>Graphs last updated: <strong>" + DateTime.Now.ToString("MMM d, yyyy 'at' h:mm tt") + "</strong></div></div></div>");

            sFooter.Append("<!-- Scripts --><script src='assets/js/jquery.min.js'></script><script src='assets/js/jquery.dropotron.min.js'></script>");
            sFooter.Append("<script src='assets/js/skel.min.js'></script><script src='assets/js/skel-viewport.min.js'></script><script src='assets/js/util.js'></script>");
            sFooter.Append("<!--[if lte IE 8]><script src='assets/js/ie/respond.min.js'></script><![endif]--><script src='assets/js/main.js'></script>");
            sFooter.Append("<script src='assets/js/jscript.js'></script></body></html>");

            TextWriter twOut = new StreamWriter(sFilePath + "/footer.php", false);
            twOut.Write(sFooter.ToString());
            twOut.Close();

            Console.WriteLine("Uploading Footer");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/footer.php");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");

            byte[] fileContents = File.ReadAllBytes(sFilePath + "/footer.php");

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload Footer Complete, status {0}" + response.StatusDescription);

            response.Close();
        }

        static void listLeagues()
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlDataReader sqlDR;
            int iCount = 0;

            SQLDBA.Open();
            SQLDBA.ExecuteSqlSP("Select_Leagues", out sqlDR);

            sbFileList.Append("<?\n\n");
            sbFileList.Append("$filearray = array(");

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    alLeagues.Add(SQLDBA.sqlGet(sqlDR, "League"));
                    alSeasons.Add(new ArrayList());
                    if (iCount == 0)
                        sbFileList.Append("\"" + SQLDBA.sqlGet(sqlDR, "League") + "\" => array()");
                    else
                        sbFileList.Append(",\n\"" + SQLDBA.sqlGet(sqlDR, "League") + "\" => array()");
                    iCount++;
                }
            }
            sbFileList.Append(");\n");

            sqlDR.Close();
            sqlDR.Dispose();
            SQLDBA.Close();
            SQLDBA.Dispose();

            foreach (string sLeague in alLeagues)
            {
                appendSeason(sLeague);
            }

            iCount = 0;
            foreach (string sLeague in alLeagues)
            {
                sbFileList.Append("\n");
                foreach (string sYear in  ((ArrayList)alSeasons[iCount]))
                {
                    appendYear(sLeague, sYear);
                }
                iCount++;
            }
            sbFileList.Append("\n?>");

            TextWriter twOut = new StreamWriter(sFilePath + "/filelist.php", false);
            twOut.Write(sbFileList.ToString());
            twOut.Close();


            Console.WriteLine("Uploading Filelist");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://ftp.hawkflightstudios.com/beatgraphs.com/filelist.php");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential("thoraxcs", "Ih8ppl");

            byte[] fileContents = File.ReadAllBytes(sFilePath + "/filelist.php");

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload Filelist Complete, status {0}" + response.StatusDescription);

            response.Close();
        }

        static void appendSeason(string sLeague)
        {
            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[1];
            SqlDataReader sqlDR;
            int iCount = 0;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, sLeague);
            SQLDBA.ExecuteSqlSP("Select_Seasons", sqlParam, out sqlDR);

            sbFileList.Append("\n");
            sbFileList.Append("$filearray[\"" + sLeague + "\"] = array(");

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    if (alLeagues.IndexOf(sLeague) != -1)
                    {
                        ((ArrayList)alSeasons[alLeagues.IndexOf(sLeague)]).Add(SQLDBA.sqlGet(sqlDR, "Year"));
                        if (iCount == 0)
                            sbFileList.Append("\"" + SQLDBA.sqlGet(sqlDR, "Year") + "\" => array()");
                        else
                            sbFileList.Append(",\n\"" + SQLDBA.sqlGet(sqlDR, "Year") + "\" => array()");
                        iCount++;
                        //Add in cancelled 2004 NHL season
                        if (sLeague == "NHL" && SQLDBA.sqlGet(sqlDR, "Year") == "2003")
                        {
                            sbFileList.Append(",\n\"2004\" => array()");
                        }
                    }
                }
            }

            sbFileList.Append(");\n\n");
            sqlDR.Close();
            sqlDR.Dispose();
            SQLDBA.Close();
            SQLDBA.Dispose();
        }

        static void appendYear(string sLeague, string sYear)
        {
            //Add in cancelled 2004 NHL season before the 2005 season
            if (sLeague == "NHL" && sYear == "2005")
            {
                sbFileList.Append("$filearray[\"" + sLeague + "\"][\"2004\"] = array(\"Season Cancelled\");\n");
            }

            SQLDatabaseAccess SQLDBA = new SQLDatabaseAccess();
            SqlParameter[] sqlParam = new SqlParameter[2];
            SqlDataReader sqlDR;
            ArrayList alWeeks = new ArrayList();
            int iCount = 0;

            SQLDBA.Open();
            sqlParam[0] = SQLDBA.CreateParameter("@Year", SqlDbType.NVarChar, 50, ParameterDirection.Input, sYear);
            sqlParam[1] = SQLDBA.CreateParameter("@League", SqlDbType.NVarChar, 50, ParameterDirection.Input, sLeague);
            SQLDBA.ExecuteSqlSP("Select_Weeks", sqlParam, out sqlDR);

            sbFileList.Append("$filearray[\"" + sLeague + "\"][\"" + sYear + "\"] = array(");

            if (sqlDR.HasRows)
            {
                while (sqlDR.Read())
                {
                    alWeeks.Add(SQLDBA.sqlGet(sqlDR, "RangeID"));
                }
                alWeeks.Reverse();
            }

            foreach (string sWeek in alWeeks)
            {
                if (iCount == 0)
                    sbFileList.Append(sWeek);
                else
                    sbFileList.Append(", " + sWeek);
                iCount++;
            }

            sbFileList.Append(");\n");
            sqlDR.Close();
            sqlDR.Dispose();
            SQLDBA.Close();
            SQLDBA.Dispose();
        }
    }
}
