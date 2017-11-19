//
// EventScript Server
// Author: McTwist (9845)
// Date: 2017-11-19
//
// Contains the server version of transforming an EventScript list object
// into either readable script or usable events.


// Load needed script file
exec("./script.cs");

// Save current brick events and return a script
function EventScriptServer_save(%brick)
{
	if (!isObject(%brick))
		return;

	%list = new ScriptObject();
	%list.count = 0;

	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		// Store variables
		%list.value[%i, "enabled"] = %brick.eventEnabled[%i];
		%list.value[%i, "delay"] = %brick.eventDelay[%i];
		%list.value[%i, "inputEventName"] = %brick.eventInput[%i];
		%list.value[%i, "outputEventName"] = %brick.eventOutput[%i];

		// Target
		if (%brick.eventTarget[%i] $= "-1")
		{
			%NTName = %brick.eventNT[%i];
			%list.value[%i, "NTName"] = getSubStr(%NTName, 1, strlen(%NTName));
			%targetClass = fxDTSBrick;
		}
		else
		{
			%list.value[%i, "targetName"] = %brick.eventTarget[%i];
			%targetClass = inputEvent_GetTargetClass(%brick.getClassName(), %brick.eventInputIdx[%i], %brick.eventTargetIdx[%i]);
		}

		// Parameters
		%paramCount = outputEvent_GetNumParametersFromIdx(%targetClass, %brick.eventOutputIdx[%i]);
		%paramList = $OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%i]];

		%params = "";
		for (%n = 0; %n < %paramCount; %n++)
		{
			%paramField = getField(%paramList, %n);
			%param = %brick.eventOutputParameter[%i, %n+1];

			// Translate parameter
			switch$ (getWord(%paramField, 0))
			{
			case "int":
				%param = atoi(%param);
			case "intList":
				%param = atoi(getWord(%paramField, 1 + (%param * 2)));
			case "float":
				%param = atof(%param);
			case "bool":
				%param = !!%param;
			case "string":
				%param = %param;
			case "datablock":
				if (isObject(%param))
					%param = %param.uiName;
				else
					%param = "";
			case "vector":
				%param = %param;
			case "list":
				%param = getWord(%paramField, 1 + (%param * 2));
			case "paintColor":
				%param = getColorIDTable(%param);
			}

			%params = setField(%params, %n, %param);
		}

		%list.value[%i, "params"] = %params;

		%list.count++;
	}

	%script = EventScript_toScript(%list);
	%list.delete();

	return %script;
}

