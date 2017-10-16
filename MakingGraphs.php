<? // Set variables

$mymethodinit = "X";

?><? include "header.php"; ?>

			<!-- Main -->
				<div id="main-wrapper">
					<div id="main" class="container">
						<div id="content">

							<!-- Post -->
								<article class="box post">
									<header>
										<h2><strong>Making the Graphs</strong></h2>
									</header>
                                    
									<h5>The Rules</h5>
                                    
									<p>Now that we know how to create a set of BeatWins without BeatLoops using all three 
                                    methods, we need to have a consistent set of rules with which to make the graphs. While 
                                    destroying BeatLoops gets rid of much of the ambiguity, it doesn’t eliminate it all. 
                                    These rules help establish some conventions to keep graphs consistent from week to week. 
                                    Take for example a graph with these two BeatPaths:
                                    <br />
                                    <br />
                                    A→B→C→E<br />
                                    A→D→E<br />
                                    </p>
                                    
									<p>From this we know D is both better than E but worse than A. However we are unable 
                                    to determine from this if D should be graphed on B’s level or on C’s level. Or take 
                                    this case:
                                    <br />
                                    <br />
                                    A→B→C<br />
                                    A→D<br />
                                    </p>

                                    <p>Here we know D is worse than A, but now it could be equal to B, equal to C, or 
                                    potentially worse than both. This brings us to the first set of rules with regards 
                                    to the graph.</p>

                                    <ul>
                                    <li>&bull; All arrows must point down, and therefore...</li>
                                    <li>&bull; A team must be graphed at least 1 level higher than its highest surviving direct BeatWin.</li>
                                    <li>&bull; A team must be graphed at least 1 level below its lowest surviving direct BeatLoss.</li>
                                    <li>&bull; Placement for teams with space between lowest loss and highest win will be determined by <a href="RatingsAndRankings.php">GraphScore</a>.</li>
                                    <li>&bull; All redundant arrows must be removed.</li>
                                    </ul>
                                    
									<p>The final rule cleans up the graph to make it easier to read, but in doing so removes 
                                    some information from the graph. Take the following example:
                                    <br />
                                    <br />
                                    A→B→C<br />
                                    A→C<br />
                                    </p>

                                    <p>Instead of graphing the extra arrow from A to C, it is removed as the relationship is 
                                    implied through B. While this link is removed from the graph, it is not removed for the 
                                    purpose of calculating a team’s rating, as every distinct path is important.</p>

                                    <h5>Informative Arrows</h5>

                                    <p>Beyond the rules that are mentioned above, each method has a special quirk when it 
                                    comes to drawing their arrows to help display the relative strengths of the BeatPaths. 
                                    Since the Standard Method doesn’t really use weightings, the setup is easy.</p>

                                    <ul>
                                    <li><strong><u>Standard Method Only</u></strong></li>
                                    <li>&bull; If a team has 2 or more net wins against another, the arrow is drawn bold.</li>
                                    <li>&bull; All other arrows are drawn solid.</li>
                                    </ul>

									<p>This takes into account the possibility of season sweeps for division rivals, or 
                                    playoff rematches. With the Iterative Method you start with the same principle, but as 
                                    the strength of a BeatWin lessens, the arrow changes form.</p>
                                    
                                    <ul>
                                    <li><strong><u>Iterative Method Only</u></strong></li>
                                    <li>&bull; If a BeatWin has strength of 1.5 or greater, the arrow is drawn bold.</li>
                                    <li>&bull; If a BeatWin has strength greater than or equal to 1 but less than 1.5 it is drawn with a solid arrow.</li>
                                    <li>&bull; If a BeatWin has strength greater than or equal to 0.5 but less than 1 it is drawn with a dashed arrow.</li>
                                    <li>&bull; If a BeatWin has strength less than 0.5 it is drawn with a dotted arrow.</li>
                                    </ul>

									<p>Using this set of rules makes it easy to spot the weakest links in the graph. Similarly, 
                                    the Weighted Method uses a set of rules to show relative weights of the BeatWins, but uses 
                                    color to differentiate it from the Iterative Method and its scale.</p>
                                    
                                    <ul>
                                    <li><strong><u>Weighted Method Only</u></strong></li>
                                    <li>&bull; If a BeatWin has strength equal to a blowout win, it will be drawn in blue.</li>
                                    <li>&bull; If a BeatWin has strength equal to a standard win, it will be drawn in black.</li>
                                    <li>&bull; If a BeatWin has strength equal to a narrow win, it will be drawn in red.</li>
                                    </ul>

									<p>The threshold for what constitutes a blowout or a narrow victory varies by sport. Additionally, 
                                    teams in the same division have the same background color, and teams in the same conference have 
                                    the same border color. As mentioned above, any team whose placement is still ambiguous will have 
                                    its placement determined by their GraphScore. Continue to the next page to see how these 
                                    are calculated.</p>

                                    <a href="RatingsAndRankings.php" class="button">Ratings and Rankings &nbsp;<div class="icon fa-arrow-right right"></div></a>
								</article>

						</div>
					</div>
				</div>

<? include "footer.php"; ?>