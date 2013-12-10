// DocumentStorage DocumentStorage([string name], [string file], [bool noMissionCleanup], [int dirtyExportDelay])
//  TODO: Base class documentation.
//
//  @extends ScriptObject
//  @arg name If specified, the created DocumentStorage object will be named this.
//  @arg name Additionally, the constructor will search for an existing object with the name and return it instead of creating one if found.
//  @arg file A default filepath to import and export the DocumentStorage data from and to when none is specified.
//  @arg noMissionCleanup If true, and if an object was created, it will not be added to the MissionCleanup group, even if one exists.
//  @arg dirtyExportDelay The delay between modifying the storage and automatically exporting. 60000 by default. Values < 0 disable this feature.

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

// DocumentStorage_Collection DocumentStorage::collection(string name, [bool noCreate])
//  Searches for the collection *name* and returns it if found.
//  If none exists and *noCreate* is not specified as true, it creates one with the name and returns it.
//  With *noCreate*, -1 is returned if requesting an unexistant collection.
//
//  @see DocumentStorage::isCollection
//  @see DocumentStorage::deleteCollection

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

// bool DocumentStorage::isCollection(string name)
//  Searches for the collection *name* and returns whether or not it exists.

function DocumentStorage::isCollection(%this, %name) {
	return isObject(%this.collection[%name]);
}

// DocumentStorage::deleteCollection(string name)
//  Deletes the collection *name* if it exists, passing silently otherwise.
//  @see DocumentStorage::clear
//  @see DocumentStorage_Collection::clear

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

// DocumentStorage::clear
//  Deletes all collections, documents and keys from this DocumentStorage.
//
//  There is no way to undo this operation. Handle with care, as it marks the storage as dirty.
//  That means that this operation will be automatically exported if *dirtyExportDelay* is enabled.
//
//  @see DocumentStorage_Collection::clear

function DocumentStorage::clear(%this) {
	%this.setDirty();

	for (%i = 0; %i < %this.size; %i++) {
		%this.collection[%this.name[%i]].delete();
		%this.collection[%this.name[%i]] = "";

		%this.name[%i] = "";
	}

	%this.size = 0;
	return %this;
}

// int DocumentStorage::import([string file])
//  Attempts to import all data found in the given file, defaulting to the constructor-specified file.
//  If one is found, all previously existing data is cleared and replaced.
//
//  Returns 0 on success, otherwise one of the following error codes:
//
//   1. The file does not exist, or none was specified and there is no default file.
//   2. The file cannot be opened for reading.
//
//  An import operation will not be auto-exported.
//  @see DocumentStorage::export

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

		return 2;
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

// int DocumentStorage::export([string file])
//  Exports all data to the given file, defaulting to the constructor-specified file.
//  The file format is a fairly simple, indention-based grouped key->value structure.
//
//  Returns 0 on success, otherwise one of the following error codes:
//
//   1. The file is not writeable, or none was specified and there is no default file.
//   2. The file cannot be opened for writing.
//   3. The file could not be created.
//
//  @see DocumentStorage::import

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

		return 2;
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
		return 3;
	}

	return 0;
}

// DocumentStorage::setDirty
//  @private

function DocumentStorage::setDirty(%this) {
	%this.dirty = 1;

	if (%this.file !$= "" && %this.dirtyExportDelay >= 0 && !isEventPending(%this.dirtyExportSchedule)) {
		%this.dirtyExportSchedule = %this.schedule(%this.dirtyExportDelay, export);
	}

	return %this;
}

// DocumentStorage_Collection DocumentStorage_Collection
//  This represents a "collection" of documents in a DocumentStorage instance.
//  It can contain any number of documents which can be created or deleted at will.
//
//  Member fields:
//
//  * `parent` ⇒ the DocumentStorage owning this collection
//  * `size` ⇒ the number of documents contained in the collection
//  * `name[N]` ⇒ the name of the Nth (`0 <= N < size`) document in the collection
//
//  @abstract

function DocumentStorage_Collection() {}

function DocumentStorage_Collection::onAdd(%this) {
	%this.size = 0;
}

function DocumentStorage_Collection::onRemove(%this) {
	for (%i = 0; %i < %this.size; %i++) {
		%this.document[%this.name[%i]].delete();
	}
}

