<? //Set variables

$homepage = "http://www.beatgraphs.com/";
$myemail = "themoose@beatgraphs.com";

?>

<? include "leagueorder.php"; ?>

<!DOCTYPE HTML>
<html>
	<head>
        <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
        <title>BeatGraphs: Objective Sports Team Ratings and Power Rankings</title>
        <meta name="description" content="Sports team ratings and power rankings for the NFL, NBA, NCAA football and other professional sports.">
        <meta name="keywords" content="NFL sports NBA NHL MLB NCAA FBS college football baseball basketball hockey NCAA team ratings power rankings leagues">
		<meta charset="utf-8" />
		<meta name="viewport" content="width=device-width, initial-scale=1" />
		<!--[if lte IE 8]><script src="assets/js/ie/html5shiv.js"></script><![endif]-->
		<link rel="stylesheet" href="assets/css/main.css" />
		<!--[if lte IE 8]><link rel="stylesheet" href="assets/css/ie8.css" /><![endif]-->
		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css">
		<link rel="icon" type="image/png" href="images/favicon.png">
	</head>
	<body class="homepage">
		<div id="page-wrapper">

			<!-- Header -->
				<div id="header-wrapper">
					<div id="header" class="container">

						<!-- Logo -->
							<h1 id="logo"><a href="index.php">BeatGraphs</a></h1>
							<p>~May all your sports dreams come true~</p>

						<!-- Nav -->
							<nav id="nav">
								<ul>
									<li><a class="icon fa-home" href="index.php"><span>Home</span></a></li>
									<li>
										<a href="HistoricalSearch.php" class="icon fa-sitemap"><span>Graphs</span></a>
										<ul>
											<li><a href="graphs.php?league=NFL">NFL</a></li>
											<li><a href="graphs.php?league=NCAAF">NCAA</a></li>
											<li><a href="graphs.php?league=NBA">NBA</a></li>
											<li><a href="graphs.php?league=NHL">NHL</a></li>
											<li><a href="graphs.php?league=MLB">MLB</a></li>
											<li><a href="HistoricalSearch.php">Historic Graph Search</a></li>
										</ul>
									</li>
									<li><a class="icon fa-gears" href="GettingStarted.php"><span>How it Works</span></a>
										<ul>
											<li><a href="GettingStarted.php">Getting Started</a></li>
											<li><a href="BeatGraphBasics.php">BeatGraph Basics</a></li>
											<li><a href="BreakingBeatLoops.php">Breaking BeatLoops</a></li>
											<li><a href="MakingGraphs.php">Making the Graphs</a></li>
											<li><a href="RatingsAndRankings.php">Ratings and Rankings</a></li>
										</ul>
                                    </li>
									<li><a class="icon fa-trophy" href="#"><span>Playoff Histories</span></a>
										<ul>
											<li><a href="NFLPlayoffs.php">NFL</a></li>
											<li><a href="NBAPlayoffs.php">NBA</a></li>
											<li><a href="MLBPlayoffs.php">MLB</a></li>
											<li><a href="NHLPlayoffs.php">NHL</a></li>
										</ul>
                                    </li>
								</ul>
							</nav>

					</div>
				</div>