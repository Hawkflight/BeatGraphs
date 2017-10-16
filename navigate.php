<!-- Main -->
<div id="main-wrapper">
  <div id="main" class="container">
    <div id="content">

      <!-- Post -->
      <article class="box post">
        <header>
          <h2>
            <strong>
              <? if ($method == "S")
	                echo "Standard";
                else if ($method == "I")
	                echo "Iterative";
                else if ($method == "W")
	                echo "Weighted";
                else
	                echo "_" . $method . "_";
                ?> Method</strong>
          </h2>
        </header>

        <div class="navbartable">
          <div class="navbarrow year">
            <? if ($season == '1970')
              {
              ?>
                <div class="navbarcell tl dead">- <? echo ($season - 1) ?> -</div>
              <?
              }
              else
              {
              ?>
              <div class="navbarcell tl">
                <a href="graphs.php?league=<? echo $league; ?>&method=<? echo $method ?>&season=<? echo ($season - 1) ?>&weekid=<? echo $weekid ?>">
                    <span class="prevtext"> <? echo ($season - 1) ?> </span>
                </a>
              </div>
              <?
              }
              ?>
            <div class="navbarcell tm">
             <?
                echo $league ?> <? echo $season; ?><?
                if ($league == "NHL" || $league == "NBA")
                {
	                echo ("-" . ($season + 1));
                }
                ?> - <?
                if ($weekid > 500)
                {
	                if ($weekid == 501)
	                {
		                if ($league == "NFL")
		                {
			                echo ("Wild Card Week");
		                }
		                else
		                {
			                echo ("First Round");
		                }
	                }
	                else if ($weekid == 502)
	                {
		                echo ("Divisional Round");
	                }
	                else if ($weekid == 503)
	                {
		                if ($league == "MLB")
		                {
			                echo ("League Championships");
		                }
		                else
		                {
			                echo ("Conference Championships");
		                }
	                }
	                else if ($weekid == "504")
	                {
		                if ($league == "MLB")
		                {
			                echo ("World Series");
		                }
		                else if ($league == "NBA")
		                {
			                echo ("NBA Finals");
		                }
		                else if ($league == "NFL")
		                {
			                echo ("Super Bowl");
		                }
		                else if ($league == "NHL")
		                {
			                echo ("Stanley Cup");
		                }
	                }
                }
                else
                {
	                echo ("Week " . $weekid);
                }
                ?>
            </div>
            <? if (count($filearray[$league][$season + 1]) == 0)
              {
              ?>
                
            <div class="navbarcell tr dead">
              - <? echo ($season + 1) ?> -
            </div>
            <?
              }
              else
              {
              ?>
              
            <div class="navbarcell tr">
              <a href="graphs.php?league=<? echo $league; ?>&method=<? echo $method ?>&season=<? echo ($season + 1) ?>&weekid=<? echo $weekid ?>">
                    <span class="posttext">
                  <? echo ($season + 1) ?> 
                </span>
              </a>
            </div>
            <?
              }
              ?>
          </div>
        </div>
        <div class="navbartable">
          <div class="navbarrow week">
            <?
	          foreach ($filearray[$league][$season] as $i => $value)
	          {
		          if ($weekid == $value)
		          {
			          echo ("<div class='navbarcell dead'>");
              }
              else
              {
                echo ("<div class='navbarcell'>");
                echo ("<a href='graphs.php?league=$league&method=$method&season=$season&weekid=$value'>");
              }

              if ($value < 500)
		          {
			          echo ($value);
		          }
		          else if ($value == 501)
		          {
			          if ($league == "NFL")
			          {
				          echo ("WC");
			          }
			          else
			          {
				          echo ("Rnd1");
			          }
		          }
		          else if ($value == 502)
		          {
			          echo ("Div");
		          }
		          else if ($value == 503)
		          {
			          if ($league == "MLB")
			          {
				          echo ("LCS");
			          }
			          else
			          {
				          echo ("Conf");
			          }
		          }
		          else if ($value == "504")
		          {
			          if ($league == "MLB")
			          {
				          echo ("WS");
			          }
			          else if ($league == "NBA")
			          {
				          echo ("Finals");
			          }
			          else if ($league == "NFL")
			          {
				          echo ("SB");
			          }
			          else if ($league == "NHL")
			          {
				          echo ("Cup");
			          }
		          }
              
              if ($weekid == $value)
		          {
			          echo ("</div>");
              }
              else
              {
                echo ("</a></div>");
              }
            }
            ?>

                </div>
            </div>
        <div class="navbartable">
          <div class="navbarrow method">
            <?
	          if ($method == "S")
	          {
		          echo ("<div class='navbarcell dead'>Standard</div>");
            }
            else
            {
              echo ("<div class='navbarcell'><a href='graphs.php?league=$league&method=S&season=$season&weekid=$weekid'>Standard</a></div>");
            }
            if ($method == "I")
            {
            echo ("<div class='navbarcell dead'>Iterative</div>");
            }
            else
            {
            echo ("<div class='navbarcell'><a href='graphs.php?league=$league&method=I&season=$season&weekid=$weekid'>Iterative</a></div>");
            }
            if ($method == "W")
            {
            echo ("<div class='navbarcell dead'>Weighted</div>");
            }
            else
            {
            echo ("<div class='navbarcell'><a href='graphs.php?league=$league&method=W&season=$season&weekid=$weekid'>Weighted</a></div>");
            }
            ?>

          </div>
        </div>
        <div class="navbartable">
          <div class="navbarrow method">
            <div class="navbarcell">
              <span class="hoverhand" id="pathboxcontrol" onclick='togglepathbox()'>Show paths and loops</span>
            </div>
            <div class="navbarcell">
			
            </div>
          </div>
        </div>

        <div id='pathbox'>
          <? include "$league/$method/$season/$weekid.php"; ?>
								</article>

    </div>
  </div>
</div>
