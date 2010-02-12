#!/usr/bin/python
#
# object_events.py - filters an object path event file by object ID,
# pretty printing only the events for the specified object.  Note that
# this will only include events for which the object is the subject,
# i.e. it's the primary ID associated with the event.  Events where,
# e.g., the object is specified as a parent will not be included.

import sys
try:
    import simplejson as json
except:
    import json
from uuid import UUID

class ObjectPathTrace:
    """
    ObjectPathTrace provides a convenient interface to object path trace data
    generated by sltrace.  It can load from a file or accept a filtered version
    of a trace.  It is mostly useful as a base for operations on path trace
    data since it mostly provides utility methods including ways to filter the
    raw trace data, extracting certain types of events, events associated with
    specific objects, and methods for cleaning up the raw data (e.g. attempting
    to fill in missing parent information).
    """

    def __init__(self, trace_file=None, raw=None, start_time=None):
        """
        Create a new ObjectPathTrace. Only one source of data should be
        specified.

        Keyword arguments:
        trace_file -- name of JSON trace file
        raw -- raw Python representation of JSON, i.e. an array of
               events (default None)
        start_time -- start time to use for this trace. Overrides any start
                      time specified in the raw trace. (default None)
        """

        # Get raw data
        if raw: self._orig = raw
        elif trace_file: self._orig = json.load(open(trace_file))
        else: self._orig = []
        # Filter and set start time from data. If specified, override with
        # user start time
        started_evts = [x for x in self._orig if x['event'] == 'started']
        if len(started_evts) > 0:
            self._start_time = started_evts[0]['time'] # FIXME convert to datetime
        if start_time: self._start_time = start_time

        # Cached data:
        # 1) By objects, object categories
        self._objects = None  # List of objects
        self._avatars = None  # List of avatars
        # 2) By event type
        self._additions = None # List of addition events
        self._removals = None # List of removal events
        self._sizes = None # List of size events
        self._locupdates = None # List of loc update events

        self._filled_parents = False

    def objects(self):
        """Returns a list of object UUIDs encountered in this trace."""
        if not self._objects:
            id_set = {}
            for x in self.addition_events():
                if 'id' in x: id_set[UUID(x['id'])] = 1
            self._objects = id_set.keys()

        return self._objects

def main():
    if len(sys.argv) < 3:
        print "Usage: object_events.py objid tracefile"
        return -1

    objid = sys.argv[1]
    trace_file = sys.argv[2]

    orig = json.load(open(trace_file))
    filtered = [evt for evt in orig
                if 'id' in evt and evt['id'] == objid]

    print json.dumps(filtered, sort_keys=False, indent=2)

    return 0

if __name__ == "__main__":
    sys.exit(main())
