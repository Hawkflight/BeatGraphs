<? // Set variables

$mymethodinit = "X";

?><? include "header.php"; ?>
<? include "filelist.php"; ?>

<script language="javascript">

var arrFileList = new Array(4);

<?
echo ("arrFileList[0] = new Array(" . count($filearray["MLB"]) . ");\n");
echo ("arrFileList[1] = new Array(" . count($filearray["NBA"]) . ");\n");
echo ("arrFileList[2] = new Array(" . count($filearray["NFL"]) . ");\n");
echo ("arrFileList[3] = new Array(" . count($filearray["NHL"]) . ");\n");
echo ("arrFileList[4] = new Array(" . count($filearray["NCAAF"]) . ");\n");

for ($i = 0; $i < date("Y") - 1970; $i++)
{
	echo ("arrFileList[0][$i] = new Array(" . count($filearray["MLB"][$i + 1970]) . ");\n");
	echo ("arrFileList[1][$i] = new Array(" . count($filearray["NBA"][$i + 1970]) . ");\n");
	echo ("arrFileList[2][$i] = new Array(" . count($filearray["NFL"][$i + 1970]) . ");\n");
	echo ("arrFileList[3][$i] = new Array(" . count($filearray["NHL"][$i + 1970]) . ");\n");
	if ($i + 1970 >= 2015)
		echo ("arrFileList[4][$i] = new Array(" . count($filearray["NCAAF"][$i + 1970]) . ");\n");


	$count = 0;
	foreach ($filearray["MLB"][$i + 1970] as $j => $value)
	{
		echo ("arrFileList[0][$i][$count] = $value;\n");	
		$count++;
	}
	$count = 0;
	foreach ($filearray["NBA"][$i + 1970] as $j => $value)
	{
		echo ("arrFileList[1][$i][$count] = $value;\n");	
		$count++;
	}
	$count = 0;
	foreach ($filearray["NFL"][$i + 1970] as $j => $value)
	{
		echo ("arrFileList[2][$i][$count] = $value;\n");	
		$count++;
	}
	$count = 0;
	if (($i + 1970) != 2004)
	{
		foreach ($filearray["NHL"][$i + 1970] as $j => $value)
		{
			echo ("arrFileList[3][$i][$count] = $value;\n");
			$count++;	
		}
	}
	$count = 0;
	if (($i + 1970) >= 2015)
	{
		foreach ($filearray["NCAAF"][$i + 1970] as $j => $value)
		{
			echo ("arrFileList[4][$i][$count] = $value;\n");
			$count++;	
		}
	}
}
?>

function SeasonPopulate()
{
	document.getElementById("Season").length = 0;
	document.getElementById("WeekID").length = 0;

	if (document.getElementById("League").selectedIndex == 0)
	{
		if (!document.getElementById("Season").disabled)
			document.getElementById("Season").disabled = !document.getElementById("Season").disabled;
		if (!document.getElementById("WeekID").disabled)
			document.getElementById("WeekID").disabled = !document.getElementById("WeekID").disabled;
		if (!document.getElementById("Graph").disabled)
			document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;
		return;
	}

	if (document.getElementById("Season").disabled)
		document.getElementById("Season").disabled = !document.getElementById("Season").disabled;
	if (!document.getElementById("WeekID").disabled)
		document.getElementById("WeekID").disabled = !document.getElementById("WeekID").disabled;
	if (!document.getElementById("Graph").disabled)
		document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;

	var newOption = document.createElement('option');
	newOption.text = "--Season--";
	newOption.value = -1;
	document.getElementById("Season").options.add(newOption);

	for (var i=arrFileList[document.getElementById("League").selectedIndex - 1].length - 1; i >= 0; i--)
	{
		if (document.getElementById("League").selectedIndex == 5 && i < 45) //Starts NCAAF at 2015
			continue;

		newOption = document.createElement('option');
		document.getElementById("Season").options.add(newOption);
		if (document.getElementById("League").selectedIndex == 2 || document.getElementById("League").selectedIndex == 4)
			newOption.text = (1970 + i) + "-" + (1971 + i);
		else
			newOption.text = (1970 + i);
		newOption.value = (1970 + i);

		try
		{
			document.getElementById("Season").options.add(newOption);
		}
		catch (ex)
		{
			//document.getElementById("Season").options[document.getElementById("Season").options.length]=new Option("Sports", "sportsvalue");
		}
	}
}

