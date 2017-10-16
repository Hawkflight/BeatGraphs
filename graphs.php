<? include "filelist.php"; ?>
<? // Set variables

$league = $_GET['league'];
$method = $_GET['method'];
$season = $_GET['season'];
$weekid = $_GET['weekid'];

if ($filearray[$league] == "")
	$league = "NFL";
if ($method != "I" && $method != "W" && $method != "T")
	$method = "S";
if ($season == "")
{
	foreach ($filearray[$league] as $i => $value)
	{
		$season = $i;
	}
}
if ($weekid == "")
{	
	foreach ($filearray[$league][$season] as $i => $value)
	{
		$weekid = $value;
	}
}

?>
<? include "header.php"; ?>
<? include "navigate.php"; ?>
<? include "footer.php"; ?>