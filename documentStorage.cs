function DocumentStorage(%name, %file, %noMissionCleanup, %dirtyExportDelay) {
	if (%name !$= "" && isObject(%name)) {
		if (%name.file !$= %file) {
			warn("WARNING: DocumentStorage() - object already exists (not recreating) but different file specified");
		}

		return nameToID(%name);
	}

	if (%dirtyExportDelay $= "") {
		%dirtyExportDelay = 60000;
	}

	%obj = new ScriptObject(%name) {
		class = DocumentStorage;

		file = %file;
		dirtyExportDelay = %dirtyExportDelay;
	};

	if (!%noMissionCleanup && isObject(MissionCleanup)) {
		MissionCleanup.add(%obj);
	}

	return %obj;
}

function DocumentStorage::onAdd(%this) {
	%this.size = 0;
	%this.dirty = 0;

	%this.exportOnRemove = 1;

	if (%this.file !$= "" && isFile(%this.file)) {
		%this.import();
	}
}

function DocumentStorage::onRemove(%this) {
	if (%this.file !$= "" && %this.exportOnRemove && %this.dirty) {
		%this.export();
	}

	for (%i = 0; %i < %this.size; %i++) {
		%this.collection[%this.name[%i]].delete();
	}
}

function DocumentStorage::purge(%this) {
	%this.setDirty();

	for (%i = 0; %i < %this.size; %i++) {
		%this.collection[%this.name[%i]].delete();
		%this.collection[%this.name[%i]] = "";

		%this.name[%i] = "";
	}

	%this.size = 0;
	return %this;
}

function DocumentStorage::import(%this, %file) {
	if (%file $= "") {
		%file = %this.file;

		if (%file $= "") {
			error("ERROR: No file specified and no default file.");
			return 1;
		}
	}

	if (!isFile(%file)) {
		error("ERROR: " @ %file @ " does not exist.");
		return 1;
	}

	%this.purge();
	%this.dirty = 0;

	cancel(%this.dirtyExportSchedule);
	%fp = new FileObject();

	if (!%fp.openForRead(%file)) {
		error("ERROR: Cannot open " @ %file @ " for reading.");
		%fp.delete();

		return 1;
	}

	while (!%fp.isEOF()) {
		%origLine = %fp.readLine();
		%line = ltrim(%origLine);

		%indent = strLen(%origLine) - strLen(%line);

		if (%indent == 0) {
			%collection = %this.collection(%line);
		}

		if (%indent == 1 && isObject(%collection)) {
			%document = %collection.document(%line);
		}

		if (%indent == 2 && isObject(%document) && getWordCount(%line) >= 2) {
			%document.set(firstWord(%line), restWords(%line));
		}
	}

	%fp.close();
	%fp.delete();

	return 0;
}

function DocumentStorage::export(%this, %file) {
	if (%file $= "") {
		%file = %this.file;

		if (%file $= "") {
			error("ERROR: No file specified and no default file.");
			return 1;
		}
	}

	if (!isWriteableFileName(%file)) {
		error("ERROR: " @ %file @ " is not writeable.");
		return 1;
	}

	%this.dirty = 0;
	cancel(%this.dirtyExportSchedule);

	%fp = new FileObject();

	if (!%fp.openForWrite(%file)) {
		error("ERROR: Cannot open " @ %file @ " for writing.");
		%fp.delete();

		return 1;
	}

	for (%i = 0; %i < %this.size; %i++) {
		%collection = %this.collection[%this.name[%i]];

		if (!%collection.size) {
			continue;
		}

		%fp.writeLine(%this.name[%i]);

		for (%j = 0; %j < %collection.size; %j++) {
			%document = %collection.document[%collection.name[%j]];

			if (!%document.size) {
				continue;
			}

			%fp.writeLine("\t" @ %collection.name[%j]);

			for (%k = 0; %k < %document.size; %k++) {
				%fp.writeLine("\t\t" @ %document.name[%k] SPC %document.value[%document.name[%k]]);
			}
		}
	}

	%fp.close();
	%fp.delete();

	if (!isFile(%file)) {
		error("ERROR: Failed to create " @ %file);
		return 1;
	}

	return 0;
}

function DocumentStorage::collection(%this, %name, %noCreate) {
	if (isObject(%this.collection[%name])) {
		return %this.collection[%name];
	}

	if (%noCreate) {
		return -1;
	}

	%this.collection[%name] = new ScriptObject() {
		class = DocumentStorage_Collection;

		name = %name;
		parent = %this;
	};

	%this.name[%this.size] = %name;
	%this.size++;

	%this.setDirty();
	return %this.collection[%name];
}

function DocumentStorage::isCollection(%this, %name) {
	return isObject(%this.collection[%name]);
}

