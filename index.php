<? // Set variables

$mymethodinit = "X";

?><? include "header.php"; ?>
<? include "dbconn.php"; ?>
<? include "filelist.php"; ?>

			<!-- Main -->
				<div id="main-wrapper">
					<div id="main" class="container">
						<div class="row">

							<!-- Sidebar -->
								<div id="sidebar" class="4u 12u(mobile)">
                                
									<!-- Highlights -->
										<section>
											<ul class="divided">
												<li>

													<!-- Highlight -->
														<article class="box highlight">
															<header>
																<h4><a href="GettingStarted.php">New to BeatGraphs?</a></h4>
															</header>
															<img class="image left" src="images/moose.jpg" alt="The MOOSE" />
															<p>To learn more of what the site is about and how we get the results you will find here, read the <a href="GettingStarted.php">How It Works section</a>. There you will be stepped through the entire graph building process.</p>
                                                            <p>Learn everything from the logic behind the three graphing systems, the process used to create the graphs, and the system to giving each team a rating.</p>
															<ul class="actions">
																<li><a href="GettingStarted.php" class="button icon fa-gear">How It Works</a></li>
															</ul>
														</article>

												</li>
											</ul>
										</section>

									<!-- Excerpts -->
										<section>
											<ul class="divided">
                                                <h1>Latest News</h1>
												I have updated BeatGraphs for the 2017-18 season which includes adding the Vegas Golden Knights, moving the Chargers to LA, and updating logos for the Cavaliers, Timberwolves, and Rams.<br/><br/>
												Additionally, I have disabled the discussion board as I have been receiving 200 bot spam posts for every real post and it doesn't seem worth the hassle. If anyone would still like to contact me or comment on things, you can reach me at the email listed in the footer of any page.<br/><br/>
												~The MOOSE
											</ul>
										</section>

								</div>

							<!-- Content -->
								<div id="content" class="8u 12u(mobile) important(mobile)">

									<!-- Post -->
										<article class="box post">

                                            <!-- <? echo $league1 ?> SECTION -->
											<?
												$curyear = date("Y");
												if (count($filearray[$league1][$curyear]) == 0)
												{
													$curyear = $curyear - 1;
												}
												$curweek = count($filearray[$league1][$curyear]) - 1;
											?>
											<header>
                                                <h4><? echo $league1 ?> Graphs - 
												<?
													if (strlen($filearray[$league1][$curyear][$curweek]) <= 2)
													{
														echo "Week " . $filearray[$league1][$curyear][$curweek];
													}
													else
													{
														getplayoffweek($league1, $filearray[$league1][$curyear][$curweek]);
													}
												 ?></h4>
						                    </header>
						                    <div class="row">
							                    <div class="4u 12u(mobile)">
                            
								                    <!-- Feature -->
									                    <section>
                                                            <div class="sectiongraph">
										                        <a href="graphs.php?league=<? echo $league1 ?>&method=S" class="image featured"><img src="<? echo $league1 ?>/S/<? echo $curyear ?>/<? echo $filearray[$league1][$curyear][$curweek] ?>.png" alt="" /></a>
                                                            </div>
									                    </section>

							                    </div>
							                    <div class="8u 12u(mobile)">

								                    <!-- Top 5 -->
                                                    <? include $league1 . "_S.php"; ?>
                                                    <? include $league1 . "_I.php"; ?>
                                                    <? include $league1 . "_W.php"; ?>

							                    </div>
						                    </div>

                                            <!-- <? echo $league2 ?> SECTION -->
											<?
												$curyear = date("Y");
												if (count($filearray[$league2][$curyear]) == 0)
												{
													$curyear = $curyear - 1;
												}
												$curweek = count($filearray[$league2][$curyear]) - 1;
											?>
											<header>
                                                <h4><? echo $league2 ?> Graphs - 
												<?
													if (strlen($filearray[$league2][$curyear][$curweek]) <= 2)
													{
														echo "Week " . $filearray[$league2][$curyear][$curweek];
													}
													else
													{
														getplayoffweek($league2, $filearray[$league2][$curyear][$curweek]);
													}
												 ?></h4>
						                    </header>
						                    <div class="row">
							                    <div class="4u 12u(mobile)">
                            
								                    <!-- Feature -->
									                    <section>
                                                            <div class="sectiongraph">
										                        <a href="graphs.php?league=<? echo $league2 ?>&method=S" class="image featured"><img src="<? echo $league2 ?>/S/<? echo $curyear ?>/<? echo $filearray[$league2][$curyear][$curweek] ?>.png" alt="" /></a>
                                                            </div>
									                    </section>

							                    </div>
							                    <div class="8u 12u(mobile)">

								                    <!-- Top 5 -->
                                                    <? include $league2 . "_S.php"; ?>
                                                    <? include $league2 . "_I.php"; ?>
                                                    <? include $league2 . "_W.php"; ?>

							                    </div>
						                    </div>

                                            <!-- <? echo $league3 ?> SECTION -->
											<?
												$curyear = date("Y");
												if (count($filearray[$league3][$curyear]) == 0)
												{
													$curyear = $curyear - 1;
												}
												$curweek = count($filearray[$league3][$curyear]) - 1;
											?>
											<header>
                                                <h4><? echo $league3 ?> Graphs - 
												<?
													if (strlen($filearray[$league3][$curyear][$curweek]) <= 2)
													{
														echo "Week " . $filearray[$league3][$curyear][$curweek];
													}
													else
													{
														getplayoffweek($league3, $filearray[$league3][$curyear][$curweek]);
													}
												 ?></h4>
						                    </header>
						                    <div class="row">
							                    <div class="4u 12u(mobile)">
                            
								                    <!-- Feature -->
									                    <section>
                                                            <div class="sectiongraph">
										                        <a href="graphs.php?league=<? echo $league3 ?>&method=S" class="image featured"><img src="<? echo $league3 ?>/S/<? echo $curyear ?>/<? echo $filearray[$league3][$curyear][$curweek] ?>.png" alt="" /></a>
                                                            </div>
									                    </section>

							                    </div>
							                    <div class="8u 12u(mobile)">

								                    <!-- Top 5 -->
                                                    <? include $league3 . "_S.php"; ?>
                                                    <? include $league3 . "_I.php"; ?>
                                                    <? include $league3 . "_W.php"; ?>

							                    </div>
						                    </div>

                                            <!-- <? echo $league4 ?> SECTION -->
											<?
												$curyear = date("Y");
												if (count($filearray[$league4][$curyear]) == 0)
												{
													$curyear = $curyear - 1;
												}
												$curweek = count($filearray[$league4][$curyear]) - 1;
											?>
											<header>
                                                <h4><? echo $league4 ?> Graphs - 
												<?
													if (strlen($filearray[$league4][$curyear][$curweek]) <= 2)
													{
														echo "Week " . $filearray[$league4][$curyear][$curweek];
													}
													else
													{
														getplayoffweek($league4, $filearray[$league4][$curyear][$curweek]);
													}
												 ?></h4>
						                    </header>
						                    <div class="row">
							                    <div class="4u 12u(mobile)">
                            
								                    <!-- Feature -->
									                    <section>
                                                            <div class="sectiongraph">
										                        <a href="graphs.php?league=<? echo $league4 ?>&method=S" class="image featured"><img src="<? echo $league4 ?>/S/<? echo $curyear ?>/<? echo $filearray[$league4][$curyear][$curweek] ?>.png" alt="" /></a>
                                                            </div>
									                    </section>

							                    </div>
							                    <div class="8u 12u(mobile)">

								                    <!-- Top 5 -->
                                                    <? include $league4 . "_S.php"; ?>
                                                    <? include $league4 . "_I.php"; ?>
                                                    <? include $league4 . "_W.php"; ?>

							                    </div>
						                    </div>

                                            <!-- <? echo $league5 ?> SECTION -->
											<?
												$curyear = date("Y");
												if (count($filearray[$league5][$curyear]) == 0)
												{
													$curyear = $curyear - 1;
												}
												$curweek = count($filearray[$league5][$curyear]) - 1;
											?>
											<header>
                                                <h4><? echo $league5 ?> Graphs - 
												<?
													if (strlen($filearray[$league5][$curyear][$curweek]) <= 2)
													{
														echo "Week " . $filearray[$league5][$curyear][$curweek];
													}
													else
													{
														getplayoffweek($league5, $filearray[$league5][$curyear][$curweek]);
													}
												 ?></h4>
						                    </header>
						                    <div class="row">
							                    <div class="4u 12u(mobile)">
                            
								                    <!-- Feature -->
									                    <section>
                                                            <div class="sectiongraph">
										                        <a href="graphs.php?league=<? echo $league5 ?>&method=S" class="image featured"><img src="<? echo $league5 ?>/S/<? echo $curyear ?>/<? echo $filearray[$league5][$curyear][$curweek] ?>.png" alt="" /></a>
                                                            </div>
									                    </section>

							                    </div>
							                    <div class="8u 12u(mobile)">

								                    <!-- Top 5 -->
                                                    <? include $league5 . "_S.php"; ?>
                                                    <? include $league5 . "_I.php"; ?>
                                                    <? include $league5 . "_W.php"; ?>

							                    </div>
						                    </div>

										</article>

								</div>

						</div>
					</div>
				</div>

