$COMPARE::VALUES = 0;
$COMPARE::SCORES = 1;
$COMPARE::EXTERN = 2;

// HeapQueue HeapQueue([string init], [number compareMode], [function compareArg])
//   desc: Returns a new instance of a HeapQueue.
//
//   arg init:
//     An initial \n-delimited set of items to ::push onto the queue.
//     Individual items are in the format of "item value\titem score".
//
//   arg compareMode: One of $COMPARE::*, determining how to compare items. $COMPARE::SCORES by default.
//   arg compareArg: If compareMode is $COMPARE::EXTERN, the function to call for comparing items.

function HeapQueue(%init, %compareMode, %compareArg) {
	return new ScriptObject() {
		class = HeapQueue;
		init = %init;

		compareMode = %compareMode;
		compareFunc = %compareFunc;
	};
}

// HeapQueue::onAdd
//   private

function HeapQueue::onAdd(%this) {
	%this.size = 0;

	if (%this.compareMode $= "") {
		%this.compareMode = $COMPARE::SCORES;
	}

	if (%this.compareMode == $COMPARE::EXTERN && !isFunction(%this.compareArg)) {
		error("ERROR: Invalid function given when using EXTERN heap comparisons.");

		%this.delete();
		return;
	}

	if (%this.init !$= "") {
		%count = getLineCount(%this.init);

		for (%i = 0; %i < %count; %i++) {
			%line = getLine(%this.init, %i);
			%this.push(getField(%line, 0), getField(%line, 1));
		}

		%this.init = "";
	}
}

// void HeapQueue::push(item, [number score])
//   desc: Pushes a new item onto the queue, moving it down to the proper position.
//   see: HeapQueue::pop
//
//   arg item: The value of the item to add (any value).
//   arg score: A numeric value to sort the item by if compareMode is $COMPARE::SCORES.

function HeapQueue::push(%this, %item, %score) {
	%this.contains[%item] = 1;
	%this.score[%item] = %score;

	%this.item[%this.size] = %item;
	%this.size++;

	%this._demote(0, %this.size - 1);
}

// * HeapQueue::pop()
//   desc: Pops (removes) the best item from the queue and finds a successor for the queue.
//   return: The value of the best item in the queue.
//   see: HeapQueue::pop

function HeapQueue::pop(%this) {
	if (!%this.size) {
		return "";
	}

	%this.size--;
	%item = %this.item[%this.size];

	if (%this.size) {
		%prev = %this.item[0];

		%this.item[0] = %item;
		%this._promote(0);

		return %prev;
	}

	return %item;
}

// HeapQueue::_demote
//   private

function HeapQueue::_demote(%this, %start, %index) {
	%item = %this.item[%index];

	while (%index > %start) {
		%parent = (%index - 1) >> 1;

		if (%this.compare(%item, %this.item[%parent])) {
			%this.item[%index] = %this.item[%parent];
			%index = %parent;

			continue;
		}

		break;
	}

	%this.item[%index] = %item;
}

// HeapQueue::_promote
//   

function HeapQueue::_promote(%this, %index) {
	%start = %index;
	%child = 2 * %index + 1;

	%item = %this.item[%index];

	while (%child < %this.size) {
		%right = %child + 1;

		if (%right < %this.size && !%this.compare(%this.item[%child], %this.item[%right])) {
			%child = %right;
		}

		%this.item[%index] = %this.item[%child];

		%index = %child;
		%child = 2 * %index + 1;
	}

	%this.item[%index] = %item;
	%this._demote(%start, %index);
}

// HeapQueue::compare(a, b)
//   desc: Used internally to determine which item to prioritize, using the configured comparator.
//   return: 1 if a is better than b, 0 otherwise.
//
//   arg a: The left child item to compare.
//   arg b: The right child item to compare.

function HeapQueue::compare(%this, %a, %b) {
	if (%this.compareMode == $COMPARE::VALUES) {
		return %a < %b;
	}

	if (%this.compareMode == $COMPARE::SCORES) {
		return %this.score[%a] < %this.score[%b];
	}

	if (%this.compareMode == $COMPARE::EXTERN) {
		return call(%this.compareFunc, %this, %a, %b);
	}

	return 0;
}