// DocumentStorage_Document DocumentStorage_Collection::document(string name, [bool noCreate])
//  Searches the collection for the document *name* and returns it if found.
//  If none exists and *noCreate* is not specified as true, it creates one with the name and returns it.
//  With *noCreate*, -1 is returned if requesting an unexistant document.
//
//  @see DocumentStorage_Collection::isDocument
//  @see DocumentStorage_collection::deleteDocument

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

// bool DocumentStorage_Collection::isDocument(string name)
//  Searches the collection for the document *name* and returns whether or not it exists.

function DocumentStorage_Collection::isDocument(%this, %name) {
	return isObject(%this.document[%name]);
}

// DocumentStorage_Collection::deleteDocument(string name)
//  Deletes the document *name* if it exists, passing silently otherwise.
//  @see DocumentStorage_Collection::clear

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

// DocumentStorage_Collection::clear
//  Deletes all documents contained in this collection.
//
//  There is no way to undo this operation. Handle with care, as it marks the storage as dirty.
//  That means that this operation will be automatically exported if *dirtyExportDelay* is enabled.
//
//  @see DocumentStorage:clear
//  @see DocumentStorage_Document::clear

function DocumentStorage_Collection::clear(%this) {
	%this.parent.setDirty();

	for (%i = 0; %i < %this.size; %i++) {
		%this.document[%this.name[%i]].delete();
		%this.document[%this.name[%i]] = "";

		%this.name[%i] = "";
	}

	%this.size = 0;
	return %this;
}

// DocumentStorage_Document DocumentStorage_Document
//  A single document in a collection with a particular name and key->value mappings.
//
//  This behaves to and has an API similar to Python dictionary objects.
//  Keys are case-insensitive and can contain empty values.
//
//  When creating a new document, if the collection owning it has a document named "$default",
//  all keys will be copied to the new document using `::copyTo`.
//
//  In this case, deleting a key from a document or clearing all keys in this case will also revert
//  to the value stored in the default document if any, unless the *force* argument to those methods is true.
//
//  Member fields:
//
//  * `parent` ⇒ the collection owning this document
//  * `size` ⇒ the number of keys on the document
//  * `name[N]` ⇒ the name of the Nth (`0 <= N < size`) key on the document
//
//  @abstract

function DocumentStorage_Document() {}

function DocumentStorage_Document::onAdd(%this) {
	%this.size = 0;
}

// DocumentStorage_Document::set(string name, value)
//  Assigns *value* to the key *name* creating the key if it doesn't exist.
//  @see DocumentStorage_Document::get
//  @see DocumentStorage_Document::setDefault

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

// value DocumentStorage_Document::setDefault(string name, value)
//  Only assigns *value* to the key *name* if it doesn't already exist.
//  Returns the final value assigned to the key.
//
//  Consider using default documents (see DocumentStorage_Document documentation) instead.

function DocumentStorage_Document::setDefault(%this, %name, %value) {
	if (!%this.isKey[%name]) {
		%this.set(%name, %value);
		return %value;
	}

	return %this.get(%name);
}

// value DocumentStorage_Document::get(string name, [default])
//  Retrieves the value of the key *name*.
//  If the key doesn't exist and *default* is specified, it's value is returned.
//  Otherwise, an empty string is returned.
//
//  @see DocumentStorage_Document::set
//  @see DocumentStorage_Document::setDefault

function DocumentStorage_Document::get(%this, %name, %default) {
	if (%this.isKey[%name]) {
		return %this.value[%name];
	}

	return %default;
}

// DocumentStorage_Document::clear([bool force])
//  Deletes all keys and clears their associated values from the document.
//  If the collection has a default document, it is copied to this document unless *force* is true.
//
//  @see DocumentStorage_Collection::clear
//  @see DocumentStorage_Document::deleteKey

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

// DocumentStorage_Document::deleteKey(string name, [bool force])
//  Deletes the key *name* from the document, passing silently if it doesn't exist.
//  If the collection has a default document, it is copied to this document unless *force* is true.
//
//  @see DocumentStorage_Document::clear

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

// DocumentStorage_Document::copyTo(DocumentStorage_Document other, [bool onlyDefault])
//  Copies every key->value mapping from this document to the *other* document.
//  If *onlyDefault* is true, existing keys on *other* will not be overwritten.

function DocumentStorage_Document::copyTo(%this, %other, %onlyDefault) {
	for (%i = 0; %i < %this.size; %i++) {
		if (!%onlyDefault || !%other.isKey[%this.name[%i]]) {
			%other.set(%this.name[%i], %this.value[%this.name[%i]]);
		}
	}

	return %this;
}