<?
function getplayoffweek($leaguename, $weekid)
{
	if ($leaguename == "MLB")
	{
		if ($weekid == "501")
		{
			echo "Wild Card Round";
		}
		else if ($weekid == "502")
		{
			echo "League Divisional Series";
		}
		else if ($weekid == "503")
		{
			echo "League Championship Series";
		}
		else if ($weekid == "504")
		{
			echo "World Series";
		}
	}
	else if ($leaguename == "NBA")
	{
		if ($weekid == "501")
		{
			echo "First Round";
		}
		else if ($weekid == "502")
		{
			echo "Conference Semifinals";
		}
		else if ($weekid == "503")
		{
			echo "Conference Finals";
		}
		else if ($weekid == "504")
		{
			echo "NBA Finals";
		}
	}
	else if ($leaguename == "NFL")
	{
		if ($weekid == "501")
		{
			echo "Wild Card Round";
		}
		else if ($weekid == "502")
		{
			echo "Divisional Round";
		}
		else if ($weekid == "503")
		{
			echo "Conference Championships";
		}
		else if ($weekid == "504")
		{
			echo "Super Bowl";
		}
	}
	else if ($leaguename == "NHL")
	{
		if ($weekid == "501")
		{
			echo "Divisional Semifinals";
		}
		else if ($weekid == "502")
		{
			echo "Divisional Finals";
		}
		else if ($weekid == "503")
		{
			echo "Conference Finals";
		}
		else if ($weekid == "504")
		{
			echo "Stanley Cup Final";
		}
	}
}
?>

<? include "footer.php"; ?>