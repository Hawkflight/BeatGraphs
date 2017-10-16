function togglepathbox() 
{
	if (document.getElementById("pathbox").style.display == "" || document.getElementById("pathbox").style.display == "none")
	{
		document.getElementById("pathbox").style.display = "inline";
		document.getElementById("pathboxcontrol").innerHTML = "Hide paths and loops";
	}
	else
	{
		document.getElementById("pathbox").style.display = "none";
		document.getElementById("pathboxcontrol").innerHTML = "Show paths and loops";
	}
}

function clearcookies()
{
	window.location = "clear.php";
}