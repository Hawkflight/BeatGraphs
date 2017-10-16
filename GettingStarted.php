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
										<h2><strong>Getting Started</strong></h2>
									</header>

									<h5>What is the goal of BeatGraphs?</h5>

									<p>The best place to start when it comes to the BeatGraphs approach is to 
                                    understand the purpose of the method we use. Every week throughout the NFL 
                                    season there is site after site proudly displaying Power Rankings which list 
                                    teams in order from best to worst. The problem is that no sites ever agree 
                                    and it can be difficult to determine who has more merit. Most rankings are 
                                    completely subjective and are severely influenced by the author's biases. 
                                    On the other end of the spectrum are those who seek to completely eliminate 
                                    human bias by making pure statistical analysis which provides a final list.</p>

                                    <p>In both cases, comments to these methods almost always boil down to "team 
                                    A beat team B, so why is B ranked higher than A?" While there usually are 
                                    straightforward answers to this question, A got a lucky bounce, B had a key 
                                    player injured, or a ref made a bad call, in the end it is irrelevant and A 
                                    still defeated B. Therefore the purpose of BeatGraphs is to make a fully 
                                    objective analysis of the league's teams to provide rankings that cannot be 
                                    defeated by a chain of A beat B beat C scenarios. In the end, when all 
                                    ambiguity is removed, if A has beaten B, A will be ranked above B.</p>

                                    <h5>Resolving the Ambiguity</h5>

									<p>Unfortunately, it's rarely so easy as to follow a chain down from one 
                                    team to another. Most of the time, either the two teams you're trying to 
                                    compare can't be linked together, or you can find contradicting paths where 
                                    both teams have links that lead to the other. In the latter case, called a 
                                    <a href="BreakingBeatloops.php">BeatLoop</a>, we must find a way to decide which of the teams involved in the 
                                    loop is the best, if any is at all. This is where the theory of this site 
                                    takes place. We currently have three methods by which we resolve these loops. 
                                    Below are the basic differences. More in depth explanation will be given later.</p>

                                    <p><strong>Standard Method</strong> - All links in a loop are destroyed giving no preference to any.<br />
                                    <strong>Iterative Method</strong> - The most common links in the loops are destroyed first.<br />
                                    <strong>Weighted Method</strong> - The links with the lowest point differential are destroyed first.</p>

                                    <p>Once all loops are resolved by these methods, we are able to draw a graph 
                                    which represents the relative strength of the teams based on these results. 
                                    From here, we calculate scores for each team based on the graph. This helps 
                                    us compare teams that have no direct relation to each other on the graph, 
                                    and gives us an understanding of how strong a team is compared to the rest 
                                    of the league. </p>

									<h5>What does it all mean?</h5>

									<p>The first thing that needs to be understood about BeatGraphs is that it 
                                    is a descriptive system, not a predictive one. By this, we mean that it is 
                                    not our goal to guess who will win next week and make money through gambling. 
                                    In fact, from a predictive standpoint, BeatGraphs doesn't do any better than 
                                    most pundits. We only track BeatGraph picks for entertainment value, and to 
                                    see if any method has significantly better performance than another. In 
                                    practice though, BeatGraphs is about taking what has already happened and 
                                    giving us a way to look at the season's results in a quick and easy way 
                                    without letting opinions or conjecture play a role.</p>

                                    <p>Now if you're ready for the nuts and bolts, see the next section where 
                                    we break it down show you how it all works, starting with the basics.</p>
                                    
                                    <a href="BeatGraphBasics.php" class="button">BeatGraph Basics &nbsp;<div class="icon fa-arrow-right right"></div></a>
								</article>

						</div>
					</div>
				</div>

<? include "footer.php"; ?>