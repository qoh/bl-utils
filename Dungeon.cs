function Dungeon() {
	return new ScriptGroup() {
		class = Dungeon;
	};
}

function Dungeon::generate(%this, %size, %specials) {
	if (%this.getCount()) {
		%error = "ERROR: Attempting to generate from an already generated dungeon.";
		%error = %error NL "Use Dungeon::reset() first or create a new Dungeon().";

		error(%error);
		return 0;
	}

	if (%size $= "") {
		%size = 15;
	}

	%this.setRoom(0, 0, "start");

	while (%this.getCount() < %size) {
		%candidates = %this.getCandidates();

		if (%candidates $= "") {
			error("ERROR: Could only generate" SPC %this.getCount() SPC "out of" SPC %size SPC "room(s).");
			return 0;
		}

		%index = getRandom(0, getFieldCount(%candidates) - 1);
		%point = getField(%candidates, %index);

		%this.setRoom(getWord(%point, 0), getWord(%point, 1), "room");
	}

	if (%specials !$= "") {
		%count = getFieldCount(%specials);

		for (%i = 0; %i < %count; %i++) {
			%leaves = %this.getLeaves();

			if (%leaves $= "") {
				warn("WARN: Could only allocate" SPC %i SPC "out of" SPC %count SPC "special room(s).");
				break;
			}

			%index = getRandom(0, getFieldCount(%leaves) - 1);
			%point = getField(%leaves, %index);

			%this.setRoom(getWord(%point, 0), getWord(%point, 1), getField(%specials, %i));
		}
	}

	return 1;
}

function Dungeon::setRoom(%this, %x, %y, %type) {
	%obj = %this.room[%x, %y];

	if (isObject(%obj)) {
		%obj.type = %type;
	}
	else {
		%this.room[%x, %y] = DungeonRoom(%this, %x, %y, %type);
	}
}

function Dungeon::getCandidates(%this) {
	%n1 = %this.getCount();

	for (%i = 0; %i < %n1; %i++) {
		%obj = %this.getObject(%i);

		%points = %this.getNeighbors(%obj.x, %obj.y);
		%n2 = getFieldCount(%points);

		for (%j = 0; %j < %n2; %j++) {
			%point = getField(%points, %j);

			%x = getWord(%point, 0);
			%y = getWord(%point, 1);

			if (getFieldCount(%this.getNeighbors(%x, %y, 1)) == 1) {
				%candidates = trim(%candidates TAB %x SPC %y);
			}
		}
	}

	return %candidates;
}

function Dungeon::getNeighbors(%this, %x1, %y1, %rooms) {
	%offsets = "1 0\t0 1\t-1 0\t0 -1";

	for (%i = 0; %i < 4; %i++) {
		%offset = getField(%offsets, %i);

		%x2 = %x1 + getWord(%offset, 0);
		%y2 = %y1 + getWord(%offset, 1);

		%check = %this.room[%x2, %y2] $= "";

		if (%rooms ? !%check : %check) {
			%neighbors = trim(%neighbors TAB %x2 SPC %y2);
		}
	}

	return %neighbors;
}

function Dungeon::getLeaves(%this) {
	%count = %this.getCount();

	for (%i = 0; %i < %count; %i++) {
		%obj = %this.getObject(%i);

		if (%obj.type $= "room") {
			%num = getFieldCount(%this.getNeighbors(%obj.x, %obj.y, 1));

			if (%num == 1) {
				%leaves = trim(%leaves TAB %obj.x SPC %obj.y);
			}
		}
	}

	return %leaves;
}

function Dungeon::visualize(%this) {
	%x1 = 0;
	%y1 = 0;
	%x2 = 0;
	%y2 = 0;

	%count = %this.getCount();

	for (%i = 0; %i < %count; %i++) {
		%obj = %this.getObject(%i);

		if (%obj.x < %x1) {
			%x1 = %obj.x;
		}

		if (%obj.y < %y1) {
			%y1 = %obj.y;
		}

		if (%obj.x > %x2) {
			%x2 = %obj.x;
		}

		if (%obj.y > %y1) {
			%y2 = %obj.y;
		}
	}

	echo("Visualizing Dungeon() with" SPC %this.getCount() SPC "rooms.");

	%char["start"] = "S";
	%char["end"] = "E";

	for (%y = %y1; %y <= %y2; %y++) {
		%line = "";

		for (%x = %x1; %x <= %x2; %x++) {
			%obj = %this.room[%x, %y];

			if (%obj $= "") {
				%line = %line @ " ";
			}
			else if (%char[%obj.type] !$= "") {
				%line = %line @ %char[%obj.type];
			}
			else {
				%line = %line @ "R";
			}
		}

		echo(%line);
	}
}

function Dungeon::reset(%this) {
	for (%i = %this.getCount() - 1; %i >= 0; %i--) {
		%obj = %this.getObject(%i);
		%this.room[%obj.x, %obj.y] = "";
		%obj.delete();
	}
}

function DungeonRoom(%dungeon, %x, %y, %type) {
	if (!isObject(%dungeon)) {
		error("ERROR: Room must be instanciated with a Dungeon!");
		return -1;
	}

	%obj = new ScriptObject() {
		class = DungeonRoom;
		type = %type;

		x = %x;
		y = %y;
	};

	%dungeon.add(%obj);
	return %obj;
}
