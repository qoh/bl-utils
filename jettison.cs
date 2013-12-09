// * parseJSON(string blob)
//   desc: Parses ´blob´, a serialized JSON type, and returns a fitting TS representation.
//   return: A string, number, JSONObject or "" on error.
//
//   notes:
//     Supported types and translations:
//       hash   -> JSONHash()
//       array  -> JSONArray()
//       string -> native string
//       number -> number in native string
//       bool   -> "1" (true) or "0" (false)
//       null   -> ""
//
//   usage:
//     %blob = "...";
//     %json = parseJSON(%blob);
//     
//     if (%json $= "") {
//       error("ERROR: Failed to parse JSON.");
//       return;
//     }
//     
//     // work with %json
//     
//     if (isJSONObject(%json)) {
//       %json.delete(); // note: deletes any children
//     }

function parseJSON(%blob) {
	return restWords(__scanJSON(%blob, 0));
}

// string getJSONType(* data)
//   desc: Returns a string description of the type of a given piece of data.
//   return: "null" | "string" | "number" | "array" | "hash"
//
//   notes:
//     Strings containing valid JSON numbers are interpreted as numbers.
//     Booleans are lost in translation.
//
//   usage:
//     %json = parseJSON("[]");
//     echo(getJSONType(%json)); // array

function getJSONType(%data) {
	if (%data $= "") {
		return "null";
	}

	%length = strLen(%data);

	if (expandEscape(getSubStr(%data, %length - 1, 1)) $= "\\c0") {
		%obj = getSubStr(%data, 0, %length - 1);

		if (%obj.superClass $= JSONObject) {
			return %obj.getJSONType();
		}
	}

	%scan = __scanJSONNumber(%data, 0);

	if (%scan !$= "" && firstWord(%scan) == %length) {
		return "number";
	}

	return "string";
}

// bool isJSONObject(* data)
//   desc: Determines whether or not the given data refers to a JSON object (hash/array).
//   return: True if the given data is a JSON object, false otherwise.
//   notes: This will not confuse normal objects or strings accidentally containing an object ID for a JSON object.

function isJSONObject(%data) {
	%length = strLen(%data);

	if (%data !$= "" && expandEscape(getSubStr(%data, %length - 1, 1)) $= "\\c0") {
		return getSubStr(%data, 0, %length - 1).superClass $= JSONObject;
	}

	return 0;
}

// string describeJSON(* data[, int depth])
//   desc: Returns a string representing the given data, using JSONObject::describe for JSON objects.
//   return: A potentially multi-line string with space indention, suitable for echo(...).

function describeJSON(%data, %depth) {
	if (!isJSONObject(%data)) {
		return %data;
	}

	return %data.describe(%depth);
}

// Private functions

function __scanJSON(%blob, %index, %type) {
	%index = skipLeftSpace(%blob, %index);

	if (%index >= strLen(%blob)) {
		return "";
	}

	if (getSubStr(%blob, %index, 4) $= "null") {
		return %index + 4 SPC "";
	}

	if (getSubStr(%blob, %index, 4) $= "true") {
		return %index + 4 SPC 1;
	}

	if (getSubStr(%blob, %index, 5) $= "false") {
		return %index + 5 SPC 0;
	}

	%char = getSubStr(%blob, %index, 1);

	if (%char $= "\"") {
		return __scanJSONString(%blob, %index + 1);
	}

	if (%char $= "[") {
		return __scanJSONArray(%blob, %index + 1);
	}

	if (%char $= "{") {
		return __scanJSONHash(%blob, %index + 1);
	}

	return __scanJSONNumber(%blob, %index);
}

function __scanJSONString(%blob, %index) {
	%length = strLen(%blob);

	for (%i = %index; %i < %length; %i++) {
		if (getSubStr(%blob, %i, 1) $= "\"" && getSubStr(%blob, %i - 1, 1) !$= "\\") {
			return %i + 1 SPC collapseEscape(getSubStr(%blob, %index, %i - %index));
		}
	}

	return "";
}

