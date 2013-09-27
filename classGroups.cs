package ClassGroupPackage {
	function GameConnection::onConnectionAccepted(%this) {
		Parent::onConnectionAccepted(%this);

		if (%this == nameToID("ServerConnection")) {
			ServerConnection.updateClassGroups();
		}
	}

	function GameConnection::onRemove(%this) {
		%count = getWordCount(%this.classGroupObjects);

		for (%i = 0; %i < %count; %i++) {
			%obj = getWord(%this.classGroupObjects, %i);

			if (isObject(%obj)) {
				%obj.delete();
			}
		}

		Parent::onRemove(%this);
	}
};

activatePackage("ClassGroupPackage");

function GameConnection::updateClassGroups(%this) {
	cancel(%this.updateClassGroups);

	for (%i = %this.getCount() - 1; %i >= 0; %i--) {
		%obj = %this.getObject(%i);

		if (%obj <= %this.highestSeenID) {
			break;
		}

		%class = %obj.getClassName();

		if (!isObject(%this.classGroup[%class])) {
			%this.classGroup[%class] = new SimSet();
			%this.classGroups = trim(%this.classGroups SPC %this.classGroup[%class]);
		}

		if (!%this.classGroup[%class].isMember(%obj)) {
			%this.classGroup[%class].add(%obj);
		}
	}

	if (%this.getCount()) {
		%highest = %this.getObject(%this.getCount() - 1);

		if (%highest > %this.highestSeenID) {
			%this.highestSeenID = %highest;
		}
	}

	%this.updateClassGroups = %this.schedule(250, updateClassGroups);
}

function GameConnection::getClassCount(%this, %type) {
	if (isObject(%this.classGroup[%type])) {
		return %this.classGroup[%type].getCount();
	}

	return 0;
}

function GameConnection::getClassObject(%this, %type, %index) {
	if (isObject(%this.classGroup[%type])) {
		return %this.classGroup[%type].getObject(%index);
	}

	return -1;
}
