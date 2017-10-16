<?
$mymonth = date("M");

if ($mymonth == "Feb" || $mymonth == "Mar" || $mymonth == "Apr" || $mymonth == "May")
{
	$league1 = "NBA";
	$league2 = "NHL";
	$league3 = "MLB";
	$league4 = "NFL";
	$league5 = "NCAAF";
}
else if ($mymonth == "Jun" || $mymonth == "Jul" || $mymonth == "Aug" || $mymonth == "Sep")
{
	$league1 = "MLB";
	$league2 = "NFL";
	$league3 = "NCAAF";
	$league4 = "NBA";
	$league5 = "NHL";
}
else if ($mymonth = "Oct" || $mymonth == "Nov" || $mymonth == "Dec" || $mymonth == "Jan")
{
	$league1 = "NFL";
	$league2 = "NCAAF";
	$league3 = "NBA";
	$league4 = "NHL";
	$league5 = "MLB";
}
else
{
	$league1 = $mymonth;
	$league2 = $mymonth;
	$league3 = $mymonth;
	$league4 = $mymonth;
	$league5 = $mymonth;
}
?>