function WeekPopulate()
{
	document.getElementById("WeekID").length = 0;

	if (document.getElementById("Season").selectedIndex == 0)
	{
		if (!document.getElementById("WeekID").disabled)
			document.getElementById("WeekID").disabled = !document.getElementById("WeekID").disabled;
		if (!document.getElementById("Graph").disabled)
			document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;
		return;
	}
	
	if (document.getElementById("WeekID").disabled)
		document.getElementById("WeekID").disabled = !document.getElementById("WeekID").disabled;
	if (!document.getElementById("Graph").disabled)
		document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;

	var newOption = document.createElement('option');
	newOption.text = "--Week--";
	newOption.value = -1;
	document.getElementById("WeekID").options.add(newOption);
	
	for (var i=0; i < arrFileList[document.getElementById("League").selectedIndex - 1][arrFileList[document.getElementById("League").selectedIndex - 1].length - document.getElementById("Season").selectedIndex].length; i++)
	{
		newOption = document.createElement('option');
		document.getElementById("WeekID").options.add(newOption);
		var arrWeeks = arrFileList[document.getElementById("League").selectedIndex - 1][arrFileList[document.getElementById("League").selectedIndex - 1].length - document.getElementById("Season").selectedIndex][i];
		var iLeague = document.getElementById("League").selectedIndex;

		if (arrWeeks < 500)
		{
			newOption.text = arrWeeks;
		}
		else if (arrWeeks == 501)
		{
			if (iLeague == 3)
			{
				newOption.text = "Wild Card";
			}
			else
			{
				newOption.text = "Round 1";
			}
		}
		else if (arrWeeks == 502)
		{
			if (iLeague == 1)
			{
				newOption.text = "LDS";
			}
			else
			{
				newOption.text = "Divisional";
			}
		}
		else if (arrWeeks == 503)
		{
			if (iLeague == 1)
			{
				newOption.text = "LCS";
			}
			else
			{
				newOption.text = "Conference";
			}
		}
		else if (arrWeeks == 504)
		{
			if (iLeague == 1)
			{
				newOption.text = "World Series";
			}
			else if (iLeague == 2)
			{
				newOption.text = "NBA Finals";
			}
			else if (iLeague == 3)
			{
				newOption.text = "Super Bowl";
			}
			else if (iLeague == 4)
			{
				newOption.text = "Stanley Cup";
			}
		}

		newOption.value = arrWeeks;
		try
		{
			document.getElementById("WeekID").options.add(newOption);
		}
		catch (ex)
		{
			//document.getElementById("Season").options[document.getElementById("Season").options.length]=new Option("Sports", "sportsvalue");
		}
	}
}

function ActivateButton()
{
	if (document.getElementById("WeekID").selectedIndex == 0)
	{
		if (!document.getElementById("Graph").disabled)
			document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;
		return;
	}
	
	if (document.getElementById("Graph").disabled)
		document.getElementById("Graph").disabled = !document.getElementById("Graph").disabled;
}

function ShowGraph()
{
	if (document.getElementById("Method").selectedIndex == 0 || document.getElementById("League").selectedIndex == 0 || document.getElementById("Season").selectedIndex == 0 || document.getElementById("WeekID").selectedIndex == 0)
	{
		alert("You must select a valid Method, League, Season, and Week to continue.");
		return;
	}

	window.location = "graphs.php?method=" + document.getElementById('Method').options[document.getElementById('Method').selectedIndex].value +
			"&league=" + document.getElementById('League').options[document.getElementById('League').selectedIndex].value +
			"&season=" + document.getElementById('Season').options[document.getElementById('Season').selectedIndex].value +
			"&weekid=" + document.getElementById('WeekID').options[document.getElementById('WeekID').selectedIndex].value;
}

</script>

			<!-- Main -->
				<div id="main-wrapper">
					<div id="main" class="container">
						<div id="content">

							<!-- Post -->
								<article class="box post">
									<header>
										<h2><strong>Historical Search</strong></h2>
									</header>

									Use the options below to look up a graph and ranking list for any week from any season dating back to 1970.<br />
                                    <br />
                                    <div style="width: 110px" title="Graph Method: ">Graph Method: </div>
                                    <select style="width: 100px" id="Method">
	                                    <option value="-1">--Method--</option>
	                                    <option value="S">Standard</option>
	                                    <option value="I">Iterative</option>
	                                    <option value="W">Weighted</option>
                                    </select><br />
                                    <br />
                                    <div style="width: 110px" title="League: ">League: </div>
                                    <select style="width: 100px" id="League" onchange="javascript:SeasonPopulate();" >
	                                    <option value="-1">--League--</option>
	                                    <option value="MLB">MLB</option>
	                                    <option value="NBA">NBA</option>
	                                    <option value="NFL">NFL</option>
	                                    <option value="NHL">NHL</option>
	                                    <option value="NCAAF">NCAAF</option>
                                    </select><br />
                                    <br />
                                    <div style="width: 110px" title="Season: ">Season: </div>
                                    <select style="width: 100px" id="Season" onchange="javascript:WeekPopulate();" disabled="disabled">
                                    </select><br />
                                    <br />
                                    <div style="width: 110px" title="Week: ">Week: </div>
                                    <select style="width: 100px" id="WeekID" onchange="javascript:ActivateButton();" disabled="disabled">
                                    </select>
                                    <br /><br />
                                    <input type="button" id="Graph" value="Search" onclick="ShowGraph()" disabled="disabled" />
								</article>

						</div>
					</div>
				</div>

<? include "footer.php"; ?>