function __scanJSONArray(%blob, %index) {
	%length = strLen(%blob);

	%obj = new ScriptObject() {
		class = JSONArray;
		superClass = JSONObject;
	};

	%first = 0;
	%ready = 1;

	while (1) {
		%index = skipLeftSpace(%blob, %index);

		if (%index >= %length) {
			%obj.delete();
			return "";
		}

		if (getSubStr(%blob, %index, 1) $= "]") {
			if (%first && %ready) {
				%obj.delete();
				return "";
			}

			return %index + 1 SPC %obj @ "\c0";
		}

		if (getSubStr(%blob, %index, 1) $= ",") {
			if (%ready) {
				return "";
			}

			%ready = 1;
			%index++;

			continue;
		}
		else if (!%ready) {
			return "";
		}

		%scan = __scanJSON(%blob, %index);

		if (%scan $= "") {
			%obj.delete();
			return "";
		}

		%index = firstWord(%scan);
		%obj.append(restWords(%scan));

		%first = 1;
		%ready = 0;
	}

	return "";
}

function __scanJSONHash(%blob, %index) {
	%length = strLen(%blob);

	%obj = new ScriptObject() {
		class = JSONHash;
		superClass = JSONObject;
	};

	%first = 0;
	%ready = 1;

	while (1) {
		%index = skipLeftSpace(%blob, %index);

		if (%index >= %length) {
			%obj.delete();
			return "";
		}

		if (getSubStr(%blob, %index, 1) $= "}") {
			if (%first && %ready) {
				%obj.delete();
				return "";
			}

			return %index + 1 SPC %obj @ "\c0";
		}

		if (getSubStr(%blob, %index, 1) $= ",") {
			if (%ready) {
				return "";
			}

			%ready = 1;
			%index++;

			continue;
		}
		else if (!%ready) {
			return "";
		}

		if (getSubStr(%blob, %index, 1) !$= "\"") {
			%obj.delete();
			return "";
		}

		%scan = __scanJSONString(%blob, %index + 1);

		if (%scan $= "") {
			%obj.delete();
			return "";
		}

		%key = restWords(%scan);
		%index = skipLeftSpace(%blob, firstWord(%scan));

		if (getSubStr(%blob, %index, 1) !$= ":") {
			%obj.delete();
			return "";
		}

		%scan = __scanJSON(%blob, %index + 1);

		if (%scan $= "") {
			%obj.delete();
			return "";
		}

		%obj.set(%key, restWords(%scan));
		%index = firstWord(%scan);

		%first = 1;
		%ready = 0;
	}

	return "";
}

function __scanJSONNumber(%blob, %index) {
	%length = strLen(%blob);
	%i = %index;

	if (getSubStr(%blob, %index, 1) $= "-") {
		%i++;
	}

	%allowZeroFirst = 0;
	%allowRadixFirst = 0;

	%first = 0;
	%radix = 0;

	if (!%allowZeroFirst) {
		%start = getSubStr(%blob, %i, 1);
	}

	for (%i; %i < %length; %i++) {
		%chr = getSubStr(%blob, %i, 1);

		if (%chr $= ".") {
			if ((!%allowRadixFirst && !%first) || %radix) {
				return "";
			}
			else {
				%first = 0;
				%radix = 1;
			}
		}
		else {
			%pos = strPos("0123456789", %chr);

			if (%pos == -1) {
				break;
			}

			%first = 1;
		}
	}

	if (!%first) {
		return "";
	}

	if (!%allowZeroFirst && %i - %index > 1 && %start $= "0") {
		return "";
	}

	return %i SPC getSubStr(%blob, %index, %i - %index);
}

// Base JSONObject methods

// string JSONObject::getJSONType()
//   desc: Determines the type of the JSON object.
//   return: The type as a string, i.e. "hash" or "array".
//   notes: Custom JSON objects that do not start with "JSON" will break this.

function JSONObject::getJSONType(%this) {
	return strLwr(getSubStr(%this.class, 4, strLen(%this.class)));
}

// string JSONObject::describe([int depth])
//   see: describeJSON

function JSONObject::describe(%this, %depth) {
	return %this.getJSONType() @ "(" @ %this.getID() @ ")";
}

// null JSONObject::addParent(->JSONObject parent)
//   desc: Adds a JSON object to another's list of parents, setting it as the main parent if it's the first.

