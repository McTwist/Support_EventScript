// Create a possible bind to a function. This will place it in respective division.
// If division and name is the same, it will replace the function.
function CreateBind(%division, %name, %cmd)
{
	if (%name $= "" || %cmd $= "")
		return;
	
	%remapid = $remapCount;
	%new = true;
	
	if (%division !$= "")
	{
		// Find division
		for (%i = 0; %i < $remapCount; %i++)
		{
			// Found division
			if ($remapDivision[%i] $= %division || (%new == false && $remapDivision[%i] $= ""))
			{
				// Put it in the end
				%remapid = %i+1;
				%new = false;
				
				// Replace function
				if ($remapName[%i] $= %name)
				{
					$remapCmd[%i] = %cmd;
					return;
				}
			}
			// End of division
			else if (%new == false)
			{
				break;
			}
		}
		
		// Move binds
		for (%i = $remapCount; %i > %remapid; %i--)
		{
			$remapDivision[%i] = $remapDivision[%i-1];
			$remapName[%i] = $remapName[%i-1];
			$remapCmd[%i] = $remapCmd[%i-1];
		}
	}
	// Append bind
	else
	{
		%new = false;
	}
	
	// Add new one
	if (%new)
		$remapDivision[%remapid] = %division;
	$remapName[%remapid] = %name;
	$remapCmd[%remapid] = %cmd;
	$remapCount++;
}
