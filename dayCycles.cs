function getDayCycleTime() {
	if (!$EnvGuiServer::DayCycleEnabled || !isObject(DayCycle)) {
		error("ERROR: DayCycle not enabled.");
		return -1;
	}

	%time = $Sim::Time / DayCycle.dayLength + DayCycle.dayOffset;
	return %time - mFloor(%time);
}

function getDayCycleStage(%time) {
	return mFloor(%time / 0.25);
}

function getDayCycleStageName(%stage) {
	switch (%stage) {
		case 0: return "dawn";
		case 1: return "day";
		case 2: return "dusk";
		case 3: return "night";

		default: return "error";
	}
}

function getMSToNextDayCycleStage(%time) {
	%cycles = (mFloor(%time / 0.25) + 1) * 0.25 - %time;
	return mFloor(%cycles * DayCycle.dayLength * 1000);
}

function getDayCycleTimeString(%time, %mod12) {
	%time = getTimeString(mFloor((%time * 86400) / 60));

	if (!%mod12) {
		if (strLen(%time) == 3) {
			return 0 @ %time;
		}

		return %time;
	}

	%time = strReplace(%time, ":", " ");

	%hour = getWord(%time, 0);
	%mins = getWord(%time, 1);

	if (%hour >= 13) {
		return %hour - 12 @ ":" @ %mins SPC "PM";
	}

	return %hour @ ":" @ %mins SPC "AM";
}

function getSunVector() {
	%azim = mDegToRad($EnvGuiServer::SunAzimuth);

	if ($EnvGuiServer::DayCycleEnabled && isObject(DayCycle)) {
		%time = getDayCycleTime();
		%badspotIsWeird = 0.583;

		if (%time < %badspotIsWeird) {
			%elev = mDegToRad((%time / %badspotIsWeird) * 180);
		}
		else {
			%elev = mDegToRad(180 + ((%time - %badspotIsWeird) / (1 - %badspotIsWeird)) * 180);
		}
	}
	else {
		%elev = mDegToRad($EnvGuiServer::SunElevation);
	}

	%h = mCos(%elev);
	return %h * mSin(%azim) SPC %h * mCos(%azim) SPC mSin(%elev);
}

function isPointInShadow(%pos, %ignore) {
	%ray = containerRayCast(%pos,
		vectorAdd(%pos, vectorScale(getSunVector(), 250)),
		$TypeMasks::StaticShapeObjectType |
		$TypeMasks::FxBrickObjectType |
		$TypeMasks::VehicleObjectType |
		$TypeMasks::PlayerObjectType |
		$TypeMasks::ItemObjectType,
		%ignore
	);

	return %ray !$= 0;
}

function syncDayCycle() {
	if (!isObject(DayCycle)) {
		error("ERROR: DayCycle does not exist.");
		return;
	}

	if (DayCycle.dayLength != 86400) {
		error("ERROR: DayCycle length is not 86400.");
		return;
	}

	%all = strReplace(getWord(getDateTime(), 1), ":", " ");

	%real = getWord(%all, 0) * 3600 + getWord(%all, 1) * 60 + getWord(%all, 2);
	%curr = $Sim::Time / 86400;

	DayCycle.setDayOffset(%real - (%curr - mFloor(%curr)));
}