// Give a script to a brick to produce events
function EventScriptServer_load(%brick, %script)
{
	if (!isObject(%brick))
		return;
	
	%list = EventScript_fromScript(%script);

	// Handle errors
	if (%list.error)
	{
		%list.delete();
		return;
	}

	%brick.clearEvents();

	%brick.numEvents = 0;

	for (%i = 0; %i < %list.count; %i++)
	{
		%line = %list.value[%i, "line"];
		%enabled = %list.value[%i, "enabled"];
		%delay = %list.value[%i, "delay"];
		%inputEventName = %list.value[%i, "inputEventName"];
		%targetName = %list.value[%i, "targetName"];
		%NTName = %list.value[%i, "NTName"];
		%outputEventName = %list.value[%i, "outputEventName"];
		%params = %list.value[%i, "params"];

		// Enabled
		%brick.eventEnabled[%brick.numEvents] = !!%enabled;

		// Delay
		%brick.eventDelay[%brick.numEvents] = mClamp(%delay, 0, 30000);

		// Input event
		%inputEventIdx = inputEvent_GetInputEventIdx(%inputEventName);
		if (%inputEventIdx < 0)
		{
			error("EventScriptServer_load :: Invalid input name \"" @ %inputEventName @ "\" on line " @ %line);
			return;
		}
		%brick.eventInput[%brick.numEvents] = %inputEventName;
		%brick.eventInputIdx[%brick.numEvents] = %inputEventIdx;

		// Target name
		if (%targetName $= "")
		{
			%targetName = "-1";
			%prev = %NTName;
			%NTName = "_" @ %NTName;
			if (!isObject(%NTName))
			{
				error("EventScriptServer_load :: Invalid target name \"" @ %prev @ "\" on line " @ %line);
				return;
			}
			%prev = "";
		}
		else
		{
			%targetIdx = inputEvent_GetTargetIndex(fxDtsBrick, %inputEventIdx, %targetName);
			if (%targetIdx < 0)
			{
				error("EventScriptServer_load :: Invalid target name \"" @ %targetName @ "\" on line " @ %line);
				return;
			}
		}

		%brick.eventTarget[%brick.numEvents] = %targetName;
		%brick.eventTargetIdx[%brick.numEvents] = %targetIdx;
		%brick.eventNT[%brick.numEvents] = %NTName;

		// Get target class
		%targetClass = %targetIdx >= 0 ? inputEvent_GetTargetClass(%brick.getClassName(), %inputEventIdx, %targetIdx) : fxDTSBrick;

		// Output event
		%outputEventIdx = outputEvent_GetOutputEventIdx(%targetClass, %outputEventName);
		if (%outputEventIdx < 0)
		{
			error("EventScriptServer_load :: Invalid output name \"" @ %outputEventName @ "\" on line " @ %line);
			return;
		}
		%brick.eventOutput[%brick.numEvents] = %outputEventName;
		%brick.eventOutputIdx[%brick.numEvents] = %outputEventIdx;

		// Parameters
		%paramCount = outputEvent_GetNumParametersFromIdx(%targetClass, %outputEventIdx);
		%paramList = $OutputEvent_parameterList[%targetClass, %outputEventIdx];
		%count = getFieldCount(%params);
		if (%paramCount != %count)
		{
			error("EventScriptServer_load :: Invalid amount of parameters for \"" @ %outputEventName @ "\" on line " @ %line);
			return;
		}

		for (%n = 0; %n <= %count; %n++)
		{
			%paramField = getField(%paramList, %n);
			%param = getField(%params, %n);
			
			// Translate parameter
			switch$ (strlwr(getWord(%paramField, 0)))
			{
			case "int":
				%min = mFloor(getWord(%paramField, 1));
				%max = mFloor(getWord(%paramField, 2));
				if (%param $= "")
					%param = mFloor(getWord(%paramField, 3));
				%param = mClamp(%param, %min, %max);

			case "intlist":
				if (%param !$= "ALL")
				{
					%count = getWordcount(%param);
					for (%m = 0; %m < %count; %m++)
						setWord(%param, %m, atoi(getWord(%param, %m)));
				}

			case "float":
				%min = atof(getWord(%paramField, 1));
				%max = atof(getWord(%paramField, 2));
				%step = mAbs(getWord(%paramField, 3));
				if (%param $= "")
					%param = atof(getWord(%paramField, 4));
				%param = mClampf(%param, %min, %max);
				%steps = mFloor((%param - %min) / %step);
				%param = %min + %steps * %step;

			case "bool":
				%param = !!%param;

			case "string":
				// TODO: Add more checks for ML control chars
				%param = getSubStr(%param, 0, mFloor(getWord(%paramField, 1)));
				%param = chatWhiteListFilter(%param);
				%param = strReplace(%param, ";", "");

			case "datablock":
				%uiName = %param;
				%type = getWord(%paramField, 1);
				
				switch$ (strlwr(%type))
				{
				case "fxbrickdata":
					%param = $uiNameTable[%param];
				case "fxlightdata":
					%param = $uiNameTable_Lights[%param];
				case "particleemitterdata":
					%param = $uiNameTable_Emitters[%param];
				case "itemdata":
					%param = $uiNameTable_Items[%param];
				case "audioprofile":
					%param = $uiNameTable_Music[%param];
					if (!isObject(%param))
						%param = $uiNameTable_Sounds[%param];
				case "playerdata":
					%param = $uiNameTable_Vehicle[%param];
					if (!isObject(%param))
						%param = $uiNameTable_Player[%param];
				case "wheeledvehicledata":
					%param = $uiNameTable_Vehicle[%param];
				case "flyingvehicledata":
					%param = $uiNameTable_Vehicle[%param];
				case "hovervehicledata":
					%param = $uiNameTable_Vehicle[%param];
				default:
					// Apparently this is also legal
					%param = $uiNameTable_[%type @ %param];
				}

				if (isObject(%param))
					%param = %param.getID();
				else
				{
					warn("EventScriptServer_load :: Datablock uiName \"" @ %uiName @ "\" does not exist on line " @ %line);
					%param = -1;
				}

			case "vector":
				%x = atof(getWord(%param, 0));
				%y = atof(getWord(%param, 1));
				%z = atof(getWord(%param, 2));
				%mag = atoi(getWord(%parmaField, 1));
				if (%mag == 0)
					%mag = 200;
				%vec = %x SPC %y SPC %z;
				if (vectorLen(%vec) > %mag)
				{
					%vec = vectorNormalize(%vec);
					%vec = vectorScale(%vec, %mag);
				}
				%param = %vec;

			case "list":
				%count = getWordcount(%paramField);
				%found = "";

				for (%m = 0; %m < %count; %m++)
				{
					if (stricmp(%param, getWord(%paramField, 1 + (%m * 2))) == 0)
					{
						%found = getWord(%paramField, 2 + (%m * 2));
						break;
					}
				}

				if (%found $= "")
				{
					warn("EventScriptServer_load :: Item name \"" @ %param @ "\" does not exist in list on line " @ %line);
					%found = getWord(%paramField, 2);
				}

				%param = %found;

			case "paintcolor":
				// Locate the closest color
				%dist = 2;
				%closest = 0;
				%par = %param;
				for (%l = 0; %l < 64; %l++)
				{
					%col = getColorIDTable(%l);
					// abs(RGB - RGB) + (A - A)^2
					%d = vectorDist(getWords(%par, 0, 3), getWords(%col, 0, 3))
						+ mPow(getWord(%par, 3) - getWord(%col, 3), 2);
					if (%d < %dist)
					{
						%dist = %d;
						%closest = %l;
						// Closest as we can get
						if (%dist == 0)
							break;
					}
				}
				if (%dist > 0)
					warn("EventScriptServer_load :: Colorset does not contain the desired color on line " @ %line);
				%param = %closest;
			}
			// Store it
			%brick.eventOutputParameter[%brick.numEvents, %n+1] = %param;
		}

		// Append client
		%brick.eventOutputAppendClient[%brick.numEvents] = $OutputEvent_AppendClient[%targetClass, %outputEventIdx];

		%brick.numEvents++;
	}

	%list.delete();
}