function JSONObject::addParent(%this, %parent) {
	if (%this.parent $= "") {
		%this.parent = %parent;
	}

	if (%this.parents $= "") {
		%this.parents = %parent;
	}
	else {
		%this.parents = %this.parents SPC %parent;
	}
}

// JSONArray methods

function JSONArray::onAdd(%this) {
	%this.length = 0;
}

function JSONArray::onRemove(%this) {
	for (%i = 0; %i < %this.length; %i++) {
		if (isJSONObject(%this.item[%i])) {
			%this.item[%i].delete();
		}
	}
}

function JSONArray::getLength(%this) {
	return %this.length;
}

function JSONArray::describe(%this, %depth) {
	%string = Parent::describe(%this, %depth);
	%string = %string @ " - " @ %this.length @ " items";

	%indent = repeatString("   ", %depth++);

	for (%i = 0; %i < %this.length; %i++) {
		%string = %string NL %indent @ %i @ ": " @ describeJSON(%this.item[%i], %depth);
	}

	return %string;
}

function JSONArray::get(%this, %index, %default) {
	if (%index < 0 || %index >= %this.length) {
		return %default;
	}

	return %this.item[%index];
}

function JSONArray::append(%this, %item) {
	if (isJSONObject(%item)) {
		%item.addParent(%this);
	}

	%this.item[%this.length] = %item;
	%this.length++;

	return %this;
}

function JSONArray::prepend(%this, %item) {
	if (isJSONObject(%item)) {
		%item.addParent(%this);
	}

	%this.length++;

	for (%i = %this.length - 1; %i > 0; %i--) {
		%this.item[%i] = %this.item[%i - 1];
	}

	%this.item[0] = %item;
}

function JSONArray::contains(%this, %item) {
	for (%i = 0; %i < %this.length; %i++) {
		if (%this.item[%i] $= %item) {
			return 1;
		}
	}

	return 0;
}

function JSONArray::remove(%this, %item, %max, %delete) {
	if (%max $= "") {
		%max = 1;
	}

	%found = 0;

	for (%i = 0; %i < %this.length; %i++) {
		if (%this.item[%i] $= %item) {
			if (isJSONObject(%this.item[%i])) {
				if (%delete) {
					%this.item[%i].delete();
				}
				else {
					%this.item[%i].removeParent(%this);
				}
			}

			%found++;
		}

		if (%found) {
			%this.item[%i] = %this.item[%i + 1];
		}
	}

	if (%found) {
		%this.length -= %found;

		for (%i = 0; %i < %found; %i++) {
			%this.item[%this.length + %i] = "";
		}
	}

	return %found;
}

function JSONArray::clear(%this, %delete) {
	for (%i = 0; %i < %this.length; %i++) {
		if (isJSONObject(%this.item[%i])) {
			if (%delete) {
				%this.item[%i].delete();
			}
			else {
				%this.item[%i].removeParent(%this);
			}
		}

		%this.item[%i] = "";
	}

	%this.length = 0;
}

// JSONHash methods

function JSONHash::onAdd(%this) {
	%this.__length = 0;
	%this.length = 0;
}

function JSONHash::onRemove(%this) {
	for (%i = 0; %i < %this.__length; %i++) {
		%item = %this.__value[%this.__key[%i]];

		if (isJSONObject(%item)) {
			%item.delete();
		}
	}
}

function JSONHash::getLength(%this) {
	return %this.__length;
}

function JSONHash::getKey(%this, %index) {
	if (%index < 0 || %index >= %this.__length) {
		return %this.__key[%index];
	}

	return "";
}

function JSONHash::isKey(%this, %key) {
	return %this.__isKey[%key] ? 1 : 0;
}

function JSONHash::isValue(%this, %value) {
	for (%i = 0; %i < %this.__length; %i++) {
		if (%this.__value[%this.__key[%i]] $= %value) {
			return 1;
		}
	}

	return 0;
}

function JSONHash::describe(%this, %depth) {
	%string = Parent::describe(%this, %depth);
	%string = %string @ " - " @ %this.__length @ " pairs";

	%indent = repeatString("   ", %depth++);

	for (%i = 0; %i < %this.__length; %i++) {
		%key = %this.__key[%i];
		%value = %this.__value[%key];

		%string = %string NL %indent @ %key @ ": " @ describeJSON(%value, %depth);
	}

	return %string;
}

