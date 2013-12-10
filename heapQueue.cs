$COMPARE::VALUES = 0;
$COMPARE::SCORES = 1;
$COMPARE::EXTERN = 2;

// HeapQueue HeapQueue([number comparator], [function externFunc])
//  Heaps are binary trees for which every parent node has a value less than or equal to any of its children.
//  This implementation uses arrays for which `heap[k] <= heap[2*k+1]` and `heap[k] <= heap[2*k+2]` for all k, counting elements from zero.
//  For the sake of comparison, non-existing elements are considered to be infinite.
//  The interesting property of a heap is that its smallest element is always the root, `heap[0]`.
//
//  The API below differs from textbook heap algorithms in two aspects:
//
//   1. We use zero-based indexing. This makes the relationship between the index for a node and the indexes for its children slightly less obvious.
//   2. Our pop method returns the smallest item (by default), not the largest (called a “min heap” in textbooks; a “max heap” is more common in texts because of its suitability for in-place sorting).
//
//  Basic example:
//
//      ==>$heap = HeapQueue();
//      ==>$heap.push("foo", 5);
//      ==>$heap.push("bar", 3);
//      ==>$heap.push("baz", 10);
//      ==>for (%i = 0; %i < 3; %i++) echo($heap.pop());
//      bar
//      foo
//      baz
//
//  @arg comparator One of `$COMPARE::*`, determining how to compare items. `$COMPARE::SCORES` by default.
//  @arg externFunc If *comparator* is `$COMPARE::EXTERN`, the function to call for comparing items.

function HeapQueue(%comparator, %externFunc) {
	return new ScriptObject() {
		class = HeapQueue;

		comparator = %comparator;
		externFunc = %externFunc;
	};
}

// HeapQueue::onAdd()
//  @private

function HeapQueue::onAdd(%this) {
	%this.size = 0;

	if (%this.comparator $= "") {
		%this.comparator = $COMPARE::SCORES;
	}

	if (%this.comparator == $COMPARE::EXTERN && !isFunction(%this.externFunc)) {
		error("ERROR: Invalid function given when using EXTERN heap comparisons.");

		%this.delete();
		return;
	}
}

// HeapQueue::push(item, [number score])
//  Pushes the value *item* onto the heap, maintaining the heap invariant.
//  If the comparator is `$COMPARE::SCORES`, *score* will be used for prioritizing.

function HeapQueue::push(%this, %item, %score) {
	%this.contains[%item] = 1;
	%this.score[%item] = %score;

	%this.item[%this.size] = %item;
	%this.size++;

	%this._demote(0, %this.size - 1);
}

// value HeapQueue::pop()
//  Pop and return the best item from the heap, maintaining the heap invariant.
//  If the heap is empty, an empty string is returned.

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

// void HeapQueue::_demote(int start, int index)
//  @private

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

// void HeapQueue::_promote(int index)
//  @private

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

// bool HeapQueue::compare(a, b)
//  Used internally to determine which item to prioritize, using the configured comparator.
//  Returns 1 if a is better than b, 0 otherwise.

function HeapQueue::compare(%this, %a, %b) {
	if (%this.comparator == $COMPARE::VALUES) {
		return %a < %b;
	}

	if (%this.comparator == $COMPARE::SCORES) {
		return %this.score[%a] < %this.score[%b];
	}

	if (%this.comparator == $COMPARE::EXTERN) {
		return call(%this.externFunc, %this, %a, %b);
	}

	return 0;
}

// HeapQueue heapify(data, [comparator], [externFunc])
//  Constructs a HeapQueue from an initial data set, passing along the other normal arguments.
//
//  *data* is a newline-separated list of items to push onto the new heap.
//  When using the `$COMPARE::SCORES` comparator, the score of each item can be specified after a tab character.
//
//  This can be used to implement a simple heapsort.
//
//      function heapsort(%values) {
//          %heap = heapify(%values, $COMPARE::VALUES);
//          %size = %heap.size;
//          for (%i = 0; %i < %size; %i++) {
//              %values = setRecord(%values, %i, %heap.pop());
//          }
//          %heap.delete();
//          return %values;
//      }

function heapify(%data, %comparator, %externFunc) {
	%heap = HeapQueue(%comparator, %externFunc);
	%count = getLineCount(%data);

	for (%i = 0; %i < %count; %i++) {
		%line = getLine(%data, %i);
		%heap.push(getField(%line, 0), getField(%line, 1));
	}

	return %heap;
}