function DocumentStorage::deleteCollection(%this, %name) {
	if (isObject(%this.collection[%name])) {
		%this.collection[%name].delete();
		%this.setDirty();
	}

	%this.collection[%name] = "";

	for (%i = 0; %i < %this.size; %i++) {
		if (%this.name[%i] $= %name) {
			%found = true;
		}

		if (%found) {
			%this.name[%i] = %this.name[%i + 1];
		}
	}

	if (%found) {
		%this.name[%this.size] = "";
		%this.size--;

		%this.setDirty();
	}

	return %this;
}

function DocumentStorage::setDirty(%this) {
	%this.dirty = 1;

	if (%this.file !$= "" && %this.dirtyExportDelay >= 0 && !isEventPending(%this.dirtyExportSchedule)) {
		%this.dirtyExportSchedule = %this.schedule(%this.dirtyExportDelay, export);
	}

	return %this;
}

function DocumentStorage_Collection::onAdd(%this) {
	%this.size = 0;
}

function DocumentStorage_Collection::onRemove(%this) {
	for (%i = 0; %i < %this.size; %i++) {
		%this.document[%this.name[%i]].delete();
	}
}

function DocumentStorage_Collection::document(%this, %name, %noCreate) {
	if (isObject(%this.document[%name])) {
		return %this.document[%name];
	}

	if (%noCreate) {
		return -1;
	}

	%this.document[%name] = new ScriptObject() {
		class = DocumentStorage_Document;

		name = %name;
		parent = %this;
	};

	%this.name[%this.size] = %name;
	%this.size++;

	if (%this.isDocument("$default")) {
		%this.document("$default").copyTo(%this.document[%name], 1);
	}

	%this.parent.setDirty();
	return %this.document[%name];
}

function DocumentStorage_Collection::isDocument(%this, %name) {
	return isObject(%this.document[%name]);
}

function DocumentStorage_Collection::deleteDocument(%this, %name) {
	if (isObject(%this.document[%name])) {
		%this.document[%name].delete();
		%this.parent.setDirty();
	}

	%this.document[%name] = "";

	for (%i = 0; %i < %this.size; %i++) {
		if (%this.name[%i] $= %name) {
			%found = true;
		}

		if (%found) {
			%this.name[%i] = %this.name[%i + 1];
		}
	}

	if (%found) {
		%this.name[%this.size] = "";
		%this.size--;

		%this.parent.setDirty();
	}

	return %this;
}

function DocumentStorage_Document::onAdd(%this) {
	%this.size = 0;
}

function DocumentStorage_Document::set(%this, %name, %value) {
	if (!%this.isKey[%name]) {
		%this.isKey[%name] = true;

		%this.name[%this.size] = %name;
		%this.size++;
	}

	%this.value[%name] = %value;
	%this.parent.parent.setDirty();

	return %this;
}

function DocumentStorage_Document::setDefault(%this, %name, %value) {
	if (!%this.isKey[%name]) {
		%this.set(%name, %value);
		return %value;
	}

	return %this.get(%name);
}

function DocumentStorage_Document::get(%this, %name, %default) {
	if (%this.isKey[%name]) {
		return %this.value[%name];
	}

	return %default;
}

function DocumentStorage_Document::clear(%this, %force) {
	for (%i = 0; %i < %this.size; %i++) {
		%this.value[%this.name[%i]] = "";
		%this.isKey[%this.name[%i]] = "";

		%this.name[%i] = "";
	}

	%this.size = 0;

	if (!%force && %this.parent.isDocument("$default")) {
		%this.parent.document("$default").copyTo(%this);
	}

	return %this;
}

function DocumentStorage_Document::deleteKey(%this, %name, %force) {
	if (!%force && %this.parent.isDocument("$default")) {
		%document = %this.parent.document("$default");

		if (%document.isKey[%name]) {
			%this.set(%name, %document.get(%name));
		}

		return %this;
	}

	if (!%this.isKey[%name]) {
		return %this;
	}

	%this.value[%name] = "";

	for (%i = 0; %i < %this.size; %i++) {
		if (%this.name[%i] $= %name) {
			%found = true;
		}

		if (%found) {
			%this.name[%i] = %this.name[%i + 1];
		}
	}

	if (%found) {
		%this.name[%this.size] = "";
		%this.size--;
	}

	%this.parent.parent.setDirty();
	return %this;
}

function DocumentStorage_Document::copyTo(%this, %other, %onlyDefault) {
	for (%i = 0; %i < %this.size; %i++) {
		if (!%onlyDefault || !%other.isKey[%this.name[%i]]) {
			%other.set(%this.name[%i], %this.value[%this.name[%i]]);
		}
	}

	return %this;
}