function JSONHash::set(%this, %key, %value) {
	if (isJSONObject(%value)) {
		%value.addParent(%this);
	}

	if (%key $= "length") {
		%this.__usePublicLength = 0;
	}

	if (!%this.__isKey[%key]) {
		%this.__isKey[%key] = 1;

		%this.__key[%this.__length] = %key;
		%this.__length++;

		if (!%this.__isKey["length"]) {
			%this.length = %this.__length;
		}
	}

	%this.__value[%key] = %value;

	if (%this.__isKeyNameSane[%key] $= "") {
		%illegal = "class superClass";

		if (striPos(" " @ %illegal @ " ", " " @ %key @ " ") != -1) {
			%this.__isKeyNameSane[%key] = 0;
		}
		else {
			%this.__isKeyNameSane[%key] = sanitizeIdentifier(%key);
		}
	}

	if (%this.__isKeyNameSane[%key]) {
		eval("%this." @ %key @ "=%value;");
	}

	return %this;
}

function JSONHash::setDefault(%this, %key, %value) {
	if (!%this.__isKey[%key]) {
		%this.set(%key, %value);
	}

	return %this.__value[%key];
}

function JSONHash::get(%this, %key, %default) {
	if (!%this.__isKey[%key]) {
		return %default;
	}

	return %this.__value[%key];
}

function JSONHash::remove(%this, %key, %delete) {
	if (!%this.__isKey[%key]) {
		return 0;
	}

	if (isJSONObject(%this.__value[%key])) {
		if (%delete) {
			%this.__value[%key].delete();
		}
		else {
			%this.__value[%key].removeParent(%this);
		}
	}

	%this.__isKey[%key] = "";
	%this.__value[%key] = "";

	if (%this.__isKeyNameSane[%key]) {
		eval("%this." @ %key @ "=\"\"";);
	}

	%this.__isKeyNameSane[%key] = "";

	for (%i = 0; %i < %this.__length; %i++) {
		if (%this.__key[%i] $= %key) {
			%found++;
		}

		if (%found) {
			%this.__key[%i] = %this.__key[%i + 1];
		}
	}

	if (%found) {
		%this.__length -= %found;

		for (%i = 0; %i < %found; %i++) {
			%this.__key[%this.__length + %i] = "";
		}
	}

	return 1;
}

function JSONArray::clear(%this, %delete) {
	for (%i = 0; %i < %this.__length; %i++) {
		%key = %this.__key[%i];

		if (isJSONObject(%this.__value[%key])) {
			if (%delete) {
				%this.__value[%key].delete();
			}
			else {
				%this.__value[%key].removeParent(%this);
			}
		}

		%this.__isKey[%key] = "";
		%this.__value[%key] = "";

		if (%this.__isKeyNameSane[%key]) {
			eval("%this." @ %key @ "=\"\"";);
		}

		%this.__isKeyNameSane[%key] = "";
		%this.__key[%i] = "";

		%this.item[%i] = "";
	}

	%this.__length = 0;
	%this.length = 0;
}

// Helper functions

function skipLeftSpace(%blob, %index) {
	%length = strLen(%blob);

	if (%index >= %length) {
		return %index;
	}

	return %index + (%length - %index - strLen(ltrim(getSubStr(%blob, %index, %length))));
}

function sanitizeIdentifier(%blob) {
	%a = "_abcdefghijklmnopqrstuvwxyz";
	%b = "0123456789";

	%length = strLen(%blob);

	for (%i = 0; %i < %length; %i++) {
		%chr = getSubStr(%blob, %i, 1);

		if (striPos(%a, %chr) == -1) {
			if (!%i || striPos(%b, %chr) == -1) {
				return 0;
			}
		}
	}

	return 1;
}

function repeatString(%string, %times) {
	for (%i = 0; %i < %times; %i++) {
		%result = %result @ %string;
	}

	return %result;
}

// Debug helper functions

function echoPointInBlob(%blob, %index) {
	echo(%blob NL repeatString(" ", %index) @ "^");
}
